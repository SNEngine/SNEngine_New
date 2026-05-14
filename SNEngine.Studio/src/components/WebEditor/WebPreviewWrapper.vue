<template>
  <div class="web-preview-wrapper">
    <WebPreview
      :html="html"
      :css="css"
      :js="js"
      :key="computedKey"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import WebPreview from '../Preview/WebPreview.vue'

const props = defineProps<{
  html: string
  css: string
  js: string
  previewKey?: number
}>()

// Если внешний previewKey не передан — используем внутренний
const internalKey = ref(0)
const computedKey = computed(() => props.previewKey ?? internalKey.value)

// Метод для принудительного обновления превью через ref
const refresh = () => {
  internalKey.value++
}

defineExpose({
  refresh
})
</script>

<style scoped>
.web-preview-wrapper {
  width: 100%;
  height: 100%;
  overflow: hidden;
  background: #ffffff;
  position: relative;
}

.web-preview-wrapper :deep(iframe) {
  width: 100%;
  height: 100%;
  border: none;
  background: white;
}
</style>