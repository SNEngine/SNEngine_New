<template>
  <div class="monaco-wrapper">
    <MonacoEditor
      :key="languageKey"
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
import { ref, watch, onUnmounted, computed, onBeforeMount } from 'vue'
import MonacoEditor from 'monaco-editor-vue3'
import * as monaco from 'monaco-editor'

import EditorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'
import JsonWorker from 'monaco-editor/esm/vs/language/json/json.worker?worker'
import CssWorker from 'monaco-editor/esm/vs/language/css/css.worker?worker'
import HtmlWorker from 'monaco-editor/esm/vs/language/html/html.worker?worker'
import TsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker'

onBeforeMount(() => {
  if (typeof self !== 'undefined' && !self.MonacoEnvironment) {
    self.MonacoEnvironment = {
      getWorker(_: string, label: string) {
        if (label === 'json') return new JsonWorker()
        if (['css', 'scss', 'less'].includes(label)) return new CssWorker()
        if (['html', 'handlebars', 'razor'].includes(label)) return new HtmlWorker()
        if (['typescript', 'javascript'].includes(label)) return new TsWorker()
        return new EditorWorker()
      }
    }
  }
})

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
const editorRef = ref<monaco.editor.IStandaloneCodeEditor | null>(null)
let timeout: number | null = null

const languageKey = computed(() => `editor-${props.language || 'plaintext'}`)

const editorOptions = {
  fontSize: 15,
  fontFamily: "'Fira Code', Consolas, monospace",
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
  timeout = window.setTimeout(() => emit('update:modelValue', value), 300)
}

watch(() => props.modelValue, (newVal) => {
  if (newVal !== internalCode.value) internalCode.value = newVal
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
  position: relative;
  overflow: hidden;
}

.monaco-editor-instance {
  width: 100% !important;
  height: 100% !important;
}
</style>