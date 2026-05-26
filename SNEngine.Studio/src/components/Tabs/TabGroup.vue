<template>
  <div 
    class="tab-group"
    :class="{ 'is-active': isActive, 'drop-target-group': isDropTarget }"
    @mousedown="onPaneMouseDown"
    @dragenter="onGroupDragEnter"
    @dragover.prevent="onGroupDragOver"
    @dragleave="onGroupDragLeave"
    @drop="onGroupDrop"
  >
    <!-- Drop zone for drag & drop tabs between panes (and reordering) -->
    <div
      class="tab-bar-dropzone"
      :class="{ 'drop-target': isDropTarget }"
      @dragenter="onDragEnter"
      @dragover.prevent="onDragOver"
      @dragleave="onDragLeave"
      @drop="onDrop"
    >
      <TabBar
        :tabs="group?.tabs || []"
        :active-file-path="group?.activeFilePath || null"
        :group-id="props.groupId"
        @activate="activateTabInGroup"
        @close="closeTabInGroup"
        @context-menu="showTabContextMenu"
        @drag-start="onTabDragStart"
        @drag-end="onTabDragEnd"
      />
    </div>

    <TabContent
      :current-component="currentComponent"
      :current-props="currentProps"
      :is-editable="currentHandler?.isEditable ?? false"
      ref="tabContentRef"
      @update:model-value="handleContentUpdate"
      @save="handleComponentSave"
      @close-tab="handleCloseActiveTab"
    />

    <ContextMenu ref="contextMenuRef" />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { lastUpdate } from '@/utils/watcherState'

import ContextMenu from '../ContextMenu/ContextMenu.vue'
import TabBar from './TabBar.vue'
import TabContent from './TabContent.vue'
import DeletedFile from '../DeletedFile/DeletedFile.vue'
import GamePreview from '../GamePreview/GamePreview.vue'

import { useTabs, type Tab, type EditorGroup } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'
import { useFileSave } from '@/composables/useFileSave'
import { useNotification } from '@/composables/useNotification'

const props = defineProps<{
  groupId: string
}>()

const emit = defineEmits<{
  (e: 'activate-group', groupId: string): void
  (e: 'group-became-empty', groupId: string): void
  (e: 'split-tab', tab: Tab, sourceGroupId: string): void
}>()

const { 
  groups,
  activeGroupId,
  setActiveGroup,
  closeTab: closeTabGlobal,
  splitTabToNewGroup,
  moveTabToGroup,
  markDirty,
  markClean
} = useTabs()

const { getFileHandler } = useFileType()
const { saveFile, getContentFromEditor } = useFileSave()
const { success, error } = useNotification()

const contextMenuRef = ref<any>(null)
const tabContentRef = ref<any>(null)

const currentHandler = ref<any>(null)

// ====================== DRAG & DROP STATE ======================
const isDropTarget = ref(false)
let dragCounter = 0  // to handle nested dragenter/leave correctly

const onTabDragStart = (_tab: Tab, _gid: string | undefined, _e: DragEvent) => {
  // Could add global "dragging tab" visual if needed later
}

const onTabDragEnd = (_tab: Tab, _e: DragEvent) => {
  isDropTarget.value = false
  dragCounter = 0
}

const onDragOver = (e: DragEvent) => {
  // Only accept our custom tab payload
  if (e.dataTransfer?.types.includes('application/x-snengine-tab')) {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    isDropTarget.value = true
  }
}

const onDragEnter = (_e: DragEvent) => {
  dragCounter++
  isDropTarget.value = true
}

const onDragLeave = (_e: DragEvent) => {
  dragCounter--
  if (dragCounter <= 0) {
    isDropTarget.value = false
    dragCounter = 0
  }
}

const onDrop = (e: DragEvent) => {
  isDropTarget.value = false
  dragCounter = 0
  e.stopPropagation()   // prevent the full-pane drop handler from also firing

  const payloadRaw = e.dataTransfer?.getData('application/x-snengine-tab')
  if (!payloadRaw) return

  try {
    const payload = JSON.parse(payloadRaw) as { tabId: string; filePath: string; fromGroupId: string }

    if (payload.fromGroupId === props.groupId) {
      moveTabToGroup({ id: payload.tabId, filePath: payload.filePath }, payload.fromGroupId, props.groupId)
    } else {
      moveTabToGroup({ id: payload.tabId, filePath: payload.filePath }, payload.fromGroupId, props.groupId)
    }
  } catch (err) {
    console.warn('[TabGroup] Failed to parse dropped tab payload (tab-bar zone)', err)
  }
}

// ====================== FULL-PANE DROP TARGET (much larger & easier to hit) ======================
const onGroupDragEnter = (_e: DragEvent) => {
  if (_e.dataTransfer?.types.includes('application/x-snengine-tab')) {
    dragCounter++
    isDropTarget.value = true
  }
}

const onGroupDragOver = (e: DragEvent) => {
  if (e.dataTransfer?.types.includes('application/x-snengine-tab')) {
    e.preventDefault()
    e.dataTransfer.dropEffect = 'move'
    isDropTarget.value = true
  }
}

const onGroupDragLeave = (_e: DragEvent) => {
  dragCounter--
  if (dragCounter <= 0) {
    isDropTarget.value = false
    dragCounter = 0
  }
}

const onGroupDrop = (e: DragEvent) => {
  isDropTarget.value = false
  dragCounter = 0

  const payloadRaw = e.dataTransfer?.getData('application/x-snengine-tab')
  if (!payloadRaw) return

  try {
    const payload = JSON.parse(payloadRaw) as { tabId: string; filePath: string; fromGroupId: string }

    if (payload.fromGroupId !== props.groupId) {
      moveTabToGroup(payload.tabId, payload.fromGroupId, props.groupId)
    } else {
      // Same group — simple "move to end" for now
      moveTabToGroup(payload.tabId, payload.fromGroupId, props.groupId)
    }
  } catch (err) {
    console.warn('[TabGroup] Failed to parse dropped tab payload (full pane)', err)
  }
}

const currentComponent = computed(() => currentHandler.value?.component || null)
const currentProps = computed(() => currentHandler.value?.props || {})

const group = computed<EditorGroup | undefined>(() => 
  groups.value.find(g => g.id === props.groupId)
)

const isActive = computed(() => activeGroupId.value === props.groupId)

// ====================== GROUP ACTIVATION ======================
const onPaneMouseDown = () => {
  if (activeGroupId.value !== props.groupId) {
    setActiveGroup(props.groupId)
    emit('activate-group', props.groupId)
  }
}

// ====================== ОБРАБОТКА УДАЛЕНИЯ ФАЙЛОВ (watcher) ======================
watch(lastUpdate, (update) => {
  if (!update) return
  const g = group.value
  if (!g) return

  const activePath = g.activeFilePath
  if (!activePath) return

  const normalizedActive = String(activePath).replace(/\\+/g, '/').replace(/\/+$/, '')
  const normalizedUpdate = String(update.path || '').replace(/\\+/g, '/').replace(/\/+$/, '')

  if (normalizedActive === normalizedUpdate && 
      (update.type === 'unlink' || update.type === 'unlinkDir')) {
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedActive,
        isDeleted: true 
      }
    }
  }
})

// ====================== СМЕНА АКТИВНОЙ ВКЛАДКИ ВНУТРИ ГРУППЫ ======================
watch(() => group.value?.activeFilePath, async (newPath) => {
  if (!newPath) {
    currentHandler.value = null
    return
  }

  const normalizedNew = newPath.replace(/\\+/g, '/').replace(/\/+$/, '')

  // Специальная обработка для Game Preview
  if (newPath === '::preview::') {
    const activeTab = group.value?.tabs.find(t => t.filePath === '::preview::')
    const autoStart = activeTab?.previewOptions?.autoStart ?? false

    if (activeTab?.previewOptions) {
      activeTab.previewOptions.autoStart = false
    }

    currentHandler.value = {
      component: GamePreview,
      props: { 
        projectPath: 'C:/Users/Siphome/Desktop/testBuild',
        autoStart
      },
      isEditable: false
    }
    return
  }

  const tab = group.value?.tabs.find(t => t.filePath.replace(/\\+/g, '/').replace(/\/+$/, '') === normalizedNew)
  if (tab?.isDeleted) {
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedNew,
        isDeleted: true 
      }
    }
    return
  }

  try {
    const handler = await getFileHandler(newPath)
    currentHandler.value = handler
  } catch (err) {
    console.warn('[TabGroup] File handler error:', err)
    currentHandler.value = {
      component: DeletedFile,
      props: { 
        filePath: normalizedNew,
        isDeleted: true 
      }
    }
  }
}, { immediate: true })

// ====================== АКТИВАЦИЯ И ЗАКРЫТИЕ ВКЛАДОК ======================
const activateTabInGroup = (tab: Tab) => {
  const g = group.value
  if (g) {
    g.activeFilePath = tab.filePath
    setActiveGroup(props.groupId)
    emit('activate-group', props.groupId)
  }
}

const closeTabInGroup = (tab: Tab) => {
  closeTabGlobal(tab, props.groupId)
  // notify parent if this group is now empty
  if ((group.value?.tabs.length || 0) === 0) {
    emit('group-became-empty', props.groupId)
  }
}

// ====================== СОХРАНЕНИЕ (per-pane) ======================
const saveCurrent = async () => {
  const g = group.value
  if (!g) return

  const activeTab = g.tabs.find(t => t.filePath === g.activeFilePath)
  if (!activeTab || activeTab.isDeleted) return

  if (!['code', 'web'].includes(activeTab.type)) return

  const editor = tabContentRef.value?.activeEditorRef
  let content = getContentFromEditor(editor, activeTab.content || '')

  if (!content && activeTab.content) content = activeTab.content

  const result = await saveFile(activeTab.filePath, content)

  if (result.success) {
    activeTab.content = content
    markClean(activeTab.filePath)
    success(`Файл сохранён`, activeTab.name)
  } else {
    error(`Не удалось сохранить файл`, activeTab.name)
  }
}

const handleContentUpdate = (newContent: string) => {
  const g = group.value
  if (!g) return
  const tab = g.tabs.find(t => t.filePath === g.activeFilePath)
  if (tab) {
    tab.content = newContent
    markDirty(tab.filePath)
  }
}

const handleComponentSave = () => saveCurrent()

const handleCloseActiveTab = () => {
  const g = group.value
  if (!g || !g.activeFilePath) return
  const activeTab = g.tabs.find(t => t.filePath === g.activeFilePath)
  if (activeTab) closeTabInGroup(activeTab)
}

// ====================== КОНТЕКСТНОЕ МЕНЮ (с разделением!) ======================
const showTabContextMenu = (e: MouseEvent, tab: Tab) => {
  const g = group.value
  if (!g) return

  const i = g.tabs.findIndex(t => t.id === tab.id)
  const isSingleGroup = groups.value.length === 1

  const menu = [
    { label: 'Закрыть вкладку', action: () => closeTabInGroup(tab) },
    { label: 'Закрыть другие', action: () => closeOtherTabs(tab) },
    { label: 'Закрыть справа', action: () => closeTabsToRight(i) },
    { label: 'Закрыть слева', action: () => closeTabsToLeft(i) },
    { type: 'separator' },
    { label: 'Разделить вправо', action: () => splitRight(tab) },
    { type: 'separator' },
    { label: 'Закрыть все', action: () => closeAllInGroup() },
    { label: 'Закрыть группу', action: () => closeThisGroup(), disabled: isSingleGroup }
  ]

  contextMenuRef.value?.show(e.clientX, e.clientY, menu)
}

const closeOtherTabs = (tab: Tab) => {
  const g = group.value
  if (!g) return
  g.tabs.filter(t => t.id !== tab.id).forEach(t => closeTabInGroup(t))
}

const closeTabsToRight = (i: number) => {
  const g = group.value
  if (!g) return
  g.tabs.slice(i + 1).forEach(t => closeTabInGroup(t))
}

const closeTabsToLeft = (i: number) => {
  const g = group.value
  if (!g) return
  g.tabs.slice(0, i).forEach(t => closeTabInGroup(t))
}

const closeAllInGroup = () => {
  const g = group.value
  if (!g) return
  while (g.tabs.length) {
    closeTabInGroup(g.tabs[0])
  }
}

const closeThisGroup = () => {
  // Close all tabs then let parent prune via events
  closeAllInGroup()
  emit('group-became-empty', props.groupId)
}

const splitRight = (tab: Tab) => {
  splitTabToNewGroup(props.groupId, tab)
  // Parent (EditorTabs) will react to groups change and render new TabGroup
}

// ====================== EXPOSE (save for active group) ======================
defineExpose({
  saveCurrent
})
</script>

<style scoped>
.tab-group {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
  min-width: 220px;
  flex: 1 1 0;
  border-right: 1px solid #252526;
}

.tab-group:last-child {
  border-right: none;
}

.tab-group.is-active {
  /* Optional ring or stronger border for active pane */
  box-shadow: inset 0 0 0 1px #3a3a3a;
}

.tab-bar-dropzone {
  position: relative;
  transition: background-color 0.1s ease;
}

.tab-bar-dropzone.drop-target {
  background: rgba(255, 82, 82, 0.12);
  outline: 2px dashed #ff5252;
  outline-offset: -4px;
  border-radius: 2px;
}

/* Full pane drop highlight (when dragging over the editor content area too) */
.tab-group.drop-target-group {
  background: #1a1a1a;
  outline: 2px dashed #ff5252;
  outline-offset: -4px;
}

.tab-group.drop-target-group .tab-bar-dropzone {
  background: rgba(255, 82, 82, 0.08);
}
</style>
