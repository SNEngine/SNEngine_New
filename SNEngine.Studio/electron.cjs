// 1. ОТКЛЮЧАЕМ ВАРНИНГИ БЕЗОПАСНОСТИ
process.env['ELECTRON_DISABLE_SECURITY_WARNINGS'] = 'true';

const { app, BrowserWindow, Menu, ipcMain } = require('electron')
const path = require('path')
const fs = require('fs/promises')
const chokidar = require('chokidar')

let mainWindow;
let watcher = null;

// Находим правильный путь к preload в зависимости от того, упаковано приложение или нет
const preloadPath = app.isPackaged 
    ? path.join(app.getAppPath(), 'preload.cjs')
    : path.join(__dirname, 'preload.cjs');

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "SNEngine Studio",
    autoHideMenuBar: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      preload: preloadPath,
      webSecurity: false,
    }
  })

  mainWindow.setTitle("SNEngine Studio")
  Menu.setApplicationMenu(null)

  if (!app.isPackaged) {
    mainWindow.loadURL('http://localhost:5173')
  } else {
    mainWindow.loadFile(path.join(__dirname, 'dist/index.html'))
  }

  // Открываем инструменты разработчика в отдельном окне
  mainWindow.webContents.openDevTools({ mode: 'detach' })
}

// ====================== PROJECT PATH ======================
ipcMain.handle('get-project-path', async () => {
  if (!app.isPackaged) {
    return 'C:/Users/Siphome/Desktop/testBuild'
  }
  
  const projectDir = path.join(app.getAppPath(), '..', 'testBuild')
  try {
    await fs.mkdir(projectDir, { recursive: true })
  } catch (e) {
    console.error('Cannot create testBuild folder:', e)
  }
  return projectDir
});

// ====================== FILE WATCHER ======================
ipcMain.on('start-watcher', (event, dirPath) => {
  if (watcher) watcher.close();

  watcher = chokidar.watch(dirPath, {
    ignored: /(^|[\/\\])\../,
    persistent: true,
    ignoreInitial: true
  });

  watcher.on('all', (eventName, filePath) => {
    if (mainWindow) {
      mainWindow.webContents.send('file-change', { 
        type: eventName, 
        path: filePath 
      });
    }
  });
});

ipcMain.on('stop-watcher', () => {
  if (watcher) {
    watcher.close();
    watcher = null;
  }
});

// ====================== FILE SYSTEM OPERATIONS ======================
ipcMain.handle('read-directory', async (_, dirPath) => {
  try {
    const entries = await fs.readdir(dirPath, { withFileTypes: true })
    
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

ipcMain.handle('read-file', async (_, filePath) => {
  try {
    return await fs.readFile(filePath, 'utf-8')
  } catch (err) {
    console.error('read-file error:', err)
    return ''
  }
})

ipcMain.handle('create-directory', async (_, dirPath) => {
  await fs.mkdir(dirPath, { recursive: true })
})

ipcMain.handle('create-file', async (_, filePath) => {
  await fs.writeFile(filePath, '', 'utf-8')
})

ipcMain.handle('rename-item', async (_, oldPath, newName) => {
  const dir = path.dirname(oldPath)
  const newPath = path.join(dir, newName)
  await fs.rename(oldPath, newPath)
})

ipcMain.handle('delete-item', async (_, itemPath) => {
  const stat = await fs.stat(itemPath)
  if (stat.isDirectory()) {
    await fs.rm(itemPath, { recursive: true, force: true })
  } else {
    await fs.unlink(itemPath)
  }
})

ipcMain.handle('write-file', async (_, filePath, content) => {
  try {
    await fs.writeFile(filePath, content, 'utf-8')
    return { success: true }
  } catch (err) {
    console.error('write-file error:', err)
    return { success: false, error: err.message }
  }
})

// ====================== APP LIFECYCLE ======================
app.whenReady().then(() => {
  createWindow()

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow()
  })
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})