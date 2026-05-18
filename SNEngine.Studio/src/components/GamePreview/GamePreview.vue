<template>
  <div class="game-preview">
    <!-- Header -->
    <div class="preview-header">
      <div class="title">
        <span>🎮 Game Preview</span>
        <span v-if="isRunning" class="status live">● LIVE</span>
      </div>

      <div class="controls">
        <button 
          @click="togglePreview"
          :disabled="isLoading"
          class="btn btn-primary"
        >
          {{ isRunning ? '⏹ Stop Preview' : '▶ Start Preview' }}
        </button>

        <div v-if="isRunning" class="fps-info">
          FPS: <strong>{{ fps }}</strong>
        </div>

        <button 
          v-if="isRunning"
          @click="restartPreview"
          class="btn btn-secondary"
        >
          ⟳ Reload
        </button>
      </div>
    </div>

    <!-- Canvas Area -->
    <div class="canvas-container">
      <canvas
        ref="canvasRef"
        width="800"
        height="450"
        class="preview-canvas"
      />

      <!-- Loading Overlay -->
      <div v-if="isLoading" class="overlay loading">
        <div class="spinner"></div>
        <p>Launching SNEngine Runtime...</p>
      </div>

      <!-- Empty State -->
      <div v-else-if="!isRunning" class="overlay empty">
        <p>Preview is not running</p>
        <button @click="startPreview" class="btn btn-primary large">
          Start Preview
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useGamePreview } from '@/composables/useGamePreview'
import { onMounted } from 'vue'

const props = defineProps<{
  projectPath: string
}>()

const emit = defineEmits<{
  (e: 'started'): void
  (e: 'stopped'): void
}>()

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

onMounted(() => {
  // Автозапуск можно включить здесь, если нужно
  // startPreview()
})
</script>

<style scoped>
.game-preview {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #1e1e1e;
  border-radius: 8px;
  overflow: hidden;
  border: 1px solid #333;
}

.preview-header {
  padding: 10px 16px;
  background: #252526;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #333;
}

.title {
  font-weight: 600;
  color: #ddd;
  display: flex;
  align-items: center;
  gap: 8px;
}

.status.live {
  color: #4ade80;
  font-size: 13px;
}

.controls {
  display: flex;
  align-items: center;
  gap: 10px;
}

.btn {
  padding: 6px 14px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-size: 14px;
  transition: all 0.2s;
}

.btn-primary {
  background: #3b82f6;
  color: white;
}

.btn-primary:hover:not(:disabled) {
  background: #2563eb;
}

.btn-secondary {
  background: #475569;
  color: white;
}

.btn-secondary:hover {
  background: #334155;
}

.fps-info {
  font-size: 13px;
  color: #94a3b8;
  padding: 4px 8px;
  background: #1e2937;
  border-radius: 4px;
}

.canvas-container {
  position: relative;
  flex: 1;
  background: #000;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.preview-canvas {
  image-rendering: pixelated;
  display: block;           /* убирает лишние отступы */
  max-width: 100%;
  max-height: 100%;
  width: auto;
  height: auto;
  box-shadow: 0 4px 20px rgba(0, 0, 0, 0.7);
}
.overlay {
  position: absolute;
  inset: 0;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #aaa;
  background: rgba(20, 20, 20, 0.95);
  gap: 16px;
}

.loading p {
  margin-top: 8px;
}

.spinner {
  width: 36px;
  height: 36px;
  border: 4px solid #334155;
  border-top-color: #60a5fa;
  border-radius: 50%;
  animation: spin 1s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.large {
  padding: 10px 24px;
  font-size: 16px;
}
</style>