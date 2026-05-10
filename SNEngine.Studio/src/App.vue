<template>
  <div class="studio">
    <header class="header">
      <div class="logo-section">
        <h1>SNEngine Studio</h1>
        <span class="version">v0.0.1-dev</span>
      </div>
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
        <EditorTabs ref="tabsRef" />
      </div>
    </div>

    <!-- Важно! -->
    <MessageBox ref="messageBoxRef" />
    <InputBox ref="inputBoxRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import DirectoryTree from "./components/DirectoryTree/DirectoryTree.vue"
import EditorTabs from "./components/Tabs/EditorTabs.vue"
import MessageBox from "./components/MessageBox/MessageBox.vue"
import InputBox from "./components/InputBox/InputBox.vue"

import { useMessageBox } from './composables/useMessageBox'
import { useInputBox } from './composables/useInputBox'

const treeWidth = ref(320)
let isResizing = false
const currentFile = ref<string | null>(null)
const tabsRef = ref<any>(null)

const messageBoxRef = ref<any>(null)
const inputBoxRef = ref<any>(null)

const { messageBox } = useMessageBox()
const { inputBox } = useInputBox()

onMounted(() => {
  messageBox.value = messageBoxRef.value
  inputBox.value = inputBoxRef.value
})

const handleFileClick = async (filePath: string) => {
  currentFile.value = filePath
  const fileName = filePath.split(/[/\\]/).pop() || filePath

  if (tabsRef.value) {
    try {
      const content = await (window as any).electron.readFile(filePath)
      tabsRef.value.openFile?.(filePath, content) || tabsRef.value.addTab?.({
        id: filePath,
        title: fileName,
        content: content,
      })
    } catch (e) {
      console.error('Failed to open file:', e)
    }
  }
}

const startResizing = (e: MouseEvent) => {
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
html, body { 
  margin: 0; padding: 0; height: 100vh; width: 100vw; 
  overflow: hidden; background: #1e1e1e; color: white;
  font-family: 'Segoe UI', system-ui, sans-serif;
}

.studio { height: 100vh; display: flex; flex-direction: column; }

.header { 
  height: 40px; 
  background: #252526; 
  display: flex; 
  align-items: center; 
  padding: 0 16px; 
  border-bottom: 1px solid #333; 
  flex-shrink: 0;
}

.logo-section { display: flex; align-items: center; gap: 12px; }
.header h1 { 
  margin: 0; 
  font-size: 13px; 
  font-weight: 600; 
  color: #FF5252; 
  text-transform: uppercase; 
  letter-spacing: 0.5px;
}
.version {
  font-size: 10px;
  color: #666;
  background: #1a1a1a;
  padding: 2px 6px;
  border-radius: 4px;
}

.workspace { flex: 1; display: flex; overflow: hidden; }

.left-panel { 
  background: #161616; 
  height: 100%; 
  overflow: hidden;
}

.splitter { 
  width: 4px; 
  cursor: col-resize; 
  background: #252526; 
  transition: background 0.2s; 
  z-index: 10;
}
.splitter:hover { background: #FF5252; }

.right-panel { 
  flex: 1; 
  overflow: hidden; 
  background: #1e1e1e; 
  display: flex;
  flex-direction: column;
}
</style>