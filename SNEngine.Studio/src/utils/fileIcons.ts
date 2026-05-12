export function getFileIcon(filename: string): string {
  const ext = filename.toLowerCase().split('.').pop() || ''

  if (!ext) return 'folder_icon'

  switch (ext) {
    case 'sn':
    case 'script':
      return 'sn_script_icon'

    case 'html':
    case 'htm':
      return 'html_icon'

    case 'css':
      return 'css_icon'

    case 'png':
    case 'jpg':
    case 'jpeg':
    case 'gif':
    case 'webp':
    case 'svg':
    case 'bmp':
      return 'image_icon'

    case 'mp3':
    case 'wav':
    case 'ogg':
    case 'm4a':
    case 'flac':
    case 'aac':
      return 'audio_icon'           // ← Теперь правильно

    case 'cs':
      return 'csharp_icon'

    case 'dll':
      return 'dll_icon'

    case 'json':
    case 'yml':
    case 'yaml':
    case 'toml':
    case 'config':
      return 'file_icon'            // Более логично, чем folder

    default:
      return 'unknown_icon'
  }
}