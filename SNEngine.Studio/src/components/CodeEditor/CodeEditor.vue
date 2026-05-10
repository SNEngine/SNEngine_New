<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :value="internalCode"
      :language="language"
      :theme="theme"
      :options="editorOptions"
      @change="handleChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted, onMounted } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'

const props = defineProps<{
  modelValue: string
  language?: string
  theme?: string
}>()

const emit = defineEmits(['update:modelValue'])

const internalCode = ref(props.modelValue)
let timeout: number | null = null

const editorOptions = {
  fontSize: 15,
  minimap: { enabled: false },
  automaticLayout: true,
  scrollBeyondLastLine: false,
  wordWrap: 'on',
  folding: true,
  lineNumbers: 'on',
  tabSize: 2,
  quickSuggestions: false,
  parameterHints: { enabled: false },
  suggestOnTriggerCharacters: false,
  renderLineHighlight: 'all',
  cursorBlinking: 'smooth',
}

const handleChange = (value: string) => {
  internalCode.value = value
  if (timeout) clearTimeout(timeout)
  timeout = window.setTimeout(() => {
    emit('update:modelValue', value)
  }, 300)
}

watch(() => props.modelValue, (val) => {
  if (val !== internalCode.value) internalCode.value = val
})

onMounted(() => {
  // Принудительно применяем тему после монтирования
  if (props.theme) {
    console.log(`🎨 Применена тема: ${props.theme}`)
  }
})

onUnmounted(() => {
  if (timeout) clearTimeout(timeout)
})
</script>

<style scoped>
.monaco-wrapper {
  width: 100%;
  height: 100%;
  background: #1e1e1e;
  border-radius: 6px;
  overflow: hidden;
}
</style>