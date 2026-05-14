const { contextBridge, ipcRenderer, webUtils } = require('electron')

contextBridge.exposeInMainWorld('electron', {
  // === Чтение данных ===
  readDirectory: (path) => ipcRenderer.invoke('read-directory', path),
  readFile: (path) => ipcRenderer.invoke('read-file', path),

  // === Манипуляции с файлами ===
  createFile: (path) => ipcRenderer.invoke('create-file', path),
  createDirectory: (path) => ipcRenderer.invoke('create-directory', path),
  renameItem: (oldPath, newName) => ipcRenderer.invoke('rename-item', oldPath, newName),
  deleteItem: (path) => ipcRenderer.invoke('delete-item', path),
  duplicateItem: (path) => ipcRenderer.invoke('duplicate-item', path),
  writeFile: (path, content) => ipcRenderer.invoke('write-file', path, content),
  showInExplorer: (path) => ipcRenderer.invoke('show-in-explorer', path),
  showFileProperties: (path) => ipcRenderer.invoke('show-file-properties', path),
  getFileStats: (path) => ipcRenderer.invoke('get-file-stats', path),
  openWith: (filePath) => ipcRenderer.invoke('open-with', filePath),
    // Внутренний Drag & Drop
  moveItem: (sourcePath, targetDir) => ipcRenderer.invoke('move-item', sourcePath, targetDir),
  copyItem: (sourcePath, targetDir) => ipcRenderer.invoke('copy-item', sourcePath, targetDir),

  // === Drag & Drop ===
getFilePath: (file) => webUtils.getPathForFile(file), // Теперь это заработает
  copyFiles: (targetDir, filePaths) => ipcRenderer.invoke('copy-files', targetDir, filePaths),
  // === Методы для File Watcher ===
  onFileChange: (callback) => {
    ipcRenderer.on('file-change', callback)
  },
  offFileChange: (callback) => {
    ipcRenderer.removeListener('file-change', callback)
  },

  // Управление watcher
  startWatcher: (path) => ipcRenderer.send('start-watcher', path),
  stopWatcher: () => ipcRenderer.send('stop-watcher')
})