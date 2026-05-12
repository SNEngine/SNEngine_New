import { markRaw } from 'vue'

// Правильные пути относительно composables/
import CodeEditor from '../components/CodeEditor/CodeEditor.vue'
import ImagePreview from '../components/ImagePreview/ImagePreview.vue'
import AudioPreview from '../components/AudioPreview/AudioPreview.vue'
import WebEditor from '../components/WebEditor/WebEditor.vue'
import UnknownFile from '../components/UnknownFile/UnknownFile.vue'

export type FileType = 'code' | 'image' | 'audio' | 'web' | 'unknown'

export interface FileHandlerResult {
  component: any
  type: FileType
  props: Record<string, any>
  language?: string
}

export function useFileType() {
  const getFileHandler = async (filePath: string): Promise<FileHandlerResult> => {
    const name = filePath.split(/[/\\]/).pop() || filePath
    const ext = name.split('.').pop()?.toLowerCase() || ''

    const textExts = ['sn', 'ts', 'js', 'json', 'txt', 'xml', 'cs', 'csproj', 'log']
    const imgExts = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg', 'bmp']
    const audioExts = ['mp3', 'wav', 'ogg', 'm4a', 'flac', 'aac']

    if (ext === 'html') {
      return {
        component: markRaw(WebEditor),
        type: 'web',
        props: { filePath, initialHtml: await readFile(filePath) }
      }
    }

    if (textExts.includes(ext)) {
      const content = await readFile(filePath)
      const language = ext === 'sn' ? 'sn' 
                     : ext === 'cs' ? 'csharp' 
                     : ext === 'ts' ? 'typescript' : 'plaintext'

      return {
        component: markRaw(CodeEditor),
        type: 'code',
        props: { modelValue: content, language, theme: 'snengine-dark' },
        language
      }
    }

    if (imgExts.includes(ext)) {
      return {
        component: markRaw(ImagePreview),
        type: 'image',
        props: { imagePath: filePath }
      }
    }

    if (audioExts.includes(ext)) {
      return {
        component: markRaw(AudioPreview),
        type: 'audio',
        props: { audioPath: filePath }
      }
    }

    return {
      component: markRaw(UnknownFile),
      type: 'unknown',
      props: { filePath }
    }
  }

  const readFile = async (path: string) => {
    try {
      return await (window as any).electron.readFile(path)
    } catch (e) {
      return `Ошибка чтения файла: ${e}`
    }
  }

  return { getFileHandler }
}