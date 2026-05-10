<template>
  <div class="web-editor">
    <div class="split-view">
      <!-- Левая панель — Код -->
      <div class="code-panel">
        <div class="section-header">HTML (полный документ)</div>
        <div class="editor-wrapper">
          <CodeEditor 
            v-model="htmlCode"
            language="html"
            theme="snengine-dark"
          />
        </div>
      </div>

      <!-- Правая панель — Превью -->
      <div class="preview-panel">
        <div class="section-header">Live Preview</div>
        <WebPreview 
          :html="htmlCode" 
          :css="cssCode"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import WebPreview from '../Preview/WebPreview.vue'

const props = defineProps<{
  filePath?: string
  initialHtml?: string
}>()

const htmlCode = ref(props.initialHtml || `<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8">
  <title>Моя Новелла</title>
</head>
<body>
  <h1>Добро пожаловать в SNEngine!</h1>
  <p>Это тест WebEditor.</p>
</body>
</html>`)

const cssCode = ref(`body {
  margin: 0;
  padding: 40px;
  font-family: system-ui, sans-serif;
  background: linear-gradient(135deg, #667eea, #764ba2);
  color: white;
  min-height: 100vh;
  text-align: center;
}

h1 { font-size: 3em; margin-bottom: 20px; }`)

onMounted(() => {
  if (props.filePath) {
    console.log('[WebEditor] Открыт файл:', props.filePath)
  }
})
</script>

<style scoped>
.web-editor {
  height: 100%;
  display: flex;
  flex-direction: column;
  background: #1e1e1e;
  overflow: hidden;
}

.split-view {
  display: flex;
  flex: 1;
  overflow: hidden;
}

.code-panel {
  width: 50%;
  display: flex;
  flex-direction: column;
  border-right: 1px solid #333;
}

.preview-panel {
  width: 50%;
  display: flex;
  flex-direction: column;
}

.section-header {
  padding: 8px 16px;
  background: #252526;
  color: #aaa;
  font-size: 13px;
  font-weight: 500;
  border-bottom: 1px solid #333;
}

.editor-wrapper {
  flex: 1;
  overflow: hidden;
}
</style>