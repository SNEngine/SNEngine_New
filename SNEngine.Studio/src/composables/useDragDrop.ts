// useDragDrop.ts
import { ref } from 'vue'

export function useDragDrop() {
  const isDragOver = ref(false)

  const copyFilesFromOS = async (targetDir: string, filePaths: string[]) => {
    if (!filePaths.length || !targetDir) {
      console.warn('⚠️ Нет файлов или целевой папки')
      return false
    }

    try {
      console.log('📥 Копирование в:', targetDir, filePaths)

      const result = await (window as any).electron?.copyFiles?.(targetDir, filePaths)

      if (result?.success) {
        console.log(`✅ Успешно скопировано ${result.copied} элемент(ов)`)
        return true
      } else {
        console.error('❌ Ошибка копирования:', result)
        return false
      }
    } catch (err: any) {
      console.error('❌ Drag & Drop ошибка:', err)
      return false
    }
  }

  const handleDragOver = (e: DragEvent) => {
    e.preventDefault()
    e.stopImmediatePropagation()   // ← важно для вложенных элементов
    isDragOver.value = true

    if (e.dataTransfer) {
      e.dataTransfer.dropEffect = 'copy'
    }
  }

  const handleDragLeave = (e: DragEvent) => {
    e.preventDefault()
    e.stopImmediatePropagation()
    isDragOver.value = false
  }

  const handleDrop = async (e: DragEvent, targetDir: string, onSuccess?: () => void) => {
    e.preventDefault()
    e.stopImmediatePropagation()
    isDragOver.value = false

    const files = Array.from(e.dataTransfer?.files || [])
    if (files.length === 0) return false

    const filePaths = files.map((f: any) => 
      (window as any).electron.getFilePath(f)
    ).filter(Boolean)

    if (filePaths.length > 0) {
      const success = await copyFilesFromOS(targetDir, filePaths)
      if (success && onSuccess) {
        onSuccess()
      }
      return success
    } else {
      console.error('❌ Не удалось получить пути файлов')
      return false
    }
  }

  return {
    isDragOver,
    handleDragOver,
    handleDragLeave,
    handleDrop
  }
}