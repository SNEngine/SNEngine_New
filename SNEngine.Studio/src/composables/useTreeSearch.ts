import { ref, computed, watch } from 'vue'

export function useTreeSearch(items: any[]) {
  const searchQuery = ref('')

  // Реактивный отфильтрованный список
  const filteredItems = computed(() => {
    const query = searchQuery.value.trim().toLowerCase()

    if (!query) return items.value || items

    return (items.value || items).filter((item: any) => 
      item.name.toLowerCase().includes(query)
    )
  })

  // Сброс поиска
  const clearSearch = () => {
    searchQuery.value = ''
  }

  // Поиск по конкретному пути (для подсветки)
  const highlightMatch = (itemName: string) => {
    const query = searchQuery.value.trim().toLowerCase()
    if (!query) return itemName

    const regex = new RegExp(`(${query})`, 'gi')
    return itemName.replace(regex, '<mark>$1</mark>')
  }

  return {
    searchQuery,
    filteredItems,
    clearSearch,
    highlightMatch
  }
}