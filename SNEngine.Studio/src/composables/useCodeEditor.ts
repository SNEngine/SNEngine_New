import { ref } from 'vue'

interface EditorOptions {
  title: string
  code: string
  language?: string
  onSave?: (newCode: string) => void
}

const isOpen = ref(false)
const currentOptions = ref<EditorOptions>({
  title: '',
  code: '',
  language: 'sn',
  onSave: undefined
})

export function useCodeEditor() {
  const openEditor = (options: EditorOptions) => {
    currentOptions.value = { ...options }
    isOpen.value = true
  }

  const closeEditor = () => isOpen.value = false

  const saveAndClose = (newCode: string) => {
    currentOptions.value.onSave?.(newCode)
    closeEditor()
  }

  return {
    isOpen,
    currentOptions,
    openEditor,
    closeEditor,
    saveAndClose
  }
}