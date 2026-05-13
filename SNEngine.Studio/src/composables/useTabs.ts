import { ref } from 'vue'

export function useTabs() {
  const tabs = ref<Array<{
    id: string
    filePath: string
    name: string
    type: string
    content?: string
    language?: string
    isDirty: boolean
  }>>([])

  const activeFilePath = ref<string | null>(null)

  // ====================== ОТКРЫТИЕ ФАЙЛА ======================
  const openFile = async (filePath: string, getFileHandler: any) => {
    const existing = tabs.value.find(t => t.filePath === filePath)
    if (existing) {
      activateTab(existing)
      return
    }

    const { component, type, props, language } = await getFileHandler(filePath)

    const newTab = {
      id: Date.now().toString(),
      filePath,
      name: filePath.split(/[/\\]/).pop() || filePath,
      type,
      content: props.modelValue || props.initialHtml || '',
      language,
      isDirty: false
    }

    tabs.value.push(newTab)
    activateTab(newTab)
  }

  const activateTab = (tab: any) => {
    activeFilePath.value = tab.filePath
  }

  const closeTab = (tab: any) => {
    const index = tabs.value.findIndex(t => t.id === tab.id)
    if (index === -1) return

    tabs.value.splice(index, 1)

    if (activeFilePath.value === tab.filePath) {
      if (tabs.value.length > 0) {
        activateTab(tabs.value[Math.max(0, index - 1)])
      } else {
        activeFilePath.value = null
      }
    }
  }

  const markDirty = (filePath: string) => {
    const tab = tabs.value.find(t => t.filePath === filePath)
    if (tab) tab.isDirty = true
  }

  const markClean = (filePath: string) => {
    const tab = tabs.value.find(t => t.filePath === filePath)
    if (tab) tab.isDirty = false
  }

  return {
    tabs,
    activeFilePath,
    openFile,
    activateTab,
    closeTab,
    markDirty,
    markClean
  }
}