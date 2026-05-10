<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :value="internalCode"
      :language="language || 'plaintext'"
      :theme="theme || 'snengine-dark'"
      :options="editorOptions"
      @change="handleChange"
      class="editor-instance"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'

const props = defineProps<{
  modelValue: string
  language?: string
  theme?: string
}>()

const emit = defineEmits(['update:modelValue'])

const internalCode = ref(props.modelValue)
let timeout: number | null = null

// Опции редактора для стабильного лейаута
const editorOptions = {
  fontSize: 15,
  fontFamily: "'Fira Code', 'Cascadia Code', Consolas, monospace",
  minimap: { enabled: false },
  automaticLayout: true, // Позволяет редактору менять размер вместе с окном
  scrollBeyondLastLine: false,
  wordWrap: 'on' as const,
  folding: true,
  lineNumbers: 'on' as const,
  tabSize: 2,
  renderLineHighlight: 'all' as const,
  cursorBlinking: 'smooth' as const,
  contextmenu: true,
}

const handleChange = (value: string) => {
  internalCode.value = value
  if (timeout) clearTimeout(timeout)
  timeout = window.setTimeout(() => {
    emit('update:modelValue', value)
  }, 300)
}

// Следим за внешним изменением кода (например, при переключении файлов)
watch(() => props.modelValue, (newVal) => {
  if (newVal !== internalCode.value) {
    internalCode.value = newVal
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
  min-height: 200px; /* Важно, чтобы контейнер не схлопывался */
  position: relative;
  background: #1e1e1e;
}

.editor-instance {
  width: 100% !important;
  height: 100% !important;
}

/* Глубокий селектор для внутренней части Monaco, если нужно подправить шрифты */
:deep(.monaco-editor) {
  padding-top: 8px;
}
</style>