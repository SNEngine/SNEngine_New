// src/composables/useFileCrud.ts
import { useMessageBox } from './useMessageBox'
import { useInputBox } from './useInputBox'
import { useNotification } from './useNotification'
import { lastUpdate } from '@/utils/watcherState'

export function useFileCrud() {
  const { showMessageBox } = useMessageBox()
  const { showInputBox } = useInputBox()
  const { success, error } = useNotification()

  // Создать файл или папку
  const createItem = async (basePath: string, isFolder: boolean, selectedItem?: any) => {
    let targetDir = basePath
    if (selectedItem?.isFolder) targetDir = selectedItem.path

    const name = await showInputBox({
      title: isFolder ? 'Новая папка' : 'Новый файл',
      message: `Создание в: ${targetDir.split(/[\\/]/).pop() || 'Корень'}`,
      placeholder: isFolder ? 'Имя папки' : 'new_file.sn'
    })

    if (!name) return

    const fullPath = `${targetDir}/${name}`.replace(/\\/g, '/')

    try {
      const electron = (window as any).electron
      if (isFolder) {
        await electron?.createDirectory?.(fullPath)
      } else {
        await electron?.createFile?.(fullPath)
      }

      lastUpdate.value = Date.now()
      success(
        isFolder ? 'Папка создана' : 'Файл создан',
        name
      )
    } catch (err) {
      error(
        `Не удалось создать ${isFolder ? 'папку' : 'файл'}`,
        name
      )
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
      success('Переименовано', newName)
    } catch (err) {
      error('Ошибка переименования', item.name)
    }
  }

  // Удалить один элемент
  const deleteItem = async (item: any) => {
    if (!item) return
    await deleteItems([item])
  }

  /**
   * Bulk delete — supports multiple items
   */
  const deleteItems = async (items: any[]) => {
    if (!items || items.length === 0) return

    const count = items.length
    const title = count === 1 
      ? 'Подтверждение удаления' 
      : `Удалить ${count} элементов?`

    const message = count === 1
      ? `Вы действительно хотите удалить "${items[0].name}"?`
      : `Вы действительно хотите удалить ${count} элементов?\nЭто действие необратимо.`

    const result = await showMessageBox({
      title,
      message,
      type: 'yesno',
      icon: 'warning'
    })

    if (result !== 'yes') return

    let successCount = 0
    let errorCount = 0

    for (const item of items) {
      try {
        await (window as any).electron?.deleteItem?.(item.path)
        successCount++
      } catch (err) {
        errorCount++
        console.error('Failed to delete', item.path, err)
      }
    }

    lastUpdate.value = Date.now()

    if (successCount > 0) {
      success(
        'Удалено', 
        count === 1 ? items[0].name : `${successCount} элемент(ов)`
      )
    }
    if (errorCount > 0) {
      error('Не удалось удалить', `${errorCount} элемент(ов)`)
    }
  }

  return { createItem, renameItem, deleteItem, deleteItems }
}