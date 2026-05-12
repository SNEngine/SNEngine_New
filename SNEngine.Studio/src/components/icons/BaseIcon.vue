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

// Импорт всех иконок
import folderSvg from '@/assets/icons/folder_icon.svg?raw'
import fileSvg from '@/assets/icons/file_icon.svg?raw'
import htmlSvg from '@/assets/icons/html_icon.svg?raw'
import cssSvg from '@/assets/icons/css_icon.svg?raw'
import imageSvg from '@/assets/icons/image_icon.svg?raw'
import snScriptSvg from '@/assets/icons/sn_script_icon.svg?raw'
import csharpSvg from '@/assets/icons/csharp_icon.svg?raw'
import dllSvg from '@/assets/icons/dll_icon.svg?raw'
import unknownSvg from '@/assets/icons/unknown_icon.svg?raw'
import deleteSvg from '@/assets/icons/delete_icon.svg?raw'
import audioSvg from '@/assets/icons/audio_icon.svg?raw'
import volumeSvg from '@/assets/icons/volume_icon.svg?raw'
import repeatSvg from '@/assets/icons/repeat_icon.svg?raw'
import repeatOneSvg from '@/assets/icons/repeat_one_icon.svg?raw'   // ← НОВОЕ

const iconMap: Record<string, string> = {
  folder_icon: folderSvg,
  file_icon: fileSvg,
  html_icon: htmlSvg,
  css_icon: cssSvg,
  image_icon: imageSvg,
  sn_script_icon: snScriptSvg,
  csharp_icon: csharpSvg,
  dll_icon: dllSvg,
  unknown_icon: unknownSvg,
  delete_icon: deleteSvg,
  audio_icon: audioSvg,
  volume_icon: volumeSvg,
  repeat_icon: repeatSvg,
  repeat_one_icon: repeatOneSvg,     // ← Добавлено
}

const svgContent = computed(() => {
  return iconMap[props.name] || 
    `<span style="color:#f66;font-size:1.1em">❌ ${props.name}</span>`
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