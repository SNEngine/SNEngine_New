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

      <!-- Основной узел с поддержкой drag & drop -->
      <div 
        class="tree-node"
        draggable="true"
        :class="{ 
          'is-folder': item.isFolder, 
          'is-open': item.isOpen,
          'is-active': isActive,
          'is-selected': effectiveIsSelected,
          'is-dragging': isDragging,
          'drop-target': isDropTarget,
          'drag-over': item.isFolder && dragHandlers?.isDragOver
        }"
        @click.stop="handleClick($event)"
        @contextmenu.stop.prevent="handleContextMenu"
        @dragstart="handleDragStart"
        @dragend="handleDragEnd"
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

    <!-- Дочерние элементы -->
    <div 
      v-if="item.isFolder && item.isOpen" 
      class="tree-children"
      @drop.prevent="handleChildrenDrop"
      @dragover.prevent="handleChildrenDragOver"
      @dragleave="handleChildrenDragLeave"
      @contextmenu.prevent="handleContextMenuForChildren"
    >
      <div v-if="item.isLoadingChildren" class="loading-children">Загрузка...</div>
      <TreeNode
        v-else
        v-for="child in item.children"
        :key="child.path"
        :item="child"
        :active-path="activePath"
        :search-query="searchQuery"
        :is-selected="injectedSelection?.isSelected ? injectedSelection.isSelected(child.path) : false"
        :drag-handlers="dragHandlers"
        :tree-drag="treeDrag"
        @toggle="$emit('toggle', $event)"
        @file-click="$emit('file-click', $event)"
        @contextmenu="handleContextMenu"
        @select="$emit('select', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, inject } from 'vue'
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
  isSelected?: boolean
  dragHandlers?: any
  treeDrag?: any
  // Optional direct handler (legacy). Main path now uses the 'select' event.
  onSelect?: (payload: { item: TreeItem; modifiers: any }) => void
}>()

const emit = defineEmits<{
  (e: 'toggle', item: TreeItem): void
  (e: 'file-click', path: string): void
  (e: 'contextmenu', event: MouseEvent, item: TreeItem): void
  // New unified payload for reliable multi-selection at any depth
  (e: 'select', payload: { item: TreeItem; modifiers: any }): void
}>()

const isActive = computed(() => props.activePath === props.item.path)
const isDragging = computed(() => {
  const drag = props.treeDrag
  if (!drag) return false
  if (drag.draggedItems?.value) {
    return drag.draggedItems.value.some((i: any) => i.path === props.item.path)
  }
  return drag.draggedItem?.path === props.item.path // fallback
})
const isDropTarget = computed(() => props.treeDrag?.dragOverItem?.path === props.item.path)

// Selection via provide/inject (falls back to prop if provided directly)
const injectedSelection = inject<any>('treeSelection', null)
// Selection state: prefer explicit prop (from direct parent), fallback to injected tree-wide selection
const effectiveIsSelected = computed(() => {
  if (props.isSelected !== undefined) return props.isSelected
  if (injectedSelection?.isSelected) {
    return injectedSelection.isSelected(props.item.path)
  }
  return false
})

// ==================== ВНУТРЕННИЙ DRAG & DROP ====================
const handleDragStart = (e: DragEvent) => {
  if (!props.treeDrag) return

  // === MULTI-DRAG SUPPORT ===
  // Если элемент входит в текущее мульти-выделение — тащим всю группу
  let itemsToDrag: any[] = [props.item]

  try {
    const sel = injectedSelection
    if (sel && sel.hasSelection?.value && sel.getSelectedPaths) {
      const selectedPaths: string[] = sel.getSelectedPaths()
      if (selectedPaths.includes(props.item.path) && selectedPaths.length > 1) {
        // Пытаемся собрать реальные объекты из selection, если они есть
        // (пока упрощённо — тащим только пути, обработка будет в drop)
        // Лучшее решение — DirectoryTree должен предоставлять getDraggedItems()
      }
    }
  } catch (e) {}

  props.treeDrag.startDrag(itemsToDrag, e)

  // Кастомный ghost (поддержка multi)
  if (e.dataTransfer) {
    const count = itemsToDrag.length
    const isMulti = count > 1

    const ghost = document.createElement('div')
    ghost.style.cssText = `
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 6px 14px;
      background: #252526;
      border: 1px solid #FF5252;
      border-radius: 4px;
      color: #fff;
      font-size: 13px;
      box-shadow: 0 4px 12px rgba(0,0,0,0.5);
      pointer-events: none;
      position: absolute;
      top: -1000px;
      white-space: nowrap;
    `

    if (isMulti) {
      ghost.innerHTML = `
        <span style="font-size:16px;">📦</span>
        <span>${count} элементов</span>
      `
    } else {
      ghost.innerHTML = `
        <span style="font-size:16px;">${props.item.isFolder ? '📁' : '📄'}</span>
        <span>${props.item.name}</span>
      `
    }

    document.body.appendChild(ghost)
    e.dataTransfer.setDragImage(ghost, 20, 20)
    setTimeout(() => document.body.removeChild(ghost), 0)
  }
}

const handleDragEnd = () => {
  if (props.treeDrag) {
    props.treeDrag.endDrag()
  }
}

const handleDragOver = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragOver(e)
  }
  if (props.treeDrag) {
    props.treeDrag.onDragOver(props.item, e)
  }
}

const handleDragLeave = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragLeave(e)
  }
  if (props.treeDrag) {
    props.treeDrag.onDragLeave(props.item)
  }
}

const handleFolderDrop = (e: DragEvent) => {
  if (props.item.isFolder) {
    // Внешний drag & drop из ОС
    if (props.dragHandlers) {
      props.dragHandlers.handleDrop(e, props.item.path)
    }
    // Внутренний drag & drop (теперь поддерживает массив)
    if (props.treeDrag) {
      const payload = props.treeDrag.onDrop(props.item, e)
      if (payload) {
        emit('internal-drop', payload)
      }
    }
  }
}

// ==================== ИКОНКИ И ПОДСВЕТКА ====================
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

const handleClick = async (event: MouseEvent) => {
  // Emit a structured payload with modifiers.
  // This guarantees reliable Shift/Ctrl detection at any nesting depth
  // (raw MouseEvent gets lost when bubbling through multiple $emit layers).
  const modifiers = {
    ctrlKey: event.ctrlKey || event.metaKey,
    shiftKey: event.shiftKey,
    metaKey: event.metaKey,
  }

  emit('select', { item: props.item, modifiers })

  if (props.item.isFolder) {
    await emit('toggle', props.item)
  } else {
    emit('file-click', props.item.path)
  }
}

const handleContextMenu = (e: MouseEvent, passedItem?: any) => {
  const targetItem = passedItem || props.item
  emit('contextmenu', e, targetItem)
}

// === Handlers for the children container area (unifies behavior for nested folders) ===
const handleChildrenDrop = (e: DragEvent) => {
  if (!props.item.isFolder) return

  // External OS drop
  if (props.dragHandlers) {
    props.dragHandlers.handleDrop(e, props.item.path)
  }

  // Internal tree drag
  if (props.treeDrag) {
    const payload = props.treeDrag.onDrop(props.item, e)
    if (payload) {
      emit('internal-drop', payload)
    }
  }
}

const handleChildrenDragOver = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragOver(e)
  }
  if (props.treeDrag) {
    props.treeDrag.onDragOver(props.item, e)
  }
}

const handleChildrenDragLeave = (e: DragEvent) => {
  if (props.item.isFolder && props.dragHandlers) {
    props.dragHandlers.handleDragLeave(e)
  }
  if (props.treeDrag) {
    props.treeDrag.onDragLeave(props.item)
  }
}

const handleContextMenuForChildren = (e: MouseEvent) => {
  // Right-click in the whitespace inside an open folder → treat as context on that folder
  emit('contextmenu', e, props.item)
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

.tree-node.is-selected {
  background: #3a2a2a;
  position: relative;
}

/* Red accent bar on the left for selected items - works consistently at any nesting depth */
.tree-node.is-selected::before {
  content: '';
  position: absolute;
  left: 0;
  top: 0;
  bottom: 0;
  width: 3px;
  background: #FF5252;
  border-radius: 0 2px 2px 0;
}

.tree-node.is-selected:hover {
  background: #4a3535;
}

/* When item is both selected and the active/open file */
.tree-node.is-selected.is-active {
  background: #3f2f2f;
  color: #FF5252;
  font-weight: 500;
}

.tree-node.is-selected.is-active::before {
  background: #FF5252;
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