<template>
  <div class="split-view">
    <!-- Левая панель — код -->
    <div class="code-panel" :style="{ width: codeWidth + '%' }">
      <CodeEditor 
        :model-value="activeCode"
        :language="activeLanguage"
        theme="snengine-dark"
        @update:modelValue="emit('update:activeCode', $event)"
        @save="emit('save')"
      />
    </div>

    <!-- Разделитель -->
    <WebResizeSplitter
      :code-width="codeWidth"
      @update:code-width="emit('update:codeWidth', $event)"
    />

    <!-- Правая панель — превью -->
    <div class="preview-panel" :style="{ width: (100 - codeWidth) + '%' }">
      <WebPreviewWrapper
        :html="html"
        :css="css"
        :js="js"
        :key="previewKey || 0"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import WebResizeSplitter from './WebResizeSplitter.vue'
import WebPreviewWrapper from './WebPreviewWrapper.vue'

const props = defineProps<{
  codeWidth: number
  activeCode: string
  activeLanguage: string
  html: string
  css: string
  js: string
  previewKey?: number
}>()

const emit = defineEmits<{
  (e: 'update:codeWidth', value: number): void
  (e: 'update:activeCode', value: string): void
  (e: 'save'): void
}>()
</script>

<style scoped>
.split-view {
  display: flex;
  width: 100%;
  height: 100%;
  overflow: hidden;
}

.code-panel,
.preview-panel {
  height: 100%;
  overflow: hidden;
  position: relative;
}

.code-panel {
  border-right: 1px solid #2a2a2a;
}

.preview-panel {
  background: #0a0a0a;
}
</style>