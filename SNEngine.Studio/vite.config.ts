import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { fileURLToPath, URL } from 'node:url'

export default defineConfig({
  plugins: [vue()],
  base: process.env.NODE_ENV === 'production' ? './' : '/',
  
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },

  server: {
    port: 5173,
    host: '127.0.0.1',
    strictPort: true,
    open: false,   // we control this via the electron:dev:all script
  },

  optimizeDeps: {
    include: ['monaco-editor']
  },

  worker: {
    format: 'es'
  }
})