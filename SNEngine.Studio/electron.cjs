// 1. ОТКЛЮЧАЕМ ВАРНИНГИ БЕЗОПАСНОСТИ [cite: 2]
process.env['ELECTRON_DISABLE_SECURITY_WARNINGS'] = 'true';

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
      contextIsolation: true,     
      preload: path.join(__dirname, 'preload.cjs'),
      webSecurity: false, // Оставляем false для доступа к файлам [cite: 2]
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
    
    // Сортируем: сначала папки, потом файлы (по алфавиту)
    const sortedEntries = entries.map(entry => ({
      name: entry.name,
      path: path.join(dirPath, entry.name),
      isFolder: entry.isDirectory(),
      isOpen: false
    })).sort((a, b) => {
      if (a.isFolder && !b.isFolder) return -1;
      if (!a.isFolder && b.isFolder) return 1;
      return a.name.localeCompare(b.name);
    });

    return sortedEntries;
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