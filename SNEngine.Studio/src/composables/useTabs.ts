import { ref, shallowRef } from 'vue'
import type { FileHandlerResult } from './useFileType'

export function useTabs() {
  const tabs = ref<Array<{
    id: string
    filePath: string
    name: string
    type: string
    component: any          // ← ДОБАВЛЕНО
    props: Record<string, any>
    content?: string
    language?: string
  }>>([])

  const activeFilePath = ref<string | null>(null)
  const currentComponent = shallowRef<{ component: any; props: Record<string, any> }>({
    component: null,
    props: {}
  })

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
      component,                    // ← ВАЖНО: сохраняем компонент
      props: props || {},
      language
    }

    tabs.value.push(newTab)
    activateTab(newTab)
  }

  const activateTab = (tab: any) => {
    activeFilePath.value = tab.filePath
    currentComponent.value = {
      component: tab.component,
      props: tab.props || {}
    }
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
        currentComponent.value = { component: null, props: {} }
      }
    }
  }

  return {
    tabs,
    activeFilePath,
    currentComponent,
    openFile,
    activateTab,
    closeTab
  }
}