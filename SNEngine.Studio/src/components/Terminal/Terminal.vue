<template>
  <div class="terminal">
    <div class="terminal-header">
      <div class="terminal-title">
        <span class="dot red"></span>
        <span class="dot yellow"></span>
        <span class="dot green"></span>
        SNEngine Runtime Console
      </div>
      <button @click="clear" class="clear-btn">Clear</button>
    </div>

    <div 
      ref="terminalBody" 
      class="terminal-body"
      @wheel="handleScroll"
    >
      <div 
        v-for="(line, index) in logs" 
        :key="index" 
        class="log-line" 
        :class="line.type"
      >
        <span class="log-time">{{ line.time }}</span>
        <span class="log-type" :class="line.type">{{ line.prefix }}</span>
        <span class="log-text">{{ line.text }}</span>
      </div>
      <div ref="bottomAnchor" class="bottom-anchor"></div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, nextTick } from 'vue'

const terminalBody = ref<HTMLElement | null>(null)
const bottomAnchor = ref<HTMLElement | null>(null)

const logs = ref<Array<{
  time: string
  type: 'stdout' | 'stderr' | 'system'
  prefix: string
  text: string
}>>([])

let autoScroll = true

const addLog = (log: { type: string; text: string }) => {
  const now = new Date()
  const time = now.toLocaleTimeString('ru-RU', { 
    hour: '2-digit', 
    minute: '2-digit', 
    second: '2-digit' 
  })

  let prefix = ''
  let type: 'stdout' | 'stderr' | 'system' = 'stdout'

  if (log.type === 'stdout') {
    prefix = '>'
    type = 'stdout'
  } else if (log.type === 'stderr') {
    prefix = '!'
    type = 'stderr'
  } else {
    prefix = '#'
    type = 'system'
  }

  logs.value.push({
    time,
    type,
    prefix,
    text: log.text.trim()
  })

  // Ограничиваем количество строк
  if (logs.value.length > 1200) {
    logs.value.shift()
  }

  nextTick(scrollToBottom)
}

const scrollToBottom = () => {
  if (autoScroll && bottomAnchor.value) {
    bottomAnchor.value.scrollIntoView({ behavior: 'instant' })
  }
}

const handleScroll = () => {
  if (!terminalBody.value) return
  const { scrollTop, scrollHeight, clientHeight } = terminalBody.value
  // Автоскролл только если пользователь почти внизу
  autoScroll = (scrollTop + clientHeight) >= scrollHeight - 80
}

const clear = () => {
  logs.value = []
  autoScroll = true
}

onMounted(() => {
  window.electron.preview.onLog(addLog)

  // Пример сообщения при запуске
  setTimeout(() => {
    addLog({ type: 'system', text: 'Runtime Console initialized' })
  }, 300)
})

onUnmounted(() => {
  window.electron.preview.offLog(addLog)
})
</script>

<style scoped>
.terminal {
  display: flex;
  flex-direction: column;
  height: 100%;
  background: #0c0c0c;
  overflow: hidden;
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 14px;
  border-radius: 6px;
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
  font-size: 13px;
  display: flex;
  align-items: center;
  gap: 8px;
}

.dot {
  display: inline-block;
  width: 9px;
  height: 9px;
  border-radius: 50%;
}

.red { background: #ff5f56; }
.yellow { background: #ffbd2e; }
.green { background: #27c93f; }

.clear-btn {
  background: #333;
  color: #aaa;
  border: none;
  padding: 4px 12px;
  font-size: 12px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.2s;
}

.clear-btn:hover {
  background: #444;
  color: white;
}

.terminal-body {
  flex: 1;
  overflow-y: auto;
  padding: 8px 12px;
  background: #0c0c0c;
  color: #0f0;
  line-height: 1.45;
  white-space: pre-wrap;
  word-break: break-all;
  scrollbar-width: thin;
  scrollbar-color: #444 transparent;
}

/* Красивый скроллбар */
.terminal-body::-webkit-scrollbar {
  width: 6px;
}

.terminal-body::-webkit-scrollbar-track {
  background: transparent;
}

.terminal-body::-webkit-scrollbar-thumb {
  background: #444;
  border-radius: 10px;
}

.terminal-body::-webkit-scrollbar-thumb:hover {
  background: #666;
}

.log-line {
  margin-bottom: 3px;
  display: flex;
  align-items: flex-start;
}

.log-time {
  color: #555;
  margin-right: 10px;
  width: 72px;
  flex-shrink: 0;
  font-size: 13px;
}

.log-type {
  margin-right: 8px;
  width: 18px;
  flex-shrink: 0;
  font-weight: bold;
  text-align: center;
}

.stdout .log-type { color: #22c55e; }
.stderr .log-type { color: #ef4444; }
.system .log-type { color: #60a5fa; }

.log-text {
  flex: 1;
  color: #ddd;
  overflow-wrap: break-word;
}

.stdout .log-text { color: #aaffaa; }
.stderr .log-text { color: #ffaaaa; }
.system .log-text { color: #aaaaff; }

.bottom-anchor {
  height: 1px;
  opacity: 0;
}
</style>