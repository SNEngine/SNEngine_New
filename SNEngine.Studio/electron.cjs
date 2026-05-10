const { app, BrowserWindow, Menu, ipcMain } = require('electron')
const path = require('path')
const fs = require('fs/promises')

function createWindow() {
  const win = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "SNEngine Studio",
    autoHideMenuBar: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,     // лучше включить
      preload: path.join(__dirname, 'preload.cjs'),  // ← КЛЮЧЕВОЕ
      webSecurity: false,
    }
  })

  win.setTitle("SNEngine Studio")
  Menu.setApplicationMenu(null)

  if (!app.isPackaged) {
    win.loadURL('http://localhost:5173')
    win.webContents.openDevTools()
  } else {
    win.loadFile(path.join(__dirname, 'dist/index.html'))
  }
}

// ====================== IPC для чтения папок ======================
ipcMain.handle('read-directory', async (_, dirPath) => {
  try {
    const entries = await fs.readdir(dirPath, { withFileTypes: true })
    return entries.map(entry => ({
      name: entry.name,
      path: path.join(dirPath, entry.name),
      isFolder: entry.isDirectory()
    }))
  } catch (err) {
    console.error('read-directory error:', err)
    return []
  }
})

app.whenReady().then(() => {
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})