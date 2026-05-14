import { ref, watch, onMounted, onUnmounted } from 'vue'
import { lastUpdate } from '@/utils/watcherState'

export interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen: boolean
  children?: TreeItem[]
  isLoadingChildren?: boolean
}

export function useDirectoryTree(basePath: string, isRoot = true) {
  const items = ref<TreeItem[]>([])
  const loading = ref(true)
  const selectedItem = ref<TreeItem | null>(null)

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
      if ((window as any).electron?.stopWatcher) {
        (window as any).electron.stopWatcher()
      }
    } catch (e) {
      // ignore
    }
  }

  const handleFileChange = () => {
    if (isRoot) {
      loadDirectory()
    }
  }

  // ====================== УПРАВЛЕНИЕ ПАПКАМИ ======================
  const toggleOpen = async (item: TreeItem) => {
    if (!item.isFolder) return

    if (!item.isOpen) {
      // Открываем папку
      if (!item.children || item.children.length === 0) {
        // Загружаем детей только один раз
        item.isLoadingChildren = true
        try {
          const result = await (window as any).electron?.readDirectory?.(item.path) || []
          item.children = result.map((child: any) => ({
            ...child,
            isOpen: false,
            children: undefined
          })).sort((a: any, b: any) => {
            if (a.isFolder && !b.isFolder) return -1
            if (!a.isFolder && b.isFolder) return 1
            return a.name.localeCompare(b.name)
          })
        } catch (err) {
          console.error('Failed to load children:', err)
        } finally {
          item.isLoadingChildren = false
        }
      }
    }
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

  // Fallback обновление при внешних изменениях
  watch(lastUpdate, () => {
    if (isRoot) loadDirectory()
  })

  return {
    items,
    loading,
    selectedItem,
    loadDirectory,
    toggleOpen
  }
}