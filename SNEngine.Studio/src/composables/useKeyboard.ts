import { onMounted, onUnmounted } from 'vue'

type KeyHandler = (e: KeyboardEvent) => void

interface Shortcut {
  handler: KeyHandler
  preventDefault: boolean
}

const shortcuts = new Map<string, Shortcut>()
let isGlobalListenerAttached = false

const handleKeyDown = (e: KeyboardEvent) => {
  // Специальная обработка Ctrl+S / Cmd+S — работает на ЛЮБОЙ раскладке
  if ((e.ctrlKey || e.metaKey) && e.code === 'KeyS') {
    console.log('🎯 Ctrl+S (или Cmd+S) нажат — layout-independent')
    const shortcut = shortcuts.get('ctrl+s')
    if (shortcut) {
      e.preventDefault()
      e.stopImmediatePropagation()
      shortcut.handler(e)
      return
    }
  }

  // Основная логика для остальных шорткатов
  const keys: string[] = []
  if (e.ctrlKey || e.metaKey) keys.push('ctrl')
  if (e.altKey) keys.push('alt')
  if (e.shiftKey) keys.push('shift')

  const key = e.key.toLowerCase()
  if (!['control', 'alt', 'shift', 'meta'].includes(key)) {
    keys.push(key === ' ' ? 'space' : key)
  }

  const comboStr = keys.sort().join('+')
  const shortcut = shortcuts.get(comboStr)

  if (shortcut) {
    if (shortcut.preventDefault) {
      e.preventDefault()
      e.stopImmediatePropagation()
    }
    shortcut.handler(e)
  }
}

export function useKeyboard() {
  const add = (combo: string, handler: KeyHandler, preventDefault = true) => {
    const normalized = combo.toLowerCase().trim()
    shortcuts.set(normalized, { handler, preventDefault })
    console.log(`✅ Шорткат зарегистрирован: ${normalized}`)
  }

  const remove = (combo: string) => {
    const normalized = combo.toLowerCase().trim()
    shortcuts.delete(normalized)
  }

  const clear = () => shortcuts.clear()

  onMounted(() => {
    if (!isGlobalListenerAttached) {
      window.addEventListener('keydown', handleKeyDown, true)
      isGlobalListenerAttached = true
      console.log('🔊 Глобальный keyboard listener запущен')
    }
  })

  onUnmounted(() => {
    // Не удаляем глобальный listener — он один на всё приложение
  })

  return { add, remove, clear }
}