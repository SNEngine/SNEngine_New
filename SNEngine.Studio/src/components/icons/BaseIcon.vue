<template>
  <span 
    v-html="svgContent" 
    class="base-icon"
    :style="{ color: color || '#ffffff' }"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { getIcon } from '@/config/icons.config'

const props = defineProps<{
  name: string
  color?: string
}>()

const svgContent = computed(() => getIcon(props.name))
</script>

<style scoped>
.base-icon {
  display: inline-block;
  width: 1.8em;
  height: 1.8em;
  transition: color 0.2s ease;
  flex-shrink: 0;
}

.base-icon :deep(svg) {
  width: 100%;
  height: 100%;
  display: block;
}
</style>