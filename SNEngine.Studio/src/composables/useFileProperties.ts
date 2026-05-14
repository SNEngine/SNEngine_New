import { ref } from 'vue'

interface FileInfo {
  name: string
  path: string
  size?: number
  created?: Date
  modified?: Date
  isFolder?: boolean
}

const isOpen = ref(false)
const currentFile = ref<FileInfo | null>(null)

export function useFileProperties() {
  const showProperties = async (file: FileInfo) => {
    let fileWithStats = { ...file }

    // Запрашиваем stats ВСЕГДА (и для файлов, и для папок)
    if ((window as any).electron?.getFileStats) {
      try {
        const stats = await (window as any).electron.getFileStats(file.path)
        
        fileWithStats = {
          ...file,
          size: stats.size,
          created: stats.created ? new Date(stats.created) : undefined,
          modified: stats.modified ? new Date(stats.modified) : undefined
        }
      } catch (e) {
        console.warn('Не удалось получить stats:', e)
      }
    }

    currentFile.value = fileWithStats
    isOpen.value = true
  }

  const close = () => {
    isOpen.value = false
    setTimeout(() => {
      currentFile.value = null
    }, 300)
  }

  return {
    isOpen,
    currentFile,
    showProperties,
    close
  }
}