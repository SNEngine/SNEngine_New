// src/composables/useWebEditor.ts
import { ref, computed } from 'vue'
import { useFileSave } from './useFileSave'
import { useNotification } from './useNotification'

export function useWebEditor(initialHtml: string = '', filePath: string = '') {
  const { saveFile } = useFileSave()
  const { success, error } = useNotification()

  const htmlCode = ref('')
  const cssCode = ref('')
  const jsCode = ref('') // Переименовано из csharpCode

  const fileName = computed(() => 
    filePath.split(/[/\\\\]/).pop() || 'Untitled.html'
  )

  // Парсинг начального HTML
  const parseInitial = (content: string) => {
    if (!content) return

    // Извлекаем CSS
    const styleMatch = content.match(/<style[^>]*>([\s\S]*?)<\/style>/i)
    cssCode.value = styleMatch ? styleMatch[1].trim() : ''

    // Извлекаем JS (ищем стандартный тег script без типа или с типом javascript)
    // Исключаем скрипты с внешними src, берем только внутреннее содержимое
    const jsMatch = content.match(/<script(?![^>]*src)[^>]*>([\s\S]*?)<\/script>/i)
    jsCode.value = jsMatch ? jsMatch[1].trim() : ''

    // Очищаем основной HTML от служебных тегов для редактора
    htmlCode.value = content
      .replace(/<!DOCTYPE[\s\S]*?>/gi, '')
      .replace(/<html[^>]*>|<\/html>|<head[^>]*>[\s\S]*?<\/head>|<body[^>]*>|<\/body>/gi, '')
      .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
      .replace(/<script[^>]*>[\s\S]*?<\/script>/gi, '')
      .trim()
  }

  // Сборка полного HTML
  const buildFullHtml = (): string => {
    let result = `<!DOCTYPE html>\n<html lang="ru">\n<head>\n  <meta charset="UTF-8">\n`
    
    if (cssCode.value.trim()) {
      result += `  <style>\n${cssCode.value}\n  </style>\n`
    }
    
    result += `</head>\n<body>\n`
    
    if (htmlCode.value.trim()) {
      result += `${htmlCode.value}\n`
    }
    
    if (jsCode.value.trim()) {
      // Сохраняем как стандартный JavaScript
      result += `  <script>\n${jsCode.value}\n  </script>\n`
    }
    
    result += `</body>\n</html>`
    return result
  }

  // Сохранение файла
  const saveToDisk = async () => {
    if (!filePath) {
      error('Не удалось сохранить', 'Путь к файлу не указан')
      return false
    }

    const fullHtml = buildFullHtml()
    try {
      const result = await saveFile(filePath, fullHtml)
      if (result) {
        success('Сохранено', `Файл ${fileName.value} успешно обновлен`)
        return true
      }
    } catch (e) {
      error('Ошибка записи', 'Не удалось записать файл на диск')
    }
    return false
  }

  // Инициализация при создании
  if (initialHtml) {
    parseInitial(initialHtml)
  }

  return {
    htmlCode,
    cssCode,
    jsCode, // Возвращаем jsCode вместо csharpCode
    fileName,
    parseInitial,
    buildFullHtml,
    saveToDisk
  }
}