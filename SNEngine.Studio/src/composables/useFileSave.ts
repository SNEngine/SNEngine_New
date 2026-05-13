import { ref } from 'vue'

export interface SaveResult {
  success: boolean
  error?: string
}

export function useFileSave() {
  const isSaving = ref(false)
  const lastSaved = ref<Date | null>(null)

  const saveFile = async (filePath: string, content: string): Promise<SaveResult> => {
    if (!filePath) return { success: false, error: 'Пустой путь' }

    isSaving.value = true
    try {
      const result = await (window as any).electron?.writeFile?.(filePath, content)
      if (result?.success) {
        lastSaved.value = new Date()
        console.log(`✅ Сохранено: ${filePath} (${content.length} символов)`)
        return { success: true }
      }
      throw new Error(result?.error || 'Неизвестная ошибка')
    } catch (error: any) {
      console.error('💥 Ошибка сохранения:', error)
      return { success: false, error: error.message }
    } finally {
      isSaving.value = false
    }
  }

  const getContentFromEditor = (editorRef: any, fallback = ''): string => {
    if (!editorRef) {
      console.warn('getContentFromEditor: editorRef === null')
      return fallback
    }

    console.log('🔍 getContentFromEditor — проверяем ref:', Object.keys(editorRef))

    // Monaco — основные пути
    if (editorRef.editorRef?.value?.getValue) {
      console.log('✅ Monaco: editorRef.value.getValue')
      return editorRef.editorRef.value.getValue()
    }
    if (editorRef.getValue && typeof editorRef.getValue === 'function') {
      console.log('✅ Monaco: direct getValue')
      return editorRef.getValue()
    }
    if (editorRef.editorRef?.getValue && typeof editorRef.editorRef.getValue === 'function') {
      console.log('✅ Monaco: editorRef.getValue')
      return editorRef.editorRef.getValue()
    }

    // Другие редакторы
    if (editorRef.internalCode?.value !== undefined) {
      console.log('✅ internalCode.value')
      return editorRef.internalCode.value
    }
    if (editorRef.htmlCode?.value !== undefined) {
      console.log('✅ htmlCode.value')
      return editorRef.htmlCode.value
    }
    if (editorRef.modelValue !== undefined) {
      console.log('✅ modelValue')
      return editorRef.modelValue
    }

    console.warn('⚠️ Не удалось извлечь содержимое — используем fallback')
    return fallback
  }

  return {
    isSaving,
    lastSaved,
    saveFile,
    getContentFromEditor
  }
}