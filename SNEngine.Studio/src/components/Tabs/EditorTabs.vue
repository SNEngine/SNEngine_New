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
import UnknownFile from '../UnknownFile/UnknownFile.vue'

const tabs = ref<Array<{
  id: string
  filePath: string
  name: string
  type: 'code' | 'image' | 'unknown'
  content?: string
  language?: string
}>>([])

const activeFilePath = ref<string | null>(null)

const currentComponent = shallowRef<{
  component: any
  props: Record<string, any>
}>({ component: null, props: {} })

// Обработка прокрутки колесиком мыши (горизонтальный скролл)
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
  let props: any = { filePath }
  let content = ''
  let language = 'plaintext'

  const textExts = ['sn', 'ts', 'js', 'json', 'txt', 'html', 'css', 'md', 'xml', 'cs', 'csproj']
  const imgExts = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp']

  if (textExts.includes(ext)) {
    component = CodeEditor
    try {
      content = await (window as any).electron.readFile(filePath)
      language = ext === 'sn' ? 'sn' : 
                (ext === 'cs' ? 'csharp' : 
                (ext === 'ts' ? 'typescript' : 'plaintext'))
      props = { modelValue: content, language, theme: 'snengine-dark' }
    } catch (e) {
      content = `Ошибка чтения:\n${e}`
      props.modelValue = content
    }
  } else if (imgExts.includes(ext)) {
    component = ImagePreview
  }

  const newTab = {
    id: Date.now().toString(),
    filePath,
    name,
    type: textExts.includes(ext) ? 'code' : (imgExts.includes(ext) ? 'image' : 'unknown'),
    content,
    language
  }

  tabs.value.push(newTab)
  activateTab(newTab)
}

const activateTab = (tab: any) => {
  activeFilePath.value = tab.filePath

  if (tab.type === 'code') {
    currentComponent.value = {
      component: markRaw(CodeEditor),
      props: {
        modelValue: tab.content || '',
        language: tab.language || 'plaintext',
        theme: 'snengine-dark'
      }
    }
  } else if (tab.type === 'image') {
    currentComponent.value = {
      component: markRaw(ImagePreview),
      props: { imagePath: tab.filePath }
    }
  } else {
    currentComponent.value = {
      component: markRaw(UnknownFile),
      props: { filePath: tab.filePath }
    }
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
  
  if (tab.type === 'image') return 'image_icon'
  if (ext === 'sn') return 'sn_script_icon'
  if (ext === 'html') return 'html_icon'
  if (ext === 'css') return 'css_icon'
  if (ext === 'cs') return 'csharp_icon'
  if (ext === 'dll') return 'dll_icon'
  return 'unknown_icon'
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
}

/* Панель вкладок с кастомным скроллом */
.tabs-bar {
  display: flex;
  background: #252526;
  border-bottom: 1px solid #333;
  overflow-x: auto;
  overflow-y: hidden;
  flex-shrink: 0;
  min-height: 35px;
  /* Firefox */
  scrollbar-width: thin;
  scrollbar-color: #444 #252526;
}

/* Скроллбар для Chrome/Electron */
.tabs-bar::-webkit-scrollbar {
  height: 3px; /* Тонкая линия */
}

.tabs-bar::-webkit-scrollbar-track {
  background: #252526;
}

.tabs-bar::-webkit-scrollbar-thumb {
  background: #444;
  border-radius: 4px;
}

.tabs-bar::-webkit-scrollbar-thumb:hover {
  background: #FF5252;
}

.tab {
  padding: 6px 14px;
  background: #2d2d2d;
  color: #969696;
  border-right: 1px solid #333;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  white-space: nowrap;
  user-select: none;
  flex-shrink: 0; /* Не дает табам сжиматься */
  height: 100%;
  box-sizing: border-box;
  font-size: 13px;
  transition: all 0.15s ease;
}

.tab:hover {
  background: #323233;
  color: #ccc;
}

.tab.active {
  background: #1e1e1e;
  color: #FF5252;
  border-top: 1px solid #FF5252; /* Тонкая линия акцента */
}

.tab-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.tab-name {
  max-width: 180px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  margin-left: 4px;
  padding: 2px 5px;
  border-radius: 3px;
  font-size: 11px;
  opacity: 0.6;
  transition: all 0.2s;
}

.tab-close:hover {
  background: #e81123;
  color: white;
  opacity: 1;
}

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
  color: #444;
}

.empty-icon { 
  font-size: 48px; 
  margin-bottom: 8px; 
  filter: grayscale(1);
  opacity: 0.3;
}
</style>