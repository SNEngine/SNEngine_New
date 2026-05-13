<template>
  <div 
    class="tabs-bar" 
    v-if="tabs.length > 0" 
    @wheel="handleWheel"
  >
    <TabItem
      v-for="tab in tabs"
      :key="tab.id"
      :tab="tab"
      :is-active="tab.filePath === activeFilePath"
      @activate="$emit('activate', $event)"
      @close="$emit('close', $event)"
      @context-menu="handleContextMenu"
    />
  </div>
</template>

<script setup lang="ts">
import TabItem from './TabItem.vue'

interface Tab {
  id: string
  filePath: string
  name: string
  type?: string
  isDirty?: boolean
}

const props = defineProps<{
  tabs: Tab[]
  activeFilePath: string | null
}>()

const emit = defineEmits<{
  (e: 'activate', tab: Tab): void
  (e: 'close', tab: Tab): void
  (e: 'context-menu', event: MouseEvent, tab: Tab): void
}>()

const handleWheel = (e: WheelEvent) => {
  (e.currentTarget as HTMLElement).scrollLeft += e.deltaY
  e.preventDefault()
}

const handleContextMenu = (event: MouseEvent, tab: Tab) => {
  emit('context-menu', event, tab)
}
</script>

<style scoped>
.tabs-bar {
  display: flex;
  background: #252526;
  overflow-x: auto;
  overflow-y: hidden;
  height: 35px;
  scrollbar-width: thin;
  scrollbar-color: #3e3e3e transparent;
}

.tabs-bar::-webkit-scrollbar {
  height: 3px;
}

.tabs-bar::-webkit-scrollbar-track {
  background: transparent;
}

.tabs-bar::-webkit-scrollbar-thumb {
  background: #3e3e3e;
  border-radius: 10px;
}

.tabs-bar:hover::-webkit-scrollbar-thumb {
  background: #4f4f4f;
}

.tabs-bar::-webkit-scrollbar-thumb:hover {
  background: #ff5252;
}
</style>