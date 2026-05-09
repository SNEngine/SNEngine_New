<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :value="internalCode"
      language="sn"
      theme="vs-dark"
      :options="editorOptions"
      @change="handleChange"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'

const props = defineProps<{ modelValue: string }>()
const emit = defineEmits(['update:modelValue'])

const internalCode = ref(props.modelValue)
let timeout: number | null = null

const editorOptions = {
  fontSize: 15,
  minimap: { enabled: false },           // ← ОТКЛЮЧИЛИ (самый тяжёлый элемент!)
  automaticLayout: true,
  scrollBeyondLastLine: false,
  wordWrap: 'on',
  folding: true,
  lineNumbers: 'on',
  glyphMargin: false,                    // ← убираем лишнее
  tabSize: 2,
  quickSuggestions: false,               // ← убираем тяжёлые подсказки
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
  }, 400)  // ← 400ms debounce
}

watch(() => props.modelValue, (val) => {
  if (val !== internalCode.value) internalCode.value = val
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