// src/composables/useFileCreation.ts
import { useNotification } from './useNotification'
import { useInputBox } from './useInputBox'
import { lastUpdate } from '@/utils/watcherState'

export interface CreateFromTemplateData {
  name: string
  content: string
  templateId?: string
}

export function useFileCreation() {
  const { success, error } = useNotification()
  const { showInputBox } = useInputBox()

  const createFromTemplate = async (data: CreateFromTemplateData) => {
    try {
      const projectPath = await window.electron.getProjectPath()
      const normalized = projectPath.replace(/\\+/g, '/').replace(/\/+$/, '')
      let fullPath = `${normalized}/${data.name}`

      // Теперь вся логика уникальности — в main процессе
      fullPath = await window.electron.getUniquePath(fullPath)

      const result = await window.electron.writeFile(fullPath, data.content)

      if (result.success) {
        lastUpdate.value = Date.now()
        success('Файл создан', data.name)
        return { success: true, path: fullPath }
      } else {
        error('Не удалось создать файл', data.name)
        return { success: false }
      }
    } catch (err: any) {
      console.error('createFromTemplate error:', err)
      error('Ошибка при создании файла')
      return { success: false }
    }
  }

  const createEmptyFile = async (basePath: string, suggestedName = 'NewFile.sn') => {
    let fullPath = `${basePath.replace(/\\+/g, '/').replace(/\/+$/, '')}/${suggestedName}`
    fullPath = await window.electron.getUniquePath(fullPath)

    try {
      const result = await window.electron.writeFile(fullPath, '')
      if (result.success) {
        lastUpdate.value = Date.now()
        success('Файл создан', suggestedName)
        return { success: true, path: fullPath }
      }
      throw new Error('writeFile failed')
    } catch (err) {
      error('Не удалось создать файл')
      return { success: false }
    }
  }

  const createFolder = async (basePath: string, suggestedName = 'New Folder') => {
    let fullPath = `${basePath.replace(/\\+/g, '/').replace(/\/+$/, '')}/${suggestedName}`
    fullPath = await window.electron.getUniquePath(fullPath, true)

    try {
      await window.electron.createDirectory?.(fullPath)
      lastUpdate.value = Date.now()
      success('Папка создана', suggestedName)
      return { success: true, path: fullPath }
    } catch (err) {
      error('Не удалось создать папку')
      return { success: false }
    }
  }

  return {
    createFromTemplate,
    createEmptyFile,
    createFolder
  }
}