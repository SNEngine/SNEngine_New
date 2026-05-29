<template>
  <Teleport to="body">
    <div 
      class="fullscreen-overlay" 
      @click.self="emit('close')"
    >
      <div class="fullscreen-content">
        <!-- Заголовок -->
        <div class="fullscreen-header">
          <div class="header-info">
            <BaseIcon name="preview_icon" color="#FF5252" class="header-icon" />
            <span>Live Preview — {{ fileName }}</span>
          </div>
          
          <button class="close-btn" @click="emit('close')">
            ✕
          </button>
        </div>

        <!-- Само превью -->
        <WebPreviewWrapper
          :html="html"
          :css="css"
          :js="js"
          class="fullscreen-preview"
        />
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'
import WebPreviewWrapper from './WebPreviewWrapper.vue'

const props = defineProps<{
  html: string
  css: string
  js: string
  fileName?: string
}>()

const emit = defineEmits<{
  (e: 'close'): void
}>()
</script>

<style scoped>
.fullscreen-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.92);
  z-index: 10000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
  backdrop-filter: blur(8px);
}

.fullscreen-content {
  width: 100%;
  height: 100%;
  max-width: 1920px;
  max-height: 1080px;
  background: #1e1e1e;
  border-radius: 12px;
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 1px solid #444;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.8);
}

.fullscreen-header {
  height: 52px;
  background: #252526;
  border-bottom: 1px solid #333;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  flex-shrink: 0;
  color: #ddd;
  font-size: 15px;
  font-weight: 500;
}

.header-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-icon {
  width: 22px;
  height: 22px;
}

.close-btn {
  width: 32px;
  height: 32px;
  background: transparent;
  border: none;
  color: #888;
  font-size: 20px;
  cursor: pointer;
  border-radius: 6px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
}

.close-btn:hover {
  background: #FF5252;
  color: white;
}

.fullscreen-preview {
  flex: 1;
  overflow: hidden;
  background: transparent;
}
</style>