<template>
  <div class="editor-tabs" @keydown.ctrl.s.prevent="handleGlobalSave">
    <!-- Панель вкладок -->
    <div 
      class="tabs-bar" 
      v-if="tabs.length > 0"
      @wheel="handleWheel"
    >
      <div 
        v-for="tab in tabs" 
        :key="tab.id"
        class="tab"
        :class="{ active: tab.filePath === activeFilePath }"
        @click="activateTab(tab)"
        @contextmenu.prevent="showTabContextMenu($event, tab)"
      >
        <BaseIcon 
          :name="getTabIconName(tab)" 
          class="tab-icon"
        />
        <span class="tab-name">{{ tab.name }}</span>
        <span class="tab-close" @click.stop="closeTab(tab)">✕</span>
      </div>
    </div>

    <!-- Контент вкладки -->
    <div class="tab-content">
      <component 
        :is="currentComponent.component" 
        v-if="currentComponent.component"
        v-bind="currentComponent.props"
        :key="activeFilePath"
        ref="activeComponentRef"
        @save="handleComponentSave"
      />
      <div v-else class="empty-state">
        <span class="empty-icon">📂</span>
        <p>Выберите файл в дереве проекта</p>
      </div>
    </div>

    <!-- Контекстное меню для вкладок -->
    <ContextMenu ref="tabContextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'

import { useTabs } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'

const { tabs, activeFilePath, currentComponent, openFile, activateTab, closeTab } = useTabs()
const { getFileHandler } = useFileType()

const tabContextMenuRef = ref<any>(null)
const activeComponentRef = ref<any>(null)

// Открытие файла из дерева
const handleOpenFile = async (filePath: string) => {
  await openFile(filePath, getFileHandler)
}

// Обработчик события save от компонента (например CodeEditor)
const handleComponentSave = async (content: string) => {
  console.log('💾 [event save] от компонента, контент длина:', content.length)
  await saveFileToDisk(content)
}

// Глобальный обработчик Ctrl+S
const handleGlobalSave = async () => {
  console.log('⌨️ [global] Ctrl+S нажат')
  
  if (!activeFilePath.value) {
    console.warn('⚠️ [global] Нет активного файла')
    return
  }

  let content = ''
  const comp = activeComponentRef.value
  console.log('🔍 [global] activeComponentRef.value:', comp)

  if (comp) {
    // 1. Monaco Editor
    const editor = comp.editorRef?.value
    console.log('   editorRef?.value:', editor)
    if (editor && typeof editor.getValue === 'function') {
      content = editor.getValue()
      console.log('📝 [global] Взято из Monaco Editor')
    }
    // 2. internalCode (Ref)
    else if (comp.internalCode?.value !== undefined) {
      content = comp.internalCode.value
      console.log('📝 [global] Взято из internalCode, значение:', content.substring(0, 100))
    }
    // 3. modelValue
    else if (comp.modelValue?.value !== undefined) {
      content = comp.modelValue.value
    }
    else if (comp.modelValue !== undefined) {
      content = comp.modelValue
    }
    // 4. code
    else if (comp.code?.value !== undefined) {
      content = comp.code.value
    }
    else if (comp.code !== undefined) {
      content = comp.code
    }
    // 5. value
    else if (comp.value?.value !== undefined) {
      content = comp.value.value
    }
    else if (comp.value !== undefined) {
      content = comp.value
    }
    // 6. textarea
    else if (comp.$el) {
      const textarea = comp.$el.querySelector('textarea')
      if (textarea) {
        content = textarea.value
        console.log('📝 [global] Взято из textarea')
      }
    }

    if (!content) {
      console.warn('⚠️ [global] Не удалось извлечь контент. Ключи comp:', Object.keys(comp))
    }
  } else {
    console.warn('⚠️ [global] comp равен null/undefined')
  }

  if (content) {
    await saveFileToDisk(content)
  } else {
    console.warn('⚠️ [global] Контент пуст, сохранение не выполнено')
  }
}

// Запись файла на диск
const saveFileToDisk = async (content: string) => {
  try {
    const result = await window.electron.writeFile(activeFilePath.value!, content)
    if (result.success) {
      console.log('✅ Файл сохранён:', activeFilePath.value)
    } else {
      console.error('❌ Ошибка сохранения:', result.error)
    }
  } catch (err) {
    console.error('❌ Исключение при сохранении:', err)
  }
}

// Контекстное меню по правому клику на вкладке
const showTabContextMenu = (e: MouseEvent, currentTab: any) => {
  const currentIndex = tabs.value.findIndex(t => t.id === currentTab.id)

  const menuItems = [
    { 
      label: 'Закрыть вкладку', 
      action: () => closeTab(currentTab) 
    },
    { 
      label: 'Закрыть другие вкладки', 
      action: () => closeOtherTabs(currentTab) 
    },
    { 
      label: 'Закрыть вкладки справа', 
      action: () => closeTabsToRight(currentIndex) 
    },
    { 
      label: 'Закрыть вкладки слева', 
      action: () => closeTabsToLeft(currentIndex) 
    },
    { 
      label: 'Закрыть все вкладки', 
      action: closeAllTabs 
    },
  ]

  if (tabContextMenuRef.value) {
    tabContextMenuRef.value.show(e.clientX, e.clientY, menuItems)
  }
}

// Вспомогательные функции закрытия
const closeOtherTabs = (currentTab: any) => {
  const toClose = tabs.value.filter(t => t.id !== currentTab.id)
  toClose.forEach(tab => closeTab(tab))
}

const closeTabsToRight = (currentIndex: number) => {
  const toClose = tabs.value.slice(currentIndex + 1)
  toClose.forEach(tab => closeTab(tab))
}

const closeTabsToLeft = (currentIndex: number) => {
  const toClose = tabs.value.slice(0, currentIndex)
  toClose.forEach(tab => closeTab(tab))
}

const closeAllTabs = () => {
  while (tabs.value.length > 0) {
    closeTab(tabs.value[0])
  }
}

const handleWheel = (e: WheelEvent) => {
  const tabsBar = e.currentTarget as HTMLElement
  if (e.deltaY !== 0) {
    tabsBar.scrollLeft += e.deltaY
    e.preventDefault()
  }
}

const getTabIconName = (tab: any) => {
  if (tab.type === 'web') return 'html_icon'
  if (tab.type === 'image') return 'image_icon'
  if (tab.type === 'audio') return 'audio_icon'
  if (tab.type === 'code') {
    const ext = tab.filePath.split('.').pop()?.toLowerCase()
    if (ext === 'sn') return 'sn_script_icon'
    if (ext === 'cs') return 'csharp_icon'
    if (ext === 'dll') return 'dll_icon'
  }
  return 'unknown_icon'
}

defineExpose({
  openFile: handleOpenFile
})
</script>

<style scoped>
/* Все стили без изменений (те же, что и раньше) */
.editor-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
}

.tabs-bar {
  display: flex;
  background: #252526;
  overflow-x: auto;
  overflow-y: hidden;
  flex-shrink: 0;
  height: 35px;
}

.tabs-bar::-webkit-scrollbar {
  height: 3px;
}

.tabs-bar::-webkit-scrollbar-track {
  background: transparent;
}

.tabs-bar::-webkit-scrollbar-thumb {
  background: #3e3e3e;
}

.tabs-bar::-webkit-scrollbar-thumb:hover {
  background: #FF5252;
}

.tab {
  padding: 0 12px;
  background: #2d2d2d;
  color: #969696;
  border-right: 1px solid #1e1e1e;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  white-space: nowrap;
  user-select: none;
  flex-shrink: 0;
  height: 100%;
  font-size: 13px;
  position: relative;
  transition: background 0.2s, color 0.2s;
}

.tab:hover {
  background: #323233;
  color: #cccccc;
}

.tab.active {
  background: #1e1e1e;
  color: #FF5252;
}

.tab.active::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: #FF5252;
}

.tab-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.tab-name {
  max-width: 160px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  margin-left: 6px;
  padding: 2px;
  border-radius: 3px;
  font-size: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  opacity: 0;
  transition: all 0.2s;
}

.tab:hover .tab-close,
.tab.active .tab-close {
  opacity: 0.6;
}

.tab-close:hover {
  background: #454545;
  color: white;
  opacity: 1 !important;
}

.tab-content {
  flex: 1;
  overflow: hidden;
  background: #1e1e1e;
}

.empty-state {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #555;
}

.empty-icon {
  font-size: 40px;
  margin-bottom: 12px;
  opacity: 0.2;
}
</style>