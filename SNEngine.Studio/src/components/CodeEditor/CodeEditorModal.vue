<template>
  <Teleport to="body">
    <div v-if="isOpen" class="modal-overlay" @click.self="closeEditor">
      <div class="modal-content">
        <div class="modal-header">
          <div class="header-left">
            <span class="file-icon">📄</span>
            <span class="file-name">{{ currentOptions.title }}</span>
            <span class="file-lang">{{ currentOptions.language }}</span>
          </div>
          <div class="header-actions">
            <button class="btn-save" @click="save">💾 Сохранить</button>
            <button class="btn-close" @click="closeEditor">✕</button>
          </div>
        </div>

        <div class="editor-container">
          <CodeEditor 
            v-model="localCode" 
            :language="currentOptions.language"
            theme="snengine-dark"
          />
        </div>

        <div class="modal-footer">
          <span class="status">Строк: {{ localCode.split('\n').length }}</span>
          <span class="shortcut">Ctrl+S — Сохранить • Esc — Закрыть</span>
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

const localCode = ref('')

// Синхронизируем код при открытии модалки
watch(isOpen, (val) => {
  if (val) {
    localCode.value = currentOptions.value.code
  }
})

const save = () => {
  if (currentOptions.value.onSave) {
    currentOptions.value.onSave(localCode.value)
  }
  saveAndClose(localCode.value)
}
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100vw;
  height: 100vh;
  background: rgba(0, 0, 0, 0.7);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

.modal-content {
  width: 90vw;
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
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.file-name { color: #d4d4d4; font-weight: 600; }
.file-lang { color: #FF5252; font-size: 12px; background: #2d2d2d; padding: 2px 8px; border-radius: 4px; text-transform: uppercase; }

.editor-container {
  flex: 1; /* Занимает всё доступное пространство */
  width: 100%;
  overflow: hidden;
}

.modal-footer {
  padding: 8px 20px;
  background: #252526;
  border-top: 1px solid #333;
  display: flex;
  justify-content: space-between;
  font-size: 12px;
  color: #888;
}

.btn-save {
  background: #FF5252;
  color: white;
  border: none;
  padding: 6px 14px;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
}

.btn-save:hover { background: #ff1744; }

.btn-close {
  background: transparent;
  border: none;
  color: #888;
  font-size: 20px;
  cursor: pointer;
  padding: 0 10px;
}

.btn-close:hover { color: white; }
</style>