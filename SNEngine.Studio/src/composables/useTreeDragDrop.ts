// useTreeDragDrop.ts
import { ref } from 'vue'

export interface DragDropPayload {
  source: any      // TreeItem который перетаскиваем
  target: any      // TreeItem (папка) куда дропаем
  isCopy: boolean  // Ctrl нажат = копирование
}

export function useTreeDragDrop() {
  const draggedItem = ref<any>(null)
  const dragOverItem = ref<any>(null)
  const isCopyMode = ref(false)

  // Проверяем, не пытаемся ли мы закинуть папку в саму себя или в своего потомка
  const isDescendant = (source: any, target: any): boolean => {
    if (!source || !target || !target.isFolder) return false
    if (source.path === target.path) return true

    let current: any = target
    while (current) {
      if (current.path === source.path) return true
      current = current.parent // если у тебя есть parent в TreeItem, иначе нужно рекурсивно проверять
    }
    return false
  }

  const startDrag = (item: any, e: DragEvent) => {
    draggedItem.value = item
    isCopyMode.value = false

    if (e.dataTransfer) {
      e.dataTransfer.effectAllowed = 'move'
      e.dataTransfer.setData('text/plain', item.path)
      // Можно добавить кастомный ghost-образ при необходимости
    }
  }

  const endDrag = () => {
    draggedItem.value = null
    dragOverItem.value = null
  }

  const onDragOver = (targetItem: any, e: DragEvent) => {
    if (!draggedItem.value) return
    if (draggedItem.value.path === targetItem.path) return
    if (!targetItem.isFolder) return
    if (isDescendant(draggedItem.value, targetItem)) return

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
    if (!draggedItem.value || !targetItem?.isFolder) return null

    e.preventDefault()
    e.stopImmediatePropagation()

    const payload: DragDropPayload = {
      source: draggedItem.value,
      target: targetItem,
      isCopy: e.ctrlKey || isCopyMode.value
    }

    // Сбрасываем состояние
    reset()

    return payload
  }

  const reset = () => {
    draggedItem.value = null
    dragOverItem.value = null
    isCopyMode.value = false
  }

  return {
    // Реактивные состояния
    draggedItem,
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