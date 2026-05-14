// useDragDrop.ts
import { ref } from 'vue'

export function useDragDrop() {
  const isDragOver = ref(false)

  /**
   * Вызывает IPC метод для копирования файлов через главный процесс Electron
   */
  const copyFilesFromOS = async (targetDir: string, filePaths: string[]) => {
    if (!filePaths.length || !targetDir) {
      console.warn('⚠️ Нет файлов или целевой папки')
      return false
    }

    try {
      console.log('📥 Начинаем копирование:', { targetDir, filePaths })

      // Обращение к объекту, экспонированному в preload.cjs
      const result = await (window as any).electron?.copyFiles?.(targetDir, filePaths)

      console.log('📦 Результат copy-files:', result)

      if (result?.success) {
        console.log(`✅ Успешно скопировано ${result.copied} файл(ов)`)
        return true
      } else {
        console.error('❌ Ошибка копирования:', result?.error || result)
        return false
      }
    } catch (err: any) {
      console.error('❌ Критическая ошибка Drag & Drop:', err)
      return false
    }
  }

  const handleDragOver = (e: DragEvent) => {
    e.preventDefault()
    e.stopPropagation() // Предотвращаем всплытие события
    isDragOver.value = true
    
    // Сообщаем операционной системе, что мы готовы копировать файлы
    if (e.dataTransfer) {
      e.dataTransfer.dropEffect = 'copy'
    }
  }

  const handleDragLeave = (e: DragEvent) => {
    e.preventDefault()
    e.stopPropagation()
    isDragOver.value = false
  }

  const handleDrop = async (e: DragEvent, targetDir: string, onSuccess?: () => void) => {
    e.preventDefault()
    e.stopPropagation()
    isDragOver.value = false

    // Получаем файлы из объекта DataTransfer
    const files = Array.from(e.dataTransfer?.files || [])
    console.log('🔍 Файлы в событии drop:', files)

    if (files.length === 0) return false

    // Извлекаем пути. 
    // ВАЖНО: f.path будет доступен только если в BrowserWindow выставлено sandbox: false
const filePaths = files.map((f: any) => {
  // Используем новый метод из preload
  return (window as any).electron.getFilePath(f)
}).filter(Boolean)

    if (filePaths.length > 0) {
      const success = await copyFilesFromOS(targetDir, filePaths)
      if (success && onSuccess) {
        onSuccess() // Обычно это вызов refresh() для обновления дерева файлов
      }
      return success
    } else {
      console.error('❌ Не удалось получить пути к файлам. Проверьте sandbox: false в electron.cjs')
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