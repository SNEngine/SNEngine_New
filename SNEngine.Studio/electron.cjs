// 1. ОТКЛЮЧАЕМ ВАРНИНГИ БЕЗОПАСНОСТИ
process.env['ELECTRON_DISABLE_SECURITY_WARNINGS'] = 'true';

const { app, BrowserWindow, Menu, ipcMain } = require('electron')
const path = require('path')
const fs = require('fs') // Используем обычный fs для доступа к sync методам и promises
const chokidar = require('chokidar')

let mainWindow;
let watcher = null;

const preloadPath = app.isPackaged 
    ? path.join(app.getAppPath(), 'preload.cjs')
    : path.join(__dirname, 'preload.cjs');

function createWindow() {
  // Путь к иконке (работает и в dev, и в собранном приложении)
  const iconPath = app.isPackaged
    ? path.join(app.getAppPath(), '../build/icons/png/512x512.png')
    : path.join(__dirname, 'build/icons/png/512x512.png')

  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "SNEngine Studio",
    autoHideMenuBar: true,
    
    // ←←←←←←←←←←←←←←←←← ИКОНКА ЗДЕСЬ
    icon: iconPath,
    
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

  mainWindow.webContents.openDevTools({ mode: 'detach' })
}

// ====================== PROJECT PATH ======================
ipcMain.handle('get-project-path', async () => {
  if (!app.isPackaged) {
    return 'C:/Users/Siphome/Desktop/testBuild'
  }
  
  const projectDir = path.join(app.getAppPath(), '..', 'testBuild')
  try {
    await fs.promises.mkdir(projectDir, { recursive: true })
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
    const entries = await fs.promises.readdir(dirPath, { withFileTypes: true })
    
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
    return await fs.promises.readFile(filePath, 'utf-8')
  } catch (err) {
    console.error('read-file error:', err)
    return ''
  }
})

ipcMain.handle('create-directory', async (_, dirPath) => {
  await fs.promises.mkdir(dirPath, { recursive: true })
})

ipcMain.handle('create-file', async (_, filePath) => {
  await fs.promises.writeFile(filePath, '', 'utf-8')
})

ipcMain.handle('rename-item', async (_, oldPath, newName) => {
  const dir = path.dirname(oldPath)
  const newPath = path.join(dir, newName)
  await fs.promises.rename(oldPath, newPath)
})

ipcMain.handle('delete-item', async (_, itemPath) => {
  const stat = await fs.promises.stat(itemPath)
  if (stat.isDirectory()) {
    await fs.promises.rm(itemPath, { recursive: true, force: true })
  } else {
    await fs.promises.unlink(itemPath)
  }
})

ipcMain.handle('duplicate-item', async (event, originalPath) => {
  try {
    const dir = path.dirname(originalPath);
    const ext = path.extname(originalPath);
    const baseName = path.basename(originalPath, ext);

    let counter = 1;
    let newPath = path.join(dir, `${baseName} — копия${ext}`);

    while (fs.existsSync(newPath)) {
      counter++;
      newPath = path.join(dir, `${baseName} — копия (${counter})${ext}`);
    }

    await fs.promises.copyFile(originalPath, newPath);
    return newPath;
  } catch (err) {
    console.error('duplicate-item error:', err);
    throw err;
  }
})

ipcMain.handle('write-file', async (_, filePath, content) => {
  try {
    await fs.promises.writeFile(filePath, content, 'utf-8')
    return { success: true }
  } catch (err) {
    console.error('write-file error:', err)
    return { success: false, error: err.message }
  }
})

ipcMain.handle('show-in-explorer', async (_, filePath) => {
  const { shell } = require('electron')
  shell.showItemInFolder(filePath)
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