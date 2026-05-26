<template>
  <div class="editor-tabs" ref="rootEl">
    <!-- Multi-group horizontal layout (VS Code style panes) -->
    <div 
      class="groups-container" 
      :class="{ 'single-group': groups.length === 1 }"
    >
      <template v-for="(group, index) in groups" :key="group.id">
        <TabGroup
          :group-id="group.id"
          :ref="(el: any) => registerGroupRef(group.id, el)"
          @activate-group="onGroupActivated"
          @group-became-empty="onGroupBecameEmpty"
          @split-tab="onSplitTab"
        />

        <!-- Resizable splitter between groups -->
        <div
          v-if="index < groups.length - 1"
          class="editor-splitter"
          @mousedown="startGroupResize(index, $event)"
        />
      </template>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'

import TabGroup from './TabGroup.vue'

import { useTabs, type Tab, type EditorGroup } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'
import { useKeyboard } from '@/composables/useKeyboard'
import { useNotification } from '@/composables/useNotification'

const { 
  groups, 
  activeGroupId, 
  setActiveGroup, 
  openTabInGroup, 
  closeGroup, 
  splitTabToNewGroup, 
  findGroupWithPreview,
  markDirty,
  markClean,
  removeEmptyGroupIfPossible,
  closeTab: closeTabGlobal
} = useTabs()

const { getFileHandler } = useFileType()
const { add: addShortcut } = useKeyboard()
const { success, error } = useNotification()

// Refs for imperative access to individual TabGroup instances (for save, etc.)
const groupRefs = ref<Record<string, any>>({})
const rootEl = ref<HTMLElement | null>(null)

// ====================== GROUP REF REGISTRY ======================
const registerGroupRef = (groupId: string, el: any) => {
  if (el) {
    groupRefs.value[groupId] = el
  } else {
    delete groupRefs.value[groupId]
  }
}

const getActiveGroupComponent = () => {
  return groupRefs.value[activeGroupId.value]
}

// ====================== PUBLIC API (kept compatible for App.vue + useFileOpener) ======================
const openFile = async (rawPath: string) => {
  const filePath = rawPath.replace(/\\+/g, '/').replace(/\/+$/, '')

  // 1. Check if file is already open in ANY group → focus it (VS Code behavior)
  for (const g of groups.value) {
    const existing = g.tabs.find(t => t.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === filePath)
    if (existing) {
      setActiveGroup(g.id)
      g.activeFilePath = existing.filePath
      return
    }
  }

  // 2. Not open anywhere → open in the currently active group
  const targetGroupId = activeGroupId.value
  const handler = await getFileHandler(filePath)

  const newTabData = {
    filePath,
    name: filePath.split(/[/\\]/).pop() || filePath,
    type: handler.type,
    content: handler.props?.modelValue || handler.props?.initialHtml || '',
    language: handler.language,
    isDirty: false
  }

  openTabInGroup(targetGroupId, newTabData)
}

const openPreviewTab = (options: { autoStart?: boolean } = {}) => {
  const previewPath = '::preview::'

  // If preview already exists anywhere, switch to that group + tab
  const existingGroupId = findGroupWithPreview()
  if (existingGroupId) {
    const group = groups.value.find(g => g.id === existingGroupId)
    if (group) {
      const tab = group.tabs.find(t => t.filePath === previewPath)
      if (tab) {
        setActiveGroup(existingGroupId)
        group.activeFilePath = previewPath
        if (options.autoStart && tab.previewOptions) {
          tab.previewOptions.autoStart = true
        }
      }
    }
    return
  }

  // Create new preview tab in the currently active group
  const previewTabData = {
    filePath: previewPath,
    name: 'Game',
    type: 'preview',
    isDirty: false,
    icon: 'game_icon',
    previewOptions: {
      autoStart: !!options.autoStart
    }
  }

  openTabInGroup(activeGroupId.value, previewTabData)
}

// ====================== SAVE (delegates to active TabGroup) ======================
const saveCurrentFile = async () => {
  const activeTabGroup = getActiveGroupComponent()
  if (activeTabGroup?.saveCurrent) {
    await activeTabGroup.saveCurrent()
  } else {
    console.warn('[EditorTabs] No active TabGroup ref for save')
  }
}

// ====================== GROUP ACTIVATION & LIFECYCLE ======================
const onGroupActivated = (groupId: string) => {
  setActiveGroup(groupId)
}

const onGroupBecameEmpty = (groupId: string) => {
  removeEmptyGroupIfPossible(groupId)
}

// ====================== SPLITTING (from TabGroup events or direct) ======================
const onSplitTab = (tab: Tab, sourceGroupId: string) => {
  // Already handled inside TabGroup via composable, but keep hook
  splitTabToNewGroup(sourceGroupId, tab)
}

// ====================== RESIZABLE SPLITTERS (horizontal panes) ======================
let isResizing = false
let resizeLeftIndex = -1
let startX = 0
let startLeftWidth = 0
let startRightWidth = 0
let containerWidth = 0

const startGroupResize = (splitterIndex: number, e: MouseEvent) => {
  isResizing = true
  resizeLeftIndex = splitterIndex
  startX = e.clientX

  const container = rootEl.value?.querySelector('.groups-container') as HTMLElement
  if (!container) return
  containerWidth = container.offsetWidth

  const children = Array.from(container.children).filter(el => el.classList.contains('tab-group')) as HTMLElement[]
  if (children[splitterIndex] && children[splitterIndex + 1]) {
    startLeftWidth = children[splitterIndex].offsetWidth
    startRightWidth = children[splitterIndex + 1].offsetWidth
  }

  document.addEventListener('mousemove', onResizeMove)
  document.addEventListener('mouseup', stopGroupResize, { once: true })
  document.body.style.cursor = 'col-resize'
  document.body.style.userSelect = 'none'
}

const onResizeMove = (e: MouseEvent) => {
  if (!isResizing || resizeLeftIndex < 0) return

  const delta = e.clientX - startX
  const container = rootEl.value?.querySelector('.groups-container') as HTMLElement
  if (!container) return

  const children = Array.from(container.children).filter(el => el.classList.contains('tab-group')) as HTMLElement[]
  const leftPane = children[resizeLeftIndex]
  const rightPane = children[resizeLeftIndex + 1]
  if (!leftPane || !rightPane) return

  let newLeft = startLeftWidth + delta
  let newRight = startRightWidth - delta

  // Enforce minimums
  const min = 220
  if (newLeft < min) {
    const diff = min - newLeft
    newLeft = min
    newRight -= diff
  }
  if (newRight < min) {
    const diff = min - newRight
    newRight = min
    newLeft -= diff
  }

  const total = startLeftWidth + startRightWidth
  leftPane.style.flex = `0 0 ${newLeft}px`
  rightPane.style.flex = `0 0 ${newRight}px`
}

const stopGroupResize = () => {
  isResizing = false
  document.removeEventListener('mousemove', onResizeMove)
  document.body.style.cursor = ''
  document.body.style.userSelect = ''

  // Leave pixel flex-basis (works well enough for now)
}

// ====================== KEYBOARD ======================
onMounted(() => {
  addShortcut('ctrl+s', saveCurrentFile, true)
})

// ====================== EXPOSE (for App.vue + header buttons) ======================
defineExpose({
  openFile,
  openPreviewTab,
  hasUnsavedChanges: () => {
    const { hasUnsavedChanges } = useTabs()
    return hasUnsavedChanges()
  },
  saveAllUnsaved: async () => {
    const { getAllDirtyTabs, markClean } = useTabs()

    const dirtyItems = getAllDirtyTabs()
    if (dirtyItems.length === 0) return true

    for (const item of dirtyItems) {
      const groupComp = groupRefs.value[item.groupId]
      const groupData = groups.value.find(g => g.id === item.groupId)
      if (!groupComp || !groupData) continue

      // Temporarily activate the dirty tab so its editor instance is mounted
      const previousActive = groupData.activeFilePath
      groupData.activeFilePath = item.tab.filePath

      // Wait for the TabGroup to react to the active tab change and mount the editor
      await nextTick()
      await new Promise(r => setTimeout(r, 60))

      try {
        if (typeof groupComp.saveCurrent === 'function') {
          await groupComp.saveCurrent()
        }
      } catch (e) {
        console.warn('[EditorTabs] Failed to save tab during "Save All"', item.tab.filePath, e)
      }

      // Restore previous active tab if it was different
      if (previousActive && previousActive !== item.tab.filePath) {
        groupData.activeFilePath = previousActive
        await nextTick()
      }
    }

    return true
  }
})
</script>

<style scoped>
.editor-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
  user-select: none;
}

.groups-container {
  display: flex;
  flex-direction: row;
  height: 100%;
  overflow: hidden;
  flex: 1;
  min-height: 0;
}

.groups-container.single-group {
  /* single pane takes full space */
}

.editor-splitter {
  width: 6px;
  background: #252526;
  cursor: col-resize;
  flex-shrink: 0;
  z-index: 10;
  transition: background 0.15s ease;
}

.editor-splitter:hover {
  background: #FF5252;
}

/* TabGroup children participate in flex layout */
.groups-container > .tab-group {
  flex: 1 1 0;
  min-width: 220px;
  height: 100%;
}
</style>
