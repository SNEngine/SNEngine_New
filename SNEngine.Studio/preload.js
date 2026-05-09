const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electronAPI', {
  runScript: (code) => ipcRenderer.invoke('run-script', code),
  saveFile: (path, content) => ipcRenderer.invoke('save-file', path, content)
})

console.log('✅ Preload script loaded')