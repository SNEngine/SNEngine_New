import { onMounted, onUnmounted } from 'vue'

type KeyHandler = (e: KeyboardEvent) => void

interface Shortcut {
  handler: KeyHandler
  preventDefault: boolean
}

const shortcuts = new Map<string, Shortcut>()
let isGlobalListenerAttached = false

const normalizeCombo = (combo: string): string => {
  return combo
    .toLowerCase()
    .split('+')
    .map(k => k.trim())
    .sort()
    .join('+')
}

const handleKeyDown = (e: KeyboardEvent) => {
  // Специально для Monaco — принудительно обрабатываем Ctrl+S
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 's') {
    console.log('🎯 Ctrl+S нажат (перехвачен до Monaco)')
    
    const shortcut = shortcuts.get('ctrl+s')
    if (shortcut) {
      e.preventDefault()
      e.stopImmediatePropagation()
      shortcut.handler(e)
      return
    }
  }

  // Основная логика
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
    const normalized = normalizeCombo(combo)
    shortcuts.set(normalized, { handler, preventDefault })
    console.log(`✅ Шорткат зарегистрирован: ${normalized}`)
  }

  const remove = (combo: string) => {
    const normalized = normalizeCombo(combo)
    shortcuts.delete(normalized)
  }

  const clear = () => shortcuts.clear()

  onMounted(() => {
    if (!isGlobalListenerAttached) {
      window.addEventListener('keydown', handleKeyDown, true) // capture phase
      isGlobalListenerAttached = true
      console.log('🔊 Глобальный клавиатурный listener запущен')
    }
  })

  return { add, remove, clear }
}