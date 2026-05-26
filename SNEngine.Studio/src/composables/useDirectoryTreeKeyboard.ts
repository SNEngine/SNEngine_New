// src/composables/useDirectoryTreeKeyboard.ts
import type { Ref } from 'vue'

export interface KeyboardDependencies {
  treeSelection: any
  getCurrentlySelectedItems: () => any[]
  findItemByPath: (path: string) => any | null
  renameItem: (item: any) => void
  deleteItems: (items: any[]) => void
  duplicateItem: (path: string) => void
  refresh: () => void
  items: Ref<any[]>
}

/**
 * Encapsulates all keyboard shortcut setup for DirectoryTree.
 * Keeps the main component much cleaner.
 */
export function useDirectoryTreeKeyboard(deps: KeyboardDependencies, addShortcut: (combo: string, handler: (e?: KeyboardEvent) => void) => void) {
  const {
    treeSelection,
    getCurrentlySelectedItems,
    findItemByPath,
    renameItem,
    deleteItems,
    duplicateItem,
    refresh,
    items,
  } = deps

  function setupKeyboardShortcuts() {
    addShortcut('f2', () => {
      const paths = treeSelection.getSelectedPaths?.() || []
      if (paths.length === 1) {
        const item = findItemByPath(paths[0])
        if (item) renameItem(item)
      }
    })

    addShortcut('delete', () => {
      const selected = getCurrentlySelectedItems()
      if (selected.length > 0) {
        deleteItems(selected)
      }
    })

    addShortcut('ctrl+d', () => {
      const selected = getCurrentlySelectedItems()
      selected.forEach((it: any) => duplicateItem(it.path))
    })

    addShortcut('ctrl+c', () => {
      const paths = treeSelection.getSelectedPaths?.() || []
      if (paths.length > 0) {
        navigator.clipboard.writeText(paths.join('\n'))
      }
    })

    addShortcut('ctrl+shift+c', () => {
      const selected = getCurrentlySelectedItems()
      if (selected.length > 0) {
        const names = selected.map((i: any) => i.name).join('\n')
        navigator.clipboard.writeText(names)
      }
    })

    addShortcut('f5', () => refresh())

    addShortcut('escape', () => {
      if (treeSelection.hasSelection?.value) {
        treeSelection.clearSelection()
      }
    })

    addShortcut('ctrl+a', (e) => {
      if (e) e.preventDefault()
      const allVisible: any[] = []
      const collect = (list: any[]) => {
        for (const it of list) {
          allVisible.push(it)
          if (it.isOpen && it.children) collect(it.children)
        }
      }
      collect(items.value)
      if (allVisible.length > 0) {
        treeSelection.selectPaths?.(allVisible.map((i: any) => i.path))
      }
    })
  }

  return {
    setupKeyboardShortcuts,
  }
}
