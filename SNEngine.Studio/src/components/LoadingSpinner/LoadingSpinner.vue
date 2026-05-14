<template>
  <div class="loading-spinner" :style="{ width: `${size}px`, height: `${size}px` }">
    <BaseIcon 
      name="loading-spinner" 
      class="spinner-icon"
      :class="{ accent }"
    />
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  size?: number      // размер в пикселях
  accent?: boolean   // использовать акцентный цвет
}>()

const { size = 32, accent = false } = props
</script>

<style scoped>
.loading-spinner {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.spinner-icon {
  animation: rotate 1.2s linear infinite;
  width: 100%;
  height: 100%;
}

/* Анимация вращения */
@keyframes rotate {
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
}

.spinner-icon.accent {
  filter: brightness(1.1);
}
</style>