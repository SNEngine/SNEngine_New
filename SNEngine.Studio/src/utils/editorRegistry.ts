import { markRaw } from 'vue'
// Добавляем ../ чтобы выйти из папки utils в папку src
import CodeEditor from '../components/CodeEditor/CodeEditor.vue'
import ImagePreview from '../components/ImagePreview/ImagePreview.vue'
import UnknownFile from '../components/UnknownFile/UnknownFile.vue'

export interface EditorConfig {
  component: any
  props: Record<string, any>
  isText: boolean
}

const TEXT_EXTS = ['sn', 'ts', 'js', 'json', 'txt', 'html', 'css', 'md', 'xml', 'cs']
const IMAGE_EXTS = ['png', 'jpg', 'jpeg', 'gif', 'webp', 'svg']

export function getEditorForFile(filePath: string): EditorConfig {
  const ext = filePath.toLowerCase().split('.').pop() || ''

  if (IMAGE_EXTS.includes(ext)) {
    return {
      component: markRaw(ImagePreview),
      props: { imagePath: filePath },
      isText: false
    }
  }

  if (TEXT_EXTS.includes(ext)) {
    return {
      component: markRaw(CodeEditor),
      props: {
        language: ext === 'sn' ? 'sn' : (ext === 'ts' ? 'typescript' : 'plaintext'),
        theme: 'snengine-dark'
      },
      isText: true
    }
  }

  return {
    component: markRaw(UnknownFile),
    props: { filePath },
    isText: false
  }
}