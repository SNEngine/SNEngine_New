<template>
  <div class="web-editor">
    <div class="split-view">
      <!-- Левая панель — Код -->
      <div class="code-panel">
        <div class="editor-wrapper">
          <CodeEditor 
            v-model="htmlCode"
            language="html"
            theme="snengine-dark"
            height="100%"
          />
        </div>
      </div>

      <!-- Правая панель — Превью -->
      <div class="preview-panel">
        <WebPreview 
          :html="htmlCode" 
          :css="cssCode"
        />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import CodeEditor from '../CodeEditor/CodeEditor.vue'
import WebPreview from '../Preview/WebPreview.vue'

// Начальные значения
const htmlCode = ref(`<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8">
  <title>Моя Новелла</title>
</head>
<body>
  <h1>Добро пожаловать в SNEngine!</h1>
  <p>Это тест WebEditor.</p>
  <button onclick="alert('Работает!')">Нажми меня</button>
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
</script>

<style scoped>
.web-editor {
  height: 100vh;
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
  width: 48%;
  display: flex;
  flex-direction: column;
  border-right: 1px solid #333;
}

.section-header {
  padding: 8px 16px;
  background: #2d2d2d;
  color: #aaa;
  font-size: 13px;
  font-weight: 500;
}

.editor-wrapper {
  flex: 1;
  min-height: 0;
}

.preview-panel {
  width: 52%;
  display: flex;
  flex-direction: column;
}
</style>