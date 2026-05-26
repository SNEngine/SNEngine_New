<template>
  <div class="studio">
    <header class="header">
      <div class="logo-section">
        <h1>SNEngine Studio</h1>
        <span class="version">v{{ appVersion }}</span>
      </div>

      <SystemStatus class="system-status" />

      <div class="header-actions">
        <!-- Preview Button -->
<button 
  class="preview-btn play-btn"
  @click="openPreviewTab"
  title="Запустить превью (Play)"
>
  <BaseIcon name="run_icon" class="btn-icon" />
</button>

        <!-- Terminal Button -->
        <button 
          class="terminal-toggle-btn"
          @click="toggleTerminal"
          :class="{ active: showTerminal }"
          title="Открыть/Закрыть Terminal (Ctrl+T)"
        >
          <BaseIcon name="terminal_icon" class="btn-icon" />
          <span>Terminal</span>
        </button>

        <!-- Fullscreen -->
        <button 
          class="fullscreen-btn"
          @click="toggleFullScreen"
          :title="isFullScreen ? 'Выключить полноэкранный режим (F11)' : 'Полноэкранный режим (F11)'"
        >
          <BaseIcon 
            :name="isFullScreen ? 'fullscreen_exit_icon' : 'fullscreen_icon'" 
          />
        </button>

        <!-- New File -->
        <button class="new-file-btn" @click="showNewFileDialog = true" title="Создать новый файл">
          <span class="plus">+</span>
          New File
        </button>
      </div>
    </header>

    <div class="workspace">
      <div class="left-panel" :style="{ width: treeWidth + 'px' }">
        <DirectoryTree 
          base-path="C:/Users/Siphome/Desktop/testBuild"
          :active-path="currentFile || ''"
          @file-click="handleFileClick"
          ref="directoryTreeRef"
        />
      </div>

      <div class="splitter" @mousedown="startResizing" />

      <div class="main-content">
        <EditorTabs ref="tabsRef" />

        <div v-if="showTerminal" class="terminal-panel">
          <div class="terminal-tabs">
            <button 
              :class="{ active: activeTerminal === 'runtime' }"
              @click="activeTerminal = 'runtime'"
            >
              Runtime Console
            </button>
            <button 
              :class="{ active: activeTerminal === 'system' }"
              @click="activeTerminal = 'system'"
            >
              System Terminal
            </button>
          </div>

          <div v-if="activeTerminal === 'runtime'" class="terminal-content">
            <Terminal />
          </div>

          <div v-if="activeTerminal === 'system'" class="terminal-content">
            <SystemTerminal />
          </div>
        </div>
      </div>
    </div>

    <!-- Game Preview modal temporarily disabled (incomplete implementation causing warnings) -->

    <MessageBox ref="messageBoxRef" />
    <InputBox ref="inputBoxRef" />

    <NewFileDialog 
      v-model:visible="showNewFileDialog"
      @create="handleCreateFile"
    />

    <Teleport to="body">
      <div class="notifications-container">
        <NotificationBox
          v-for="notif in notifications"
          :key="notif.id"
          v-bind="notif"
          @dismiss="remove"
        />
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import BaseIcon from "./components/icons/BaseIcon.vue"

import DirectoryTree from "./components/DirectoryTree/DirectoryTree.vue"
import EditorTabs from "./components/Tabs/EditorTabs.vue"
import MessageBox from "./components/MessageBox/MessageBox.vue"
import InputBox from "./components/InputBox/InputBox.vue"
import NotificationBox from "./components/NotificationBox/NotificationBox.vue"
import NewFileDialog from "./components/NewFileDialog/NewFileDialog.vue"
import SystemStatus from "./components/SystemStatus/SystemStatus.vue"
import Terminal from "./components/Terminal/Terminal.vue"
import SystemTerminal from "./components/Terminal/SystemTerminal.vue"

import { useMessageBox } from './composables/useMessageBox'
import { useInputBox } from './composables/useInputBox'
import { useNotification } from './composables/useNotification'
import { useFileCreation } from './composables/useFileCreation'

const treeWidth = ref(320)
let isResizing = false
const currentFile = ref<string | null>(null)
const appVersion = ref('0.0.1')
const currentProjectPath = ref('C:/Users/Siphome/Desktop/testBuild')

const tabsRef = ref<any>(null)
const directoryTreeRef = ref<any>(null)
const messageBoxRef = ref<any>(null)
const inputBoxRef = ref<any>(null)

const showNewFileDialog = ref(false)
const isFullScreen = ref(false)
const showTerminal = ref(false)
const activeTerminal = ref<'runtime' | 'system'>('runtime')

const { notifications, remove } = useNotification()
const { messageBox } = useMessageBox()
const { inputBox } = useInputBox()
const { createFromTemplate } = useFileCreation()

// ====================== PREVIEW (Unity Play mode style) ======================
const openPreviewTab = () => {
  if (tabsRef.value?.openPreviewTab) {
    // Immediately start the game when opened from header button
    tabsRef.value.openPreviewTab({ autoStart: true })
  } else {
    console.error('openPreviewTab method not found on EditorTabs')
  }
}

const toggleFullScreen = async () => {
  try {
    if (window.electron?.toggleFullScreen) {
      const newState = await window.electron.toggleFullScreen()
      isFullScreen.value = newState
    }
  } catch (err) {
    console.error('Full screen toggle error:', err)
  }
}

const toggleTerminal = () => {
  showTerminal.value = !showTerminal.value
}

onMounted(async () => {
  messageBox.value = messageBoxRef.value
  inputBox.value = inputBoxRef.value

  try {
    if (window.electron?.getAppVersion) {
      appVersion.value = await window.electron.getAppVersion()
    }
  } catch (e) {
    console.warn('Не удалось получить версию приложения')
  }

  const handleKeyDown = (e: KeyboardEvent) => {
    if (e.key === 'F11') {
      e.preventDefault()
      toggleFullScreen()
    }
    if (e.ctrlKey && e.key.toLowerCase() === 't') {
      e.preventDefault()
      toggleTerminal()
    }
  }

  window.addEventListener('keydown', handleKeyDown)
})

const openFileSafely = async (filePath: string, retries = 8) => {
  await nextTick()
  for (let i = 0; i < retries; i++) {
    if (tabsRef.value?.openFile) {
      await tabsRef.value.openFile(filePath)
      return
    }
    await new Promise(r => setTimeout(r, 40))
  }
}

const handleCreateFile = async (data: { name: string; content: string; templateId: string }) => {
  try {
    const result = await createFromTemplate(data)
    if (result.success && result.path) {
      if (directoryTreeRef.value?.refresh) {
        await directoryTreeRef.value.refresh()
      }
      await nextTick()
      await openFileSafely(result.path)
    }
  } catch (err) {
    console.error('Error creating file:', err)
  }
}

const handleFileClick = async (filePath: string) => {
  currentFile.value = filePath
  await openFileSafely(filePath)
}

const startResizing = (e: MouseEvent) => {
  isResizing = true
  document.addEventListener('mousemove', onMouseMove)
  document.addEventListener('mouseup', stopResizing)
}

const onMouseMove = (e: MouseEvent) => {
  if (isResizing) treeWidth.value = Math.max(200, Math.min(e.clientX, 600))
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
  justify-content: space-between;
  padding: 0 12px; 
  border-bottom: 1px solid #333; 
  flex-shrink: 0;
  position: relative;
}

.logo-section { 
  display: flex; 
  align-items: center; 
  gap: 12px;
  z-index: 2;
}

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

.system-status {
  position: absolute;
  left: 50%;
  transform: translateX(-50%);
  display: flex;
  justify-content: center;
  pointer-events: none;
}

.system-status > * {
  pointer-events: auto;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 20px;
  z-index: 2;
}

.fullscreen-btn {
  background: none;
  border: none;
  color: #aaaaaa;
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 4px;
  cursor: pointer;
}

.fullscreen-btn:hover {
  background: #333333;
  color: #ffffff;
}

.new-file-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  background: #FF5252;
  color: white;
  border: none;
  padding: 6px 14px;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
}

.new-file-btn:hover {
  background: #ff6b6b;
  transform: translateY(-1px);
}

.plus {
  font-size: 18px;
  line-height: 1;
  font-weight: bold;
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

.notifications-container {
  position: fixed;
  bottom: 24px;
  right: 24px;
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  z-index: 10000;
  pointer-events: none;
}

.notifications-container > * {
  pointer-events: auto;
}

.preview-btn {
  background: #FF5252;
  color: white;
  border: none;
  padding: 6px 14px;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 6px;
}

.preview-btn:hover {
  background: #555;
  transform: translateY(-1px);
}

/* Play button (green) - used for immediate game preview */
.preview-btn.play-btn {
  background: #22c55e; /* nice green */
  color: white;
  padding: 0;
  border-radius: 6px;
  width: 34px;
  height: 34px;
  display: flex;
  align-items: center;
  justify-content: center;
  border: none;
}

.preview-btn.play-btn:hover {
  background: #16a34a;
  transform: scale(1.05);
}

.preview-btn.play-btn .btn-icon {
  width: 18px;
  height: 18px;
  color: white; /* ensures currentColor works well */
}

.terminal-toggle-btn {
  background: #FF5252;
  color: #ccc;
  border: 1px solid #555;
  padding: 6px 14px;
  border-radius: 4px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 6px;
}

.terminal-toggle-btn:hover {
  background: #555;
  color: white;
}

.terminal-toggle-btn.active {
  background: #FF5252;
  color: white;
  border-color: #FF5252;
}

.main-content {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  position: relative;
}

/* Панель терминала и стили для вкладок переключения */
.terminal-panel {
  height: 320px;
  min-height: 200px;
  border-top: 2px solid #FF5252;
  background: #0c0c0c;
  overflow: hidden;
  display: flex;
  flex-direction: column;
  z-index: 10;
}

.terminal-tabs {
  display: flex;
  background: #181818;
  border-bottom: 1px solid #2d2d2d;
  padding: 0 4px;
  height: 32px;
  align-items: flex-end;
}

.terminal-tabs button {
  background: transparent;
  border: none;
  color: #858585;
  padding: 6px 16px;
  font-size: 12px;
  cursor: pointer;
  border-top-left-radius: 4px;
  border-top-right-radius: 4px;
  transition: all 0.15s ease;
  height: 28px;
  display: flex;
  align-items: center;
}

.terminal-tabs button:hover {
  color: #cccccc;
  background: #222222;
}

.terminal-tabs button.active {
  color: #ffffff;
  background: #0c0c0c;
  border-bottom: 2px solid #FF5252;
  font-weight: 500;
}

.terminal-content {
  flex: 1;
  overflow: hidden;
  height: calc(100% - 32px);
}

.preview-modal {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

.preview-modal-content {
  width: 920px;
  max-width: 96vw;
  max-height: 92vh;
  background: #1e1e1e;
  border-radius: 10px;
  border: 1px solid #444;
  overflow: hidden;
  box-shadow: 0 20px 70px rgba(0, 0, 0, 0.7);
  display: flex;
  flex-direction: column;
}

.preview-modal-header {
  height: 46px;
  background: #252526;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  font-weight: 600;
  color: #ddd;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
}

.close-btn {
  background: none;
  border: none;
  color: #aaa;
  font-size: 22px;
  cursor: pointer;
  padding: 4px 10px;
  border-radius: 4px;
}

.close-btn:hover {
  background: #333;
  color: #fff;
}
</style>