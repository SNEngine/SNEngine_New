<template>
  <div class="directory-tree-container">
    <div class="tree-header" v-if="isRoot">
      <div class="search-box">
        <input 
          v-model="searchQuery"
          placeholder="Поиск по файлам..."
          class="search-input"
        />
      </div>
      <button class="refresh-btn" @click="refresh" title="Обновить">⟳</button>
    </div>

    <div 
      class="tree-viewport" 
      @contextmenu.prevent="onContextMenu($event, null)"
      @click="selectedItem = null"
    >
      <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>

      <div class="tree-content">
        <div 
          v-for="item in filteredItems" 
          :key="item.path"
          class="tree-item"
        >
          <div 
            class="tree-node"
            :class="{ 
              'is-folder': item.isFolder, 
              'is-open': item.isOpen,
              'is-active': activePath === item.path || selectedItem?.path === item.path
            }"
            @click.stop="toggleItem(item)"
            @contextmenu.stop.prevent="onContextMenu($event, item)"
          >
            <BaseIcon 
              :name="item.isFolder ? 'folder_icon' : getFileIcon(item.name)" 
              :color="item.isFolder ? '#FFCA28' : '#FF5252'"
              class="node-icon"
            />
            <span class="node-name" v-html="highlightMatch(item.name)"></span>
          </div>

          <div v-if="item.isFolder && item.isOpen" class="tree-children">
            <DirectoryTree 
              :base-path="item.path" 
              :active-path="activePath"
              :is-sub-tree="true"
              @file-click="emitFileClick"
            />
          </div>
        </div>
      </div>
    </div>

    <ContextMenu ref="contextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'
import { getFileIcon } from '@/utils/fileIcons'

import { useDirectoryTree } from '@/composables/useDirectoryTree'
import { useTreeSearch } from '@/composables/useTreeSearch'
import { useContextMenu } from '@/composables/useContextMenu'

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const isRoot = !props.isSubTree

// Composables
const {
  items,
  loading,
  selectedItem,
  loadDirectory,
  createItem,
  renameItem,
  deleteItem,
  toggleOpen
} = useDirectoryTree(props.basePath, isRoot)

const { searchQuery, filteredItems, highlightMatch } = useTreeSearch(items)
const { contextMenuRef, show: showContextMenu } = useContextMenu()

// Контекстное меню
const onContextMenu = (e: MouseEvent, item: any) => {
  selectedItem.value = item

  const menuItems = [
    { label: 'Создать файл', icon: 'file_icon', action: () => createItem(false) },
    { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(true) },
  ]

  if (item) {
    menuItems.push(
      { type: 'separator' },
      { label: 'Переименовать', action: renameItem },
      { label: 'Удалить', icon: 'delete_icon', action: deleteItem, danger: true }
    )
  }

  showContextMenu(e, menuItems)
}

const toggleItem = (item: any) => {
  selectedItem.value = item
  if (!item.isFolder) {
    emit('file-click', item.path)
    return
  }
  toggleOpen(item)
}

const emitFileClick = (path: string) => emit('file-click', path)
const refresh = () => loadDirectory()

onMounted(loadDirectory)
watch(() => props.basePath, loadDirectory)
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

.tree-header {
  padding: 8px 10px;
  border-bottom: 1px solid #2a2a2a;
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.search-input {
  width: 100%;
  background: #252526;
  border: 1px solid #3c3c3c;
  color: #ccc;
  padding: 4px 8px;
  border-radius: 3px;
  font-size: 12px;
  outline: none;
}

.refresh-btn {
  background: transparent;
  border: none;
  color: #888;
  cursor: pointer;
  padding: 0 4px;
}

.tree-viewport {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

.tree-node {
  display: flex;
  align-items: center;
  padding: 4px 10px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  transition: background 0.1s;
}

.tree-node:hover { background: #2a2d2e; }
.tree-node.is-active { background: #37373d; color: #FF5252; }

.node-icon { 
  margin-right: 6px; 
  width: 16px; 
  height: 16px;
  flex-shrink: 0; 
}

.tree-children {
  height: auto; 
  margin-left: 12px;
  border-left: 1px solid #2d2d2d;
}

.loading { 
  padding: 15px; 
  color: #666; 
  font-size: 12px; 
  text-align: center; 
}

/* Скроллбар */
.tree-viewport::-webkit-scrollbar { width: 4px; }
.tree-viewport::-webkit-scrollbar-thumb { background: #333; }
.tree-viewport::-webkit-scrollbar-thumb:hover { background: #444; }
</style>