const { contextBridge, ipcRenderer } = require('electron')

contextBridge.exposeInMainWorld('electron', {
  readDirectory: (path) => ipcRenderer.invoke('read-directory', path)
})