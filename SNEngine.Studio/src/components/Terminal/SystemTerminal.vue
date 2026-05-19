<template>
  <div class="system-terminal">
    <div class="terminal-header">
      <div class="terminal-title">
        <span class="dot red"></span>
        <span class="dot yellow"></span>
        <span class="dot green"></span>
        System Terminal — {{ shellName }}
      </div>
      
      <div class="controls">
        <select v-model="selectedShell" class="shell-select" @change="restartShell">
          <option value="powershell">PowerShell</option>
          <option value="cmd">Command Prompt (cmd)</option>
          <option value="bash">Bash (Git Bash)</option>
        </select>
        <button @click="clear" class="clear-btn">Clear</button>
        <button @click="restartShell" class="restart-btn">Restart</button>
      </div>
    </div>

    <div ref="terminalContainer" class="terminal-body"></div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'
import { Terminal } from 'xterm'
import { FitAddon } from 'xterm-addon-fit'
import 'xterm/css/xterm.css'

const terminalContainer = ref<HTMLElement | null>(null)
const selectedShell = ref<'powershell' | 'cmd' | 'bash'>('powershell')
const shellName = ref('PowerShell')
const currentProjectPath = ref('')

const terminalId = 'system-terminal-main'
let term: any = null
let fitAddon: any = null
let removeDataListener: any = null
let removeExitListener: any = null

// Получение пути проекта
const loadProjectPath = async () => {
  try {
    currentProjectPath.value = await window.electron.getProjectPath()
    console.log('[SystemTerminal] Project path loaded:', currentProjectPath.value)
  } catch (e) {
    console.warn('Не удалось получить путь проекта')
    currentProjectPath.value = process.env.USERPROFILE || ''
  }
}

// Запуск терминала
const startShell = async () => {
  window.electron.terminal.kill(terminalId)
  term?.reset?.()
  term?.clear?.()

  await loadProjectPath() // ← важно: await!

  window.electron.terminal.init(
    terminalId, 
    selectedShell.value, 
    currentProjectPath.value || undefined
  )
}

const clear = () => {
  term?.clear()
  term?.write('\x1b[2J\x1b[H')

  const cmd = selectedShell.value === 'powershell' ? 'Clear-Host\r\n' :
              selectedShell.value === 'cmd' ? 'cls\r\n' : 'clear\r\n'

  window.electron.terminal.write(terminalId, cmd)
  term?.focus()
}

const restartShell = () => startShell()

onMounted(async () => {
  term = new Terminal({
    theme: { 
      background: '#0c0c0c', 
      foreground: '#ffffff', 
      cursor: '#FF5252' 
    },
    cursorBlink: true,
    fontFamily: 'Consolas, "Courier New", monospace',
    fontSize: 14,
    convertEol: true,
    scrollback: 2000,
  })

  fitAddon = new FitAddon()
  term.loadAddon(fitAddon)
  term.open(terminalContainer.value!)

  await nextTick()
  fitAddon.fit()

  await startShell() // ← теперь с await

  removeDataListener = window.electron.terminal.onData(terminalId, (data: any) => {
    term.write(data.text)
  })

  removeExitListener = window.electron.terminal.onExit(terminalId, () => {
    term.write('\r\n\x1b[31m[Shell exited]\x1b[0m\r\n')
  })

  term.onData((data: string) => {
    window.electron.terminal.write(terminalId, data)
  })

  window.addEventListener('resize', () => fitAddon.fit())
  term.focus()
})

onUnmounted(() => {
  removeDataListener?.()
  removeExitListener?.()
  window.electron.terminal.kill(terminalId)
  term?.dispose()
})
</script>

<style scoped>
/* Стили оставляем без изменений */
.system-terminal {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #0c0c0c;
  font-family: monospace;
  font-size: 14px;
  overflow: hidden;
}

.terminal-header {
  height: 36px;
  background: #1f1f1f;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 12px;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
  user-select: none;
}

.terminal-title {
  color: #ccc;
  display: flex;
  align-items: center;
  gap: 8px;
}

.dot {
  display: inline-block;
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.red { background: #ff5f56; }
.yellow { background: #ffbd2e; }
.green { background: #27c93f; }

.controls {
  display: flex;
  gap: 8px;
}

.shell-select, .clear-btn, .restart-btn {
  background: #222;
  color: #ddd;
  border: 1px solid #444;
  padding: 4px 10px;
  border-radius: 4px;
  font-size: 12px;
  cursor: pointer;
}

.shell-select:hover, .clear-btn:hover, .restart-btn:hover {
  background: #333;
  color: white;
}

.terminal-body {
  flex: 1;
  background: #0c0c0c;
  position: relative;
  overflow: hidden;
  min-height: 0;
}

:deep(.xterm) {
  height: 100% !important;
  padding: 4px 8px;
}

:deep(.xterm-viewport) {
  background-color: #0c0c0c !important;
  overflow: hidden !important;
}
</style>