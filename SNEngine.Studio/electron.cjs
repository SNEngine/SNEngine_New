// electron.cjs
process.env['ELECTRON_DISABLE_SECURITY_WARNINGS'] = 'true';

const { app, BrowserWindow, Menu, ipcMain, shell } = require('electron');
const path = require('path');
const fs = require('fs');
const chokidar = require('chokidar');

let mainWindow = null;
let watcher = null;

// ====================== PRELOAD PATH ======================
const getPreloadPath = () => {
  if (app.isPackaged) {
    return path.join(process.resourcesPath, 'app.asar', 'preload.cjs');
  }
  return path.join(__dirname, 'preload.cjs');
};

// ====================== ICON PATH (ИСПРАВЛЕНО) ======================
const getIconPath = () => {
  // Для Windows/Linux лучше всего использовать PNG или ICO. 
  // Если вы добавили его в 'files', путь будет консистентным.
  const iconPath = path.join(__dirname, 'build', 'icons', 'png', '512x512.png');
  
  if (fs.existsSync(iconPath)) {
    return iconPath;
  }
  
  // Резервный вариант (например, если в dist)
  return path.join(__dirname, 'icon.png'); 
};

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    title: "SNEngine Studio",
    autoHideMenuBar: true,
    icon: getIconPath(),   // ← теперь надёжно

    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: false,
      preload: getPreloadPath(),
      webSecurity: false,
    }
  });

  Menu.setApplicationMenu(null);

  if (!app.isPackaged) {
    mainWindow.loadURL('http://localhost:5173');
    mainWindow.webContents.openDevTools({ mode: 'detach' });
  } else {
    mainWindow.loadFile(path.join(__dirname, 'dist', 'index.html'));
    // mainWindow.webContents.openDevTools({ mode: 'detach' });
  }
}

// ====================== PROJECT PATH ======================
ipcMain.handle('get-project-path', async () => {
  return 'C:/Users/Siphome/Desktop/testBuild';
});

// ====================== FILE WATCHER ======================
ipcMain.on('start-watcher', (event, dirPath) => {
  if (watcher) watcher.close();

  watcher = chokidar.watch(dirPath, {
    ignored: /(^|[\/\\])\../,
    persistent: true,
    ignoreInitial: true,
    awaitWriteFinish: true
  });

  watcher.on('all', (eventName, filePath) => {
    console.log(`[Watcher] ${eventName}: ${filePath}`);

    if (mainWindow) {
      mainWindow.webContents.send('file-change', { 
        type: eventName, 
        path: filePath 
      });
    }

    // Уведомляем Vue о удалении
    if (['unlink', 'unlinkDir'].includes(eventName)) {
      if (mainWindow) {
        mainWindow.webContents.send('notify-unlink', filePath);
      }
    }
  });
});

ipcMain.on('stop-watcher', () => {
  if (watcher) {
    watcher.close();
    watcher = null;
  }
});

// ====================== FILE OPERATIONS ======================
ipcMain.handle('read-directory', async (_, dirPath) => {
  try {
    const entries = await fs.promises.readdir(dirPath, { withFileTypes: true });
    return entries.map(entry => ({
      name: entry.name,
      path: path.join(dirPath, entry.name),
      isFolder: entry.isDirectory(),
      isOpen: false
    })).sort((a, b) => {
      if (a.isFolder && !b.isFolder) return -1;
      if (!a.isFolder && b.isFolder) return 1;
      return a.name.localeCompare(b.name);
    });
  } catch (err) {
    console.error('read-directory error:', err);
    return [];
  }
});

ipcMain.handle('read-file', async (_, filePath) => {
  try {
    return await fs.promises.readFile(filePath, 'utf-8');
  } catch (err) {
    console.error('read-file error:', err);
    return '';
  }
});

ipcMain.handle('create-directory', async (_, dirPath) => {
  await fs.promises.mkdir(dirPath, { recursive: true });
});

ipcMain.handle('create-file', async (_, filePath) => {
  await fs.promises.writeFile(filePath, '', 'utf-8');
});

ipcMain.handle('rename-item', async (_, oldPath, newName) => {
  const dir = path.dirname(oldPath);
  const newPath = path.join(dir, newName);
  await fs.promises.rename(oldPath, newPath);
});

ipcMain.handle('delete-item', async (_, itemPath) => {
  const stat = await fs.promises.stat(itemPath);
  if (stat.isDirectory()) {
    await fs.promises.rm(itemPath, { recursive: true, force: true });
  } else {
    await fs.promises.unlink(itemPath);
  }
});

ipcMain.handle('duplicate-item', async (_, originalPath) => {
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
});

ipcMain.handle('write-file', async (_, filePath, content) => {
  try {
    await fs.promises.writeFile(filePath, content, 'utf-8');
    return { success: true };
  } catch (err) {
    console.error('write-file error:', err);
    return { success: false, error: err.message };
  }
});

ipcMain.handle('show-in-explorer', async (_, filePath) => {
  shell.showItemInFolder(filePath);
});

// ====================== FILE PROPERTIES (нативное окно Windows) ======================
ipcMain.handle('show-file-properties', async (_, filePath) => {
  try {
    const { exec } = require('child_process');
    
    // Самый надёжный способ открыть окно "Свойства" в Windows
    // explorer /select, — выделяет файл + показывает контекстное меню "Свойства"
    exec(`explorer.exe /select,"${filePath}"`, (error) => {
      if (error) {
        console.error('show-file-properties error:', error);
        // Фоллбэк
        shell.showItemInFolder(filePath);
      }
    });
  } catch (err) {
    console.error('show-file-properties failed:', err);
    shell.showItemInFolder(filePath);
  }
});

ipcMain.handle('get-file-stats', async (_, filePath) => {
  try {
    const stats = await fs.promises.stat(filePath);
    return {
      size: stats.size,
      created: stats.birthtime,
      modified: stats.mtime
    };
  } catch (err) {
    console.error('get-file-stats error:', err);
    return { size: 0, created: null, modified: null };
  }
});

ipcMain.handle('open-with', async (event, filePath) => {
  try {
    const { shell } = require('electron')
    await shell.openPath(filePath)
    return { success: true }
  } catch (err) {
    console.error('open-with error:', err)
    return { success: false, error: err.message }
  }
});

// ====================== DRAG & DROP: Копирование файлов из ОС ======================
ipcMain.handle('copy-files', async (_, targetDir, sourcePaths) => {
  try {
    const results = [];
    let copiedCount = 0;

    for (const sourcePath of sourcePaths) {
      const sourceName = path.basename(sourcePath);
      let destPath = path.join(targetDir, sourceName);

      // Авто-нумерация при конфликте имени
      if (fs.existsSync(destPath)) {
        const ext = path.extname(sourceName);
        const baseName = path.basename(sourceName, ext);
        let counter = 1;

        do {
          destPath = path.join(targetDir, `${baseName} — копия (${counter})${ext}`);
          counter++;
        } while (fs.existsSync(destPath));
      }

      const stats = await fs.promises.stat(sourcePath);

      if (stats.isDirectory()) {
        await copyDir(sourcePath, destPath);
      } else {
        await fs.promises.copyFile(sourcePath, destPath);
      }

      results.push({ source: sourcePath, dest: destPath });
      copiedCount++;
    }

    console.log(`✅ Drag & Drop: скопировано ${copiedCount} элемент(ов) в ${targetDir}`);
    return { success: true, copied: copiedCount, items: results };

  } catch (err) {
    console.error('copy-files error:', err);
    return { success: false, error: err.message };
  }
});

// ====================== ВНУТРЕННИЙ DRAG & DROP (MOVE / COPY) ======================

ipcMain.handle('move-item', async (_, sourcePath, targetDir) => {
  try {
    const fileName = path.basename(sourcePath);
    let destPath = path.join(targetDir, fileName);

    // Если файл уже существует — добавляем (1), (2) и т.д.
    if (fs.existsSync(destPath)) {
      const ext = path.extname(fileName);
      const base = path.basename(fileName, ext);
      let counter = 1;
      do {
        destPath = path.join(targetDir, `${base} (${counter})${ext}`);
        counter++;
      } while (fs.existsSync(destPath));
    }

    await fs.promises.rename(sourcePath, destPath);
    return { success: true, newPath: destPath };
  } catch (err) {
    console.error('move-item error:', err);
    return { success: false, error: err.message };
  }
});

ipcMain.handle('copy-item', async (_, sourcePath, targetDir) => {
  try {
    const fileName = path.basename(sourcePath);
    let destPath = path.join(targetDir, fileName);

    if (fs.existsSync(destPath)) {
      const ext = path.extname(fileName);
      const base = path.basename(fileName, ext);
      let counter = 1;
      do {
        destPath = path.join(targetDir, `${base} — копия (${counter})${ext}`);
        counter++;
      } while (fs.existsSync(destPath));
    }

    const stats = await fs.promises.stat(sourcePath);
    if (stats.isDirectory()) {
      await copyDir(sourcePath, destPath);
    } else {
      await fs.promises.copyFile(sourcePath, destPath);
    }

    return { success: true, newPath: destPath };
  } catch (err) {
    console.error('copy-item error:', err);
    return { success: false, error: err.message };
  }
});

// ====================== Рекурсивное копирование папок ======================
async function copyDir(src, dest) {
  await fs.promises.mkdir(dest, { recursive: true });
  const entries = await fs.promises.readdir(src, { withFileTypes: true });

  for (const entry of entries) {
    const srcPath = path.join(src, entry.name);
    const destPath = path.join(dest, entry.name);

    if (entry.isDirectory()) {
      await copyDir(srcPath, destPath);
    } else {
      await fs.promises.copyFile(srcPath, destPath);
    }
  }
}

// ====================== SCRIPT TEMPLATES ======================

const getTemplatesPath = () => {
  if (!app.isPackaged) {
    // Разработка: должен быть E:\repos\SNEngine\SNEngine.Studio\ScriptTemplates
    return path.join(__dirname, 'ScriptTemplates');
  }
  // Production (после сборки)
  return path.join(process.resourcesPath, 'app.asar', 'ScriptTemplates');
};

// Чтение всех шаблонов
ipcMain.handle('get-all-templates', async () => {
  try {
    const templatesRoot = getTemplatesPath();
    console.log(`[Templates] Looking in: ${templatesRoot}`);

    if (!fs.existsSync(templatesRoot)) {
      console.warn('⚠️ ScriptTemplates folder not found at:', templatesRoot);
      return [];
    }

    const categories = await fs.promises.readdir(templatesRoot, { withFileTypes: true });
    const templates = [];

    for (const category of categories) {
      if (!category.isDirectory()) continue;

      const catPath = path.join(templatesRoot, category.name);
      const files = await fs.promises.readdir(catPath);

      const metadataFile = files.find(f => f === 'metadata.json');
      const templateFile = files.find(f => f.startsWith('template.'));

      if (!metadataFile || !templateFile) {
        console.warn(`⚠️ Template folder "${category.name}" is incomplete`);
        continue;
      }

      const metadataPath = path.join(catPath, metadataFile);
      const templatePath = path.join(catPath, templateFile);

      const metadata = JSON.parse(await fs.promises.readFile(metadataPath, 'utf-8'));
      const content = await fs.promises.readFile(templatePath, 'utf-8');

      templates.push({
        ...metadata,
        content: content
      });
    }

    console.log(`✅ Successfully loaded ${templates.length} templates`);
    return templates.sort((a, b) => a.name.localeCompare(b.name));
  } catch (err) {
    console.error('❌ get-all-templates error:', err);
    return [];
  }
});

// Получить один шаблон по id
ipcMain.handle('get-template', async (_, templateId) => {
  try {
    const all = await ipcMain.handlers['get-all-templates']?.() || [];
    return all.find(t => t.id === templateId);
  } catch (err) {
    console.error('get-template error:', err);
    return null;
  }
});

// ====================== APP VERSION ======================
ipcMain.handle('get-app-version', () => {
  return app.getVersion();
});

// ====================== UNIQUE FILE NAME ======================
ipcMain.handle('get-unique-path', async (_, desiredPath, isFolder = false) => {
  try {
    if (!fs.existsSync(desiredPath)) {
      return desiredPath;
    }

    const lastSlash = Math.max(desiredPath.lastIndexOf('/'), desiredPath.lastIndexOf('\\'));
    const dir = desiredPath.substring(0, lastSlash + 1);
    const fileName = desiredPath.substring(lastSlash + 1);

    let baseName = fileName;
    let ext = '';

    if (!isFolder && fileName.includes('.')) {
      const dotIndex = fileName.lastIndexOf('.');
      baseName = fileName.substring(0, dotIndex);
      ext = fileName.substring(dotIndex);
    }

    baseName = baseName.replace(/\s*\(\d+\)$/, '');

    let maxNumber = 0;
    const entries = await fs.promises.readdir(dir || '.', { withFileTypes: true });

    const escapedBase = baseName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const escapedExt = ext.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    const regex = new RegExp(`^${escapedBase}\\s*\\((\\d+)\\)${escapedExt}$`, 'i');

    entries.forEach(entry => {
      const match = entry.name.match(regex);
      if (match) {
        const num = parseInt(match[1], 10);
        if (num > maxNumber) maxNumber = num;
      }
    });

    return `${dir}${baseName} (${maxNumber + 1})${ext}`;
  } catch (err) {
    console.error('get-unique-path error:', err);
    return desiredPath;
  }
});

// ====================== APP LIFECYCLE ======================
app.whenReady().then(() => {
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) createWindow();
  });
});

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit();
});