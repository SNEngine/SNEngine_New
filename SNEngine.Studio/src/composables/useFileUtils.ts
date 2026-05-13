import { lastUpdate } from '@/utils/watcherState'
import { useMessageBox } from './useMessageBox'

export function useFileUtils() {
  const { showMessageBox } = useMessageBox()

  const duplicateItem = async (path: string) => {
    if (!path) return

    try {
      const electron = (window as any).electron
      
      if (electron?.duplicateItem) {
        await electron.duplicateItem(path)
      }

      lastUpdate.value = Date.now()
    } catch (err: any) {
      await showMessageBox({
        title: 'Ошибка дублирования',
        message: 'Не удалось создать копию объекта',
        icon: 'error'
      })
    }
  }

  const copyPath = async (path: string) => {
    try {
      await navigator.clipboard.writeText(path)
    } catch (err) {
      await showMessageBox({
        title: 'Ошибка',
        message: 'Не удалось скопировать путь в буфер обмена',
        icon: 'error'
      })
    }
  }

  const copyName = async (path: string) => {
    const name = path.split(/[/\\]/).pop() || ''
    try {
      await navigator.clipboard.writeText(name)
    } catch (err) {
      await showMessageBox({
        title: 'Ошибка',
        message: 'Не удалось скопировать имя файла',
        icon: 'error'
      })
    }
  }

  const showInExplorer = (path: string) => {
    if ((window as any).electron?.showInExplorer) {
      (window as any).electron.showInExplorer(path)
    }
  }

  return {
    duplicateItem,
    copyPath,
    copyName,
    showInExplorer
  }
}