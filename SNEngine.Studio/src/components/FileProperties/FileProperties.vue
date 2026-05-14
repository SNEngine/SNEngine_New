<template>
  <div class="properties-modal" @click.self="$emit('close')">
    <div class="properties-window">
      <!-- Заголовок -->
      <div class="properties-header">
        <BaseIcon name="info_icon" color="#FF5252" class="header-icon" />
        <div>
          <div class="title">Свойства: {{ fileName }}</div>
          <div class="subtitle">{{ fileType }}</div>
        </div>
        <button class="close-btn" @click="$emit('close')">✕</button>
      </div>

      <!-- Вкладки -->
      <div class="tabs">
        <div 
          v-for="tab in tabs" 
          :key="tab.id"
          class="tab" 
          :class="{ active: activeTab === tab.id }"
          @click="activeTab = tab.id"
        >
          {{ tab.label }}
        </div>
      </div>

      <!-- Содержимое -->
      <div class="properties-content">
        <!-- Общие -->
        <div v-if="activeTab === 'general'" class="tab-content">
          <div class="info-row">
            <div class="label">Имя:</div>
            <div class="value">{{ file.name }}</div>
          </div>
          <div class="info-row">
            <div class="label">Тип:</div>
            <div class="value">{{ fileType }} ({{ fileExtension?.toUpperCase() }})</div>
          </div>
          <div class="info-row">
            <div class="label">Путь:</div>
            <div class="value path">{{ file.path }}</div>
          </div>
          <div class="info-row">
            <div class="label">Размер:</div>
            <div class="value">{{ formatSize(file.size) }}</div>
          </div>
        </div>

        <!-- Подробно -->
        <div v-if="activeTab === 'details'" class="tab-content">
          <div class="info-row">
            <div class="label">Создан:</div>
            <div class="value">{{ formatDate(file.created) }}</div>
          </div>
          <div class="info-row">
            <div class="label">Изменён:</div>
            <div class="value">{{ formatDate(file.modified) }}</div>
          </div>
          <div class="info-row">
            <div class="label">Открыт:</div>
            <div class="value">{{ formatDate(new Date()) }}</div>
          </div>
        </div>
      </div>

      <!-- Нижние кнопки -->
      <div class="properties-footer">
        <button class="btn" @click="$emit('close')">OK</button>
        <button class="btn secondary" @click="$emit('close')">Отмена</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  file: {
    name: string
    path: string
    size?: number
    created?: Date
    modified?: Date
    isFolder?: boolean
  }
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()

const activeTab = ref<'general' | 'details'>('general')

const tabs = [
  { id: 'general', label: 'Общие' },
  { id: 'details', label: 'Подробно' }
]

const fileName = props.file.name
const fileExtension = props.file.name.split('.').pop() || ''
const fileType = props.file.isFolder ? 'Папка' : 'Файл'

const formatSize = (bytes?: number) => {
  if (!bytes) return '—'
  if (bytes < 1024) return bytes + ' байт'
  if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' КБ'
  return (bytes / (1024 * 1024)).toFixed(1) + ' МБ'
}

const formatDate = (date?: Date) => {
  if (!date) return '—'
  return date.toLocaleString('ru-RU', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  })
}
</script>

<style scoped>
.properties-modal {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.75);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 100000;
}

.properties-window {
  background: #1f1f1f;
  border: 1px solid #3a3a3a;
  border-radius: 8px;
  width: 420px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.8);
  overflow: hidden;
}

.properties-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px 16px;
  background: #252526;
  border-bottom: 1px solid #3a3a3a;
}

.header-icon { width: 28px; height: 28px; }

.title {
  font-weight: 600;
  font-size: 14px;
  color: #ffffff;
}

.subtitle {
  font-size: 12px;
  color: #888;
}

.close-btn {
  margin-left: auto;
  background: none;
  border: none;
  color: #888;
  font-size: 18px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 4px;
}

.close-btn:hover { color: #FF5252; background: #3a3a3a; }

.tabs {
  display: flex;
  background: #252526;
  border-bottom: 1px solid #333;
}

.tab {
  padding: 8px 16px;
  cursor: pointer;
  font-size: 12.5px;
  color: #aaa;
  transition: all 0.2s;
}

.tab:hover { color: #ddd; }
.tab.active {
  color: #FF5252;
  border-bottom: 2px solid #FF5252;
  background: #1f1f1f;
}

.properties-content {
  padding: 16px;
  min-height: 180px;
}

.info-row {
  display: flex;
  margin-bottom: 10px;
  font-size: 13px;
}

.label {
  width: 90px;
  color: #888;
  flex-shrink: 0;
}

.value {
  flex: 1;
  color: #eeeeee;
  word-break: break-all;
}

.path {
  font-family: monospace;
  font-size: 12px;
  color: #aaa;
}

.properties-footer {
  padding: 12px 16px;
  display: flex;
  gap: 8px;
  justify-content: flex-end;
  background: #252526;
  border-top: 1px solid #3a3a3a;
}

.btn {
  padding: 6px 16px;
  border-radius: 4px;
  font-size: 13px;
  cursor: pointer;
}

.btn.secondary {
  background: transparent;
  border: 1px solid #3a3a3a;
  color: #ccc;
}

.btn:not(.secondary) {
  background: #FF5252;
  color: white;
  border: none;
}
</style>