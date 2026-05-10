<template>
  <div class="image-preview">
    <div class="image-container">
      <img
        v-if="imagePath"
        :src="getImageSrc(imagePath)"
        class="preview-image"
        alt="Preview"
        @error="onError"
      />
      <div v-else class="empty-state">
        <span class="empty-icon">🖼️</span>
        <p>Нет изображения</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  imagePath?: string
}>()

const getImageSrc = (path: string) => {
  if (!path) return ''
  const normalized = path.replace(/\\/g, '/')
  return `file:///${normalized}`
}

const onError = () => {
  console.error('Не удалось загрузить изображение:', props.imagePath)
}
</script>

<style scoped>
.image-preview {
  height: 100%;
  background: #1e1e1e;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.image-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #121212;
}

.preview-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.8);
}

.empty-state {
  text-align: center;
  color: #555;
}

.empty-icon {
  font-size: 90px;
  opacity: 0.15;
  display: block;
  margin-bottom: 16px;
}
</style>