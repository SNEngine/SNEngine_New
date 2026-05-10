<template>
  <span 
    v-html="svgContent" 
    class="base-icon"
    :style="{ color: color || '#ffffff' }"
  />
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  name: string
  color?: string
}>()

// Жёсткие импорты — теперь никаких сюрпризов
import folderSvg from '@/assets/icons/folder_icon.svg?raw'
import htmlSvg from '@/assets/icons/html_icon.svg?raw'
import cssSvg from '@/assets/icons/css_icon.svg?raw'
import imageSvg from '@/assets/icons/image_icon.svg?raw'
import snScriptSvg from '@/assets/icons/sn_script_icon.svg?raw'

const iconMap: Record<string, string> = {
  folder_icon: folderSvg,
  html_icon: htmlSvg,
  css_icon: cssSvg,
  image_icon: imageSvg,
  sn_script_icon: snScriptSvg,
}

const svgContent = computed(() => {
  return iconMap[props.name] || `<span style="color:#ff5555">❌ ${props.name}</span>`
})
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