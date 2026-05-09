import { createApp } from 'vue'
import App from './App.vue'
import { registerSnLanguage } from './monaco/sn-language'

registerSnLanguage()
createApp(App).mount('#app')