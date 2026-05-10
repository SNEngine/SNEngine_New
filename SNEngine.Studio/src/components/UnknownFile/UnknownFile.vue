<template>
  <div class="unknown-file">
    <div class="content">
      <!-- Используем нашу BaseIcon вместо эмодзи -->
      <BaseIcon 
        name="unknown_icon" 
        color="#FF5252"
        class="unknown-icon"
      />
      
      <h2>Формат не поддерживается</h2>
      <p>Студия не может отобразить файл с расширением <span>.{{ extension }}</span></p>
      <code class="path">{{ filePath }}</code>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'   // ← правильный путь

const props = defineProps<{ filePath: string }>()

const extension = computed(() => 
  props.filePath.split('.').pop()?.toLowerCase() || '???'
)
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

.content {
  text-align: center;
  max-width: 400px;
  padding: 20px;
}

.unknown-icon {
  width: 90px;
  height: 90px;
  margin-bottom: 20px;
  opacity: 0.9;
}

h2 {
  color: #999;
  margin-bottom: 10px;
}

span {
  color: #FF5252;
  font-weight: bold;
}

.path {
  display: block;
  margin-top: 25px;
  font-size: 11px;
  background: #1a1a1a;
  padding: 8px;
  border-radius: 4px;
  word-break: break-all;
  color: #888;
}
</style>