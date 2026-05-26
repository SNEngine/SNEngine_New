// src/composables/useTreeSelection.ts
import { ref, computed } from 'vue'

export interface SelectableTreeItem {
  path: string
  isFolder: boolean
  // We only need path for selection tracking
}

export function useTreeSelection() {
  // Using Set of paths for O(1) lookup and easy dedup
  const selectedPaths = ref<Set<string>>(new Set())
  const anchorPath = ref<string | null>(null) // for future Shift+click range

  const selectedCount = computed(() => selectedPaths.value.size)
  const hasSelection = computed(() => selectedCount.value > 0)

  /** Check if a specific path is currently selected */
  function isSelected(path: string): boolean {
    return selectedPaths.value.has(path)
  }

  /** Clear all selection */
  function clearSelection() {
    selectedPaths.value.clear()
    anchorPath.value = null
  }

  /** Select only this one item (standard click) */
  function selectSingle(item: SelectableTreeItem) {
    selectedPaths.value.clear()
    selectedPaths.value.add(item.path)
    anchorPath.value = item.path
  }

  /** Toggle selection of one item (Ctrl / Cmd + click) */
  function toggleSelection(item: SelectableTreeItem) {
    if (selectedPaths.value.has(item.path)) {
      selectedPaths.value.delete(item.path)
      if (anchorPath.value === item.path) {
        anchorPath.value = null
      }
    } else {
      selectedPaths.value.add(item.path)
      anchorPath.value = item.path
    }
  }

  /**
   * Handle click with modifier keys support.
   * Accepts either a MouseEvent (legacy) or a modifiers object { ctrlKey, shiftKey, ... }
   */
  function handleItemClick(
    item: SelectableTreeItem,
    eventOrModifiers: any
  ): 'single' | 'toggle' | 'range' {
    const isCtrlOrCmd = eventOrModifiers?.ctrlKey || eventOrModifiers?.metaKey
    const isShift = eventOrModifiers?.shiftKey

    if (isShift && anchorPath.value) {
      return 'range'
    }

    if (isCtrlOrCmd) {
      toggleSelection(item)
      return 'toggle'
    }

    selectSingle(item)
    return 'single'
  }

  /**
   * Select multiple paths at once (used after bulk operations or range)
   */
  function selectPaths(paths: string[]) {
    selectedPaths.value.clear()
    for (const p of paths) {
      selectedPaths.value.add(p)
    }
    if (paths.length > 0) {
      anchorPath.value = paths[paths.length - 1]
    }
  }

  /**
   * Range selection (Shift + Click)
   * visibleItems must be a flat, ordered array of currently visible items
   * (respecting open folders and search filter).
   */
  function selectRange(targetItem: SelectableTreeItem, visibleItems: SelectableTreeItem[]) {
    if (!anchorPath.value) {
      selectSingle(targetItem)
      return
    }

    const startIndex = visibleItems.findIndex(i => i.path === anchorPath.value)
    const endIndex = visibleItems.findIndex(i => i.path === targetItem.path)

    if (startIndex === -1 || endIndex === -1) {
      // Fallback if we can't find them in the visible list
      selectSingle(targetItem)
      return
    }

    const [from, to] = startIndex < endIndex 
      ? [startIndex, endIndex] 
      : [endIndex, startIndex]

    // Range selection usually replaces the current selection
    selectedPaths.value.clear()

    for (let i = from; i <= to; i++) {
      const item = visibleItems[i]
      if (item) {
        selectedPaths.value.add(item.path)
      }
    }

    // Update anchor to the clicked item (common behavior)
    anchorPath.value = targetItem.path
  }

  /**
   * Get currently selected paths as array (convenience)
   */
  function getSelectedPaths(): string[] {
    return Array.from(selectedPaths.value)
  }

  /**
   * Remove paths that no longer exist (called after delete / refresh)
   */
  function removeNonExistentPaths(existingPaths: Set<string>) {
    const toDelete: string[] = []
    for (const p of selectedPaths.value) {
      if (!existingPaths.has(p)) toDelete.push(p)
    }
    toDelete.forEach(p => selectedPaths.value.delete(p))
  }

  return {
    // State
    selectedPaths,
    anchorPath,
    selectedCount,
    hasSelection,

    // Queries
    isSelected,

    // Mutations
    clearSelection,
    selectSingle,
    toggleSelection,
    selectPaths,
    handleItemClick,
    removeNonExistentPaths,
    getSelectedPaths,
    selectRange
  }
}
