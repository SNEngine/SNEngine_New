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

// Нормализация пути (чтобы сравнивать корректно)
const normalizePath = (p) => p ? p.replace(/\\/g, '/').toLowerCase() : ''

// ====================== ЗАКРЫТИЕ ВКЛАДКИ ПРИ УДАЛЕНИИ ======================
watch(lastUpdate, (update) => {
  if (!update || !update.path) return

  console.log('📡 lastUpdate получен:', update)

  const activePath = activeFilePath.value
  if (!activePath) return

  const normalizedActive = normalizePath(activePath)
  const normalizedUpdate = normalizePath(update.path)

  if (normalizedActive === normalizedUpdate && 
      (update.type === 'unlink' || update.type === 'unlinkDir')) {
    
    const tabToClose = tabs.value.find(t => normalizePath(t.filePath) === normalizedActive)
    if (tabToClose) {
      console.log('🗑️ Закрываем вкладку удалённого файла:', activePath)
      closeTab(tabToClose)
      success('Вкладка закрыта', 'Файл был удалён')
    }
  }
})

// ====================== СМЕНА АКТИВНОЙ ВКЛАДКИ ======================
watch(activeFilePath, async (newPath) => {
  if (!newPath) {
    currentHandler.value = null
    return
  }

  try {
    currentHandler.value = await getFileHandler(newPath)
  } catch (err) {
    console.warn('File handler error:', err)
    currentHandler.value = null
  }
})

// ====================== ОТКРЫТИЕ ФАЙЛА ======================
const openFile = async (filePath: string) => {
  const existing = tabs.value.find(t => t.filePath === filePath)
  if (existing) {
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

// ====================== СОХРАНЕНИЕ ======================
const saveCurrentFile = async () => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (!activeTab) return

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

defineExpose({ openFile })
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