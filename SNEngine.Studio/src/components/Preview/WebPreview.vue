<template>
  <div class="web-preview">
    <div class="preview-frame">
      <iframe 
        :srcdoc="renderedHtml"
        class="preview-iframe"
        sandbox="allow-scripts allow-same-origin"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'

const props = defineProps<{
  html?: string
  css?: string
}>()

const renderedHtml = ref('')

const updatePreview = () => {
  const html = props.html?.trim() || ''
  const css = props.css?.trim() || ''

  renderedHtml.value = `
<!DOCTYPE html>
<html lang="ru">
<head>
  <meta charset="UTF-8">
  <style>
    * { margin: 0; padding: 0; box-sizing: border-box; }
    body { 
      min-height: 100vh;
      font-family: system-ui, sans-serif;
      ${css}
    }
  </style>
</head>
<body>
  ${html || '<div style="text-align:center; padding:80px; color:#aaa; font-size:18px;">Превью пустое.<br>Напишите HTML и CSS в редакторе слева.</div>'}
</body>
</html>`
}

watch(() => [props.html, props.css], updatePreview, { immediate: true })
onMounted(updatePreview)
</script>

<style scoped>
.web-preview {
  height: 100%;
  background: #ffffff;
  overflow: hidden;
}

.preview-frame {
  width: 100%;
  height: 100%;
}

.preview-iframe {
  width: 100%;
  height: 100%;
  border: none;
  background: white;
}
</style>