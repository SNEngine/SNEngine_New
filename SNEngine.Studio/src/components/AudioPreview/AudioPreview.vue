<template>
  <div class="audio-preview">
    <div class="audio-player-container">
      <!-- Заголовок -->
      <div class="audio-header">
        <BaseIcon 
          name="audio_icon" 
          color="#FF5252"
          class="audio-icon" 
        />
        <div class="audio-info">
          <div class="audio-name">{{ fileName }}</div>
          <div class="audio-meta">
            {{ formatTime(currentTime) }} / {{ formatTime(duration) }}
          </div>
        </div>
      </div>

      <!-- Область с кнопкой Play + спиннер -->
      <div class="play-area">
        <!-- Спиннер по центру -->
        <LoadingSpinner 
          v-if="isLoading"
          size="80"
          accent
          class="audio-loader"
        />

        <!-- Большая кнопка Play/Pause -->
        <button class="play-button" @click="togglePlay" :disabled="isLoading">
          <span v-if="!isPlaying">▶</span>
          <span v-else>⏸</span>
        </button>
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
      </div>

      <!-- Аудио-элемент -->
      <audio 
        :key="normalizedSrc"
        ref="audioPlayer" 
        :src="normalizedSrc"
        @timeupdate="onTimeUpdate"
        @loadedmetadata="onLoadedMetadata"
        @ended="onEnded"
        :loop="isRepeat"
      ></audio>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import { useFilePreview } from '@/composables/useFilePreview'
import LoadingSpinner from '../LoadingSpinner/LoadingSpinner.vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  audioPath: string
}>()

const audioPlayer = ref<HTMLAudioElement | null>(null)

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
} = useFilePreview(props.audioPath, audioPlayer)

// Принудительный reload при смене файла
watch(() => props.audioPath, () => {
  if (audioPlayer.value) {
    audioPlayer.value.load()
  }
}, { immediate: true })
</script>

<style scoped>
.audio-preview {
  height: 100%;
  background: #0a0a0a;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px 20px;
  font-family: 'Segoe UI', system-ui, sans-serif;
  position: relative;
  overflow: hidden;
}

.audio-player-container {
  width: 100%;
  max-width: 620px;
  text-align: center;
  z-index: 1;
}

.audio-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20px;
  margin-bottom: 40px;
}

.audio-icon { width: 64px; height: 64px; }

.audio-name {
  font-size: 22px;
  font-weight: 600;
  color: #FF5252;
  margin-bottom: 6px;
}

.audio-meta { font-size: 14px; color: #777; }

/* Область кнопки + спиннер */
.play-area {
  position: relative;
  width: 96px;
  height: 96px;
  margin: 0 auto 40px;
}

.play-button {
  width: 100%;
  height: 100%;
  border-radius: 50%;
  background: #FF5252;
  color: white;
  border: none;
  font-size: 42px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  box-shadow: 0 10px 40px rgba(255, 82, 82, 0.35);
  transition: all 0.2s ease;
  position: relative;
  z-index: 2;
}

.play-button:hover {
  transform: scale(1.1);
  box-shadow: 0 15px 50px rgba(255, 82, 82, 0.5);
}

.play-button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* Спиннер строго по центру кнопки */
.audio-loader {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%);
  z-index: 3;
}

.progress-wrapper { margin-bottom: 30px; }

.progress-container {
  height: 6px;
  background: #333;
  border-radius: 3px;
  cursor: pointer;
  position: relative;
  margin-bottom: 8px;
}

.progress-bar {
  height: 100%;
  background: #FF5252;
  border-radius: 3px;
  transition: width 0.1s linear;
}

.progress-thumb {
  position: absolute;
  top: 50%;
  transform: translate(-50%, -50%);
  width: 14px;
  height: 14px;
  background: white;
  border: 2px solid #FF5252;
  border-radius: 50%;
  box-shadow: 0 2px 8px rgba(0,0,0,0.4);
  pointer-events: none;
  opacity: 0;
  transition: opacity 0.2s;
}

.progress-container:hover .progress-thumb {
  opacity: 1;
}

.time-row {
  display: flex;
  justify-content: space-between;
  font-size: 13px;
  color: #888;
}

.remaining { color: #666; }

.bottom-controls {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 40px;
}

.repeat-btn {
  background: none;
  border: none;
  color: #666;
  cursor: pointer;
  padding: 8px;
  border-radius: 6px;
  transition: all 0.2s;
}

.repeat-btn:hover { color: #aaa; background: rgba(255,255,255,0.05); }
.repeat-btn.active { color: #FF5252; }

.volume-group {
  display: flex;
  align-items: center;
  gap: 12px;
}

.volume-slider {
  width: 180px;
  accent-color: #FF5252;
}
</style>