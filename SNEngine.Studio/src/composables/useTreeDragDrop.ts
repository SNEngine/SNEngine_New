// useTreeDragDrop.ts
import { ref, computed } from 'vue'

export interface DragDropPayload {
  sources: any[]   // Массив TreeItem (поддержка мульти)
  target: any      // TreeItem (папка) куда дропаем
  isCopy: boolean  // Ctrl нажат = копирование
}

export function useTreeDragDrop() {
  const draggedItems = ref<any[]>([])
  const dragOverItem = ref<any>(null)
  const isCopyMode = ref(false)

  const draggedCount = computed(() => draggedItems.value.length)
  const isMultiDrag = computed(() => draggedCount.value > 1)

  /**
   * Проверяет, является ли target потомком (или равен) source.
   * Использует сравнение путей — надёжнее, чем .parent (которого нет в TreeItem).
   */
  const isDescendant = (source: any, target: any): boolean => {
    if (!source?.path || !target?.path) return false
    if (source.path === target.path) return true

    // Нормализуем пути для Windows
    const src = source.path.replace(/\\/g, '/').toLowerCase()
    const tgt = target.path.replace(/\\/g, '/').toLowerCase()

    // target является потомком source, если начинается с source + /
    return tgt.startsWith(src + '/')
  }

  /**
   * Начать перетаскивание.
   * items — массив. Если передан один элемент, преобразуем в массив.
   */
  const startDrag = (items: any | any[], e: DragEvent) => {
    const list = Array.isArray(items) ? items : [items]
    draggedItems.value = list
    isCopyMode.value = false

    if (e.dataTransfer) {
      e.dataTransfer.effectAllowed = 'move'

      // Передаём все пути через dataTransfer (для fallback)
      const paths = list.map(i => i.path).join('\n')
      e.dataTransfer.setData('text/plain', paths)
      e.dataTransfer.setData('application/x-snengine-paths', JSON.stringify(list.map(i => i.path)))
    }
  }

  const endDrag = () => {
    draggedItems.value = []
    dragOverItem.value = null
  }

  const onDragOver = (targetItem: any, e: DragEvent) => {
    if (draggedItems.value.length === 0) return

    // Не даём дропать на один из перетаскиваемых элементов
    const isOverDragged = draggedItems.value.some(i => i.path === targetItem.path)
    if (isOverDragged) return

    if (!targetItem.isFolder) return

    // Проверяем, чтобы ни одна из перетаскиваемых папок не была предком цели
    const hasInvalid = draggedItems.value.some(source => isDescendant(source, targetItem))
    if (hasInvalid) return

    e.preventDefault()
    e.dataTransfer!.dropEffect = e.ctrlKey ? 'copy' : 'move'

    dragOverItem.value = targetItem
    isCopyMode.value = e.ctrlKey
  }

  const onDragLeave = (targetItem: any) => {
    if (dragOverItem.value?.path === targetItem.path) {
      dragOverItem.value = null
    }
  }

  const onDrop = (targetItem: any, e: DragEvent): DragDropPayload | null => {
    console.log('[DragDebug] onDrop called', {
      targetPath: targetItem?.path,
      draggedItemsCount: draggedItems.value.length,
      ctrlKey: e.ctrlKey
    });

    if (!targetItem?.isFolder) {
      console.log('[DragDebug] onDrop rejected: target is not folder');
      return null;
    }

    let sources = draggedItems.value;

    // Fallback: if in-memory draggedItems was cleared
    if (sources.length === 0) {
      console.log('[DragDebug] onDrop: draggedItems empty, trying dataTransfer fallback');
      try {
        const json = e.dataTransfer?.getData('application/x-snengine-paths');
        if (json) {
          const paths: string[] = JSON.parse(json);
          sources = paths.map(p => ({ path: p }));
          console.log('[DragDebug] onDrop: reconstructed sources from dataTransfer', paths);
        }
      } catch (err) {
        console.warn('[DragDebug] Failed to reconstruct from dataTransfer', err);
      }
    }

    if (sources.length === 0) {
      console.log('[DragDebug] onDrop rejected: no sources');
      return null;
    }

    e.preventDefault();
    e.stopImmediatePropagation();

    const payload: DragDropPayload = {
      sources: [...sources],
      target: targetItem,
      isCopy: e.ctrlKey || isCopyMode.value
    };

    console.log('[DragDebug] onDrop SUCCESS, returning payload', {
      sources: payload.sources.map(s => s.path),
      target: payload.target.path,
      isCopy: payload.isCopy
    });

    reset();
    return payload;
  }

  const reset = () => {
    draggedItems.value = []
    dragOverItem.value = null
    isCopyMode.value = false
  }

  return {
    // Реактивные состояния
    draggedItems,
    draggedCount,
    isMultiDrag,
    dragOverItem,
    isCopyMode,

    // Методы
    startDrag,
    endDrag,
    onDragOver,
    onDragLeave,
    onDrop,
    reset,
    isDescendant
  }
}
