import { markRaw, type Component } from 'vue'

// Импорты компонентов
import CodeEditor from '../components/CodeEditor/CodeEditor.vue'
import ImagePreview from '../components/ImagePreview/ImagePreview.vue'
import AudioPreview from '../components/AudioPreview/AudioPreview.vue'
import VideoPreview from '../components/VideoPreview/VideoPreview.vue'
import WebEditor from '../components/WebEditor/WebEditor.vue'
import UnknownFile from '../components/UnknownFile/UnknownFile.vue'

export type FileType = 'code' | 'image' | 'audio' | 'video' | 'web' | 'unknown'

export interface FileHandlerResult {
  component: Component
  type: FileType
  props: Record<string, any>
  language?: string
  icon?: string
  isEditable?: boolean   // ← Новый флаг
}

// ==================== КОНФИГУРАЦИЯ РАСШИРЕНИЙ ====================

const TEXT_EXTENSIONS = new Set([
  'sn', 'ts', 'js', 'json', 'txt', 'xml', 'cs', 'csproj',
  'log', 'md', 'markdown', 'vue', 'html', 'htm', 'css', 'scss',
  'less', 'yaml', 'yml', 'toml', 'ini', 'env', 'gitignore'
])

const IMAGE_EXTENSIONS = new Set([
  'png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp', 'ico', 'tiff'
])

const AUDIO_EXTENSIONS = new Set([
  'mp3', 'wav', 'ogg', 'm4a', 'flac', 'aac', 'wma', 'opus'
])

const VIDEO_EXTENSIONS = new Set([
  'mp4', 'webm', 'mov', 'avi', 'mkv', 'm4v', 'ogv', 'flv', 'wmv'
])

const LANGUAGE_MAP: Record<string, string> = {
  sn: 'sn',
  cs: 'csharp',
  ts: 'typescript',
  js: 'javascript',
  json: 'json',
  html: 'html',
  htm: 'html',
  css: 'css',
  scss: 'scss',
  less: 'less',
  md: 'markdown',
  markdown: 'markdown',
  vue: 'vue',
  yaml: 'yaml',
  yml: 'yaml',
  xml: 'xml',
  txt: 'plaintext',
  log: 'plaintext'
}

// ==================== ОСНОВНАЯ ФУНКЦИЯ ====================

export function useFileType() {
  
  const getFileHandler = async (filePath: string): Promise<FileHandlerResult> => {
    const name = filePath.split(/[/\\]/).pop() || filePath
    const ext = name.split('.').pop()?.toLowerCase() || ''

    // === HTML (WebEditor) ===
    if (ext === 'html' || ext === 'htm') {
      return {
        component: markRaw(WebEditor),
        type: 'web',
        props: { filePath, initialHtml: await safeReadFile(filePath) },
        language: 'html',
        icon: 'html_icon',
        isEditable: true
      }
    }

    // === Видео ===
    if (VIDEO_EXTENSIONS.has(ext)) {
      return {
        component: markRaw(VideoPreview),
        type: 'video',
        props: { videoPath: filePath },
        icon: 'video_icon',
        isEditable: false
      }
    }

    // === Аудио ===
    if (AUDIO_EXTENSIONS.has(ext)) {
      return {
        component: markRaw(AudioPreview),
        type: 'audio',
        props: { audioPath: filePath },
        icon: 'audio_icon',
        isEditable: false
      }
    }

    // === Изображения ===
    if (IMAGE_EXTENSIONS.has(ext)) {
      return {
        component: markRaw(ImagePreview),
        type: 'image',
        props: { imagePath: filePath },
        icon: 'image_icon',
        isEditable: false
      }
    }

    // === Текстовые / редактируемые файлы ===
    if (TEXT_EXTENSIONS.has(ext)) {
      const content = await safeReadFile(filePath)
      const language = LANGUAGE_MAP[ext] || 'plaintext'

      return {
        component: markRaw(CodeEditor),
        type: 'code',
        props: { 
          modelValue: content, 
          language, 
          theme: 'snengine-dark' 
        },
        language,
        icon: ext === 'sn' ? 'sn_script_icon' : 'code_icon',
        isEditable: true
      }
    }

    // === Неизвестный файл ===
    return {
      component: markRaw(UnknownFile),
      type: 'unknown',
      props: { filePath },
      icon: 'unknown_icon',
      isEditable: false
    }
  }

  const safeReadFile = async (path: string): Promise<string> => {
    try {
      return await (window as any).electron?.readFile?.(path) || ''
    } catch (error) {
      console.error('Ошибка чтения файла:', error)
      return `// Ошибка чтения: ${error}`
    }
  }

  const getFileType = (filePath: string): FileType => {
    const ext = filePath.split('.').pop()?.toLowerCase() || ''
    
    if (ext === 'html' || ext === 'htm') return 'web'
    if (VIDEO_EXTENSIONS.has(ext)) return 'video'
    if (AUDIO_EXTENSIONS.has(ext)) return 'audio'
    if (IMAGE_EXTENSIONS.has(ext)) return 'image'
    if (TEXT_EXTENSIONS.has(ext)) return 'code'
    return 'unknown'
  }

  return { 
    getFileHandler, 
    getFileType,
    TEXT_EXTENSIONS,
    IMAGE_EXTENSIONS,
    AUDIO_EXTENSIONS,
    VIDEO_EXTENSIONS
  }
}