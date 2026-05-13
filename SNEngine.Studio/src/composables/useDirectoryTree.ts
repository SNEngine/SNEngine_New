import { ref, watch, onMounted, onUnmounted } from 'vue'
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

  // ====================== ЗАГРУЗКА ======================
  const loadDirectory = async () => {
    loading.value = true
    try {
      const result = await (window as any).electron?.readDirectory?.(basePath) || []
      
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

  // ====================== WATCHER ======================
  const startWatcher = () => {
    if (!isRoot) return
    try {
      if ((window as any).electron?.startWatcher) {
        (window as any).electron.startWatcher(basePath)
      }
    } catch (e) {
      console.warn('Не удалось запустить watcher:', e)
    }
  }

  const stopWatcher = () => {
    if (!isRoot) return
    try {
      (window as any).electron?.stopWatcher?.()
    } catch (e) {
      // ignore
    }
  }

  const handleFileChange = () => {
    if (isRoot) {
      loadDirectory()
    }
  }

  // ====================== ОПЕРАЦИИ ======================
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

    const fullPath = `${targetDir}/${name}`.replace(/\\/g, '/')

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

  const toggleOpen = (item: TreeItem) => {
    item.isOpen = !item.isOpen
  }

  // ====================== LIFECYCLE ======================
  onMounted(() => {
    loadDirectory()
    startWatcher()

    // Подписка на изменения от Electron
    ;(window as any).electron?.onFileChange?.(handleFileChange)
  })

  onUnmounted(() => {
    stopWatcher()
    ;(window as any).electron?.offFileChange?.(handleFileChange)
  })

  // Fallback обновление
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
    toggleOpen
  }
}