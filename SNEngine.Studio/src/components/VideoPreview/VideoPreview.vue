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
          ref="videoPlayer" 
          :src="videoSrc"
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
import { ref, onMounted, watch, computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{ videoPath: string }>()

const videoPlayer = ref<HTMLVideoElement | null>(null)
const isPlaying = ref(false)
const isRepeat = ref(false)
const currentTime = ref(0)
const duration = ref(0)
const volume = ref(0.8)
const progress = ref(0)

const fileName = props.videoPath.split(/[/\\]/).pop() || 'video'
const videoSrc = `file://${props.videoPath.replace(/\\/g, '/')}`

const remainingTime = computed(() => Math.max(0, duration.value - currentTime.value))

const togglePlay = () => {
  if (!videoPlayer.value) return
  isPlaying.value ? videoPlayer.value.pause() : videoPlayer.value.play()
  isPlaying.value = !isPlaying.value
}

const toggleRepeat = () => isRepeat.value = !isRepeat.value

const seek = (e: MouseEvent) => {
  if (!videoPlayer.value) return
  const rect = (e.currentTarget as HTMLElement).getBoundingClientRect()
  const percent = (e.clientX - rect.left) / rect.width
  videoPlayer.value.currentTime = percent * duration.value
}

const onTimeUpdate = () => {
  if (!videoPlayer.value) return
  currentTime.value = videoPlayer.value.currentTime
  progress.value = duration.value ? (currentTime.value / duration.value) * 100 : 0
}

const onLoadedMetadata = () => {
  if (videoPlayer.value) duration.value = videoPlayer.value.duration
}

const onEnded = () => {
  isPlaying.value = false
  progress.value = 0
}

const setVolume = () => {
  if (videoPlayer.value) videoPlayer.value.volume = volume.value
}

const toggleFullscreen = () => {
  if (!videoPlayer.value) return
  document.fullscreenElement 
    ? document.exitFullscreen() 
    : videoPlayer.value.requestFullscreen()
}

const formatTime = (time: number) => {
  const min = Math.floor(time / 60)
  const sec = Math.floor(time % 60)
  return `${min}:${sec.toString().padStart(2, '0')}`
}

onMounted(() => {
  if (videoPlayer.value) videoPlayer.value.volume = volume.value
})

watch(() => props.videoPath, () => {
  if (videoPlayer.value) {
    videoPlayer.value.load()
    isPlaying.value = false
    currentTime.value = 0
    progress.value = 0
  }
})
</script>

<style scoped>
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
  text-align: left;
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
}

video {
  width: 100%;
  max-height: 65vh;
  display: block;
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
  z-index: 10;
  box-shadow: 0 8px 25px rgba(255, 82, 82, 0.4);
}

.play-overlay.hidden {
  opacity: 0;
  pointer-events: none;
}

/* Прогресс и контролы */
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