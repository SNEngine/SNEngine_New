import { ref } from 'vue'

export interface FileUpdateEvent {
  type: 'add' | 'change' | 'unlink' | 'addDir' | 'unlinkDir'
  path: string
  timestamp: number
}

export const lastUpdate = ref<FileUpdateEvent | null>(null)

export const notifyFileUpdate = (type: FileUpdateEvent['type'], path: string) => {
  lastUpdate.value = {
    type,
    path,
    timestamp: Date.now()
  }
}

// Инициализация слушателей (вызовите это один раз при старте приложения, например в App.vue или прямо здесь)
if (window.electron && window.electron.onFileChange) {
  window.electron.onFileChange((data: { type: string; path: string }) => {
    notifyFileUpdate(data.type as any, data.path)
  })
}