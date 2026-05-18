<template>
  <div class="game-preview">
    <div class="preview-header">
      <div class="header-left">
        <BaseIcon name="game_icon" color="#FF5252" class="header-icon" />
        <div>
          <div class="title">Game Preview</div>
          <div v-if="isRunning" class="status live">● LIVE • {{ fps }} FPS</div>
        </div>
      </div>

      <div class="controls">
        <button 
          @click="togglePreview"
          :disabled="isLoading"
          class="btn btn-primary"
        >
          {{ isRunning ? '⏹ Stop Preview' : '▶ Start Preview' }}
        </button>

        <button 
          v-if="isRunning"
          @click="restartPreview"
          class="btn btn-secondary"
        >
          ⟳ Reload
        </button>

        <button 
          v-if="isRunning"
          @click="toggleFullscreen"
          class="btn btn-secondary"
          :class="{ active: isFullscreen }"
        >
          ⛶ {{ isFullscreen ? 'Exit' : 'Full' }}
        </button>
      </div>
    </div>

    <div class="canvas-container" ref="containerRef">
      <canvas
        ref="canvasRef"
        class="preview-canvas"
        @dblclick="toggleFullscreen"
      />

      <div v-if="isLoading" class="overlay loading">
        <LoadingSpinner size="64" accent />
        <p>Launching SNEngine Runtime...</p>
      </div>

      <div v-else-if="!isRunning" class="overlay empty">
        <div class="empty-icon">
          <BaseIcon name="game_icon" color="#444" size="96" />
        </div>
        <div class="empty-title">Game Preview</div>
        <p class="empty-description">
          Запустите превью, чтобы увидеть игру в реальном времени
        </p>
        <button @click="startPreview" class="btn btn-primary large">
          <span class="play-icon">▶</span>
          Start Preview
        </button>
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

onMounted(() => {
})
</script>

<style scoped>
.game-preview {
  display: flex;
  flex-direction: column;
  width: 100%;
  background: #1e1e1e;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #333;
}

.preview-header {
  height: 54px;
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
  width: 28px;
  height: 28px;
}

.title {
  font-size: 15px;
  font-weight: 600;
  color: #ddd;
}

.status.live {
  font-size: 13px;
  color: #4ade80;
}

.controls {
  display: flex;
  align-items: center;
  gap: 8px;
}

.btn {
  padding: 6px 16px;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  font-weight: 500;
  transition: all 0.2s ease;
}

.btn-primary {
  background: #FF5252;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #ff3333;
  transform: translateY(-1px);
}

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none !important;
}

.btn-secondary {
  background: #333;
  color: #ccc;
  border: 1px solid #444;
}

.btn-secondary:hover {
  background: #444;
  color: #fff;
}

.btn-secondary.active {
  background: #FF5252;
  border-color: #ff3333;
  color: #fff;
}

.canvas-container {
  position: relative;
  width: 100%;
  aspect-ratio: 16 / 9;
  background: #0a0a0a;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
  box-sizing: border-box;
}

.preview-canvas {
  image-rendering: pixelated;
  width: 100%;
  height: 100%;
  object-fit: contain;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.8);
}

.overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(10, 10, 10, 0.85);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 10;
}

.loading {
  gap: 16px;
}

.loading p {
  color: #aaa;
  font-size: 14px;
  margin: 0;
}

.empty {
  gap: 16px;
  text-align: center;
  padding: 24px;
}

.empty-icon {
  opacity: 0.6;
  margin-bottom: 4px;
  display: flex;
  justify-content: center;
}

.empty-title {
  font-size: 22px;
  font-weight: 600;
  color: #aaa;
  margin: 0;
}

.empty-description {
  color: #666;
  font-size: 15px;
  max-width: 320px;
  line-height: 1.4;
  margin: 0 auto;
}

.large {
  padding: 12px 32px;
  font-size: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  margin: 8px auto 0 auto;
}

.play-icon {
  font-size: 18px;
}

:fullscreen .canvas-container {
  padding: 0;
  width: 100vw;
  height: 100vh;
  aspect-ratio: auto;
}

:fullscreen .preview-canvas {
  border-radius: 0;
  border: none;
  max-width: none;
  max-height: none;
  width: 100%;
  height: 100%;
}
</style>