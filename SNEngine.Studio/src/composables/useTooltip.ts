// src/composables/useTooltip.ts
import { ref, onUnmounted } from 'vue'

export type TooltipPosition = 'top' | 'bottom' | 'left' | 'right' | 'auto'

export interface TooltipOptions {
  text: string
  position?: TooltipPosition
  delay?: number
  hideDelay?: number
  maxWidth?: number
  offset?: number
}

export function useTooltip(options: TooltipOptions) {
  const isVisible = ref(false)
  const tooltipText = ref(options.text)
  const coords = ref({ x: 0, y: 0 })
  const timeout = ref<number | null>(null)

  const show = (e: MouseEvent) => {
    const target = e.currentTarget as HTMLElement
    const rect = target.getBoundingClientRect()
    const offset = options.offset ?? 12

    let x = rect.left
    let y = rect.bottom + offset

    // Для позиции bottom — строго снизу по центру
    if (options.position === 'bottom') {
      x = rect.left + rect.width / 2
      y = rect.bottom + offset
    }

    coords.value = { x, y }

    if (timeout.value) clearTimeout(timeout.value)
    timeout.value = window.setTimeout(() => {
      isVisible.value = true
    }, options.delay ?? 450)
  }

  const hide = () => {
    if (timeout.value) {
      clearTimeout(timeout.value)
      timeout.value = null
    }
    setTimeout(() => {
      isVisible.value = false
    }, options.hideDelay ?? 200)
  }

  onUnmounted(() => {
    if (timeout.value) clearTimeout(timeout.value)
  })

  return {
    isVisible,
    tooltipText,
    coords,
    show,
    hide
  }
}