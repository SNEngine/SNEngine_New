<template>
  <div
    class="tree-drop-background"
    :class="{ 'drag-over': isDragOver }"
    @drop.prevent="handleDrop"
    @dragover.prevent="handleDragOver"
    @dragleave="handleDragLeave"
    @click="handleBackgroundClick"
  >
    <slot />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  basePath: string
  treeSelection?: any
  handleRootDrop?: (e: DragEvent) => void
  handleRootDragOver?: (e: DragEvent) => void
  handleRootDragLeave?: (e: DragEvent) => void
}>()

const emit = defineEmits<{
  (e: 'background-click'): void
  (e: 'drop', event: DragEvent): void
}>()

const isDragOver = ref(false)

function handleDragOver(e: DragEvent) {
  isDragOver.value = true
  props.handleRootDragOver?.(e)
}

function handleDragLeave(e: DragEvent) {
  isDragOver.value = false
  props.handleRootDragLeave?.(e)
}

function handleDrop(e: DragEvent) {
  console.log('[DragDebug] TreeDropBackground: drop event received on background');
  isDragOver.value = false
  props.handleRootDrop?.(e)
  emit('drop', e)
}

function handleBackgroundClick(e: MouseEvent) {
  // Просто сообщаем родителю, что кликнули по фону (пустому месту).
  // Родитель сам решит, что делать (сейчас — только сброс выделения).
  if (!(e.target as HTMLElement).closest('.tree-node')) {
    emit('background-click')
  }
}

</script>

<style scoped>
.tree-drop-background {
  flex: 1;
  position: relative;
}

.tree-drop-background.drag-over {
  background: #2a2a3a;
  border: 2px dashed #FF5252;
}
</style>
