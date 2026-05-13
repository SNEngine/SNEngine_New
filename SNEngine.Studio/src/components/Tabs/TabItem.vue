<template>
  <div 
    class="tab"
    :class="{ active: isActive }"
    @click="$emit('activate', tab)"
    @contextmenu.prevent="$emit('context-menu', $event, tab)"
  >
    <BaseIcon :name="getTabIconName(tab)" class="tab-icon" />
    <span class="tab-name">
      {{ tab.name }}
      <span v-if="tab.isDirty" class="dirty-indicator">*</span>
    </span>
    <span class="tab-close" @click.stop="$emit('close', tab)">✕</span>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'

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
}>()

defineEmits<{
  (e: 'activate', tab: Tab): void
  (e: 'close', tab: Tab): void
  (e: 'context-menu', event: MouseEvent, tab: Tab): void
}>()

const getTabIconName = (tab: Tab) => {
  const ext = tab.filePath.split('.').pop()?.toLowerCase() || ''
  if (['png', 'jpg', 'jpeg', 'gif', 'webp'].includes(ext)) return 'image_icon'
  if (['mp3', 'wav', 'ogg', 'm4a'].includes(ext)) return 'audio_icon'
  if (['html', 'htm'].includes(ext)) return 'html_icon'
  if (ext === 'sn') return 'sn_script_icon'
  if (ext === 'cs') return 'csharp_icon'
  return 'unknown_icon'
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
</style>