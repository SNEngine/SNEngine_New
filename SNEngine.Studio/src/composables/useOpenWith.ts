export function useOpenWith() {
  const openWith = async (filePath: string) => {
    if (!filePath) return

    try {
      await (window as any).electron?.openWith?.(filePath)
    } catch (err) {
      console.error('Не удалось открыть файл:', err)
    }
  }

  return { openWith }
}