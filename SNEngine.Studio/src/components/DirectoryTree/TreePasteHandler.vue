<template>
  <div @paste="handlePaste">
    <slot />
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  basePath: string
  getCurrentlySelectedItems?: () => any[]
  copyItem?: (sourcePath: string, targetDir: string) => Promise<boolean>
  refresh?: () => void
  handleDropFromClipboard?: (targetDir: string, filePaths: string[]) => Promise<boolean>
}>()

const emit = defineEmits<{
  (e: 'pasted'): void
}>()

async function handlePaste(e: ClipboardEvent) {
  // 1. Files from OS (Explorer)
  const files = Array.from(e.clipboardData?.files || [])
  if (files.length > 0) {
    const lastSelected = props.getCurrentlySelectedItems?.().find((i: any) => i.isFolder)
    const targetPath = lastSelected?.path || props.basePath

    // Note: Getting real file paths from File objects usually requires Electron main process
    const filePaths = files
      .map((f: any) => (window as any).electron?.getFilePath?.(f))
      .filter(Boolean)

    if (filePaths.length > 0 && props.handleDropFromClipboard) {
      const success = await props.handleDropFromClipboard(targetPath, filePaths)
      if (success) props.refresh?.()
      emit('pasted')
    }
    return
  }

  // 2. Internal path (Ctrl+C from within the app)
  const text = e.clipboardData?.getData('text/plain')?.trim()
  if (text && text.startsWith(props.basePath) && props.copyItem) {
    const lastSelected = props.getCurrentlySelectedItems?.().find((i: any) => i.isFolder)
    const targetPath = lastSelected?.path || props.basePath

    const success = await props.copyItem(text, targetPath)
    if (success) props.refresh?.()
    emit('pasted')
  }
}
</script>
