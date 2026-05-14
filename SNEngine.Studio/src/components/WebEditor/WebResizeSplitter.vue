=<template>
  <div 
    class="splitter" 
    @mousedown="startDragging"
  >
    <div class="splitter-line"></div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const props = defineProps<{
  codeWidth: number
}>()

const emit = defineEmits<{
  (e: 'update:codeWidth', value: number): void
}>()

const isDragging = ref(false)

const startDragging = (e: MouseEvent) => {
  isDragging.value = true
  document.addEventListener('mousemove', onMouseMove)
  document.addEventListener('mouseup', stopDragging)
  document.body.style.cursor = 'col-resize'
  e.preventDefault()
}

const onMouseMove = (e: MouseEvent) => {
  if (!isDragging.value) return

  // Получаем контейнер (родительский .web-content)
  const container = (e.currentTarget as HTMLElement).parentElement
  if (!container) return

  const rect = container.getBoundingClientRect()
  const offset = e.clientX - rect.left
  let percentage = Math.round((offset / rect.width) * 100)

  // Ограничиваем диапазон
  percentage = Math.max(15, Math.min(85, percentage))

  emit('update:codeWidth', percentage)
}

const stopDragging = () => {
  isDragging.value = false
  document.removeEventListener('mousemove', onMouseMove)
  document.removeEventListener('mouseup', stopDragging)
  document.body.style.cursor = 'default'
}
</script>

<style scoped>
.splitter {
  width: 6px;
  background: #252526;
  cursor: col-resize;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s ease;
  z-index: 20;
  position: relative;
  flex-shrink: 0;
}

.splitter:hover {
  background: #FF5252;
}

.splitter-line {
  width: 2px;
  height: 120px;
  background: #444;
  border-radius: 2px;
  opacity: 0.6;
  transition: opacity 0.2s;
}

.splitter:hover .splitter-line {
  opacity: 1;
  background: #FF5252;
}
</style>