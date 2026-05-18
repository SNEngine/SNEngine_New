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

    <div ref="terminalBody" class="terminal-body" @wheel="handleScroll">
      <div v-for="(line, index) in logs" :key="index" class="log-line" :class="line.type">
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
  const time = now.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit', second: '2-digit' })

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
    text: log.text
  })

  // Ограничиваем количество строк
  if (logs.value.length > 1000) {
    logs.value.shift()
  }

  nextTick(scrollToBottom)
}

const scrollToBottom = () => {
  if (autoScroll && bottomAnchor.value) {
    bottomAnchor.value.scrollIntoView({ behavior: 'smooth' })
  }
}

const handleScroll = () => {
  if (!terminalBody.value) return
  const { scrollTop, scrollHeight, clientHeight } = terminalBody.value
  autoScroll = (scrollTop + clientHeight) >= scrollHeight - 50
}

const clear = () => {
  logs.value = []
}

onMounted(() => {
  // Подписываемся на логи из C#
  window.electron.preview.onLog(addLog)

  // Пример системного сообщения
  setTimeout(() => {
    addLog({ type: 'system', text: 'Terminal initialized' })
  }, 500)
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
  border: 1px solid #333;
  border-radius: 6px;
  overflow: hidden;
  font-family: 'Consolas', 'Courier New', monospace;
  font-size: 14px;
}

.terminal-header {
  height: 36px;
  background: #1f1f1f;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 12px;
  border-bottom: 1px solid #333;
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
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.red { background: #ff5f56; }
.yellow { background: #ffbd2e; }
.green { background: #27c93f; }

.clear-btn {
  background: #333;
  color: #aaa;
  border: none;
  padding: 3px 10px;
  font-size: 12px;
  border-radius: 3px;
  cursor: pointer;
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
  line-height: 1.4;
  white-space: pre-wrap;
  word-break: break-all;
}

.log-line {
  margin-bottom: 2px;
  display: flex;
}

.log-time {
  color: #555;
  margin-right: 8px;
  width: 70px;
  flex-shrink: 0;
}

.log-type {
  margin-right: 8px;
  width: 20px;
  flex-shrink: 0;
  font-weight: bold;
}

.stdout .log-type { color: #0f0; }
.stderr .log-type { color: #ff5555; }
.system .log-type { color: #55aaff; }

.log-text {
  flex: 1;
  color: #ddd;
}

.stdout .log-text { color: #aaffaa; }
.stderr .log-text { color: #ffaaaa; }
.system .log-text { color: #aaaaff; }

.bottom-anchor {
  height: 1px;
}
</style>