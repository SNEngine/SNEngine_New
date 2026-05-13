// src/composables/useNotification.ts
import { ref } from 'vue'

export type NotificationType = 'success' | 'error' | 'warning' | 'info'

interface Notification {
  id: string
  type: NotificationType
  title?: string
  message: string
  duration?: number
}

const notifications = ref<Notification[]>([])

export function useNotification() {
  const show = (options: Omit<Notification, 'id'>) => {
    const id = 'notif-' + Date.now() + Math.random().toString(36).slice(2, 7)

    notifications.value.push({
      id,
      type: options.type || 'info',
      title: options.title,
      message: options.message,
      duration: options.duration ?? 4000
    })

    // Автоудаление после анимации
    setTimeout(() => remove(id), (options.duration ?? 4000) + 800)
  }

  const remove = (id: string) => {
    const idx = notifications.value.findIndex(n => n.id === id)
    if (idx > -1) notifications.value.splice(idx, 1)
  }

  const success = (message: string, title?: string) => show({ type: 'success', message, title, duration: 3200 })
  const error   = (message: string, title?: string) => show({ type: 'error',   message, title, duration: 5000 })
  const warning = (message: string, title?: string) => show({ type: 'warning', message, title })
  const info    = (message: string, title?: string) => show({ type: 'info',    message, title })

  return {
    notifications,
    show,
    success,
    error,
    warning,
    info,
    remove
  }
}