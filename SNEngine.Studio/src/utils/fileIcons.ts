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
      return 'image_icon'

    case 'cs':
      return 'csharp_icon'

    case 'dll':
      return 'dll_icon'

    case 'json':
    case 'yml':
    case 'yaml':
    case 'toml':
    case 'config':
      return 'folder_icon'   // или сделаем config_icon позже

    default:
      return 'unknown_icon'
  }
}