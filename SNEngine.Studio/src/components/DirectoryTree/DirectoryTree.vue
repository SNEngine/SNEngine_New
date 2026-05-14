<template>
  <div class="directory-tree-container">
    <!-- Шапка (только для корневого дерева) -->
    <TreeHeader
      v-if="isRoot"
      v-model="searchQuery"
      @refresh="refresh"
    />

    <div 
      class="tree-viewport"
      @contextmenu.prevent="onContextMenu($event, null)"
      @click="selectedItem = null"
    >
      <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>

      <div class="tree-content">
        <TreeNode
          v-for="item in filteredItems"
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
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import TreeHeader from './TreeHeader.vue'
import TreeNode from './TreeNode.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'

import { useDirectoryTree } from '@/composables/useDirectoryTree'
import { useTreeSearch } from '@/composables/useTreeSearch'
import { useContextMenu } from '@/composables/useContextMenu'
import { useFileCrud } from '@/composables/useFileCrud'
import { useFileUtils } from '@/composables/useFileUtils'
import { useKeyboard } from '@/composables/useKeyboard'

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const isRoot = !props.isSubTree

// Основной state дерева
const {
  items,
  loading,
  selectedItem,
  loadDirectory,
  toggleOpen
} = useDirectoryTree(props.basePath, isRoot)

// Операции с файлами
const { createItem, renameItem, deleteItem } = useFileCrud()
const { showInExplorer, copyPath, copyName, duplicateItem } = useFileUtils()

// Поиск
const { searchQuery, filteredItems } = useTreeSearch(items)

// Контекстное меню
const { contextMenuRef, show: showContextMenu } = useContextMenu()

// Горячие клавиши
const { add } = useKeyboard()

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
      { label: 'Удалить', icon: 'error_icon', action: () => deleteItem(item), danger: true },
      { type: 'separator' },
      { label: 'Показать в проводнике', icon: 'explorer_icon', action: () => showInExplorer(item.path) },
      { label: 'Копировать путь', icon: 'info_icon', action: () => copyPath(item.path) },
      { label: 'Копировать имя', icon: 'info_icon', action: () => copyName(item.path) }
    )
  }

  showContextMenu(e, menuItems)
}

// ====================== ЭМИТЫ ======================
const emitFileClick = (path: string) => emit('file-click', path)
const refresh = () => loadDirectory()

// ====================== ГОРЯЧИЕ КЛАВИШИ ======================
const setupKeyboardShortcuts = () => {
  add('f2', () => selectedItem.value && renameItem(selectedItem.value))
  add('delete', () => selectedItem.value && deleteItem(selectedItem.value))
  add('ctrl+d', () => selectedItem.value && duplicateItem(selectedItem.value.path))
  add('f5', () => isRoot && refresh())
  add('ctrl+c', () => selectedItem.value && copyName(selectedItem.value.path))
  add('ctrl+shift+c', () => selectedItem.value && copyPath(selectedItem.value.path))
}

// ====================== LIFECYCLE ======================
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