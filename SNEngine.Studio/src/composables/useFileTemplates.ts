// src/composables/useFileTemplates.ts
import { ref, computed, onMounted } from 'vue'

export interface ScriptTemplate {
  id: string
  name: string
  description: string
  icon: string
  extension: string
  category: string
  defaultName: string
  content: string
}

const templates = ref<ScriptTemplate[]>([])

export function useFileTemplates() {
  const loadTemplates = async () => {
    try {
      const loaded = await window.electron.getAllTemplates()
      templates.value = loaded
      console.log(`✅ Loaded ${loaded.length} templates from ScriptTemplates`)
    } catch (err) {
      console.error('Failed to load templates:', err)
      // Fallback (на случай, если папки ещё нет)
      templates.value = [
        {
          id: 'empty',
          name: 'Empty File',
          description: 'Blank file with no content',
          icon: 'file',
          extension: '.sn',
          category: 'General',
          defaultName: 'NewFile',
          content: '// New empty file\n'
        }
      ]
    }
  }

  const getTemplate = (id: string) => 
    templates.value.find(t => t.id === id)

  return {
    templates: computed(() => templates.value),
    loadTemplates,
    getTemplate
  }
}