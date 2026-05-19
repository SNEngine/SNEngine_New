// src/composables/useGameLauncher.ts
import { ref, readonly } from 'vue'

type LaunchState = 'idle' | 'building' | 'running' | 'stopping' | 'error'

export function useGameLauncher() {
  const state = ref<LaunchState>('idle')
  const fps = ref(0)
  const errorMessage = ref<string | null>(null)
  const isRunning = ref(false)

  let currentProjectPath = ''

  const compileProject = async (projectPath: string): Promise<boolean> => {
    state.value = 'building'
    console.log(`[Launcher] Compiling project: ${projectPath}`)

    // TODO: реальная компиляция
    await new Promise(resolve => setTimeout(resolve, 600))
    console.log('[Launcher] Compilation completed (mock)')
    return true
  }

  const start = async (projectPath: string, width = 800, height = 450) => {
    if (state.value === 'running') {
      await stop()
    }

    currentProjectPath = projectPath
    errorMessage.value = null
    state.value = 'building'

    try {
      const compileSuccess = await compileProject(projectPath)
      if (!compileSuccess) throw new Error('Compilation failed')

      // Запускаем runtime
      const result = await window.electron.preview.start(projectPath, width, height)
      if (!result.success) {
        throw new Error('Failed to start SNEngine Runtime')
      }

      // Только после успешного запуска
      state.value = 'running'
      isRunning.value = true

      console.log(`[Launcher] Preview started successfully`)
    } catch (err: any) {
      console.error('[Launcher] Launch error:', err)
      state.value = 'error'
      errorMessage.value = err.message || 'Unknown error'
      isRunning.value = false
      await stop()
    }
  }

  const stop = async () => {
    if (state.value === 'idle') return

    state.value = 'stopping'

    try {
      await window.electron.preview.stop()
    } catch (err) {
      console.warn('[Launcher] Error stopping preview:', err)
    }

    state.value = 'idle'
    isRunning.value = false
    fps.value = 0
  }

  const restart = async (width = 800, height = 450) => {
    if (!currentProjectPath) return
    await stop()
    await new Promise(r => setTimeout(r, 400))
    await start(currentProjectPath, width, height)
  }

  return {
    state: readonly(state),
    isRunning: readonly(isRunning),
    fps: readonly(fps),
    errorMessage: readonly(errorMessage),
    start,
    stop,
    restart,
    compileProject
  }
}