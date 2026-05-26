<template>
  <div 
    class="tab"
    :class="{ active: isActive }"
    draggable="true"
    @click="$emit('activate', tab)"
    @contextmenu.prevent="$emit('context-menu', $event, tab)"
    @dragstart="onDragStart"
    @dragend="onDragEnd"
  >
    <BaseIcon 
      :name="getTabIconName(tab)" 
      class="tab-icon" 
    />
    <span class="tab-name">
      {{ tab.name }}
      <span v-if="tab.isDirty" class="dirty-indicator">*</span>
    </span>
    <span class="tab-close" @click.stop="$emit('close', tab)">✕</span>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'
import { getFileIcon } from '@/config/icons.config'

interface Tab {
  id: string
  filePath: string
  name: string
  type?: string
  isDirty?: boolean
}

const props = defineProps<{
  tab: Tab
  isActive: boolean
  groupId?: string
}>()

const emit = defineEmits<{
  (e: 'activate', tab: Tab): void
  (e: 'close', tab: Tab): void
  (e: 'context-menu', event: MouseEvent, tab: Tab): void
  (e: 'drag-start', tab: Tab, groupId: string | undefined, event: DragEvent): void
  (e: 'drag-end', tab: Tab, event: DragEvent): void
}>()

// Теперь используем единую функцию из icons.config.ts
const getTabIconName = (tab: Tab): string => {
  if (tab.filePath === '::preview::' || tab.type === 'preview') {
    return 'game_icon'
  }
  if (!tab.filePath) return 'unknown_icon'
  return getFileIcon(tab.filePath)
}

const onDragStart = (e: DragEvent) => {
  if (!props.groupId) return

  // Use a custom MIME type so only our tab system accepts the drop
  const payload = JSON.stringify({
    tabId: props.tab.id,
    filePath: props.tab.filePath,
    fromGroupId: props.groupId
  })

  e.dataTransfer?.setData('application/x-snengine-tab', payload)
  // Also set plain text fallback
  e.dataTransfer?.setData('text/plain', props.tab.name)

  // Mark as internal tab drag
  if (e.dataTransfer) e.dataTransfer.effectAllowed = 'move'

  emit('drag-start', props.tab, props.groupId, e)
  // Add a class for visual feedback on the dragged element
  ;(e.currentTarget as HTMLElement)?.classList.add('dragging')
}

const onDragEnd = (e: DragEvent) => {
  ;(e.currentTarget as HTMLElement)?.classList.remove('dragging')
  emit('drag-end', props.tab, e)
}
</script>

<style scoped>
.tab {
  flex-shrink: 0;
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 0 10px;
  height: 100%;
  background: #2d2d2d;
  color: #969696;
  border-right: 1px solid #1e1e1e;
  cursor: pointer;
  font-size: 13px;
  position: relative;
  transition: background 0.2s, color 0.2s;
  min-width: 100px;
  max-width: 200px;
}

.tab:hover {
  background: #323233;
  color: #cccccc;
}

.tab.active {
  background: #1e1e1e;
  color: #ff5252;
}

.tab.active::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 2px;
  background: #ff5252;
}

.tab-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.tab-name {
  flex: 1;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  display: flex;
  align-items: center;
  gap: 2px;
}

.tab-close {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 18px;
  height: 18px;
  border-radius: 4px;
  font-size: 10px;
  opacity: 0;
  margin-left: 4px;
  transition: opacity 0.2s, background 0.2s;
}

.tab:hover .tab-close,
.tab.active .tab-close {
  opacity: 0.6;
}

.tab-close:hover {
  background: #454545;
  color: white;
  opacity: 1 !important;
}

.dirty-indicator {
  color: #ff5252;
  font-weight: bold;
  font-size: 14px;
  line-height: 1;
}

/* Drag & Drop visual feedback */
.tab.dragging {
  opacity: 0.4;
  background: #3a3a3a;
  border: 1px dashed #ff5252;
}
</style>