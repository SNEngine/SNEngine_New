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
  let lastFrameId = 0

  // ========== Запуск превью ==========
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
      window.electron.preview.stop()
    } finally {
      isLoading.value = false
    }
  }

  // ========== Инициализация Canvas ==========
  const initCanvas = (width: number, height: number) => {
    if (!canvasRef.value) return

    canvasRef.value.width = width
    canvasRef.value.height = height

    ctxRef.value = canvasRef.value.getContext('2d', { alpha: false })
    if (ctxRef.value) {
      ctxRef.value.imageSmoothingEnabled = false
    }
  }

  // ========== Вертикальный flip ImageData (OpenGL → Canvas) ==========
  const flipImageDataVertically = (imageData: ImageData): ImageData => {
    const { width, height, data } = imageData
    const flipped = ctxRef.value!.createImageData(width, height)
    const stride = width * 4

    for (let y = 0; y < height; y++) {
      const sourceY = height - 1 - y
      const sourceRow = data.subarray(sourceY * stride, (sourceY + 1) * stride)
      flipped.data.set(sourceRow, y * stride)
    }

    return flipped
  }

// ========== Главный рендер-луп ==========
const startRenderLoop = () => {
  const loop = async () => {
    const frame = await window.electron.preview.getFrame()

    if (frame && frame.data && frame.width && frame.height && ctxRef.value) {
      try {
        // Динамически меняем размер canvas под реальное разрешение игры
        if (canvasRef.value!.width !== frame.width || canvasRef.value!.height !== frame.height) {
          canvasRef.value!.width = frame.width
          canvasRef.value!.height = frame.height

          ctxRef.value = canvasRef.value!.getContext('2d', { alpha: false })
          if (ctxRef.value) {
            ctxRef.value.imageSmoothingEnabled = false
          }
        }

        let imageData = new ImageData(
          new Uint8ClampedArray(frame.data),
          frame.width,
          frame.height
        )

        // ←←← Исправление переворота
        imageData = flipImageDataVertically(imageData)

        ctxRef.value!.putImageData(imageData, 0, 0)
      } catch (e) {
        console.warn('[Preview] Failed to draw frame:', e)
      }
    }

    // FPS
    frameCount++
    const now = Date.now()
    if (now - lastFrameTime.value >= 1000) {
      fps.value = frameCount
      frameCount = 0
      lastFrameTime.value = now
    }

    animationFrameId = requestAnimationFrame(loop)
  }
  loop()
}

  const stopPreview = async () => {
    cancelAnimationFrame(animationFrameId)
    await window.electron.preview.stop()
    isRunning.value = false
  }

  onUnmounted(() => {
    stopPreview()
  })

  return {
    canvasRef,
    isRunning: readonly(isRunning),
    isLoading: readonly(isLoading),
    fps: readonly(fps),
    startPreview,
    stopPreview
  }
}