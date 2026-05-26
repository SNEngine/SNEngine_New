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
    const dragged = treeDrag.draggedItems?.value || []

    if (dragged.length > 0) {
      const normalizedRootPath = props.basePath.replace(/\\/g, '/')
      const rootTarget = { path: normalizedRootPath, isFolder: true }

      if (treeDrag.onDragOver) {
        treeDrag.onDragOver(rootTarget, e)
      } else {
        e.preventDefault()
        e.dataTransfer!.dropEffect = e.ctrlKey ? 'copy' : 'move'
      }
      return
    }

    handleDragOver(e)
  }

  function handleRootDragLeave(e: DragEvent) {
    handleDragLeave(e)
  }

  function handleRootDrop(e: DragEvent) {
    console.log('[DragDebug] handleRootDrop called (from useDirectoryTreeDrag)');

    const dragged = treeDrag.draggedItems?.value || []

    if (dragged.length > 0) {
      console.log('[DragDebug] handleRootDrop: internal drag detected');
      const normalizedRootPath = props.basePath.replace(/\\/g, '/')
      const rootTarget = { path: normalizedRootPath, isFolder: true }

      const payload = treeDrag.onDrop(rootTarget, e)

      if (payload) {
        payload.target = { ...payload.target, path: normalizedRootPath }
        console.log('[DragDebug] handleRootDrop: got payload, forwarding to handleInternalDropFn');
        handleInternalDropFn(payload)
      } else {
        console.log('[DragDebug] handleRootDrop: onDrop returned null - drop will be ignored');
      }
      return
    }

    console.log('[DragDebug] handleRootDrop: external OS drop');
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
