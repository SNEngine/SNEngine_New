// src/composables/useGamePreview.ts
import { ref, onUnmounted, readonly } from 'vue'

export function useGamePreview() {
  const canvasRef = ref<HTMLCanvasElement | null>(null)
  const ctxRef = ref<CanvasRenderingContext2D | null>(null)

  const isRunning = ref(false)
  const isLoading = ref(false)
  const fps = ref(0)
  const lastFrameTime = ref(Date.now())

  let animationFrameId = 0
  let frameCount = 0
  let lastRenderTime = 0

  const TARGET_FPS = 60
  const FRAME_TIME = 1000 / TARGET_FPS

  let currentImageData: ImageData | null = null
  let flippedImageData: ImageData | null = null

  // ====================== ИНИЦИАЛИЗАЦИЯ CANVAS ======================
  const initCanvas = (width: number, height: number) => {
    if (!canvasRef.value) return

    canvasRef.value.width = width
    canvasRef.value.height = height

    ctxRef.value = canvasRef.value.getContext('2d', { 
      alpha: false, 
      willReadFrequently: false 
    })

    if (ctxRef.value) {
      ctxRef.value.imageSmoothingEnabled = false
    }
  }

  // ====================== ВЕРТИКАЛЬНЫЙ FLIP ======================
  const flipImageDataVertically = (src: ImageData, dst: ImageData) => {
    const { width, height, data } = src
    const stride = width * 4

    for (let y = 0; y < height; y++) {
      const srcY = height - 1 - y
      dst.data.set(
        data.subarray(srcY * stride, (srcY + 1) * stride), 
        y * stride
      )
    }
  }

  // ====================== ОСНОВНОЙ РЕНДЕР ЦИКЛ ======================
  const startRenderLoop = () => {
    const loop = async () => {
      const now = Date.now()

      if (now - lastRenderTime < FRAME_TIME) {
        animationFrameId = requestAnimationFrame(loop)
        return
      }

      lastRenderTime = now

      try {
        const frame = await window.electron.preview.getFrame()

        if (frame?.data && frame.width && frame.height && ctxRef.value) {
          // Адаптация размера canvas под пришедший кадр
          if (canvasRef.value!.width !== frame.width || canvasRef.value!.height !== frame.height) {
            initCanvas(frame.width, frame.height)
          }

          const uint8 = new Uint8ClampedArray(frame.data)

          if (!currentImageData || currentImageData.width !== frame.width) {
            currentImageData = new ImageData(uint8, frame.width, frame.height)
            flippedImageData = ctxRef.value!.createImageData(frame.width, frame.height)
          } else {
            currentImageData.data.set(uint8)
          }

          flipImageDataVertically(currentImageData, flippedImageData!)
          ctxRef.value!.putImageData(flippedImageData!, 0, 0)
        }
      } catch (e) {
        console.warn('[Preview Render] draw error:', e)
      }

      // FPS Counter
      frameCount++
      if (now - lastFrameTime.value >= 1000) {
        fps.value = frameCount
        frameCount = 0
        lastFrameTime.value = now
      }

      animationFrameId = requestAnimationFrame(loop)
    }

    loop()
  }

  // ====================== ПОЛНЫЙ ЗАПУСК (совместимость со старым кодом) ======================
  const startPreview = async (projectPath: string, width = 800, height = 450) => {
    if (isRunning.value) await stopPreview()

    isLoading.value = true

    try {
      const result = await window.electron.preview.start(projectPath, width, height)
      if (!result.success) throw new Error('Failed to start preview')

      initCanvas(width, height)
      isRunning.value = true
      startRenderLoop()
    } catch (err) {
      console.error('[Preview] Start failed:', err)
      await window.electron.preview.stop().catch(() => {})
    } finally {
      isLoading.value = false
    }
  }

  // ====================== ТОЛЬКО РЕНДЕР (рекомендуется с Launcher) ======================
const startRenderOnly = (width = 800, height = 450) => {
    if (isRunning.value) return

    setTimeout(() => {
      initCanvas(width, height)
      isRunning.value = true
      startRenderLoop()
    }, 0)
  }

  // ====================== ОСТАНОВКА ======================
  const stopPreview = async () => {
    if (animationFrameId) {
      cancelAnimationFrame(animationFrameId)
      animationFrameId = 0
    }

    currentImageData = null
    flippedImageData = null
    isRunning.value = false
    fps.value = 0

    try {
      await window.electron.preview.stop()
    } catch (e) {
      console.warn('[Preview] Stop error:', e)
    }
  }

  // Cleanup
  onUnmounted(stopPreview)

  return {
    canvasRef,
    isRunning: readonly(isRunning),
    isLoading: readonly(isLoading),
    fps: readonly(fps),

    // Методы
    startPreview,     // Полный запуск (для старого использования)
    startRenderOnly,  // Только рендер (используй вместе с useGameLauncher)
    stopPreview
  }
}