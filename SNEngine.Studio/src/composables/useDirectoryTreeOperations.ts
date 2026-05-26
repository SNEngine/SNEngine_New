// src/composables/useDirectoryTreeOperations.ts
import type { Ref } from 'vue'

export interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen?: boolean
  children?: TreeItem[]
}

/**
 * Provides common operations and helpers for DirectoryTree.
 * This helps keep DirectoryTree.vue much slimmer.
 */
export function useDirectoryTreeOperations(
  items: Ref<TreeItem[]>,
  treeSelection: any, // from useTreeSelection
  finalItems: Ref<TreeItem[]>
) {
  /**
   * Returns a flat, ordered list of all currently visible items.
   * Respects opened folders and search filter.
   */
  function getVisibleItemsFlat(): TreeItem[] {
    const result: TreeItem[] = []

    const traverse = (list: TreeItem[]) => {
      for (const item of list) {
        result.push(item)
        if (item.isFolder && item.isOpen && item.children?.length) {
          traverse(item.children)
        }
      }
    }

    traverse(finalItems.value)
    return result
  }

  /**
   * Gets all currently selected TreeItem objects (recursively).
   */
  function getCurrentlySelectedItems(): TreeItem[] {
    const paths = treeSelection.getSelectedPaths?.() || []
    const result: TreeItem[] = []

    const find = (list: TreeItem[]) => {
      for (const it of list) {
        if (paths.includes(it.path)) result.push(it)
        if (it.children) find(it.children)
      }
    }

    find(items.value)
    return result
  }

  /**
   * Finds a TreeItem by path anywhere in the tree.
   */
  function findItemByPath(path: string): TreeItem | null {
    let found: TreeItem | null = null

    const search = (list: TreeItem[]) => {
      for (const it of list) {
        if (it.path === path) {
          found = it
          return
        }
        if (it.children) search(it.children)
      }
    }

    search(items.value)
    return found
  }

  /**
   * Handles selection (single, toggle, range) coming from TreeNode.
   * Designed to be passed as onSelect / @select handler.
   */
  function handleSelect(payload: any, legacyEvent?: MouseEvent) {
    let item: any
    let modifiers: any = {}

    if (payload && typeof payload === 'object' && 'item' in payload) {
      item = payload.item
      modifiers = payload.modifiers || {}
    } else {
      item = payload
      if (legacyEvent) {
        modifiers = {
          ctrlKey: legacyEvent.ctrlKey || legacyEvent.metaKey,
          shiftKey: legacyEvent.shiftKey,
          metaKey: legacyEvent.metaKey,
        }
      }
    }

    if (!item) return

    const isCtrlOrCmd = modifiers.ctrlKey || modifiers.metaKey
    const isShift = modifiers.shiftKey

    if (isShift && treeSelection.anchorPath?.value) {
      const visible = getVisibleItemsFlat()
      treeSelection.selectRange?.(item, visible)
    } else if (isCtrlOrCmd) {
      treeSelection.toggleSelection?.(item)
    } else {
      treeSelection.selectSingle?.(item)
    }
  }

  return {
    getVisibleItemsFlat,
    getCurrentlySelectedItems,
    findItemByPath,
    handleSelect,
  }
}
