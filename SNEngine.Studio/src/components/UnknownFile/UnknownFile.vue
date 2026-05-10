<template>
  <div class="unknown-file">
    <div class="content">
      <div class="icon">🚫</div>
      <h2>Формат не поддерживается</h2>
      <p>Студия не может отобразить файл с расширением <span>.{{ extension }}</span></p>
      <code class="path">{{ filePath }}</code>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{ filePath: string }>()
const extension = computed(() => props.filePath.split('.').pop()?.toLowerCase() || '???')
</script>

<style scoped>
.unknown-file {
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #111;
  color: #666;
}
.content { text-align: center; max-width: 400px; padding: 20px; }
.icon { font-size: 64px; margin-bottom: 20px; opacity: 0.5; }
h2 { color: #999; margin-bottom: 10px; }
span { color: #FF5252; font-weight: bold; }
.path { 
  display: block; 
  margin-top: 25px; 
  font-size: 11px; 
  background: #1a1a1a; 
  padding: 8px; 
  border-radius: 4px; 
  word-break: break-all;
}
</style>