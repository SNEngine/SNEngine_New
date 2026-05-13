<template>
  <div class="editor-tabs">
    <!-- Панель вкладок -->
    <TabBar
      :tabs="tabs"
      :active-file-path="activeFilePath"
      @activate="activateTab"
      @close="closeTab"
      @context-menu="showTabContextMenu"
    />

    <!-- Основной контент редактора -->
    <TabContent
      :current-component="currentComponent"
      :current-props="currentProps"
      ref="tabContentRef"
      @update:model-value="handleContentUpdate"
      @save="handleComponentSave"
    />

    <ContextMenu ref="tabContextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onUnmounted } from 'vue'
import { lastUpdate } from '@/utils/watcherState'

import ContextMenu from '../ContextMenu/ContextMenu.vue'
import TabBar from './TabBar.vue'
import TabContent from './TabContent.vue'

import { useTabs } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'
import { useFileSave } from '@/composables/useFileSave'
import { useKeyboard } from '@/composables/useKeyboard'   // ← интегрируем

const { tabs, activeFilePath, activateTab, closeTab, markDirty, markClean } = useTabs()
const { getFileHandler } = useFileType()
const { saveFile, getContentFromEditor } = useFileSave()
const { add: addShortcut } = useKeyboard()   // ← используем composable

const tabContextMenuRef = ref<any>(null)
const tabContentRef = ref<any>(null)

const currentHandler = ref<any>(null)

// Текущий компонент и пропсы (полностью через useFileType)
const currentComponent = computed(() => currentHandler.value?.component || null)
const currentProps = computed(() => currentHandler.value?.props || {})

// Обновляем handler при смене активной вкладки
watch(activeFilePath, async (newPath) => {
  if (!newPath) {
    currentHandler.value = null
    return
  }
  currentHandler.value = await getFileHandler(newPath)
})

// ====================== LIVE RELOAD (внешнее изменение файла) ======================
watch(lastUpdate, async () => {
  for (const tab of tabs.value) {
    if (tab.isDirty) continue // не трогаем файлы, которые пользователь редактирует

    try {
      const freshContent = await (window as any).electron?.readFile?.(tab.filePath)
      if (freshContent !== undefined && freshContent !== tab.content) {
        tab.content = freshContent

        // Обновляем Monaco, если вкладка активна
        if (tab.filePath === activeFilePath.value && tabContentRef.value?.activeEditorRef) {
          const editor = tabContentRef.value.activeEditorRef
          if (editor.editorRef?.value?.setValue) {
            editor.editorRef.value.setValue(freshContent)
          } else if (typeof editor.setValue === 'function') {
            editor.setValue(freshContent)
          }
        }
      }
    } catch (e) {
      console.warn('Не удалось обновить файл извне:', tab.filePath)
    }
  }
})

// ====================== ОТКРЫТИЕ ФАЙЛА ======================
const handleOpenFile = async (filePath: string) => {
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

// ====================== ИЗМЕНЕНИЕ + СОХРАНЕНИЕ ======================
const handleContentUpdate = (newContent: string) => {
  const tab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (tab) {
    tab.content = newContent
    markDirty(tab.filePath)
  }
}

const saveCurrentFile = async () => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (!activeTab) return

  const editor = tabContentRef.value?.activeEditorRef
  let content = getContentFromEditor(editor, activeTab.content || '')

  if (!content && activeTab.content) content = activeTab.content

  const result = await saveFile(activeTab.filePath, content)
  if (result.success) {
    activeTab.content = content
    markClean(activeTab.filePath)
    console.log('✅ Файл успешно сохранён:', activeTab.filePath)
  } else {
    console.error('❌ Ошибка сохранения:', result.error)
  }
}

const handleComponentSave = () => saveCurrentFile()

// ====================== ГЛОБАЛЬНЫЙ Ctrl+S через useKeyboard ======================
onMounted(() => {
  addShortcut('ctrl+s', () => {
    saveCurrentFile()
  }, true)
})

// ====================== КОНТЕКСТНОЕ МЕНЮ ======================
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

defineExpose({ openFile: handleOpenFile })
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