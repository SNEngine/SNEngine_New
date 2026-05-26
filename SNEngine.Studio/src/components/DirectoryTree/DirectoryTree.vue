<template>
  <div 
    class="directory-tree-container"
    :class="{ 'drag-over': isDragOver }"
    @contextmenu.prevent="onContextMenu($event, null)"
    @click="handleViewportClick"
    @drop.prevent="handleRootDrop"
    @dragover.prevent="handleRootDragOver"
    @dragleave="handleRootDragLeave"
    @paste="handlePaste"
  >
    <TreeHeader
      v-if="isRoot"
      v-model="searchQuery"
      v-model:sortField="sortField"
      v-model:sortOrder="sortOrder"
      @refresh="refresh"
    />

    <div class="tree-viewport">
      <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>

      <div class="tree-content">
        <TreeNode
          v-for="item in finalItems"
          :key="item.path"
          :item="item"
          :active-path="activePath"
          :search-query="searchQuery"
          :is-selected="treeSelection.isSelected(item.path)"
          :drag-handlers="dragHandlers"
          :tree-drag="treeDrag"
          :on-select="onSelectItem"
          @toggle="toggleOpen"
          @file-click="emitFileClick"
          @contextmenu="onContextMenu"
          @select="onSelectItem"
          @internal-drop="handleInternalDrop"
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
import { ref, watch, computed, provide } from 'vue'
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
import { useOpenWith } from '@/composables/useOpenWith'
import { useDragDrop } from '@/composables/useDragDrop'
import { useTreeDragDrop } from '@/composables/useTreeDragDrop'
import { useTreeSelection } from '@/composables/useTreeSelection'

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

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

const dragHandlers = {
  isDragOver,
  handleDragOver,
  handleDragLeave,
  handleDrop
}

// ====================== ВЫБОР ЭЛЕМЕНТА (MULTI-SELECTION) ======================

/**
 * Returns a flat, ordered list of all currently visible items in the tree.
 * Respects opened folders and the current search filter.
 */
function getVisibleItemsFlat(): any[] {
  const result: any[] = []

  const traverse = (items: any[]) => {
    for (const item of items) {
      result.push(item)
      // Only descend if the folder is open and has children
      if (item.isFolder && item.isOpen && item.children && item.children.length > 0) {
        traverse(item.children)
      }
    }
  }

  traverse(finalItems.value)
  return result
}

/**
 * Unified selection handler.
 * Accepts both the new structured payload { item, modifiers } (recommended)
 * and the legacy (item, event) format for compatibility.
 */
const onSelectItem = (payload: any, legacyEvent?: MouseEvent) => {
  // Normalize payload
  let item: any
  let modifiers: any = {}

  if (payload && typeof payload === 'object' && 'item' in payload) {
    // New structured format (works reliably from any depth)
    item = payload.item
    modifiers = payload.modifiers || {}
  } else {
    // Legacy format
    item = payload
    if (legacyEvent) {
      modifiers = {
        ctrlKey: legacyEvent.ctrlKey || legacyEvent.metaKey,
        shiftKey: legacyEvent.shiftKey,
        metaKey: legacyEvent.metaKey,
      }
    }
  }

  if (!item) return

  const isCtrlOrCmd = modifiers.ctrlKey || modifiers.metaKey
  const isShift = modifiers.shiftKey

  if (isShift && treeSelection.anchorPath.value) {
    // Range selection (Shift + Click)
    const visibleItems = getVisibleItemsFlat()
    treeSelection.selectRange(item, visibleItems)
  } else if (isCtrlOrCmd) {
    // Toggle selection (Ctrl/Cmd + Click)
    treeSelection.toggleSelection(item)
  } else {
    // Normal single selection
    treeSelection.selectSingle(item)
  }
}

// ====================== PASTE (Ctrl + V) ======================
const handlePaste = async (e: ClipboardEvent) => {
  // 1. Файлы из внешнего источника (Проводник)
  const files = Array.from(e.clipboardData?.files || [])
  if (files.length > 0) {
    const lastSelected = getCurrentlySelectedItems().find(i => i.isFolder)
    const targetPath = lastSelected?.path || props.basePath
    const filePaths = files.map((f: any) => (window as any).electron.getFilePath(f)).filter(Boolean)

    if (filePaths.length > 0) {
      const success = await handleDropFromClipboard(targetPath, filePaths)
      if (success) refresh()
    }
    return
  }

  // 2. Текст из буфера (Ctrl+C внутри студии) — считаем, что это путь
  const text = e.clipboardData?.getData('text/plain')?.trim()
  if (text && text.startsWith(props.basePath)) {
    const lastSelected = getCurrentlySelectedItems().find(i => i.isFolder)
    const targetPath = lastSelected?.path || props.basePath
    const success = await copyItem(text, targetPath)
    if (success) refresh()
  }
}

const handleDropFromClipboard = async (targetDir: string, filePaths: string[]) => {
  try {
    const result = await (window as any).electron?.copyFiles?.(targetDir, filePaths)
    return result?.success || false
  } catch (err) {
    console.error('Paste error:', err)
    return false
  }
}

// ====================== ДРОП В КОРЕНЬ ======================
const handleRootDragOver = (e: DragEvent) => {
  if (treeDrag.draggedItems?.value?.length > 0) {
    e.preventDefault()
    e.dataTransfer!.dropEffect = e.ctrlKey ? 'copy' : 'move'
  } else {
    handleDragOver(e)
  }
}

const handleRootDragLeave = (e: DragEvent) => {
  handleDragLeave(e)
}

const handleRootDrop = (e: DragEvent) => {
  if (treeDrag.draggedItems?.value?.length > 0) {
    const payload = treeDrag.onDrop({ path: props.basePath, isFolder: true }, e)
    if (payload) handleInternalDrop(payload)
    return
  }
  handleDrop(e, props.basePath, refresh)
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
  if (!(e.target as HTMLElement).closest('.tree-node')) {
    // Unify behavior: if something is selected (especially a folder), prefer showing that
    const selected = getCurrentlySelectedItems()
    const targetFolder = selected.find(i => i.isFolder) || selected[0]
    const targetPath = targetFolder?.path || props.basePath
    showInExplorer(targetPath)
  }
  treeSelection.clearSelection()
}

// ====================== КОНТЕКСТНОЕ МЕНЮ (с поддержкой multi-selection) ======================
const onContextMenu = (e: MouseEvent, item: any) => {
  const isClickOnSelected = item && treeSelection.isSelected(item.path)
  const hasMultiSelection = treeSelection.selectedCount.value > 1

  // Улучшенное поведение:
  // - Если уже есть множественное выделение и кликнули на элемент внутри него → не сбрасываем.
  // - Если кликнули вне выделения и нет мульти → тогда выбираем только этот элемент.
  // Это решает жалобу "при пкм сбрасывается множественное".
  if (item && !isClickOnSelected && !hasMultiSelection) {
    treeSelection.selectSingle(item)
  } else if (item && !isClickOnSelected && hasMultiSelection) {
    // Кликнули правой кнопкой на элемент вне текущего мульти-выделения.
    // В большинстве случаев пользователь хочет работать именно с этим элементом.
    // Но чтобы не бесить при bulk-работе, мы НЕ сбрасываем автоматически.
    // Можно раскомментировать следующую строку, если хочешь более агрессивное поведение:
    // treeSelection.selectSingle(item)
  }

  const currentSelected = getCurrentlySelectedItems()
  const count = currentSelected.length

  // Background (null) or item-specific context menu
  if (!item) {
    // === Background click (empty space in this tree view) ===
    // Unify with nested folders: prefer a selected folder as target if available.
    const selected = getCurrentlySelectedItems()
    const targetFolder = selected.find(i => i.isFolder) || selected[0]
    const target = targetFolder?.isFolder ? targetFolder.path : props.basePath

    const bgItems = [
      { label: 'Создать файл', icon: 'file_icon', action: () => createItem(target, false) },
      { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(target, true) },
      { type: 'separator' },
      { label: 'Открыть в проводнике', icon: 'explorer_icon', action: () => showInExplorer(target) },
    ]
    showContextMenu(e, bgItems)
    return
  }

  // === Clicked on a specific item ===
  const menuItems = [
    { label: 'Создать файл', icon: 'file_icon', action: () => createItem(props.basePath, false, item) },
    { label: 'Создать папку', icon: 'folder_icon', action: () => createItem(props.basePath, true, item) },
  ]

  if (count > 1) {
    // Multi-selection context menu
    menuItems.push(
      { type: 'separator' },
      { label: `Удалить ${count} элементов`, icon: 'error_icon', action: () => deleteItems(currentSelected), danger: true },
      { label: `Дублировать ${count} элементов`, icon: 'copy_icon', action: () => currentSelected.forEach(it => duplicateItem(it.path)) },
      { type: 'separator' },
      { label: 'Копировать пути', icon: 'info_icon', action: () => {
          navigator.clipboard.writeText(currentSelected.map(i => i.path).join('\n'))
        } 
      }
    )
  } else {
    // Single item actions
    menuItems.push(
      { type: 'separator' },
      { label: 'Переименовать', icon: 'edit_icon', action: () => renameItem(item) },
      { label: 'Дублировать', icon: 'copy_icon', action: () => duplicateItem(item.path) },
      { label: 'Свойства', icon: 'info_icon', action: () => showProperties(item) },
      { label: 'Открыть с помощью...', icon: 'open_with_icon', action: () => openWith(item.path) },
      { label: 'Удалить', icon: 'error_icon', action: () => deleteItem(item), danger: true },
      { type: 'separator' },
      { label: 'Показать в проводнике', icon: 'explorer_icon', action: () => showInExplorer(item.path) },
      { label: 'Копировать путь', icon: 'info_icon', action: () => copyPath(item.path) },
      { label: 'Копировать имя', icon: 'info_icon', action: () => copyName(item.path) }
    )
  }

  showContextMenu(e, menuItems)
}

const emitFileClick = (path: string) => emit('file-click', path)
const refresh = () => loadDirectory()

// ====================== КЛАВИАТУРА (с поддержкой multi-selection) ======================
const setupKeyboardShortcuts = () => {
  add('f2', () => {
    const paths = treeSelection.getSelectedPaths()
    if (paths.length === 1) {
      const item = findItemByPath(paths[0])
      if (item) renameItem(item)
    }
  })

  add('delete', () => {
    const selected = getCurrentlySelectedItems()
    if (selected.length > 0) {
      deleteItems(selected)
    }
  })

  add('ctrl+d', () => {
    const selected = getCurrentlySelectedItems()
    selected.forEach(it => duplicateItem(it.path))
  })

  add('ctrl+c', () => {
    const paths = treeSelection.getSelectedPaths()
    if (paths.length > 0) {
      navigator.clipboard.writeText(paths.join('\n'))
    }
  })

  add('ctrl+shift+c', () => {
    // Copy names of selected
    const selected = getCurrentlySelectedItems()
    if (selected.length > 0) {
      const names = selected.map(i => i.name).join('\n')
      navigator.clipboard.writeText(names)
    }
  })

  add('f5', () => refresh())  // allow refresh on sub-trees too for consistency

  // Clear selection
  add('escape', () => {
    if (treeSelection.hasSelection.value) {
      treeSelection.clearSelection()
    }
  })

  // Select all visible items (basic version)
  add('ctrl+a', (e) => {
    e.preventDefault()
    const allVisible: any[] = []
    const collect = (list: any[]) => {
      for (const it of list) {
        allVisible.push(it)
        if (it.isOpen && it.children) collect(it.children)
      }
    }
    collect(items.value)
    if (allVisible.length > 0) {
      treeSelection.selectPaths(allVisible.map(i => i.path))
    }
  })
}

// Helper to get currently selected TreeItem objects from the tree data
function getCurrentlySelectedItems(): any[] {
  const paths = treeSelection.getSelectedPaths()
  const result: any[] = []
  const find = (list: any[]) => {
    for (const it of list) {
      if (paths.includes(it.path)) result.push(it)
      if (it.children) find(it.children)
    }
  }
  find(items.value)
  return result
}

function findItemByPath(path: string): any | null {
  let found: any = null
  const search = (list: any[]) => {
    for (const it of list) {
      if (it.path === path) { found = it; return }
      if (it.children) search(it.children)
    }
  }
  search(items.value)
  return found
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
  transition: background 0.15s, border 0.15s;
}

.directory-tree-container.drag-over {
  background: #2a2a3a;
  border: 2px dashed #FF5252;
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