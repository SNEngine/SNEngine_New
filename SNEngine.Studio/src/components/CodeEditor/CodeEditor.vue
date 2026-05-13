<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :value="internalCode"
      :language="language || 'plaintext'"
      :theme="theme || 'snengine-dark'"
      :options="editorOptions"
      @change="handleChange"
      @mount="onEditorMount"
      class="monaco-editor-instance"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'
import * as monaco from 'monaco-editor'

const props = defineProps<{
  modelValue: string
  language?: string
  theme?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'save', content: string): void
}>()

const internalCode = ref(props.modelValue)
let timeout: number | null = null
const editorRef = ref<monaco.editor.IStandaloneCodeEditor | null>(null)

const editorOptions = {
  fontSize: 15,
  fontFamily: "'Fira Code', Consolas, monospace",
  minimap: { enabled: false },
  automaticLayout: true,           // ← важно
  scrollBeyondLastLine: false,     // ← важно
  wordWrap: 'on' as const,
  folding: true,
  lineNumbers: 'on' as const,
  tabSize: 2,
  renderLineHighlight: 'all' as const,
  cursorBlinking: 'smooth' as const,
  contextmenu: true,
  scrollbar: {
    verticalScrollbarSize: 10,
    horizontalScrollbarSize: 10,
    alwaysConsumeMouseWheel: false
  }
}

const onEditorMount = (editor: monaco.editor.IStandaloneCodeEditor) => {
  editorRef.value = editor

  // Добавляем действие Ctrl+S
  editor.addAction({
    id: 'save-file',
    label: 'Сохранить файл',
    keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS],
    run: () => {
      const value = editor.getValue()
      internalCode.value = value
      emit('update:modelValue', value)
      emit('save', value)
    }
  })
}

const handleChange = (value: string) => {
  internalCode.value = value
  if (timeout) clearTimeout(timeout)
  timeout = window.setTimeout(() => {
    emit('update:modelValue', value)
  }, 300)
}

watch(() => props.modelValue, (newVal) => {
  if (newVal !== internalCode.value) {
    internalCode.value = newVal
  }
})

onUnmounted(() => {
  if (timeout) clearTimeout(timeout)
})

defineExpose({
  editorRef,
  internalCode
})
</script>

<style scoped>
.monaco-wrapper {
  width: 100%;
  height: 100%;           /* обязательно */
  min-height: 100%;
  position: relative;
  overflow: hidden;
}

.monaco-editor-instance {
  width: 100% !important;
  height: 100% !important;
}
</style>