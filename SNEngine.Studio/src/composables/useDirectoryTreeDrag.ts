// src/composables/useDirectoryTreeDrag.ts
import type { Ref } from 'vue'

export interface DragPayload {
  sources?: any[]
  target?: any
  isCopy?: boolean
}

/**
 * Handles root-level and internal drag & drop logic for DirectoryTree.
 */
export function useDirectoryTreeDrag(
  props: { basePath: string },
  treeDrag: any,
  dragDrop: any, // from useDragDrop
  handleInternalDropFn: (payload: any) => void | Promise<void>,
  refreshFn: () => void
) {
  const { isDragOver, handleDragOver, handleDragLeave, handleDrop } = dragDrop

  function handleRootDragOver(e: DragEvent) {
    if (treeDrag.draggedItems?.value?.length > 0) {
      e.preventDefault()
      e.dataTransfer!.dropEffect = e.ctrlKey ? 'copy' : 'move'
    } else {
      handleDragOver(e)
    }
  }

  function handleRootDragLeave(e: DragEvent) {
    handleDragLeave(e)
  }

  function handleRootDrop(e: DragEvent) {
    if (treeDrag.draggedItems?.value?.length > 0) {
      const payload = treeDrag.onDrop({ path: props.basePath, isFolder: true }, e)
      if (payload) handleInternalDropFn(payload)
      return
    }
    handleDrop(e, props.basePath, refreshFn)
  }

  // Note: handleInternalDrop is kept in the main component for now
  // because it has strong coupling with selection + file utils.
  // We expose a thin wrapper if needed in the future.

  return {
    isDragOver,
    handleDragOver,
    handleDragLeave,
    handleDrop,

    handleRootDragOver,
    handleRootDragLeave,
    handleRootDrop,
  }
}
