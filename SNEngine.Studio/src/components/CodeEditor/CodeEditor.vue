<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :value="internalCode"
      :language="language || 'plaintext'"
      :theme="theme || 'snengine-dark'"
      :options="editorOptions"
      @change="handleChange"
      @mount="onEditorMount"
      class="editor-instance"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onUnmounted } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'
import * as monaco from 'monaco-editor'
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'
import jsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker'
import cssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker'
import htmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker'
import tsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker'

self.MonacoEnvironment = {
  getWorker(_: any, label: string) {
    if (label === 'json') return new jsonWorker()
    if (label === 'css' || label === 'scss' || label === 'less') return new cssWorker()
    if (label === 'html' || label === 'handlebars' || label === 'razor') return new htmlWorker()
    if (label === 'typescript' || label === 'javascript') return new tsWorker()
    return new editorWorker()
  }
}

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
  fontFamily: "'Fira Code', 'Cascadia Code', Consolas, monospace",
  minimap: { enabled: false },
  automaticLayout: true,
  scrollBeyondLastLine: false,
  wordWrap: 'on' as const,
  folding: true,
  lineNumbers: 'on' as const,
  tabSize: 2,
  renderLineHighlight: 'all' as const,
  cursorBlinking: 'smooth' as const,
  contextmenu: true,
}

const onEditorMount = (editor: monaco.editor.IStandaloneCodeEditor) => {
  editorRef.value = editor

  editor.addAction({
    id: 'save-file',
    label: 'Сохранить файл',
    keybindings: [monaco.KeyMod.CtrlCmd | monaco.KeyCode.KeyS],
    run: () => {
      const currentValue = editor.getValue()
      internalCode.value = currentValue
      emit('update:modelValue', currentValue)
      emit('save', currentValue)
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
  height: 100%;
  min-height: 200px;
  position: relative;
  background: #1e1e1e;
}

.editor-instance {
  width: 100% !important;
  height: 100% !important;
}

:deep(.monaco-editor) {
  padding-top: 8px;
}
</style>