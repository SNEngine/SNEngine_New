<template>
  <div class="editor-tabs">
    <TabBar
      :tabs="tabs"
      :active-file-path="activeFilePath"
      @activate="activateTab"
      @close="closeTab"
      @context-menu="showTabContextMenu"
    />

    <TabContent
      :current-component="currentComponent"
      :current-props="currentProps"
      :is-editable="currentHandler?.isEditable ?? false"
      ref="tabContentRef"
      @update:model-value="handleContentUpdate"
      @save="handleComponentSave"
      @close-tab="handleCloseActiveTab"
    />

    <ContextMenu ref="tabContextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { lastUpdate } from '@/utils/watcherState'

import ContextMenu from '../ContextMenu/ContextMenu.vue'
import TabBar from './TabBar.vue'
import TabContent from './TabContent.vue'
import DeletedFile from '../DeletedFile/DeletedFile.vue'
import GamePreview from '../GamePreview/GamePreview.vue'   // ← добавили

import { useTabs } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'
import { useFileSave } from '@/composables/useFileSave'
import { useKeyboard } from '@/composables/useKeyboard'
import { useNotification } from '@/composables/useNotification'

const { tabs, activeFilePath, activateTab, closeTab, markDirty, markClean } = useTabs()
const { getFileHandler } = useFileType()
const { saveFile, getContentFromEditor } = useFileSave()
const { add: addShortcut } = useKeyboard()
const { success, error } = useNotification()

const tabContextMenuRef = ref<any>(null)
const tabContentRef = ref<any>(null)

const currentHandler = ref<any>(null)

const currentComponent = computed(() => currentHandler.value?.component || null)
const currentProps = computed(() => currentHandler.value?.props || {})

// ====================== ОБРАБОТКА УДАЛЕНИЯ ======================
watch(lastUpdate, (update) => {
  if (!update) return

  const activePath = activeFilePath.value
  if (!activePath) return

  const normalizedActive = String(activePath).replace(/\\+/g, '/').replace(/\/+$/, '')
  const normalizedUpdate = String(update.path || '').replace(/\\+/g, '/').replace(/\/+$/, '')

  if (normalizedActive === normalizedUpdate && 
      (update.type === 'unlink' || update.type === 'unlinkDir')) {
    
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedActive,
        isDeleted: true 
      }
    }
  }
})

// ====================== СМЕНА АКТИВНОЙ ВКЛАДКИ ======================
watch(activeFilePath, async (newPath) => {
  if (!newPath) {
    currentHandler.value = null
    return
  }

  const normalizedNew = newPath.replace(/\\+/g, '/').replace(/\/+$/, '')

  // Специальная обработка для Game Preview
  if (newPath === '::preview::') {
    const activeTab = tabs.value.find(t => t.filePath === '::preview::')
    const autoStart = activeTab?.previewOptions?.autoStart ?? false

    // Clear the flag so it doesn't auto-start on subsequent tab switches
    if (activeTab?.previewOptions) {
      activeTab.previewOptions.autoStart = false
    }

    currentHandler.value = {
      component: GamePreview,
      props: { 
        projectPath: 'C:/Users/Siphome/Desktop/testBuild',
        autoStart
      },
      isEditable: false
    }
    return
  }

  const tab = tabs.value.find(t => t.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === normalizedNew)
  if (tab?.isDeleted) {
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedNew,
        isDeleted: true 
      }
    }
    return
  }

  try {
    const handler = await getFileHandler(newPath)
    currentHandler.value = handler
  } catch (err) {
    console.warn('File handler error:', err)
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedNew,
        isDeleted: true 
      }
    }
  }
})

// ====================== ОТКРЫТИЕ ФАЙЛА ======================
const openFile = async (rawPath: string) => {
  const filePath = rawPath.replace(/\\+/g, '/').replace(/\/+$/, '')

  const existing = tabs.value.find(t => t.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === filePath)
  if (existing) {
    if (existing.isDeleted) existing.isDeleted = false
    activateTab(existing)
    return
  }

  const handler = await getFileHandler(filePath)

  const newTab = {
    id: Date.now().toString(),
    filePath,
    name: filePath.split(/[/\\]/).pop() || filePath,
    type: handler.type,
    content: handler.props.modelValue || handler.props.initialHtml || '',
    language: handler.language,
    isDirty: false
  }

  tabs.value.push(newTab)
  activateTab(newTab)
}

// ====================== СПЕЦИАЛЬНЫЙ ТАБ ДЛЯ PREVIEW ======================
const openPreviewTab = (options: { autoStart?: boolean } = {}) => {
  const previewPath = '::preview::'

  const existing = tabs.value.find(t => t.filePath === previewPath)
  if (existing) {
    activateTab(existing)
    // If already open and autoStart requested, we can't easily force start from here
    // (the component instance is managed by TabContent). For now we just activate.
    return
  }

  const previewTab = {
    id: 'preview-tab',
    filePath: previewPath,
    name: 'Game',
    type: 'preview',
    isDirty: false,
    icon: 'game_icon',
    // Pass autoStart down via the tab metadata
    previewOptions: {
      autoStart: !!options.autoStart
    }
  }

  tabs.value.push(previewTab)
  activateTab(previewTab)
}

// ====================== СОХРАНЕНИЕ ======================
const saveCurrentFile = async () => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (!activeTab || activeTab.isDeleted) return

  if (!['code', 'web'].includes(activeTab.type)) return

  const editor = tabContentRef.value?.activeEditorRef
  let content = getContentFromEditor(editor, activeTab.content || '')

  if (!content && activeTab.content) content = activeTab.content

  const result = await saveFile(activeTab.filePath, content)

  if (result.success) {
    activeTab.content = content
    markClean(activeTab.filePath)
    success(`Файл сохранён`, activeTab.name)
  } else {
    error(`Не удалось сохранить файл`, activeTab.name)
  }
}

const handleContentUpdate = (newContent: string) => {
  const tab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (tab) {
    tab.content = newContent
    markDirty(tab.filePath)
  }
}

const handleComponentSave = () => saveCurrentFile()

const handleCloseActiveTab = () => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (activeTab) closeTab(activeTab)
}

onMounted(() => {
  addShortcut('ctrl+s', saveCurrentFile, true)
})

// Контекстное меню
const showTabContextMenu = (e: MouseEvent, tab: any) => {
  const i = tabs.value.findIndex(t => t.id === tab.id)
  const menu = [
    { label: 'Закрыть вкладку', action: () => closeTab(tab) },
    { label: 'Закрыть другие', action: () => closeOtherTabs(tab) },
    { label: 'Закрыть справа', action: () => closeTabsToRight(i) },
    { label: 'Закрыть слева', action: () => closeTabsToLeft(i) },
    { type: 'separator' },
    { label: 'Закрыть все', action: closeAllTabs },
  ]
  tabContextMenuRef.value?.show(e.clientX, e.clientY, menu)
}

const closeOtherTabs = (tab: any) => tabs.value.filter(t => t.id !== tab.id).forEach(closeTab)
const closeTabsToRight = (i: number) => tabs.value.slice(i + 1).forEach(closeTab)
const closeTabsToLeft = (i: number) => tabs.value.slice(0, i).forEach(closeTab)

const closeAllTabs = () => {
  while (tabs.value.length) closeTab(tabs.value[0])
  activeFilePath.value = null
  currentHandler.value = null
}

defineExpose({ 
  openFile,
  openPreviewTab   // ← новый метод
})
</script>

<style scoped>
.editor-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
  user-select: none;
}
</style>