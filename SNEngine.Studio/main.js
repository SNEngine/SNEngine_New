const { app, BrowserWindow } = require('electron')
const path = require('path')

app.disableHardwareAcceleration()
app.commandLine.appendSwitch('js-flags', '--max-old-space-size=4096')

const createWindow = () => {
  const mainWindow = new BrowserWindow({
    width: 1600,
    height: 1000,
    minWidth: 1200,
    minHeight: 800,
    show: false,
    center: true,
    autoHideMenuBar: true,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true,
      backgroundThrottling: false,
      preload: path.join(__dirname, 'preload.js'),
      spellcheck: false
    }
  })

  mainWindow.once('ready-to-show', () => {
    mainWindow.show()
  })

  // В dev режиме загружаем Vite dev server
  if (process.env.NODE_ENV === 'development' || process.argv.includes('--dev')) {
    mainWindow.loadURL('http://localhost:5173')
    mainWindow.webContents.openDevTools()
  } else {
    // В production загружаем собранный файл
    mainWindow.loadFile(path.join(__dirname, 'dist/index.html'))
  }
}

app.whenReady().then(createWindow)

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})