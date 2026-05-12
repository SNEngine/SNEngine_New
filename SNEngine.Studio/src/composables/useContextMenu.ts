import { ref } from 'vue'

export interface ContextMenuItem {
  label: string
  icon?: string
  action?: () => void | Promise<void>
  type?: 'separator'
  danger?: boolean
}

export function useContextMenu() {
  const contextMenuRef = ref<any>(null)

  const show = (e: MouseEvent, menuItems: ContextMenuItem[]) => {
    e.preventDefault()
    e.stopPropagation()

    if (contextMenuRef.value) {
      contextMenuRef.value.show(e.clientX, e.clientY, menuItems)
    }
  }

  const hide = () => {
    if (contextMenuRef.value) {
      contextMenuRef.value.hide()
    }
  }

  return {
    contextMenuRef,
    show,
    hide
  }
}