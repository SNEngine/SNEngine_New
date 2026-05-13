<template>
  <div 
    class="notification"
    :class="[type, { show: isVisible }]"
    @click="dismiss"
  >
    <BaseIcon 
      :name="icon" 
      class="notification-icon"
      :color="iconColor"
    />
    <div class="notification-content">
      <div class="notification-title" v-if="title">{{ title }}</div>
      <div class="notification-message">{{ message }}</div>
    </div>
    <button class="close-btn" @click.stop="dismiss">×</button>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'

export interface NotificationProps {
  id: string
  type?: 'success' | 'error' | 'warning' | 'info'
  title?: string
  message: string
  duration?: number
}

const props = withDefaults(defineProps<NotificationProps>(), {
  type: 'info',
  duration: 4000
})

const emit = defineEmits<{
  (e: 'dismiss', id: string): void
}>()

const isVisible = ref(false)

const iconMap = {
  success: 'success_icon',
  error: 'error_icon',
  warning: 'warning_icon',
  info: 'info_icon'
} as const

const icon = iconMap[props.type]
const iconColor = {
  success: '#4ade80',
  error: '#f87171',
  warning: '#fbbf24',
  info: '#60a5fa'
}[props.type]

let timer: ReturnType<typeof setTimeout> | null = null

const dismiss = () => {
  if (timer) clearTimeout(timer)
  isVisible.value = false
  setTimeout(() => emit('dismiss', props.id), 300) // ждём окончания анимации ухода
}

onMounted(() => {
  // Небольшая задержка для запуска анимации
  setTimeout(() => {
    isVisible.value = true
  }, 10)

  if (props.duration > 0) {
    timer = setTimeout(dismiss, props.duration)
  }
})

onUnmounted(() => {
  if (timer) clearTimeout(timer)
})
</script>

<style scoped>
.notification {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: #1f1f1f;
  border: 1px solid;
  border-radius: 10px;
  padding: 14px 18px;
  min-width: 300px;
  max-width: 420px;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.7),
              0 10px 10px -5px rgba(0, 0, 0, 0.5);
  margin-bottom: 10px;
  cursor: pointer;
  overflow: hidden;
  
  /* Начальное состояние — из пустоты */
  opacity: 0;
  transform: translate(80px, 60px) scale(0.7) rotate(8deg);
  transition: all 0.45s cubic-bezier(0.34, 1.56, 0.64, 1);
}

.notification.show {
  opacity: 1;
  transform: translate(0, 0) scale(1) rotate(0deg);
}

/* Цвета бордеров */
.success { border-color: #4ade80; }
.error   { border-color: #f87171; }
.warning { border-color: #fbbf24; }
.info    { border-color: #60a5fa; }

.notification-icon {
  width: 24px;
  height: 24px;
  flex-shrink: 0;
  margin-top: 2px;
}

.notification-content {
  flex: 1;
  font-size: 13.5px;
  line-height: 1.45;
}

.notification-title {
  font-weight: 600;
  margin-bottom: 3px;
  color: #ffffff;
}

.notification-message {
  color: #e0e0e0;
}

.close-btn {
  background: none;
  border: none;
  color: #777;
  font-size: 22px;
  line-height: 1;
  padding: 0 6px;
  margin: -6px -8px 0 0;
  cursor: pointer;
  transition: color 0.2s;
}

.close-btn:hover {
  color: #fff;
}

/* Анимация ухода при закрытии */
.notification {
  transition: all 0.3s ease-out;
}
.notification:not(.show) {
  opacity: 0;
  transform: translate(30px, 40px) scale(0.85);
}
</style>