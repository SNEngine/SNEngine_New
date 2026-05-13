// src/composables/useFileCrud.ts
import { useMessageBox } from './useMessageBox'
import { useInputBox } from './useInputBox'
import { lastUpdate } from '@/utils/watcherState'

export function useFileCrud() {
  const { showMessageBox } = useMessageBox()
  const { showInputBox } = useInputBox()

  // Создать файл или папку
  const createItem = async (basePath: string, isFolder: boolean, selectedItem?: any) => {
    let targetDir = basePath
    if (selectedItem?.isFolder) {
      targetDir = selectedItem.path
    }

    const name = await showInputBox({
      title: isFolder ? 'Новая папка' : 'Новый файл',
      message: `Создание в: ${targetDir.split(/[\\/]/).pop() || 'Корень'}`,
      placeholder: isFolder ? 'Имя папки' : 'new_file.sn'
    })

    if (!name) return

    const fullPath = `${targetDir}/${name}`.replace(/\\/g, '/')

    try {
      if (isFolder) {
        await (window as any).electron?.createDirectory?.(fullPath)
      } else {
        await (window as any).electron?.createFile?.(fullPath)
      }
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({
        title: 'Ошибка',
        message: `Не удалось создать ${isFolder ? 'папку' : 'файл'}`,
        icon: 'error'
      })
    }
  }

  // Переименовать
  const renameItem = async (item: any) => {
    if (!item) return

    const newName = await showInputBox({
      title: 'Переименование',
      message: `Новое имя для "${item.name}":`,
      value: item.name
    })

    if (!newName || newName === item.name) return

    try {
      await (window as any).electron?.renameItem?.(item.path, newName)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({
        title: 'Ошибка переименования',
        message: 'Не удалось переименовать элемент',
        icon: 'error'
      })
    }
  }

  // Удалить
  const deleteItem = async (item: any) => {
    if (!item) return

    const result = await showMessageBox({
      title: 'Подтверждение удаления',
      message: `Вы действительно хотите удалить "${item.name}"?`,
      type: 'yesno',
      icon: 'warning'
    })

    if (result !== 'yes') return

    try {
      await (window as any).electron?.deleteItem?.(item.path)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({
        title: 'Ошибка удаления',
        message: 'Не удалось удалить элемент',
        icon: 'error'
      })
    }
  }

  return {
    createItem,
    renameItem,
    deleteItem
  }
}