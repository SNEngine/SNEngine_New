import { createApp } from 'vue'
import App from './App.vue'

// Регистрируем ВСЕ языки и темы
import { registerSnLanguage } from './monaco/sn-language'
import { registerHtmlLanguage } from './monaco/html-language'
import { registerCssLanguage } from './monaco/css-language'
import { registerNagatoroTheme } from './monaco/nagatoro-theme'
import { registerSnEngineTheme } from './monaco/snengine-theme'

registerSnLanguage()
registerHtmlLanguage()
registerCssLanguage()
registerNagatoroTheme()
registerSnEngineTheme()

createApp(App).mount('#app')