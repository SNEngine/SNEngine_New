// src/config/icons.config.ts

// Динамический импорт всех SVG
const rawIcons = import.meta.glob('@/assets/icons/*.svg', { 
  query: '?raw',     
  import: 'default', 
  eager: true 
})

// Карта иконок: имя_файла → SVG содержимое
export const iconMap = Object.fromEntries(
  Object.entries(rawIcons).map(([path, content]) => {
    const fileName = path.split('/').pop()?.replace('.svg', '') || ''
    return [fileName, content as string]
  })
)

// ====================== КОНФИГУРАЦИЯ РАСШИРЕНИЙ ======================
const extensionToIcon: Record<string, string> = {
  // Code
  'sn': 'sn_script_icon',
  'cs': 'csharp_icon',
  'html': 'html_icon',
  'htm': 'html_icon',
  'css': 'css_icon',
  'scss': 'css_icon',
  'less': 'css_icon',

  // Media
  'png': 'image_icon',
  'jpg': 'image_icon',
  'jpeg': 'image_icon',
  'gif': 'image_icon',
  'webp': 'image_icon',
  'svg': 'image_icon',
  'bmp': 'image_icon',
  'ico': 'image_icon',

  // Audio
  'mp3': 'audio_icon',
  'wav': 'audio_icon',
  'ogg': 'audio_icon',
  'm4a': 'audio_icon',
  'flac': 'audio_icon',
  'aac': 'audio_icon',

  // === ВИДЕО ===
  'mp4': 'video_icon',
  'webm': 'video_icon',
  'mov': 'video_icon',
  'avi': 'video_icon',
  'mkv': 'video_icon',
  'm4v': 'video_icon',
  'ogv': 'video_icon',

  // Other
  'dll': 'dll_icon',
  'txt': 'file_icon',
  'log': 'file_icon',
  'json': 'file_icon',
  'yml': 'file_icon',
  'yaml': 'file_icon',
  'md': 'file_icon',
}

export function getFileIcon(filename: string): string {
  if (!filename) return 'folder_icon'

  const ext = filename.toLowerCase().split('.').pop() || ''
  return extensionToIcon[ext] || 'file_icon'
}

export function getIcon(name: string): string {
  return iconMap[name] || 
    `<span style="color:#f66;font-size:1.1em">❌ ${name}</span>`
}