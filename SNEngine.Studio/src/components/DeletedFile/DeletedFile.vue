<template>
  <div class="deleted-file">
    <div class="content">
      <!-- Иконка -->
      <BaseIcon 
        name="deleted_icon" 
        color="#FF5252"
        class="deleted-icon"
      />
      
      <h2>Файл удалён</h2>
      <p>Файл был удалён или перемещён за пределы проекта.</p>
      
      <div class="file-info">
        <div class="path-label">Путь:</div>
        <code class="path">{{ filePath }}</code>
      </div>

      <div class="actions">
        <button class="btn-close" @click="closeTab">
          Закрыть вкладку
        </button>
        <button class="btn-restore" @click="restoreFile" v-if="canRestore">
          Восстановить (если возможно)
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  filePath: string
  tabId?: string
}>()

const emit = defineEmits<{
  (e: 'close-tab'): void
  (e: 'restore', path: string): void
}>()

const extension = computed(() => 
  props.filePath.split('.').pop()?.toLowerCase() || ''
)

const canRestore = computed(() => {
  // Можно добавить логику проверки корзины позже
  return false
})

const closeTab = () => {
  emit('close-tab')
}

const restoreFile = () => {
  emit('restore', props.filePath)
}
</script>

<style scoped>
.deleted-file {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #161616;
  color: #888;
  user-select: none;
}

.content {
  text-align: center;
  max-width: 420px;
  padding: 40px 20px;
}

.deleted-icon {
  width: 96px;
  height: 96px;
  margin-bottom: 24px;
  opacity: 0.85;
}

h2 {
  color: #FF5252;
  margin-bottom: 12px;
  font-size: 22px;
}

p {
  margin-bottom: 28px;
  line-height: 1.5;
}

.file-info {
  background: #1e1e1e;
  border: 1px solid #333;
  border-radius: 6px;
  padding: 12px 16px;
  margin-bottom: 32px;
  text-align: left;
}

.path-label {
  font-size: 11px;
  color: #666;
  margin-bottom: 4px;
}

.path {
  font-size: 13px;
  color: #ccc;
  word-break: break-all;
  font-family: monospace;
}

.actions {
  display: flex;
  gap: 12px;
  justify-content: center;
}

.btn-close,
.btn-restore {
  padding: 8px 20px;
  border-radius: 4px;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-close {
  background: #333;
  border: 1px solid #444;
  color: #ddd;
}

.btn-close:hover {
  background: #444;
  color: white;
}

.btn-restore {
  background: #FF5252;
  border: none;
  color: white;
}

.btn-restore:hover {
  background: #ff6b6b;
}
</style>