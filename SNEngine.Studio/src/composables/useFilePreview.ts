import { ref, computed, watch, unref, type Ref } from 'vue'

export function useFilePreview(path: Ref<string | undefined> | string, mediaRef?: Ref<HTMLMediaElement | null>) {
  const currentPath = computed(() => unref(path) || '')
  
  const isLoading = ref(true)
  const zoomLevel = ref(1)

  const normalizedSrc = computed(() => {
    const p = currentPath.value
    if (!p) return ''
    const normalized = p.replace(/\\/g, '/')
    return `file:///${normalized}`
  })

  const fileName = computed(() => {
    const p = currentPath.value
    return p.split(/[/\\]/).pop() || ''
  })

  // Медиа-состояние
  const isPlaying = ref(false)
  const isRepeat = ref(false)
  const currentTime = ref(0)
  const duration = ref(0)
  const volume = ref(0.8)

  const progress = computed(() => 
    duration.value ? (currentTime.value / duration.value) * 100 : 0
  )

  const remainingTime = computed(() => 
    Math.max(0, duration.value - currentTime.value)
  )

  // Логика зума
  const setZoom = (level: number) => {
    zoomLevel.value = Math.max(0.1, Math.min(level, 5))
  }

  const resetZoom = () => {
    zoomLevel.value = 1
  }

  const handleWheelZoom = (e: WheelEvent) => {
    if (e.ctrlKey || e.metaKey) {
      const step = 0.1
      const delta = e.deltaY > 0 ? -step : step
      setZoom(zoomLevel.value + delta)
    }
  }

  // Управление воспроизведением
  const togglePlay = () => {
    if (!mediaRef?.value) return
    if (isPlaying.value) {
      mediaRef.value.pause()
    } else {
      mediaRef.value.play()
    }
    isPlaying.value = !isPlaying.value
  }

  const toggleRepeat = () => {
    isRepeat.value = !isRepeat.value
  }

  const formatTime = (time: number) => {
    const min = Math.floor(time / 60)
    const sec = Math.floor(time % 60)
    return `${min}:${sec.toString().padStart(2, '0')}`
  }

  // Обработчики событий
  const onLoadingFinished = () => {
    isLoading.value = false
  }

  const onTimeUpdate = () => {
    if (mediaRef?.value) currentTime.value = mediaRef.value.currentTime
  }

  const onLoadedMetadata = () => {
    if (mediaRef?.value) {
      duration.value = mediaRef.value.duration
      isLoading.value = false
    }
  }

  const onEnded = () => {
    isPlaying.value = false
    if (!isRepeat.value) currentTime.value = 0
  }

  // Автоматическое обновление громкости
  watch(volume, (newVolume) => {
    if (mediaRef?.value) {
      mediaRef.value.volume = newVolume
    }
  })

  // Сброс при смене файла
  watch(currentPath, () => {
    isLoading.value = true
    resetZoom()
    if (mediaRef?.value) {
      mediaRef.value.load()
    }
    isPlaying.value = false
    currentTime.value = 0
  })

  return {
    normalizedSrc,
    fileName,
    isLoading,
    zoomLevel,
    isPlaying,
    isRepeat,
    progress,
    remainingTime,
    currentTime,
    duration,
    volume,
    togglePlay,
    toggleRepeat,
    formatTime,
    setZoom,
    resetZoom,
    handleWheelZoom,
    onLoadingFinished,
    onTimeUpdate,
    onLoadedMetadata,
    onEnded
  }
}