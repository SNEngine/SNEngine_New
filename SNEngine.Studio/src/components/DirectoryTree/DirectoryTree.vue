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
      <button class="refresh-btn" @click="refresh" title="Обновить (F5)">⟳</button>
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
              :name="getItemIcon(item)" 
              :color="getItemIconColor(item)"
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

import { getFileIcon } from '@/config/icons.config'

import { useDirectoryTree } from '@/composables/useDirectoryTree'
import { useTreeSearch } from '@/composables/useTreeSearch'
import { useContextMenu } from '@/composables/useContextMenu'

// Новые composables
import { useFileCrud } from '@/composables/useFileCrud'
import { useFileUtils } from '@/composables/useFileUtils'
import { useKeyboard } from '@/composables/useKeyboard'   // ← НОВОЕ

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const isRoot = !props.isSubTree

// Основной tree state
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

const { searchQuery, filteredItems, highlightMatch } = useTreeSearch(items)
const { contextMenuRef, show: showContextMenu } = useContextMenu()

// ====================== ГОРЯЧИЕ КЛАВИШИ ======================
const { add } = useKeyboard()

// ====================== ИКОНКИ ======================
const getItemIcon = (item: any): string => {
  if (item.isFolder) return 'folder_icon'
  return getFileIcon(item.name)
}

const getItemIconColor = (item: any): string => {
  if (item.isFolder) return '#FFCA28'

  const ext = item.name.toLowerCase().split('.').pop() || ''

  switch (ext) {
    case 'sn': return '#FF5252'
    case 'cs': return '#00B4FF'
    case 'html': case 'htm': return '#FF6B6B'
    case 'css': case 'scss': case 'less': return '#00C4B4'
    case 'png': case 'jpg': case 'jpeg': case 'gif': case 'webp': return '#FF9F1C'
    case 'mp3': case 'wav': case 'ogg': case 'm4a': case 'flac': return '#9B59B6'
    case 'dll': return '#8E44AD'
    default: return '#A0A0A0'
  }
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
      { label: 'Удалить', icon: 'error_icon', action: () => deleteItem(item), danger: true },
      { type: 'separator' },
      { label: 'Показать в проводнике', icon: 'explorer_icon', action: () => showInExplorer(item.path) },
      { label: 'Копировать путь', icon: 'info_icon', action: () => copyPath(item.path) },
      { label: 'Копировать имя', icon: 'info_icon', action: () => copyName(item.path) }
    )
  }

  showContextMenu(e, menuItems)
}

// ====================== ТОГГЛ + ЭМИТ ======================
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

// ====================== LIFECYCLE + ШОРТКАТЫ ======================
onMounted(() => {
  loadDirectory()

  // Горячие клавиши (работают глобально, но безопасно проверяют selectedItem)
  add('f2', () => {
    if (selectedItem.value) renameItem(selectedItem.value)
  })

  add('delete', () => {
    if (selectedItem.value) deleteItem(selectedItem.value)
  })

  add('ctrl+d', () => {
    if (selectedItem.value) duplicateItem(selectedItem.value.path)
  })

  add('f5', () => {
    if (isRoot) refresh()
  })

  // Дополнительные удобные шорткаты
  add('ctrl+c', () => {
    if (selectedItem.value) copyName(selectedItem.value.path)
  })

  add('ctrl+shift+c', () => {
    if (selectedItem.value) copyPath(selectedItem.value.path)
  })
})

watch(() => props.basePath, loadDirectory)
</script>

<style scoped>
/* (стили без изменений) */
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

.tree-viewport::-webkit-scrollbar { width: 4px; }
.tree-viewport::-webkit-scrollbar-thumb { background: #333; }
.tree-viewport::-webkit-scrollbar-thumb:hover { background: #444; }
</style>