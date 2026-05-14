<template>
  <div class="web-editor">
    <WebToolbar
      :mode="mode"
      :current-tab="currentTab"
      :is-fullscreen="isFullscreenPreview"
      @update:mode="mode = $event"
      @update:current-tab="currentTab = $event"
      @refresh="refreshPreview"
      @toggle-fullscreen="toggleFullscreenPreview"
      @save="save"
    />

    <WebContentArea
      :mode="mode"
      :code-width="codeWidth"
      :active-code="activeCode"
      :active-language="activeLanguage"
      :html-code="htmlCode"
      :css-code="cssCode"
      :js-code="jsCode"
      :preview-key="previewKey"
      @update:code-width="codeWidth = $event"
      @update:active-code="handleActiveCodeUpdate"
      @save="save"
    />

    <WebFullscreenPreview
      v-if="isFullscreenPreview"
      :html="htmlCode"
      :css="cssCode"
      :js="jsCode"
      :file-name="fileName"
      @close="toggleFullscreenPreview"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import { useWebEditor } from '@/composables/useWebEditor'

import WebToolbar from './WebToolbar.vue'
import WebContentArea from './WebContentArea.vue'
import WebFullscreenPreview from './WebFullscreenPreview.vue'

const props = defineProps<{
  initialHtml?: string
  filePath?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
}>()

// Подключаем логику из useWebEditor (где уже JS вместо C#)
const { 
  htmlCode, 
  cssCode, 
  jsCode, 
  fileName, 
  buildFullHtml, 
  saveToDisk 
} = useWebEditor(props.initialHtml, props.filePath)

// Состояние интерфейса
const mode = ref<'split' | 'code' | 'preview'>('split')
const currentTab = ref<'html' | 'css' | 'javascript'>('html')
const codeWidth = ref(50)
const previewKey = ref(0)
const isFullscreenPreview = ref(false)

// Определение языка для Monaco Editor
const activeLanguage = computed(() => {
  if (currentTab.value === 'html') return 'html'
  if (currentTab.value === 'css') return 'css'
  return 'javascript' // Было csharp
})

// Текущий код в редакторе
const activeCode = computed({
  get: () => {
    if (currentTab.value === 'html') return htmlCode.value
    if (currentTab.value === 'css') return cssCode.value
    return jsCode.value
  },
  set: (val) => {
    handleActiveCodeUpdate(val)
  }
})

// Обработка изменений кода
const handleActiveCodeUpdate = (newValue: string) => {
  if (currentTab.value === 'html') htmlCode.value = newValue
  else if (currentTab.value === 'css') cssCode.value = newValue
  else jsCode.value = newValue
}

// Автоматическое обновление превью при печати
watch([htmlCode, cssCode, jsCode], () => {
  previewKey.value++
  emit('update:modelValue', buildFullHtml())
})

// Сохранение
const save = async () => {
  await saveToDisk()
  emit('update:modelValue', buildFullHtml())
}

const refreshPreview = () => { previewKey.value++ }
const toggleFullscreenPreview = () => { isFullscreenPreview.value = !isFullscreenPreview.value }

onMounted(() => {
  if (!props.initialHtml || props.initialHtml.trim() === '') {
    htmlCode.value = `<h1>Привет!</h1>\n<p>Тут теперь JS вместо C#.</p>`
  }
})
</script>

<style scoped>
.web-editor {
  display: flex;
  flex-direction: column;
  height: 100%;
  width: 100%;
  background: #1e1e1e;
  color: #cccccc;
  overflow: hidden;
}
</style>