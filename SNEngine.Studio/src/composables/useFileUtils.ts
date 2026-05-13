// src/composables/useFileUtils.ts
import { lastUpdate } from '@/utils/watcherState'
import { useMessageBox } from './useMessageBox'
import { useNotification } from './useNotification'

export function useFileUtils() {
  const { showMessageBox } = useMessageBox()
  const { success, error } = useNotification()

  const duplicateItem = async (path: string) => {
    if (!path) return

    const name = path.split(/[/\\]/).pop() || 'file'

    try {
      const electron = (window as any).electron
      
      if (electron?.duplicateItem) {
        await electron.duplicateItem(path)
      } else {
        // Fallback
        const content = await electron?.readFile?.(path)
        if (content !== undefined) {
          const newPath = path.replace(name, `Копия ${name}`)
          await electron?.writeFile?.(newPath, content)
        }
      }

      lastUpdate.value = Date.now()
      success('Файл дублирован', name)
    } catch (err) {
      error('Не удалось дублировать файл', name)
    }
  }

  const copyPath = async (path: string) => {
    try {
      await navigator.clipboard.writeText(path)
      success('Путь скопирован', path.split(/[/\\]/).pop())
    } catch {
      error('Не удалось скопировать путь')
    }
  }

  const copyName = async (path: string) => {
    const name = path.split(/[/\\]/).pop() || ''
    try {
      await navigator.clipboard.writeText(name)
      success('Имя скопировано', name)
    } catch {
      error('Не удалось скопировать имя')
    }
  }

  const showInExplorer = (path: string) => {
    (window as any).electron?.showInExplorer?.(path)
  }

  return {
    duplicateItem,
    copyPath,
    copyName,
    showInExplorer
  }
}