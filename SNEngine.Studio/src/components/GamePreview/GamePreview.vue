<template>
  <div class="game-preview">
    <!-- Минимальная шапка только с названием и статусом -->
    <div class="preview-header">
      <div class="header-left">
        <div v-if="isRunning" class="status live">
          ● LIVE • {{ fps }} FPS
        </div>
      </div>

      <div class="controls">
        <button 
          @click="togglePreview"
          :disabled="isLoading"
          class="btn btn-primary"
        >
          {{ isRunning ? '⏹ Stop' : '▶ Start Preview' }}
        </button>

        <button 
          v-if="isRunning"
          @click="restartPreview"
          class="btn btn-secondary"
        >
          ⟳ Reload
        </button>
      </div>
    </div>

    <!-- Основная область превью -->
    <div class="canvas-container" ref="containerRef">
      <canvas
        ref="canvasRef"
        class="preview-canvas"
        @dblclick="toggleFullscreen"
      />

      <!-- Loading -->
      <div v-if="isLoading" class="overlay loading">
        <LoadingSpinner size="64" accent />
        <p>Launching SNEngine Runtime...</p>
      </div>

      <!-- Пустое состояние -->
      <div v-else-if="!isRunning" class="overlay empty">
        <div class="empty-icon">
          <BaseIcon name="game_icon" color="#444" size="80" />
        </div>
        <div class="empty-title">Game Preview</div>
        <p class="empty-description">
          Нажмите кнопку Start Preview, чтобы запустить игру
        </p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useGamePreview } from '@/composables/useGamePreview'
import { onMounted, ref } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import LoadingSpinner from '../LoadingSpinner/LoadingSpinner.vue'

const props = defineProps<{
  projectPath: string
}>()

const emit = defineEmits<{
  (e: 'started'): void
  (e: 'stopped'): void
}>()

const containerRef = ref<HTMLElement | null>(null)
const isFullscreen = ref(false)

const {
  canvasRef,
  isRunning,
  isLoading,
  fps,
  startPreview: startPreviewComposable,
  stopPreview
} = useGamePreview()

const startPreview = () => {
  startPreviewComposable(props.projectPath, 800, 450)
  emit('started')
}

const togglePreview = () => {
  if (isRunning.value) {
    stopPreview()
    emit('stopped')
  } else {
    startPreview()
  }
}

const restartPreview = async () => {
  await stopPreview()
  setTimeout(() => startPreview(), 300)
}

const toggleFullscreen = async () => {
  if (!containerRef.value) return
  try {
    if (!document.fullscreenElement) {
      await containerRef.value.requestFullscreen()
      isFullscreen.value = true
    } else {
      await document.exitFullscreen()
      isFullscreen.value = false
    }
  } catch (err) {
    console.error('Fullscreen error:', err)
  }
}

document.addEventListener('fullscreenchange', () => {
  isFullscreen.value = !!document.fullscreenElement
})
</script>

<style scoped>
.game-preview {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #1e1e1e;
  overflow: hidden;
}

.preview-header {
  height: 48px;
  background: #252526;
  border-bottom: 1px solid #333;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16px;
  flex-shrink: 0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.header-icon {
  width: 24px;
  height: 24px;
}

.title {
  font-size: 15px;
  font-weight: 600;
  color: #ddd;
}

.status.live {
  font-size: 13px;
  color: #4ade80;
  margin-left: 8px;
}

.controls {
  display: flex;
  gap: 8px;
}

.btn {
  padding: 5px 14px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  transition: all 0.2s;
}

.btn-primary {
  background: #FF5252;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #ff3333;
}

.btn-secondary {
  background: #333;
  color: #ccc;
}

.canvas-container {
  flex: 1;
  position: relative;
  background: #0a0a0a;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.preview-canvas {
  image-rendering: pixelated;
  max-width: 100%;
  max-height: 100%;
  object-fit: contain;
}

.overlay {
  position: absolute;
  inset: 0;
  background: rgba(10, 10, 10, 0.9);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.empty {
  gap: 20px;
  text-align: center;
}

.empty-icon {
  opacity: 0.5;
}

.empty-title {
  font-size: 24px;
  font-weight: 600;
  color: #aaa;
}

.empty-description {
  color: #666;
  max-width: 340px;
}

/* Fullscreen */
:fullscreen .game-preview {
  height: 100vh;
}

:fullscreen .preview-header {
  height: 52px;
}
</style>