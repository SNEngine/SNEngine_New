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

  const normalizePath = (p: string) => p.replace(/\\/g, '/')
  const openFolders = new Set<string>()

  // Сохраняем текущее состояние открытых папок перед обновлением
  const saveOpenState = () => {
    const collect = (list: TreeItem[]) => {
      for (const item of list) {
        if (item.isFolder && item.isOpen) {
          openFolders.add(normalizePath(item.path))
        }
        if (item.children) collect(item.children)
      }
    }
    collect(items.value)
  }

  // Рекурсивная загрузка папки и всех её открытых подпапок
  const fetchDirectoryRecursive = async (path: string): Promise<TreeItem[]> => {
    try {
      const result = await (window as any).electron?.readDirectory?.(path) || []
      
      const loadedItems = await Promise.all(result.map(async (item: any) => {
        const normPath = normalizePath(item.path)
        const isOpen = openFolders.has(normPath)
        
        const treeItem: TreeItem = {
          ...item,
          isOpen: isOpen,
          children: undefined
        }

        // Если папка должна быть открыта, загружаем её детей немедленно
        if (item.isFolder && isOpen) {
          treeItem.children = await fetchDirectoryRecursive(item.path)
        }

        return treeItem
      }))

      return loadedItems
    } catch (err) {
      console.error(`Error loading directory ${path}:`, err)
      return []
    }
  }

  const loadDirectory = async () => {
    loading.value = true
    try {
      // Запускаем рекурсивное восстановление всей структуры от корня
      items.value = await fetchDirectoryRecursive(basePath)
    } finally {
      loading.value = false
    }
  }

  // ====================== TOGGLE ======================
  const toggleOpen = async (item: TreeItem) => {
    if (!item.isFolder) return

    if (!item.isOpen) {
      // Загружаем детей только если их еще нет
      if (!item.children || item.children.length === 0) {
        item.isLoadingChildren = true
        try {
          item.children = await fetchDirectoryRecursive(item.path)
        } finally {
          item.isLoadingChildren = false
        }
      }
    }

    item.isOpen = !item.isOpen

    const normPath = normalizePath(item.path)
    if (item.isOpen) openFolders.add(normPath)
    else openFolders.delete(normPath)
  }

  // ====================== WATCHER ======================
  const startWatcher = () => {
    if (!isRoot) return
    (window as any).electron?.startWatcher?.(basePath)
  }

  const stopWatcher = () => {
    if (!isRoot) return
    (window as any).electron?.stopWatcher?.()
  }

  const handleFileChange = () => {
    if (isRoot) {
      saveOpenState()
      loadDirectory()
    }
  }

  // ====================== LIFECYCLE ======================
  onMounted(() => {
    loadDirectory()
    startWatcher()
    ;(window as any).electron?.onFileChange?.(handleFileChange)
  })

  onUnmounted(() => {
    stopWatcher()
    ;(window as any).electron?.offFileChange?.(handleFileChange)
  })

  watch(lastUpdate, () => {
    if (isRoot) {
      saveOpenState()
      loadDirectory()
    }
  })

  return {
    items,
    loading,
    selectedItem,
    loadDirectory,
    toggleOpen
  }
}