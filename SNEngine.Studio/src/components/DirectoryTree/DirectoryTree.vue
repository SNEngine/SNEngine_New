<template>
  <div class="directory-tree-container">
    <!-- Background drop/paste/context handling is now delegated to TreeDropBackground + TreePasteHandler -->
    <TreeHeader
      v-if="isRoot"
      v-model="searchQuery"
      v-model:sortField="sortField"
      v-model:sortOrder="sortOrder"
      @refresh="refresh"
    />

    <TreePasteHandler
      :base-path="basePath"
      :get-currently-selected-items="getCurrentlySelectedItems"
      :copy-item="copyItem"
      :refresh="refresh"
      :handle-drop-from-clipboard="handleDropFromClipboard"
      @pasted="() => {}"
    >
      <TreeDropBackground
        :base-path="basePath"
        :tree-selection="treeSelection"
        :handle-root-drop="handleRootDrop"
        :handle-root-drag-over="handleRootDragOver"
        :handle-root-drag-leave="handleRootDragLeave"
        @background-click="handleViewportClick"
        @drop="handleRootDrop"
      >
        <TreeViewport
          :loading="loading"
          :items="finalItems"
          :active-path="activePath"
          :search-query="searchQuery"
          :drag-handlers="dragHandlers"
          :tree-drag="treeDrag"
          :on-select="onSelectItem"
          :tree-selection="treeSelection"
          @toggle="toggleOpen"
          @file-click="emitFileClick"
          @contextmenu="onContextMenu"
          @select="onSelectItem"
          @internal-drop="handleInternalDrop"
        />
      </TreeDropBackground>
    </TreePasteHandler>

    <ContextMenu ref="contextMenuRef" />

    <FileProperties 
      v-if="isOpen && currentFile"
      :file="currentFile"
      @close="close"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, provide } from 'vue'
import TreeHeader from './TreeHeader.vue'
import TreeViewport from './TreeViewport.vue'
import TreeDropBackground from './TreeDropBackground.vue'
import TreePasteHandler from './TreePasteHandler.vue'
import ContextMenu from '../ContextMenu/ContextMenu.vue'
import FileProperties from '../FileProperties/FileProperties.vue'

import { useDirectoryTree } from '@/composables/useDirectoryTree'
import { useTreeSearch } from '@/composables/useTreeSearch'
import { useContextMenu } from '@/composables/useContextMenu'
import { useFileCrud } from '@/composables/useFileCrud'
import { useFileUtils } from '@/composables/useFileUtils'
import { useKeyboard } from '@/composables/useKeyboard'
import { useFileProperties } from '@/composables/useFileProperties'
import { useOpenWith } from '@/composables/useOpenWith'
import { useDragDrop } from '@/composables/useDragDrop'
import { useTreeDragDrop } from '@/composables/useTreeDragDrop'
import { useTreeSelection } from '@/composables/useTreeSelection'

// New slimming composables
import { useDirectoryTreeOperations } from '@/composables/useDirectoryTreeOperations'
import { useDirectoryTreeDrag } from '@/composables/useDirectoryTreeDrag'
import { useDirectoryTreeContextMenu } from '@/composables/useDirectoryTreeContextMenu'
import { useDirectoryTreeKeyboard } from '@/composables/useDirectoryTreeKeyboard'

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const { basePath } = props

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const isRoot = !props.isSubTree

const { items, loading, loadDirectory, toggleOpen } = useDirectoryTree(props.basePath, isRoot)
const treeSelection = useTreeSelection()

// Provide selection context so deep TreeNode children can access it without prop drilling
provide('treeSelection', treeSelection)

const { createItem, renameItem, deleteItem, deleteItems } = useFileCrud()
const { showInExplorer, copyPath, copyName, duplicateItem, moveItem, copyItem } = useFileUtils()
const { searchQuery, filteredItems } = useTreeSearch(items)
const { contextMenuRef, show: showContextMenu } = useContextMenu()
const { add } = useKeyboard()
const { showProperties, isOpen, currentFile, close } = useFileProperties()
const { openWith } = useOpenWith()

// ====================== DRAG & DROP ======================
const { isDragOver, handleDragOver, handleDragLeave, handleDrop } = useDragDrop()
const treeDrag = useTreeDragDrop()



// Paste logic is now primarily handled inside TreePasteHandler component.
// Keeping a minimal stub here in case direct @paste is still needed on root.

const handleDropFromClipboard = async (targetDir: string, filePaths: string[]) => {
  try {
    const result = await (window as any).electron?.copyFiles?.(targetDir, filePaths)
    return result?.success || false
  } catch (err) {
    console.error('Paste error:', err)
    return false
  }
}



// ====================== ВНУТРЕННИЙ DRAG & DROP (с поддержкой multi) ======================
const handleInternalDrop = async (payload: any) => {
  if (!payload?.sources?.length || !payload?.target) return

  let sources = [...payload.sources]
  const target = payload.target
  const isCopy = payload.isCopy

  // === Smart Multi-Drag ===
  // Если тащим только один элемент, но он входит в текущее выделение > 1 элементов,
  // то тащим всю выделенную группу (очень удобно для пользователя)
  if (sources.length === 1) {
    const single = sources[0]
    const selectedPaths = treeSelection.getSelectedPaths()
    if (selectedPaths.includes(single.path) && selectedPaths.length > 1) {
      // Собираем все выделенные элементы из дерева
      const allSelected = getCurrentlySelectedItems()
      if (allSelected.length > 1) {
        sources = allSelected
      }
    }
  }

  let successCount = 0

  for (const source of sources) {
    if (source.path === target.path) continue

    try {
      let ok = false
      if (isCopy) {
        ok = await copyItem(source.path, target.path)
      } else {
        ok = await moveItem(source.path, target.path)
      }
      if (ok) successCount++
    } catch (err) {
      console.error('Multi drag error on', source.path, err)
    }
  }

  if (successCount > 0) {
    refresh()
    treeSelection.clearSelection()
  }
}

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
  // Просто сбрасываем выделение при клике на пустое место.
  // Больше не открываем проводник — это бесит.
  treeSelection.clearSelection()
}

// ====================== КОНТЕКСТНОЕ МЕНЮ ======================
const onContextMenu = (e: MouseEvent, item: any) => {
  const isClickOnSelected = item && treeSelection.isSelected(item.path)
  const hasMulti = treeSelection.selectedCount.value > 1

  // Selection behavior on right-click (kept here for now as it mutates selection)
  if (item && !isClickOnSelected && !hasMulti) {
    treeSelection.selectSingle(item)
  }

  const currentSelected = operations.getCurrentlySelectedItems()
  const count = currentSelected.length

  const menuItems = buildContextMenuItems({
    item,
    currentSelected,
    count,
    createItem,
    renameItem,
    duplicateItem,
    deleteItem,
    deleteItems,
    showProperties,
    openWith,
    showInExplorer,
    copyPath,
    copyName,
    basePath: props.basePath,
  })

  showContextMenu(e, menuItems)
}

const emitFileClick = (path: string) => emit('file-click', path)
const refresh = () => loadDirectory()

// ====================== NEW OPERATIONS COMPOSABLES (must be after finalItems / refresh) ======================
const dragDropApi = useDragDrop()
const treeDragApi = useTreeDragDrop()

const operations = useDirectoryTreeOperations(items, treeSelection, finalItems as any)

const treeDragHandlers = useDirectoryTreeDrag(
  props,
  treeDragApi,
  dragDropApi,
  (payload: any) => { /* handled in handleInternalDrop below */ },
  refresh
)

const { buildContextMenuItems } = useDirectoryTreeContextMenu()

// Re-export convenient drag handlers for the template
const dragHandlers = {
  isDragOver: treeDragHandlers.isDragOver ?? isDragOver,
  handleDragOver: treeDragHandlers.handleDragOver ?? handleDragOver,
  handleDragLeave: treeDragHandlers.handleDragLeave ?? handleDragLeave,
  handleDrop: treeDragHandlers.handleDrop ?? handleDrop,
}

// Expose helpers from operations (used in keyboard, context, etc.)
const getCurrentlySelectedItems = operations.getCurrentlySelectedItems
const findItemByPath = operations.findItemByPath

// Selection handler delegates to operations
const onSelectItem = (payload: any, legacyEvent?: MouseEvent) => {
  operations.handleSelect(payload, legacyEvent)
}

// Thin root drag handlers (for template binding)
const handleRootDragOver = (e: DragEvent) => treeDragHandlers.handleRootDragOver?.(e)
const handleRootDragLeave = (e: DragEvent) => treeDragHandlers.handleRootDragLeave?.(e)
const handleRootDrop = (e: DragEvent) => treeDragHandlers.handleRootDrop?.(e)

// ====================== КЛАВИАТУРА ======================
const { setupKeyboardShortcuts } = useDirectoryTreeKeyboard(
  {
    treeSelection,
    getCurrentlySelectedItems,
    findItemByPath,
    renameItem,
    deleteItems,
    duplicateItem,
    refresh,
    items,
  },
  add
)

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
  transition: background 0.15s, border 0.15s;
}

.directory-tree-container.drag-over {
  background: #2a2a3a;
  border: 2px dashed #FF5252;
}

/* Viewport styles moved to TreeViewport.vue */
</style>