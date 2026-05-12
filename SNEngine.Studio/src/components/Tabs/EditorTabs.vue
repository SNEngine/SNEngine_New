<template>
  <div class="editor-tabs" @keydown.ctrl.s.prevent="handleGlobalSave">
    <!-- Панель вкладок -->
    <div class="tabs-bar" v-if="tabs.length > 0" @wheel="handleWheel">
      <div 
        v-for="tab in tabs" 
        :key="tab.id"
        class="tab"
        :class="{ active: tab.filePath === activeFilePath }"
        @click="activateTab(tab)"
        @contextmenu.prevent="showTabContextMenu($event, tab)"
      >
        <BaseIcon :name="getTabIconName(tab)" class="tab-icon" />
        <span class="tab-name">{{ tab.name }}</span>
        <span class="tab-close" @click.stop="closeTab(tab)">✕</span>
      </div>
    </div>

    <!-- Динамический редактор -->
    <div class="tab-content">
      <component 
        v-if="currentComponent"
        :is="currentComponent"
        v-bind="currentProps"
        ref="activeEditorRef"
        @update:modelValue="handleContentUpdate"
        @save="handleComponentSave"
      />
      <div v-else class="empty-state">
        <span class="empty-icon">📂</span>
        <p>Выберите файл в дереве проекта</p>
      </div>
    </div>

    <ContextMenu ref="tabContextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'

import CodeEditor from '../CodeEditor/CodeEditor.vue'
import ImagePreview from '../ImagePreview/ImagePreview.vue'
import AudioPreview from '../AudioPreview/AudioPreview.vue'
import WebEditor from '../WebEditor/WebEditor.vue'
import UnknownFile from '../UnknownFile/UnknownFile.vue'

import { useTabs } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'

const { tabs, activeFilePath, openFile, activateTab, closeTab } = useTabs()
const { getFileHandler } = useFileType()

const tabContextMenuRef = ref<any>(null)
const activeEditorRef = ref<any>(null)

// ====================== ОПРЕДЕЛЕНИЕ ТИПА РЕДАКТОРА ======================
const getEditorComponent = (filePath: string) => {
  const ext = filePath.split('.').pop()?.toLowerCase() || ''
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) return ImagePreview
  if (['mp3', 'wav', 'ogg', 'm4a', 'flac'].includes(ext)) return AudioPreview
  if (['html', 'htm'].includes(ext)) return WebEditor
  if (['sn', 'cs', 'txt', 'js', 'ts', 'json', 'css', 'md', 'log'].includes(ext)) return CodeEditor
  return UnknownFile
}

const currentComponent = computed(() => {
  if (!activeFilePath.value) return null
  return getEditorComponent(activeFilePath.value)
})

const currentProps = computed(() => {
  const tab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (!tab) return {}
  const ext = activeFilePath.value.split('.').pop()?.toLowerCase() || ''

  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg'].includes(ext)) return { imagePath: activeFilePath.value }
  if (['mp3', 'wav', 'ogg', 'm4a', 'flac'].includes(ext)) return { audioPath: activeFilePath.value }
  if (['html', 'htm'].includes(ext)) return { filePath: activeFilePath.value, initialHtml: tab.content }
  if (['sn', 'cs', 'txt', 'js', 'ts', 'json', 'css', 'md', 'log'].includes(ext)) {
    return { modelValue: tab.content || '', language: getLanguageFromExt(ext), theme: 'snengine-dark' }
  }
  return { filePath: activeFilePath.value }
})

const getLanguageFromExt = (ext: string) => {
  const map: Record<string, string> = {
    sn: 'sn', cs: 'csharp', js: 'javascript', ts: 'typescript',
    json: 'json', css: 'css', md: 'markdown', txt: 'plaintext', log: 'plaintext'
  }
  return map[ext] || 'plaintext'
}

// ====================== ОТКРЫТИЕ ФАЙЛА ======================
const handleOpenFile = async (filePath: string) => {
  let fileContent = ''
  const ext = filePath.split('.').pop()?.toLowerCase() || ''
  if (!['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'mp3', 'wav', 'ogg'].includes(ext)) {
    try {
      if ((window as any).electron?.readFile) {
        fileContent = await (window as any).electron.readFile(filePath)
      }
    } catch (e) {}
  }

  const existing = tabs.value.find(t => t.filePath === filePath)
  if (existing) {
    existing.content = fileContent
    activateTab(existing)
  } else {
    const newTab = { id: Date.now().toString(), filePath, name: filePath.split(/[/\\]/).pop() || filePath, type: 'code', content: fileContent, language: getLanguageFromExt(ext) }
    tabs.value.push(newTab)
    activateTab(newTab)
  }
}

// ====================== ОБНОВЛЕНИЕ СОДЕРЖИМОГО ======================
const handleContentUpdate = (newContent: string) => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (activeTab) activeTab.content = newContent
}

// ====================== НАДЁЖНОЕ СОХРАНЕНИЕ ======================
const saveCurrentFile = async () => {
  const activeTab = tabs.value.find(t => t.filePath === activeFilePath.value)
  if (!activeTab) return

  let content = ''
  const comp = activeEditorRef.value

  // Для CodeEditor — напрямую из Monaco
  if (comp?.editorRef?.value && typeof comp.editorRef.value.getValue === 'function') {
    content = comp.editorRef.value.getValue()
  } 
  // Для других редакторов
  else if (comp?.internalCode?.value) {
    content = comp.internalCode.value
  } 
  else if (comp?.htmlCode?.value) {
    content = comp.htmlCode.value
  } 
  else {
    content = activeTab.content || ''
  }

  console.log('💾 Сохранение:', activeTab.filePath, `(${content.length} символов)`)

  try {
    if ((window as any).electron?.writeFile) {
      const result = await (window as any).electron.writeFile(activeTab.filePath, content)
      if (result.success) {
        console.log('✅ ФАЙЛ УСПЕШНО СОХРАНЁН!')
      }
    }
  } catch (e) {
    console.error('💥 Ошибка:', e)
  }
}

const handleGlobalSave = () => saveCurrentFile()
const handleComponentSave = (content: string) => saveCurrentFile()

// ====================== КОНТЕКСТНОЕ МЕНЮ + ОСТАЛЬНОЕ ======================
const showTabContextMenu = (e: MouseEvent, currentTab: any) => {
  const i = tabs.value.findIndex(t => t.id === currentTab.id)
  const menu = [
    { label: 'Закрыть вкладку', action: () => closeTab(currentTab) },
    { label: 'Закрыть другие', action: () => closeOtherTabs(currentTab) },
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
const closeAllTabs = () => { while (tabs.value.length) closeTab(tabs.value[0]) }

const handleWheel = (e: WheelEvent) => { (e.currentTarget as HTMLElement).scrollLeft += e.deltaY; e.preventDefault() }

const getTabIconName = (tab: any) => {
  const ext = tab.filePath.split('.').pop()?.toLowerCase() || ''
  if (['png', 'jpg', 'jpeg', 'gif', 'webp'].includes(ext)) return 'image_icon'
  if (['mp3', 'wav', 'ogg', 'm4a'].includes(ext)) return 'audio_icon'
  if (['html', 'htm'].includes(ext)) return 'html_icon'
  if (ext === 'sn') return 'sn_script_icon'
  if (ext === 'cs') return 'csharp_icon'
  return 'unknown_icon'
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

/* Панель вкладок с кастомным скроллом */
.tabs-bar {
  display: flex;
  background: #252526;
  overflow-x: auto;
  overflow-y: hidden;
  height: 35px;
  /* Firefox */
  scrollbar-width: thin;
  scrollbar-color: #3e3e3e transparent;
}

/* Стилизация скроллбара (Chrome, Edge, Safari) */
.tabs-bar::-webkit-scrollbar {
  height: 3px;
}

.tabs-bar::-webkit-scrollbar-track {
  background: transparent;
}

.tabs-bar::-webkit-scrollbar-thumb {
  background: #3e3e3e;
  border-radius: 10px;
}

.tabs-bar:hover::-webkit-scrollbar-thumb {
  background: #4f4f4f;
}

.tabs-bar::-webkit-scrollbar-thumb:hover {
  background: #ff5252;
}

/* Стили вкладок */
.tab {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 10px;
  height: 100%;
  background: #2d2d2d;
  color: #969696;
  border-right: 1px solid #1e1e1e;
  cursor: pointer;
  font-size: 13px;
  position: relative;
  transition: background 0.2s, color 0.2s;
  min-width: 100px;
  max-width: 200px;
}

.tab:hover {
  background: #323233;
  color: #cccccc;
}

.tab.active {
  background: #1e1e1e;
  color: #ff5252;
}

.tab.active::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: #ff5252;
}

.tab-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

/* Исправление длинных названий */
.tab-name {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border-radius: 4px;
  font-size: 10px;
  opacity: 0;
  margin-left: 4px;
  transition: opacity 0.2s, background 0.2s;
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

/* Контентная область */
.tab-content {
  flex: 1;
  overflow: hidden;
  background: #1e1e1e;
  position: relative;
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