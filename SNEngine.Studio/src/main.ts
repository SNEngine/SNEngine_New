import { createApp } from 'vue'
import App from './App.vue'

// ====================== MONACO SETUP ======================
import { registerSnLanguage } from './monaco/sn-language'
import { registerHtmlLanguage } from './monaco/html-language'
import { registerCssLanguage } from './monaco/css-language'
import { registerSnEngineTheme } from './monaco/snengine-theme'

// Центральная функция инициализации Monaco
function setupMonaco() {
  console.log('🔧 Настройка Monaco...')

  try {
    registerSnLanguage()
    registerHtmlLanguage()
    registerCssLanguage()
    registerSnEngineTheme()

    console.log('✅ Monaco успешно настроен (языки + темы)')
  } catch (error) {
    console.error('❌ Ошибка при настройке Monaco:', error)
  }
}

// Запускаем настройку **до** создания приложения
setupMonaco()

// Создаём приложение
createApp(App).mount('#app')