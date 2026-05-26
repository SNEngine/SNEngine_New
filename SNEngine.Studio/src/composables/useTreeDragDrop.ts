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

  // Проверка на попытку перетащить папку в саму себя или в потомка
  const isDescendant = (source: any, target: any): boolean => {
    if (!source || !target || !target.isFolder) return false
    if (source.path === target.path) return true

    let current: any = target
    while (current) {
      if (current.path === source.path) return true
      current = current.parent
    }
    return false
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
    if (draggedItems.value.length === 0 || !targetItem?.isFolder) return null

    e.preventDefault()
    e.stopImmediatePropagation()

    const payload: DragDropPayload = {
      sources: [...draggedItems.value],
      target: targetItem,
      isCopy: e.ctrlKey || isCopyMode.value
    }

    reset()
    return payload
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
