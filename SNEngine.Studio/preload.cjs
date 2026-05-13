const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electron', {
  // === Чтение данных ===
  readDirectory: (path) => ipcRenderer.invoke('read-directory', path),
  readFile: (path) => ipcRenderer.invoke('read-file', path),

  // === Манипуляции с файлами (новое для контекстного меню) ===
  createFile: (path) => ipcRenderer.invoke('create-file', path),
  createDirectory: (path) => ipcRenderer.invoke('create-directory', path),
  renameItem: (oldPath, newName) => ipcRenderer.invoke('rename-item', oldPath, newName),
  deleteItem: (path) => ipcRenderer.invoke('delete-item', path),
  writeFile: (path, content) => ipcRenderer.invoke('write-file', path, content),
  showInExplorer: (path) => ipcRenderer.invoke('show-in-explorer', path),

  // === Методы для File Watcher ===
  onFileChange: (callback) => {
    ipcRenderer.on('file-change', callback)
  },
  offFileChange: (callback) => {
    ipcRenderer.removeListener('file-change', callback)
  },

  // Управление жизненным циклом watcher
  startWatcher: (path) => ipcRenderer.send('start-watcher', path),
  stopWatcher: () => ipcRenderer.send('stop-watcher')
})