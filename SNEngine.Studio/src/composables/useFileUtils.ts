// src/composables/useFileUtils.ts
import { lastUpdate } from '@/utils/watcherState'
import { useMessageBox } from './useMessageBox'
import { useNotification } from './useNotification'

export function useFileUtils() {
  const { showMessageBox } = useMessageBox()
  const { success, error } = useNotification()

  // ====================== СУЩЕСТВУЮЩИЕ МЕТОДЫ ======================

  const duplicateItem = async (path: string) => {
    if (!path) return

    const name = path.split(/[/\\]/).pop() || 'file'

    try {
      const electron = (window as any).electron
      await electron?.duplicateItem?.(path)

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

  // ====================== НОВЫЕ МЕТОДЫ ДЛЯ DRAG & DROP ======================

  const moveItem = async (sourcePath: string, targetDir: string) => {
    try {
      const electron = (window as any).electron
      const result = await electron?.moveItem?.(sourcePath, targetDir)

      if (result?.success) {
        lastUpdate.value = Date.now()
        success('Файл перемещён', result.newPath.split(/[/\\]/).pop())
        return true
      } else {
        throw new Error(result?.error || 'Неизвестная ошибка')
      }
    } catch (err: any) {
      error('Не удалось переместить файл', err.message)
      return false
    }
  }

  const copyItem = async (sourcePath: string, targetDir: string) => {
    try {
      const electron = (window as any).electron
      const result = await electron?.copyItem?.(sourcePath, targetDir)

      if (result?.success) {
        lastUpdate.value = Date.now()
        success('Файл скопирован', result.newPath.split(/[/\\]/).pop())
        return true
      } else {
        throw new Error(result?.error || 'Неизвестная ошибка')
      }
    } catch (err: any) {
      error('Не удалось скопировать файл', err.message)
      return false
    }
  }

  return {
    // Старые
    duplicateItem,
    copyPath,
    copyName,
    showInExplorer,

    // Новые для Drag & Drop
    moveItem,
    copyItem,
  }
}