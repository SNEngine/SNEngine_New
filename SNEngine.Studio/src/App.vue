<template>
  <div class="studio">
    <header class="header">
      <h1>SNEngine Studio</h1>
    </header>

    <div class="workspace">
      <div class="left-panel" :style="{ width: treeWidth + 'px' }">
        <DirectoryTree 
          base-path="C:/Users/Siphome/Desktop/testBuild"
          :active-path="currentFile || ''"
          @file-click="handleFileClick"
        />
      </div>

      <div class="splitter" @mousedown="startResizing" />

      <div class="right-panel">
        <component 
          :is="currentEditor.component" 
          v-if="currentEditor.component"
          v-bind="currentEditor.props"
          :key="currentFile"
        />
        <div v-else class="empty-editor">
          <div class="welcome-screen">
            <span class="welcome-icon">🚀</span>
            <p>Выберите файл для начала работы</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, shallowRef, markRaw } from 'vue'
import DirectoryTree from "./components/DirectoryTree/DirectoryTree.vue"
import CodeEditor from "./components/CodeEditor/CodeEditor.vue"
import ImagePreview from "./components/ImagePreview/ImagePreview.vue"
import UnknownFile from "./components/UnknownFile/UnknownFile.vue"

const treeWidth = ref(320)
let isResizing = false
const currentFile = ref<string | null>(null)
const currentEditor = shallowRef<{ component: any, props: any }>({ component: null, props: {} })

const handleFileClick = async (filePath: string) => {
  currentFile.value = filePath
  const ext = filePath.toLowerCase().split('.').pop() || ''
  
  // 1. Изображения
  if (['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp'].includes(ext)) {
    currentEditor.value = {
      component: markRaw(ImagePreview),
      props: { imagePath: filePath }
    }
    return
  }

  // 2. Текстовые файлы (чтение через Electron)
  const textExts = ['sn', 'ts', 'js', 'json', 'txt', 'html', 'css', 'md']
  if (textExts.includes(ext)) {
    try {
      const content = await (window as any).electron.readFile(filePath)
      currentEditor.value = {
        component: markRaw(CodeEditor),
        props: {
          modelValue: content,
          language: ext === 'sn' ? 'sn' : (ext === 'ts' ? 'typescript' : ext),
          theme: 'snengine-dark'
        }
      }
    } catch (err) {
      console.error("Ошибка чтения файла:", err)
    }
    return
  }

  // 3. Неизвестный формат
  currentEditor.value = {
    component: markRaw(UnknownFile),
    props: { filePath }
  }
}

const startResizing = () => {
  isResizing = true
  document.addEventListener('mousemove', onMouseMove)
  document.addEventListener('mouseup', stopResizing)
}

const onMouseMove = (e: MouseEvent) => {
  if (isResizing) {
    treeWidth.value = Math.max(200, Math.min(e.clientX, 600))
  }
}

const stopResizing = () => {
  isResizing = false
  document.removeEventListener('mousemove', onMouseMove)
  document.removeEventListener('mouseup', stopResizing)
}
</script>

<style>
/* Глобальные настройки */
html, body { 
  margin: 0; 
  padding: 0; 
  height: 100vh; 
  width: 100vw;
  overflow: hidden; 
  background: #1e1e1e; 
  color: white;
  font-family: 'Segoe UI', system-ui, -apple-system, sans-serif;
}

.studio { 
  height: 100vh; 
  display: flex; 
  flex-direction: column; 
}

/* Исправленный заголовок */
.header { 
  height: 40px; 
  background: #252526;
  display: flex; 
  align-items: center; 
  padding: 0 16px;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
}

.header h1 { 
  margin: 0; 
  font-size: 13px; 
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #FF5252;
}

.workspace { 
  flex: 1; 
  display: flex; 
  overflow: hidden; 
}

.left-panel { 
  background: #161616; 
  height: 100%; 
  overflow: hidden;
}

.splitter { 
  width: 2px; 
  cursor: col-resize; 
  background: #252526; 
  transition: background 0.2s;
}

.splitter:hover { 
  background: #FF5252; 
}

.right-panel { 
  flex: 1; 
  position: relative; 
  background: #1e1e1e; 
}

.empty-editor { 
  height: 100%; 
  display: flex; 
  align-items: center; 
  justify-content: center; 
}

.welcome-screen { 
  text-align: center; 
  color: #555;
}

.welcome-icon { 
  font-size: 48px; 
  display: block; 
  margin-bottom: 10px; 
  opacity: 0.2; 
}
</style>