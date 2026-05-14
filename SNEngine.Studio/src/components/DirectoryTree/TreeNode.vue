<template>
  <div class="tree-item">
    <Tooltip 
      position="bottom"
      :delay="800"
      :hide-delay="300"
      :offset="8"
    >
      <template #content>
        <div class="tooltip-inner">
          <BaseIcon 
            :name="iconName" 
            :color="iconColor"
            style="width: 20px; height: 20px; flex-shrink: 0;"
          />
          <div class="tooltip-info">
            <div class="tooltip-type">
              {{ item.isFolder ? 'ПАПКА' : 'ФАЙЛ' }}
            </div>
            <div class="tooltip-name">{{ item.name }}</div>
            <div class="tooltip-path">{{ item.path }}</div>
            
            <div v-if="!item.isFolder" class="tooltip-ext">
              .{{ item.name.split('.').pop()?.toUpperCase() }}
            </div>
            <div v-else-if="item.children?.length" class="tooltip-ext">
              Элементов: {{ item.children.length }}
            </div>
          </div>
        </div>
      </template>

      <div 
        class="tree-node"
        :class="{ 
          'is-folder': item.isFolder, 
          'is-open': item.isOpen,
          'is-active': isActive,
          'is-selected': props.selectedItem?.path === props.item.path,
          'drag-over': item.isFolder && dragHandlers?.isDragOver
        }"
        @click.stop="handleClick"
        @contextmenu.stop.prevent="handleContextMenu"
        @dragover.prevent="handleDragOver"
        @dragleave="handleDragLeave"
        @drop.prevent="handleFolderDrop"
      >
        <BaseIcon 
          :name="iconName" 
          :color="iconColor"
          class="node-icon"
        />
        <span class="node-name" v-html="highlightedName"></span>
      </div>
    </Tooltip>

    <div v-if="item.isFolder && item.isOpen" class="tree-children">
      <div v-if="item.isLoadingChildren" class="loading-children">Загрузка...</div>
      <TreeNode
        v-else
        v-for="child in item.children"
        :key="child.path"
        :item="child"
        :active-path="activePath"
        :search-query="searchQuery"
        :selected-item="selectedItem"
        :drag-handlers="dragHandlers"
        @toggle="$emit('toggle', $event)"
        @file-click="$emit('file-click', $event)"
        @contextmenu="handleContextMenu"
        @select="$emit('select', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import Tooltip from '../Tooltip/Tooltip.vue'
import { getFileIcon } from '@/config/icons.config'

export interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen: boolean
  children?: TreeItem[]
  isLoadingChildren?: boolean
}

const props = defineProps<{
  item: TreeItem
  activePath?: string
  searchQuery?: string
  selectedItem?: TreeItem | null
  dragHandlers?: {
    isDragOver: boolean
    handleDragOver: (e: DragEvent) => void
    handleDragLeave: (e: DragEvent) => void
    handleDrop: (e: DragEvent, targetDir: string) => Promise<boolean>
  }
}>()

const emit = defineEmits<{
  (e: 'toggle', item: TreeItem): void
  (e: 'file-click', path: string): void
  (e: 'contextmenu', event: MouseEvent, item: TreeItem): void
  (e: 'select', item: TreeItem): void
}>()

const isActive = computed(() => props.activePath === props.item.path)

// ==================== DRAG & DROP ====================
const handleDragOver = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragOver(e)
  }
}

const handleDragLeave = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragLeave(e)
  }
}

const handleFolderDrop = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDrop(e, props.item.path)
  }
}

const iconName = computed(() => props.item.isFolder ? 'folder_icon' : getFileIcon(props.item.name))

const iconColor = computed(() => {
  if (props.item.isFolder) return '#FFCA28'
  const ext = props.item.name.toLowerCase().split('.').pop() || ''
  switch (ext) {
    case 'sn': return '#FF5252'
    case 'js': case 'ts': return '#00B4FF'
    case 'html': case 'htm': return '#FF6B6B'
    case 'css': return '#00C4B4'
    case 'png': case 'jpg': case 'jpeg': case 'webp': return '#FF9F1C'
    case 'mp3': case 'wav': return '#9B59B6'
    default: return '#A0A0A0'
  }
})

const highlightedName = computed(() => {
  const name = props.item.name
  const query = props.searchQuery?.trim().toLowerCase()
  if (!query) return name
  const regex = new RegExp(`(${query})`, 'gi')
  return name.replace(regex, '<mark>$1</mark>')
})

const handleClick = async () => {
  emit('select', props.item)                    // ← ВАЖНО: выбираем элемент
  if (props.item.isFolder) {
    await emit('toggle', props.item)
  } else {
    emit('file-click', props.item.path)
  }
}

const handleContextMenu = (e: MouseEvent, item?: TreeItem) => {
  emit('contextmenu', e, item || props.item)
}
</script>

<style scoped>
.tree-item { user-select: none; }

.tree-node {
  display: flex;
  align-items: center;
  padding: 4px 10px;
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  transition: background 0.1s;
  border-radius: 3px;
}

.tree-node:hover { background: #2a2d2e; }
.tree-node.is-active {
  background: #37373d;
  color: #FF5252;
  font-weight: 500;
}

.node-icon {
  margin-right: 8px;
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.node-name {
  flex: 1;
  overflow: hidden;
  text-overflow: ellipsis;
}

mark {
  background: #FF5252;
  color: white;
  padding: 0 2px;
  border-radius: 2px;
}

.tree-children {
  margin-left: 20px;
  border-left: 1px solid #2d2d2d;
  padding-left: 4px;
}

.loading-children {
  padding: 4px 10px;
  color: #888;
  font-size: 12px;
  font-style: italic;
}

/* Стили тултипа */
.tooltip-inner {
  display: flex;
  align-items: flex-start;
  gap: 10px;
}

.tooltip-info {
  flex: 1;
  min-width: 0;
}

.tooltip-type {
  color: #FF5252;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
}

.tooltip-name {
  font-size: 14px;
  font-weight: 600;
  color: #ffffff;
  margin: 2px 0 6px;
  word-break: break-all;
}

.tooltip-path {
  font-size: 11.5px;
  color: #aaaaaa;
  line-height: 1.3;
  word-break: break-all;
}

.tooltip-ext {
  margin-top: 6px;
  font-size: 11px;
  color: #888;
}
</style>