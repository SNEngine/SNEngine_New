<template>
  <div class="editor-layout">
    <!-- Левая панель (дерево) -->
    <div class="panel left-panel" :style="{ width: leftWidth + 'px' }">
      <slot name="left">
        <!-- По умолчанию — дерево -->
        <DirectoryTree 
          :base-path="projectPath"
          @file-click="openFile"
        />
      </slot>
    </div>

    <!-- Разделитель -->
    <div class="splitter" @mousedown="startResize" />

    <!-- Правая панель (редакторы) -->
    <div class="panel right-panel">
      <slot name="right">
        <div v-if="activeFile" class="editor-area">
          <!-- Здесь будет CodeEditor или WebEditor в будущем -->
          <CodeEditorModal 
            :file-path="activeFile"
            @close="activeFile = null"
          />
        </div>
        <div v-else class="empty-state">
          <p>Выберите файл в дереве проектов</p>
        </div>
      </slot>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, defineProps } from 'vue'
import DirectoryTree from '../DirectoryTree/DirectoryTree.vue'
import CodeEditorModal from '../CodeEditor/CodeEditorModal.vue'

const props = defineProps<{
  projectPath: string
}>()

const leftWidth = ref(320)
const activeFile = ref<string | null>(null)

const emit = defineEmits<{
  (e: 'file-open', path: string): void
}>()

let isResizing = false

const startResize = (e: MouseEvent) => {
  isResizing = true
  document.addEventListener('mousemove', onMouseMove)
  document.addEventListener('mouseup', onMouseUp)
}

const onMouseMove = (e: MouseEvent) => {
  if (isResizing) {
    leftWidth.value = Math.max(180, Math.min(e.clientX - 20, 600))
  }
}

const onMouseUp = () => {
  isResizing = false
  document.removeEventListener('mousemove', onMouseMove)
  document.removeEventListener('mouseup', onMouseUp)
}

const openFile = (path: string) => {
  activeFile.value = path
  emit('file-open', path)
}
</script>

<style scoped>
.editor-layout {
  display: flex;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
}

.panel {
  height: 100%;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.left-panel {
  border-right: 1px solid #333;
  min-width: 180px;
}

.splitter {
  width: 4px;
  background: #2a2a2a;
  cursor: col-resize;
  transition: background 0.2s;
  z-index: 10;
}

.splitter:hover {
  background: #FF5252;
}

.right-panel {
  flex: 1;
  min-width: 300px;
}

.empty-state {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #666;
  font-size: 1.1rem;
}
</style>