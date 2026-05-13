<template>
  <div class="web-editor">
    <div class="web-toolbar">
      <div class="mode-buttons">
        <button :class="{ active: mode === 'split' }" @click="mode = 'split'">
          <BaseIcon name="split_icon" :color="mode === 'split' ? '#fff' : '#888'" /> Split
        </button>
        <button :class="{ active: mode === 'code' }" @click="mode = 'code'">
          <BaseIcon name="code_icon" :color="mode === 'code' ? '#fff' : '#888'" /> Code
        </button>
        <button :class="{ active: mode === 'preview' }" @click="mode = 'preview'">
          <BaseIcon name="preview_icon" :color="mode === 'preview' ? '#fff' : '#888'" /> Preview
        </button>
      </div>

      <div v-if="mode !== 'preview'" class="code-tabs">
        <button :class="{ active: currentTab === 'html' }" @click="currentTab = 'html'">
          <BaseIcon name="html_icon" :color="currentTab === 'html' ? '#fff' : '#888'" /> HTML
        </button>
        <button :class="{ active: currentTab === 'css' }"  @click="currentTab = 'css'">
          <BaseIcon name="css_icon" :color="currentTab === 'css' ? '#fff' : '#888'" /> CSS
        </button>
        <button :class="{ active: currentTab === 'csharp' }" @click="currentTab = 'csharp'">
          <BaseIcon name="csharp_icon" :color="currentTab === 'csharp' ? '#fff' : '#888'" /> C#
        </button>
      </div>

      <div class="spacer"></div>

      <div class="toolbar-actions">
        <button @click="refreshPreview" class="action-btn" title="Обновить превью">
          <BaseIcon name="refresh_icon" color="#ccc" />
        </button>
        <button 
          @click="toggleFullscreenPreview" 
          :class="{ active: isFullscreenPreview }"
          class="action-btn"
          title="Полноэкранное превью"
        >
          <BaseIcon name="fullscreen_icon" :color="isFullscreenPreview ? '#ff5252' : '#ccc'" />
        </button>
        <button @click="save" class="save-btn">
          <BaseIcon name="save_icon" color="#fff" /> Сохранить
        </button>
      </div>
    </div>

    <div class="web-content" ref="containerRef">
      <div v-if="mode === 'split'" class="split-view">
        <div class="code-panel" :style="{ width: codeWidth + '%' }">
          <CodeEditor 
            v-model="activeCode"
            :language="activeLanguage"
            theme="snengine-dark"
            @save="save"
          />
        </div>

        <div class="splitter" @mousedown="startResize">
          <div class="splitter-line"></div>
        </div>

        <div class="preview-panel" :style="{ width: (100 - codeWidth) + '%' }">
          <WebPreview 
            :key="previewKey"
            :html="htmlCode" 
            :css="cssCode" 
            :csharp="csharpCode"
          />
        </div>
      </div>

      <div v-else-if="mode === 'code'" class="full-panel">
        <CodeEditor 
          v-model="activeCode"
          :language="activeLanguage"
          theme="snengine-dark"
          @save="save"
        />
      </div>

      <div v-else-if="mode === 'preview'" class="full-panel">
        <WebPreview 
          :key="previewKey"
          :html="htmlCode" 
          :css="cssCode" 
          :csharp="csharpCode" 
        />
      </div>
    </div>

    <Teleport to="body" v-if="isFullscreenPreview">
      <div class="fullscreen-overlay" @click.self="toggleFullscreenPreview">
        <div class="fullscreen-content">
          <div class="fullscreen-header">
            <span>🌐 Live Preview — {{ fileName }}</span>
            <button @click="toggleFullscreenPreview">✕</button>
          </div>
          <WebPreview 
            :html="htmlCode" 
            :css="cssCode" 
            :csharp="csharpCode" 
            class="fullscreen-iframe" 
          />
        </div>
      </div>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, computed, onMounted } from 'vue'
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import WebPreview from '../Preview/WebPreview.vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  filePath?: string
  initialHtml?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'save', content: string): void
}>()

const mode = ref<'split' | 'code' | 'preview'>('split')
const currentTab = ref<'html' | 'css' | 'csharp'>('html')
const codeWidth = ref(50)
const isFullscreenPreview = ref(false)
const isResizing = ref(false)
const previewKey = ref(0)
const containerRef = ref<HTMLElement | null>(null)

const htmlCode = ref('')
const cssCode = ref('')
const csharpCode = ref('')

const fileName = computed(() => props.filePath?.split(/[/\\]/).pop() || 'Untitled.html')

const activeLanguage = computed(() => currentTab.value === 'csharp' ? 'csharp' : currentTab.value)
const activeCode = computed({
  get: () => {
    if (currentTab.value === 'html') return htmlCode.value
    if (currentTab.value === 'css') return cssCode.value
    return csharpCode.value
  },
  set: (val) => {
    if (currentTab.value === 'html') htmlCode.value = val
    else if (currentTab.value === 'css') cssCode.value = val
    else csharpCode.value = val
  }
})

const buildFullHtml = () => {
  let result = `<!DOCTYPE html>\n<html lang="ru">\n<head>\n  <meta charset="UTF-8">\n`
  if (cssCode.value.trim()) result += `  <style>\n${cssCode.value}\n  </style>\n`
  result += `</head>\n<body>\n`
  if (htmlCode.value.trim()) result += `${htmlCode.value}\n`
  if (csharpCode.value.trim()) result += `  <script type="text/csharp">\n${csharpCode.value}\n  <\/script>\n`
  result += `</body>\n</html>`
  return result
}

const save = () => {
  const fullHtml = buildFullHtml()
  emit('save', fullHtml)
  emit('update:modelValue', fullHtml)
}

const parseInitial = (content: string) => {
  if (!content) return
  const styleMatch = content.match(/<style[^>]*>([\s\S]*?)<\/style>/i)
  cssCode.value = styleMatch ? styleMatch[1].trim() : ''
  const csharpMatch = content.match(/<script type="text\/csharp"[^>]*>([\s\S]*?)<\/script>/i)
  csharpCode.value = csharpMatch ? csharpMatch[1].trim() : ''
  htmlCode.value = content
    .replace(/<!DOCTYPE[\s\S]*?>/gi, '')
    .replace(/<html[^>]*>|<\/html>|<head[^>]*>[\s\S]*?<\/head>|<body[^>]*>|<\/body>/gi, '')
    .replace(/<style[^>]*>[\s\S]*?<\/style>/gi, '')
    .replace(/<script type="text\/csharp"[^>]*>[\s\S]*?<\/script>/gi, '')
    .trim()
}

watch(() => props.initialHtml, (newVal) => {
  if (newVal) parseInitial(newVal)
}, { immediate: true })

watch([htmlCode, cssCode, csharpCode], () => {
  emit('update:modelValue', buildFullHtml())
})

onMounted(() => {
  if (!props.initialHtml || props.initialHtml.trim() === '') {
    htmlCode.value = `<h1>Hello from SNEngine!</h1>\n<p>Start editing to see changes.</p>`
  }
})

const startResize = (e: MouseEvent) => {
  isResizing.value = true
  document.addEventListener('mousemove', onResize)
  document.addEventListener('mouseup', stopResize)
  document.body.style.cursor = 'col-resize'
}

const onResize = (e: MouseEvent) => {
  if (!isResizing.value || !containerRef.value) return
  const rect = containerRef.value.getBoundingClientRect()
  const offset = e.clientX - rect.left
  const percentage = (offset / rect.width) * 100
  codeWidth.value = Math.max(15, Math.min(85, percentage))
}

const stopResize = () => {
  isResizing.value = false
  document.removeEventListener('mousemove', onResize)
  document.removeEventListener('mouseup', stopResize)
  document.body.style.cursor = 'default'
}

const refreshPreview = () => { previewKey.value++ }
const toggleFullscreenPreview = () => { isFullscreenPreview.value = !isFullscreenPreview.value }
</script>

<style scoped>
.web-editor {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #1e1e1e;
  overflow: hidden;
  color: #ddd;
}

.web-toolbar {
  height: 54px;
  background: #252526;
  border-bottom: 1px solid #333;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 16px;
  flex-shrink: 0;
}

.mode-buttons, .code-tabs {
  display: flex;
  background: #1a1a1b;
  padding: 3px;
  border-radius: 8px;
  border: 1px solid #3c3c3c;
}

.mode-buttons button, .code-tabs button {
  padding: 6px 14px;
  background: transparent;
  color: #888;
  border: none;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 8px;
}

.mode-buttons button:hover, .code-tabs button:hover {
  color: #fff;
  background: #2d2d2d;
}

.mode-buttons button.active, .code-tabs button.active {
  background: #ff5252;
  color: white;
  box-shadow: 0 2px 6px rgba(255, 82, 82, 0.4);
}

.spacer { flex: 1; }

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.action-btn {
  width: 34px;
  height: 34px;
  background: #333;
  border: 1px solid #444;
  color: #ccc;
  border-radius: 6px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
}

.action-btn:hover { background: #444; color: #fff; }
.action-btn.active { color: #ff5252; border-color: #ff5252; }

.save-btn {
  background: linear-gradient(135deg, #ff5252 0%, #d32f2f 100%);
  color: white;
  border: 1px solid rgba(255,255,255,0.1);
  padding: 8px 18px;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 10px rgba(211, 47, 47, 0.3);
}

.save-btn:hover { transform: translateY(-1px); box-shadow: 0 6px 14px rgba(211, 47, 47, 0.4); }

.web-content {
  flex: 1;
  display: flex;
  overflow: hidden;
  position: relative;
}

.split-view {
  display: flex;
  width: 100%;
  height: 100%;
}

.code-panel, .preview-panel {
  height: 100%;
  overflow: hidden;
  position: relative;
}

.splitter {
  width: 6px;
  background: #252526;
  cursor: col-resize;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.2s;
  z-index: 10;
}

.splitter:hover { background: #ff5252; }
.splitter-line { width: 1px; height: 30px; background: #444; }

.full-panel { width: 100%; height: 100%; }

.fullscreen-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.9);
  z-index: 10000;
  display: flex;
  padding: 20px;
  backdrop-filter: blur(8px);
}

.fullscreen-content {
  flex: 1;
  background: #1e1e1e;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #333;
}

.fullscreen-header {
  height: 48px;
  background: #252526;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  border-bottom: 1px solid #333;
}

.fullscreen-iframe { flex: 1; border: none; background: white; }

.base-icon {
  width: 1.2em;
  height: 1.2em;
}
</style>