<template>
  <div class="game-preview">
    <div class="preview-header">
      <div class="header-left">
        <div v-if="launcher.isRunning.value" class="status live">
          ● LIVE • {{ preview.fps.value }} FPS
        </div>
        <div v-else-if="launcher.state.value === 'building'" class="status building">
          ⏳ Building...
        </div>
        <div v-else-if="launcher.state.value === 'error'" class="status error">
          ❌ {{ launcher.errorMessage.value || 'Error' }}
        </div>
      </div>

      <div class="controls">
        <button 
          @click="togglePreview"
          :disabled="isBusy"
          class="btn btn-primary"
        >
          {{ launcher.isRunning.value ? '⏹ Stop' : '▶ Start Preview' }}
        </button>

        <button 
          v-if="launcher.isRunning.value"
          @click="restartPreview"
          class="btn btn-secondary"
        >
          ⟳ Reload
        </button>
      </div>
    </div>

    <div class="canvas-container" ref="containerRef">
      <canvas
        v-show="launcher.isRunning.value"
        :ref="(el) => { preview.canvasRef.value = el as HTMLCanvasElement }"
        class="preview-canvas"
        @dblclick="toggleFullscreen"
      />

      <div v-if="isBusy" class="overlay loading">
        <LoadingSpinner size="64" accent />
        <p>
          {{ launcher.state.value === 'building' 
            ? 'Compiling project...' 
            : 'Launching SNEngine Runtime...' 
          }}
        </p>
      </div>

      <div v-else-if="!launcher.isRunning.value && !launcher.errorMessage.value" class="overlay empty">
        <div class="empty-icon">
          <BaseIcon name="game_icon" color="#444" size="80" />
        </div>
        <div class="empty-title">Game Preview</div>
        <p class="empty-description">
          Нажмите кнопку Start Preview, чтобы запустить игру
        </p>
      </div>

<div v-if="launcher.errorMessage.value && !launcher.isRunning.value" class="overlay error-overlay">
        <p class="error-text">{{ launcher.errorMessage.value }}</p>
        <button @click="launcher.stop()" class="btn btn-secondary">Close</button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useGameLauncher } from '@/composables/useGameLauncher'
import { useGamePreview } from '@/composables/useGamePreview'
import { ref, computed } from 'vue'
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

const launcher = useGameLauncher()
const preview = useGamePreview()

const isBusy = computed(() => 
  launcher.state.value === 'building' || 
  launcher.state.value === 'stopping' ||
  preview.isLoading.value
)

const togglePreview = async () => {
  if (launcher.isRunning.value) {
    await preview.stopPreview()
    await launcher.stop()
    emit('stopped')
  } else {
    await launcher.start(props.projectPath, 800, 450)

    if (launcher.isRunning.value) {
      preview.startRenderOnly(800, 450)
      emit('started')
    }
  }
}

const restartPreview = async () => {
  await preview.stopPreview()
  await launcher.restart(800, 450)
  if (launcher.isRunning.value) {
    preview.startRenderOnly(800, 450)
  }
}

const toggleFullscreen = async () => {
  if (!containerRef.value) return
  try {
    if (!document.fullscreenElement) {
      await containerRef.value.requestFullscreen()
    } else {
      await document.exitFullscreen()
    }
  } catch (err) {
    console.error('Fullscreen error:', err)
  }
}
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

.status {
  font-size: 13px;
  font-weight: 500;
  padding: 2px 10px;
  border-radius: 4px;
}

.status.live   { color: #4ade80; }
.status.building { color: #facc15; }
.status.error  { color: #f87171; }

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

.btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
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
  background: #000;
}

.overlay {
  position: absolute;
  inset: 0;
  background: rgba(10, 10, 10, 0.92);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  z-index: 10;
  gap: 12px;
}

.error-overlay {
  background: rgba(20, 20, 20, 0.95);
}

.error-text {
  color: #f87171;
  max-width: 80%;
  text-align: center;
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

:fullscreen .game-preview {
  height: 100vh;
}

:fullscreen .preview-header {
  height: 52px;
}
</style>