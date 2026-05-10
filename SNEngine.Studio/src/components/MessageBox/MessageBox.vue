<template>
  <Teleport to="body">
    <div v-if="visible" class="messagebox-overlay" @click.self="handleOverlayClick">
      <div class="messagebox">
        <div class="messagebox-header">
          <span class="title">{{ title }}</span>
        </div>
        
        <div class="messagebox-content">
          <BaseIcon v-if="iconName" :name="iconName" class="message-icon" />
          <p class="message">{{ message }}</p>
        </div>

        <div class="messagebox-footer">
          <button 
            v-for="btn in buttons" 
            :key="btn.key"
            class="msg-btn"
            :class="btn.type"
            @click="handleClick(btn.key)"
          >
            {{ btn.text }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'          // ← Добавили computed
import BaseIcon from '../icons/BaseIcon.vue'

export type MessageBoxType = 'ok' | 'okcancel' | 'yesno' | 'yesnocancel'

export interface MessageBoxOptions {
  title?: string
  message: string
  type?: MessageBoxType
  icon?: 'info' | 'warning' | 'error' | 'question'
}

const visible = ref(false)
const currentOptions = ref<MessageBoxOptions>({ message: '' })
const resolvePromise = ref<((value: string) => void) | null>(null)

const title = computed(() => currentOptions.value.title || 'SNEngine Studio')
const message = computed(() => currentOptions.value.message)
const iconName = computed(() => {
  const map: Record<string, string> = {
    warning: 'unknown_icon',   // можно заменить на warning_icon позже
    error: 'unknown_icon',
    question: 'unknown_icon',
    info: 'unknown_icon'
  }
  return map[currentOptions.value.icon || ''] || null
})

const show = (options: MessageBoxOptions): Promise<string> => {
  currentOptions.value = {
    title: 'SNEngine Studio',
    type: 'ok',
    ...options
  }
  visible.value = true

  return new Promise<string>((resolve) => {
    resolvePromise.value = resolve
  })
}

const handleClick = (result: string) => {
  visible.value = false
  if (resolvePromise.value) {
    resolvePromise.value(result)
    resolvePromise.value = null
  }
}

const handleOverlayClick = () => {
  if (currentOptions.value.type === 'ok') handleClick('ok')
}

const buttons = computed(() => {
  const t = currentOptions.value.type || 'ok'
  
  if (t === 'ok') return [{ key: 'ok', text: 'OK', type: 'primary' }]
  if (t === 'okcancel') {
    return [
      { key: 'ok', text: 'OK', type: 'primary' },
      { key: 'cancel', text: 'Отмена', type: 'secondary' }
    ]
  }
  if (t === 'yesno') {
    return [
      { key: 'yes', text: 'Да', type: 'primary' },
      { key: 'no', text: 'Нет', type: 'secondary' }
    ]
  }
  if (t === 'yesnocancel') {
    return [
      { key: 'yes', text: 'Да', type: 'primary' },
      { key: 'no', text: 'Нет', type: 'secondary' },
      { key: 'cancel', text: 'Отмена', type: 'secondary' }
    ]
  }
  return [{ key: 'ok', text: 'OK', type: 'primary' }]
})

defineExpose({ show })
</script>

<style scoped>
.messagebox-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.85);
  backdrop-filter: blur(6px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
}

.messagebox {
  width: 420px;
  background: #1e1e1e;
  border: 1px solid #333;
  border-radius: 8px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.7);
  overflow: hidden;
}

.messagebox-header {
  background: #252526;
  padding: 12px 16px;
  border-bottom: 1px solid #333;
  font-weight: 600;
  color: #FF5252;
}

.messagebox-content {
  padding: 24px 20px;
  display: flex;
  gap: 16px;
  align-items: flex-start;
  min-height: 80px;
}

.message-icon {
  width: 48px;
  height: 48px;
  flex-shrink: 0;
  color: #FF5252;
}

.message {
  margin: 4px 0 0 0;
  line-height: 1.5;
  color: #ddd;
  flex: 1;
}

.messagebox-footer {
  padding: 12px 16px;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  background: #252526;
  border-top: 1px solid #333;
}

.msg-btn {
  padding: 8px 20px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  cursor: pointer;
  min-width: 80px;
}

.msg-btn.primary {
  background: #FF5252;
  color: white;
}
.msg-btn.primary:hover { background: #ff1744; }

.msg-btn.secondary {
  background: #333;
  color: #ddd;
}
.msg-btn.secondary:hover { background: #444; }
</style>