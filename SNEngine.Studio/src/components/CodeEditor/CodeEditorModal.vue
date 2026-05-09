<template>
  <Teleport to="body">
    <div class="modal-overlay" @click.self="close">
      <div class="modal-content">
        <!-- Заголовок -->
        <div class="modal-header">
          <div class="header-left">
            <span class="file-icon">📄</span>
            <span class="file-name">{{ currentOptions.title }}</span>
            <span class="file-lang">.sn</span>
          </div>
          <div class="header-actions">
            <button class="btn-save" @click="save">💾 Save (Ctrl+S)</button>
            <button class="btn-close" @click="close">✕</button>
          </div>
        </div>

        <!-- Редактор -->
        <div class="editor-container">
          <CodeEditor v-model="localCode" />
        </div>

        <!-- Футер -->
        <div class="modal-footer">
          <span class="status">Ready • {{ localCode.split('\n').length }} lines</span>
          <span class="shortcut">Ctrl+S — Save • Esc — Close</span>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import CodeEditor from './CodeEditor.vue'
import { useCodeEditor } from '../../composables/useCodeEditor'

const { isOpen, currentOptions, saveAndClose, closeEditor } = useCodeEditor()
const localCode = ref(currentOptions.value.code)

watch(() => currentOptions.value.code, (val) => localCode.value = val)

const save = () => saveAndClose(localCode.value)
const close = () => closeEditor()
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.85);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
  backdrop-filter: blur(4px);
}

.modal-content {
  width: 92%;
  max-width: 1200px;
  height: 85vh;
  background: #1e1e1e;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.6);
  border: 1px solid #333;
}

.modal-header {
  padding: 12px 20px;
  background: #252526;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #333;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.file-icon { font-size: 18px; }
.file-name { color: #d4d4d4; font-weight: 600; }
.file-lang { color: #6a9955; font-size: 13px; background: #2d2d2d; padding: 2px 8px; border-radius: 4px; }

.header-actions { display: flex; gap: 8px; }

.btn-save {
  background: #0d8f0d;
  color: white;
  border: none;
  padding: 6px 16px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
  transition: background 0.2s;
}
.btn-save:hover { background: #0a6b0a; }

.btn-close {
  background: #c42b1c;
  color: white;
  border: none;
  width: 32px;
  height: 32px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 16px;
  transition: background 0.2s;
}
.btn-close:hover { background: #9c2116; }

.editor-container {
  flex: 1;
  padding: 8px;
  background: #1e1e1e;
}

.modal-footer {
  padding: 8px 20px;
  background: #252526;
  border-top: 1px solid #333;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 12px;
  color: #858585;
}
</style>