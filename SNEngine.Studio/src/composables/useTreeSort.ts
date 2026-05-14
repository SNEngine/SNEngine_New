import { ref, provide, inject, computed, type Ref } from 'vue'

export type SortField = 'name' | 'modified' | 'type'
export type SortOrder = 'asc' | 'desc'

interface TreeContext {
  sortField: Ref<SortField>
  sortOrder: Ref<SortOrder>
  searchQuery: Ref<string>
}

export function useTreeSort() {
  const context = inject<TreeContext>('tree-context', null)

  const sortField = context ? context.sortField : ref<SortField>('name')
  const sortOrder = context ? context.sortOrder : ref<SortOrder>('asc')
  const searchQuery = context ? context.searchQuery : ref('')

  // Предоставляем контекст только на корневом уровне
  if (!context) {
    provide('tree-context', { sortField, sortOrder, searchQuery })
  }

  const sortArray = (arr: any[]): any[] => {
    return [...arr].sort((a, b) => {
      // Папки всегда сверху
      if (a.isFolder && !b.isFolder) return -1
      if (!a.isFolder && b.isFolder) return 1

      let result = 0

      if (sortField.value === 'name') {
        result = a.name.localeCompare(b.name)
      } else if (sortField.value === 'type') {
        const extA = a.name.includes('.') ? a.name.split('.').pop()?.toLowerCase() || '' : ''
        const extB = b.name.includes('.') ? b.name.split('.').pop()?.toLowerCase() || '' : ''
        result = extA.localeCompare(extB) || a.name.localeCompare(b.name)
      } else if (sortField.value === 'modified') {
        const dateA = a.modified ? new Date(a.modified).getTime() : 0
        const dateB = b.modified ? new Date(b.modified).getTime() : 0
        result = dateA - dateB
      }

      return sortOrder.value === 'asc' ? result : -result
    })
  }

  return { 
    sortField, 
    sortOrder, 
    searchQuery, 
    sortArray 
  }
}