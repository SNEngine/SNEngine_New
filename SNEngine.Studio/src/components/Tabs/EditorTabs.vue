<template>
  <div class="editor-tabs">
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
      >
        <BaseIcon 
          :name="getTabIconName(tab)" 
          class="tab-icon"
        />
        <span class="tab-name">{{ tab.name }}</span>
        <span class="tab-close" @click.stop="closeTab(tab)">✕</span>
      </div>
    </div>

    <div class="tab-content">
      <component 
        :is="currentComponent.component" 
        v-if="currentComponent.component"
        v-bind="currentComponent.props"
        :key="activeFilePath"
      />
      <div v-else class="empty-state">
        <span class="empty-icon">📂</span>
        <p>Выберите файл в дереве проекта</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, shallowRef, markRaw } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import ImagePreview from '../ImagePreview/ImagePreview.vue'
import AudioPreview from '../AudioPreview/AudioPreview.vue'   // ← НОВЫЙ
import UnknownFile from '../UnknownFile/UnknownFile.vue'
import WebEditor from '../WebEditor/WebEditor.vue'

const tabs = ref<Array<{
  id: string
  filePath: string
  name: string
  type: 'code' | 'image' | 'web' | 'audio' | 'unknown'   // ← добавлен audio
  content?: string
  language?: string
}>>([])

const activeFilePath = ref<string | null>(null)

const currentComponent = shallowRef<{
  component: any
  props: Record<string, any>
}>({ component: null, props: {} })

const handleWheel = (e: WheelEvent) => {
  const tabsBar = e.currentTarget as HTMLElement
  if (e.deltaY !== 0) {
    tabsBar.scrollLeft += e.deltaY
    e.preventDefault()
  }
}

const openFile = async (filePath: string) => {
  const name = filePath.split(/[/\\]/).pop() || filePath
  const ext = name.split('.').pop()?.toLowerCase() || ''

  const existing = tabs.value.find(t => t.filePath === filePath)
  if (existing) {
    activateTab(existing)
    return
  }

  let component: any = UnknownFile
  let type: 'code' | 'image' | 'web' | 'audio' | 'unknown' = 'unknown'
  let content = ''
  let language = 'plaintext'

  const textExts = ['sn', 'ts', 'js', 'json', 'txt', 'xml', 'cs', 'csproj', 'log']
  const imgExts = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp']
  const audioExts = ['mp3', 'wav', 'ogg', 'm4a', 'flac', 'aac']   // ← новые расширения

  if (ext === 'html') {
    component = WebEditor
    type = 'web'
    try { content = await (window as any).electron.readFile(filePath) } catch (e) { content = `Ошибка чтения HTML:\n${e}` }
  } 
  else if (textExts.includes(ext)) {
    component = CodeEditor
    type = 'code'
    try {
      content = await (window as any).electron.readFile(filePath)
      language = ext === 'sn' ? 'sn' : (ext === 'cs' ? 'csharp' : (ext === 'ts' ? 'typescript' : 'plaintext'))
    } catch (e) { content = `Ошибка чтения:\n${e}` }
  } 
  else if (imgExts.includes(ext)) {
    component = ImagePreview
    type = 'image'
  }
  else if (audioExts.includes(ext)) {          // ← обработка аудио
    component = AudioPreview
    type = 'audio'
  }

  const newTab = { id: Date.now().toString(), filePath, name, type, content, language }
  tabs.value.push(newTab)
  activateTab(newTab)
}

const activateTab = (tab: any) => {
  activeFilePath.value = tab.filePath

  if (tab.type === 'web') {
    currentComponent.value = { component: markRaw(WebEditor), props: { filePath: tab.filePath, initialHtml: tab.content || '' } }
  } 
  else if (tab.type === 'code') {
    currentComponent.value = { component: markRaw(CodeEditor), props: { modelValue: tab.content || '', language: tab.language || 'plaintext', theme: 'snengine-dark' } }
  } 
  else if (tab.type === 'image') {
    currentComponent.value = { component: markRaw(ImagePreview), props: { imagePath: tab.filePath } }
  } 
  else if (tab.type === 'audio') {             // ← новый блок
    currentComponent.value = { component: markRaw(AudioPreview), props: { audioPath: tab.filePath } }
  } 
  else {
    currentComponent.value = { component: markRaw(UnknownFile), props: { filePath: tab.filePath } }
  }
}

const closeTab = (tab: any) => {
  const index = tabs.value.findIndex(t => t.id === tab.id)
  tabs.value.splice(index, 1)
  if (activeFilePath.value === tab.filePath) {
    if (tabs.value.length > 0) {
      activateTab(tabs.value[Math.max(0, index - 1)])
    } else {
      activeFilePath.value = null
      currentComponent.value = { component: null, props: {} }
    }
  }
}

const getTabIconName = (tab: any) => {
  const ext = tab.name.split('.').pop()?.toLowerCase() || ''
  if (tab.type === 'web') return 'html_icon'
  if (tab.type === 'image') return 'image_icon'
  if (tab.type === 'audio') return 'audio_icon'      // ← новая иконка
  if (ext === 'sn') return 'sn_script_icon'
  if (ext === 'cs') return 'csharp_icon'
  if (ext === 'dll') return 'dll_icon'
  return 'unknown_icon'
}

defineExpose({ openFile })
</script>

<style scoped>
/* (стили остались без изменений — полностью как в твоём файле) */
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