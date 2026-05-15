<template>
  <div class="video-preview">
    <div class="video-player-container">
      <!-- Заголовок -->
      <div class="video-header">
        <BaseIcon 
          name="video_icon" 
          color="#FF5252"
          class="video-icon" 
        />
        <div class="video-info">
          <div class="video-name">{{ fileName }}</div>
          <div class="video-meta">
            {{ formatTime(currentTime) }} / {{ formatTime(duration) }}
          </div>
        </div>
      </div>

      <!-- Видео контейнер -->
      <div class="video-wrapper">
        <video 
          :key="normalizedSrc"
          ref="videoPlayer" 
          :src="normalizedSrc"
          @timeupdate="onTimeUpdate"
          @loadedmetadata="onLoadedMetadata"
          @ended="onEnded"
          @click="togglePlay"
          :loop="isRepeat"
        ></video>

        <!-- Большая кнопка Play по центру -->
        <button 
          class="play-overlay" 
          :class="{ hidden: isPlaying }"
          @click="togglePlay"
        >
          ▶
        </button>

        <!-- Спиннер по центру -->
        <LoadingSpinner 
          v-if="isLoading"
          size="64"
          accent
          class="video-loader"
        />
      </div>

      <!-- Прогресс-бар -->
      <div class="progress-wrapper">
        <div class="progress-container" @click="seek">
          <div class="progress-bar">
            <div class="progress-fill" :style="{ width: progress + '%' }"></div>
          </div>
          <div class="progress-thumb" :style="{ left: progress + '%' }"></div>
        </div>
        <div class="time-row">
          <div class="time-display">{{ formatTime(currentTime) }}</div>
          <div class="time-display remaining">-{{ formatTime(remainingTime) }}</div>
        </div>
      </div>

      <!-- Нижние контролы -->
      <div class="bottom-controls">
        <button class="control-btn" @click="togglePlay">
          <span v-if="!isPlaying">▶</span>
          <span v-else>⏸</span>
        </button>

        <button class="repeat-btn" :class="{ active: isRepeat }" @click="toggleRepeat">
          <BaseIcon 
            :name="isRepeat ? 'repeat_one_icon' : 'repeat_icon'" 
            color="#FF5252" 
          />
        </button>

        <div class="volume-group">
          <BaseIcon name="volume_icon" color="#AAAAAA" class="volume-icon" />
          <input 
            type="range" 
            min="0" 
            max="1" 
            step="0.01" 
            v-model="volume" 
            @input="setVolume"
            class="volume-slider"
          />
        </div>

        <button class="fullscreen-btn" @click="toggleFullscreen">
          ⛶
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFilePreview } from '@/composables/useFilePreview'
import LoadingSpinner from '../LoadingSpinner/LoadingSpinner.vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  videoPath: string
}>()

const videoPlayer = ref<HTMLVideoElement | null>(null)

// Общий composable
const {
  normalizedSrc,
  fileName,
  isLoading,
  isPlaying,
  isRepeat,
  currentTime,
  duration,
  progress,
  remainingTime,
  volume,
  togglePlay,
  toggleRepeat,
  formatTime,
  onTimeUpdate,
  onLoadedMetadata,
  onEnded,
  setVolume
} = useFilePreview(props.videoPath, videoPlayer)

// Полноэкранный режим
const toggleFullscreen = () => {
  if (!videoPlayer.value) return
  if (document.fullscreenElement) {
    document.exitFullscreen()
  } else {
    videoPlayer.value.requestFullscreen()
  }
}

const seek = (e: MouseEvent) => {
  if (!videoPlayer.value) return
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  const percent = (e.clientX - rect.left) / rect.width
  videoPlayer.value.currentTime = percent * duration.value
}

// Принудительный reload при смене файла
watch(() => props.videoPath, () => {
  if (videoPlayer.value) {
    videoPlayer.value.load()
  }
}, { immediate: true })
</script>

<style scoped>
/* Стили без изменений */
.video-preview {
  height: 100%;
  background: #0a0a0a;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.video-player-container {
  width: 100%;
  max-width: 920px;
}

.video-header {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-bottom: 16px;
  padding: 0 4px;
}

.video-icon {
  width: 42px;
  height: 42px;
  flex-shrink: 0;
}

.video-info {
  flex: 1;
  min-width: 0;
}

.video-name {
  font-size: 18px;
  font-weight: 600;
  color: #ffffff;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.video-meta {
  font-size: 13px;
  color: #888;
}

.video-wrapper {
  position: relative;
  background: #000;
  border-radius: 10px;
  overflow: hidden;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.7);
  margin-bottom: 16px;
  min-height: 300px;
}

video {
  width: 100%;
  max-height: 65vh;
  display: block;
}

.video-loader {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  z-index: 10;
}

.play-overlay {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  width: 88px;
  height: 88px;
  background: rgba(255, 82, 82, 0.95);
  color: white;
  border: none;
  border-radius: 50%;
  font-size: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  z-index: 5;
  box-shadow: 0 8px 25px rgba(255, 82, 82, 0.4);
}

.play-overlay.hidden {
  opacity: 0;
  pointer-events: none;
}

.progress-wrapper { margin: 12px 0; }
.progress-container { 
  height: 6px; 
  background: #333; 
  border-radius: 3px; 
  cursor: pointer; 
  position: relative; 
}
.progress-bar { height: 100%; background: #FF5252; border-radius: 3px; }
.progress-thumb {
  position: absolute; 
  top: 50%; 
  transform: translate(-50%, -50%);
  width: 14px; 
  height: 14px; 
  background: white; 
  border: 2px solid #FF5252;
  border-radius: 50%; 
}

.time-row { 
  display: flex; 
  justify-content: space-between; 
  font-size: 13px; 
  color: #aaa; 
  margin-top: 4px;
}

.bottom-controls {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 28px;
  margin-top: 8px;
}

.control-btn, .repeat-btn, .fullscreen-btn {
  background: none;
  border: none;
  color: #bbb;
  font-size: 24px;
  cursor: pointer;
  padding: 6px;
  border-radius: 6px;
}

.control-btn:hover, .repeat-btn:hover, .fullscreen-btn:hover {
  color: #FF5252;
  background: rgba(255,255,255,0.08);
}

.repeat-btn.active { color: #FF5252; }

.volume-group {
  display: flex;
  align-items: center;
  gap: 10px;
}

.volume-slider {
  width: 160px;
  accent-color: #FF5252;
}
</style>