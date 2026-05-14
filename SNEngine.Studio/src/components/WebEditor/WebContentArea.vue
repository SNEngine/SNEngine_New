<template>
  <div class="web-content" ref="containerRef">
    <WebSplitView
      v-if="mode === 'split'"
      :code-width="codeWidth"
      :active-code="activeCode"
      :active-language="activeLanguage"
      :html="htmlCode"
      :css="cssCode"
      :js="jsCode"
      :preview-key="previewKey"
      @update:code-width="emit('update:codeWidth', $event)"
      @update:active-code="emit('update:activeCode', $event)"
      @save="emit('save')"
    />

    <div v-else-if="mode === 'code'" class="full-panel">
      <CodeEditor 
        :model-value="activeCode"
        :language="activeLanguage"
        theme="snengine-dark"
        @update:modelValue="emit('update:activeCode', $event)"
        @save="emit('save')"
      />
    </div>

    <div v-else-if="mode === 'preview'" class="full-panel">
      <WebPreviewWrapper
        :html="htmlCode"
        :css="cssCode"
        :js="jsCode"
        :preview-key="previewKey"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import WebSplitView from './WebSplitView.vue'
import WebPreviewWrapper from './WebPreviewWrapper.vue'

const props = defineProps<{
  mode: 'split' | 'code' | 'preview'
  codeWidth: number
  activeCode: string
  activeLanguage: string
  htmlCode: string
  cssCode: string
  jsCode: string // Заменили csharpCode
  previewKey: number
}>()

const emit = defineEmits<{
  (e: 'update:codeWidth', value: number): void
  (e: 'update:activeCode', value: string): void
  (e: 'save'): void
}>()

const containerRef = ref<HTMLElement | null>(null)
</script>

<style scoped>
.web-content {
  flex: 1;
  display: flex;
  overflow: hidden;
  position: relative;
  background: #1e1e1e;
}

.full-panel {
  width: 100%;
  height: 100%;
}
</style>