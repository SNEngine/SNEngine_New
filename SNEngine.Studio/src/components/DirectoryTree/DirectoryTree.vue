<template>
  <div class="directory-tree-container">
    <TreeHeader
      v-if="isRoot"
      v-model="searchQuery"
      v-model:sortField="sortField"
      v-model:sortOrder="sortOrder"
      @refresh="refresh"
    />

    <div 
      class="tree-viewport"
      @contextmenu.prevent="onContextMenu($event, null)"
      @click="handleViewportClick"
    >
      <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>

      <div class="tree-content">
        <TreeNode
          v-for="item in finalItems"
          :key="item.path"
          :item="item"
          :active-path="activePath"
          :search-query="searchQuery"
          @toggle="toggleOpen"
          @file-click="emitFileClick"
          @contextmenu="onContextMenu"
        />
      </div>
    </div>

    <ContextMenu ref="contextMenuRef" />

    <FileProperties 
      v-if="isOpen && currentFile"
      :file="currentFile"
      @close="close"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed } from 'vue'
import TreeHeader from './TreeHeader.vue'
import TreeNode from './TreeNode.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'
import FileProperties from '../FileProperties/FileProperties.vue'

import { useDirectoryTree } from '@/composables/useDirectoryTree'
import { useTreeSearch } from '@/composables/useTreeSearch'
import { useContextMenu } from '@/composables/useContextMenu'
import { useFileCrud } from '@/composables/useFileCrud'
import { useFileUtils } from '@/composables/useFileUtils'
import { useKeyboard } from '@/composables/useKeyboard'
import { useFileProperties } from '@/composables/useFileProperties'
import { useOpenWith } from '@/composables/useOpenWith'   // ← новый

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const isRoot = !props.isSubTree

const { items, loading, selectedItem, loadDirectory, toggleOpen } = useDirectoryTree(props.basePath, isRoot)
const { createItem, renameItem, deleteItem } = useFileCrud()
const { showInExplorer, copyPath, copyName, duplicateItem } = useFileUtils()
const { searchQuery, filteredItems } = useTreeSearch(items)
const { contextMenuRef, show: showContextMenu } = useContextMenu()
const { add } = useKeyboard()
const { showProperties, isOpen, currentFile, close } = useFileProperties()
const { openWith } = useOpenWith()   // ← новый

// ====================== СОРТИРОВКА ======================
const sortField = ref<'name' | 'modified' | 'type'>('name')
const sortOrder = ref<'asc' | 'desc'>('asc')

const finalItems = computed(() => {
  let arr = [...filteredItems.value]

  arr.sort((a, b) => {
    if (a.isFolder && !b.isFolder) return -1
    if (!a.isFolder && b.isFolder) return 1
    return 0
  })

  if (sortField.value === 'name') {
    arr.sort((a, b) => a.name.localeCompare(b.name))
  } else if (sortField.value === 'modified') {
    arr.sort((a, b) => {
      const dateA = a.modified ? new Date(a.modified).getTime() : 0
      const dateB = b.modified ? new Date(b.modified).getTime() : 0
      return dateA - dateB
    })
  } else if (sortField.value === 'type') {
    arr.sort((a, b) => {
      const extA = a.name.includes('.') ? a.name.split('.').pop()?.toLowerCase() || '' : ''
      const extB = b.name.includes('.') ? b.name.split('.').pop()?.toLowerCase() || '' : ''
      return extA.localeCompare(extB) || a.name.localeCompare(b.name)
    })
  }

  if (sortOrder.value === 'desc') arr.reverse()

  return arr
})

// ====================== КЛИК ПО ПУСТОМУ МЕСТУ ======================
const handleViewportClick = (e: MouseEvent) => {
  if (!(e.target as HTMLElement).closest('.tree-node')) {
    showInExplorer(props.basePath)
  }
  selectedItem.value = null
}

// ====================== КОНТЕКСТНОЕ МЕНЮ ======================
const onContextMenu = (e: MouseEvent, item: any) => {
  selectedItem.value = item

  const menuItems = [
    { label: 'Создать файл', icon: 'file_icon', action: () => createItem(props.basePath, false, item) },
    { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(props.basePath, true, item) },
  ]

  if (item) {
    menuItems.push(
      { type: 'separator' },
      { label: 'Переименовать', icon: 'edit_icon', action: () => renameItem(item) },
      { label: 'Дублировать', icon: 'copy_icon', action: () => duplicateItem(item.path) },
      { label: 'Свойства', icon: 'info_icon', action: () => showProperties(item) },
      { 
        label: 'Открыть с помощью...', 
        icon: 'open_with_icon', 
        action: () => openWith(item.path) 
      },
      { label: 'Удалить', icon: 'error_icon', action: () => deleteItem(item), danger: true },
      { type: 'separator' },
      { label: 'Показать в проводнике', icon: 'explorer_icon', action: () => showInExplorer(item.path) },
      { label: 'Копировать путь', icon: 'info_icon', action: () => copyPath(item.path) },
      { label: 'Копировать имя', icon: 'info_icon', action: () => copyName(item.path) }
    )
  } else {
    menuItems.push({ label: 'Открыть в проводнике', icon: 'explorer_icon', action: () => showInExplorer(props.basePath) })
  }

  showContextMenu(e, menuItems)
}

const emitFileClick = (path: string) => emit('file-click', path)
const refresh = () => loadDirectory()

const setupKeyboardShortcuts = () => {
  add('f2', () => selectedItem.value && renameItem(selectedItem.value))
  add('delete', () => selectedItem.value && deleteItem(selectedItem.value))
  add('ctrl+d', () => selectedItem.value && duplicateItem(selectedItem.value.path))
  add('f5', () => isRoot && refresh())
  add('ctrl+c', () => selectedItem.value && copyName(selectedItem.value.path))
  add('ctrl+shift+c', () => selectedItem.value && copyPath(selectedItem.value.path))
}

watch(() => props.basePath, loadDirectory, { immediate: true })
setupKeyboardShortcuts()
</script>

<style scoped>
.directory-tree-container {
  display: flex;
  flex-direction: column;
  background: #161616;
  color: #eeeeee;
  user-select: none;
  width: 100%;
  height: 100%;
}

:not(.tree-children) > .directory-tree-container {
  height: 100%;
}

.tree-viewport {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

.loading {
  padding: 15px;
  color: #666;
  font-size: 12px;
  text-align: center;
}

.tree-viewport::-webkit-scrollbar {
  width: 4px;
}

.tree-viewport::-webkit-scrollbar-thumb {
  background: #333;
}

.tree-viewport::-webkit-scrollbar-thumb:hover {
  background: #444;
}
</style>