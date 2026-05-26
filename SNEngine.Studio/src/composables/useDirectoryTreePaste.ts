// src/composables/useDirectoryTreePaste.ts

export interface PasteDependencies {
  getCurrentlySelectedItems: () => any[]
  copyItem: (source: string, target: string) => Promise<boolean>
  refresh: () => void
  basePath: string
}

/**
 * Handles Ctrl+V paste behavior for DirectoryTree (both OS files and internal paths).
 */
export function useDirectoryTreePaste(deps: PasteDependencies) {
  const { getCurrentlySelectedItems, copyItem, refresh, basePath } = deps

  async function handlePasteFromOS(files: File[]) {
    if (files.length === 0) return false

    const lastSelected = getCurrentlySelectedItems().find((i: any) => i.isFolder)
    const targetPath = lastSelected?.path || basePath

    // Note: Actual file path extraction from File objects requires electron bridge.
    // This is left as a thin wrapper for now.
    console.warn('OS file paste via clipboardData.files is handled in main component for electron path resolution.')
    return false
  }

  async function handleInternalPathPaste(textPath: string) {
    if (!textPath || !textPath.startsWith(basePath)) return false

    const lastSelected = getCurrentlySelectedItems().find((i: any) => i.isFolder)
    const targetPath = lastSelected?.path || basePath

    const success = await copyItem(textPath, targetPath)
    if (success) refresh()
    return success
  }

  return {
    handlePasteFromOS,
    handleInternalPathPaste,
  }
}
