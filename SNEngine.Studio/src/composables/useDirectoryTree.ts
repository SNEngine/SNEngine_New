import { ref, watch } from 'vue'
import { lastUpdate } from '@/utils/watcherState'
import { useMessageBox } from './useMessageBox'
import { useInputBox } from './useInputBox'

export interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen: boolean
}

export function useDirectoryTree(basePath: string, isRoot = true) {
  const items = ref<TreeItem[]>([])
  const loading = ref(true)
  const selectedItem = ref<TreeItem | null>(null)

  const { showMessageBox } = useMessageBox()
  const { showInputBox } = useInputBox()

  // Загрузка директории
  const loadDirectory = async () => {
    loading.value = true
    try {
      const result = await (window as any).electron.readDirectory(basePath)
      
      items.value = result.map((item: any) => ({
        ...item,
        isOpen: false
      })).sort((a: any, b: any) => {
        if (a.isFolder && !b.isFolder) return -1
        if (!a.isFolder && b.isFolder) return 1
        return a.name.localeCompare(b.name)
      })
    } catch (err) {
      console.error('Directory read error:', err)
    } finally {
      loading.value = false
    }
  }

  // Создать файл/папку
  const createItem = async (isFolder: boolean) => {
    let targetDir = basePath
    if (selectedItem.value?.isFolder) {
      targetDir = selectedItem.value.path
    }

    const name = await showInputBox({
      title: isFolder ? 'Новая папка' : 'Новый файл',
      message: `Создание в: ${targetDir.split(/[\\/]/).pop() || 'Корень'}`,
      placeholder: isFolder ? 'Имя папки' : 'new_file.sn'
    })

    if (!name) return

    const fullPath = `${targetDir}/${name}`

    try {
      if (isFolder) {
        await (window as any).electron.createDirectory(fullPath)
      } else {
        await (window as any).electron.createFile(fullPath)
      }
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ 
        title: 'Ошибка', 
        message: 'Не удалось создать объект', 
        icon: 'error' 
      })
    }
  }

  // Переименовать
  const renameItem = async () => {
    if (!selectedItem.value) return

    const newName = await showInputBox({
      title: 'Переименование',
      message: `Введите новое имя для ${selectedItem.value.name}:`,
      value: selectedItem.value.name
    })

    if (!newName || newName === selectedItem.value.name) return

    try {
      await (window as any).electron.renameItem(selectedItem.value.path, newName)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ 
        title: 'Ошибка', 
        message: 'Ошибка переименования', 
        icon: 'error' 
      })
    }
  }

  // Удалить
  const deleteItem = async () => {
    if (!selectedItem.value) return

    const result = await showMessageBox({
      title: 'Удаление',
      message: `Вы действительно хотите удалить ${selectedItem.value.name}?`,
      type: 'yesno',
      icon: 'warning'
    })

    if (result !== 'yes') return

    try {
      await (window as any).electron.deleteItem(selectedItem.value.path)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ 
        title: 'Ошибка', 
        message: 'Не удалось удалить', 
        icon: 'error' 
      })
    }
  }

  // Watcher для автоматического обновления
  watch(lastUpdate, () => {
    if (isRoot) loadDirectory()
  })

  return {
    items,
    loading,
    selectedItem,
    loadDirectory,
    createItem,
    renameItem,
    deleteItem,
    toggleOpen: (item: TreeItem) => {
      item.isOpen = !item.isOpen
    }
  }
}