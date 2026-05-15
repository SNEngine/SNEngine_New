<template>
  <div class="image-preview" @wheel.prevent="handleWheelZoom">
    <div class="image-container">
      <LoadingSpinner 
        v-if="isLoading && imagePath" 
        :size="48" 
        accent 
        class="loader" 
      />

      <img
        v-show="imagePath && !isLoading"
        :src="normalizedSrc"
        class="preview-image"
        :style="{ transform: `scale(${zoomLevel})` }"
        :alt="fileName"
        @load="onLoadingFinished"
        @error="onLoadingFinished"
      />
      
      <div v-if="!imagePath" class="empty-state">
        <span class="empty-icon">🖼️</span>
        <p>Нет изображения</p>
      </div>

      <Transition name="fade">
        <div v-if="zoomLevel !== 1" class="zoom-badge" @click="resetZoom">
          {{ Math.round(zoomLevel * 100) }}%
        </div>
      </Transition>
    </div>
  </div>
</template>

<script setup lang="ts">
import { toRef } from 'vue'
import { useFilePreview } from '@/composables/useFilePreview'
import LoadingSpinner from '../LoadingSpinner/LoadingSpinner.vue'

const props = defineProps<{
  imagePath?: string
}>()

const { 
  normalizedSrc, 
  fileName, 
  isLoading, 
  zoomLevel,
  handleWheelZoom,
  resetZoom,
  onLoadingFinished 
} = useFilePreview(toRef(props, 'imagePath'))
</script>

<style scoped>
.image-preview {
  height: 100%;
  background: #1e1e1e;
  display: flex;
  position: relative;
  overflow: hidden; /* Обрезает изображение при сильном увеличении */
}

.image-container {
  width: 100%;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #121212;
  position: relative;
}

.preview-image {
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
  /* Плавный переход для комфортного зума */
  transition: transform 0.15s cubic-bezier(0.25, 0.46, 0.45, 0.94);
  will-change: transform;
}

.loader {
  position: absolute;
  z-index: 2;
}

.zoom-badge {
  position: absolute;
  bottom: 20px;
  right: 20px;
  background: rgba(255, 82, 82, 0.8);
  color: white;
  padding: 4px 12px;
  border-radius: 20px;
  font-size: 12px;
  font-weight: bold;
  cursor: pointer;
  backdrop-filter: blur(4px);
  user-select: none;
  z-index: 10;
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

/* Анимация появления плашки зума */
.fade-enter-active, .fade-leave-active {
  transition: opacity 0.3s;
}
.fade-enter-from, .fade-leave-to {
  opacity: 0;
}
</style>