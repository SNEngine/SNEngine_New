import { ref } from 'vue'

export function useFileOpener() {
  const tabsRef = ref<any>(null)

  const setTabsRef = (ref: any) => {
    tabsRef.value = ref
  }

  const openFile = async (filePath: string) => {
    if (!tabsRef.value?.openFile) {
      console.warn('EditorTabs not ready')
      return
    }
    
    // Всегда используем один метод
    await tabsRef.value.openFile(filePath)
  }

  return {
    setTabsRef,
    openFile
  }
}