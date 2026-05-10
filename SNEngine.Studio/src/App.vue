<template>
  <div class="studio">
    <div class="header">
      <h1>SNEngine Studio</h1>
      <div class="version">v0.0.1-dev</div>
    </div>

    <div class="main-controls">
      <button @click="openWebEditor" class="big-btn primary">
        🌐 Открыть WebEditor
      </button>
      
      <button @click="openSceneEditor" class="big-btn secondary">
        📝 Открыть Редактор Сцены (.sn)
      </button>
    </div>

    <!-- Здесь будет WebEditor -->
    <WebEditor v-if="showWebEditor" />
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import WebEditor from "./components/WebEditor/WebEditor.vue"
import { useCodeEditor } from "./composables/useCodeEditor"

const { openEditor } = useCodeEditor()
const showWebEditor = ref(false)

const openWebEditor = () => {
  showWebEditor.value = true
}

const openSceneEditor = () => {
  openEditor({
    title: "scene_main.sn",
    code: `name: main_scene

print "Привет из SNEngine!"

playerHealth = 100

if playerHealth < 50 then
    print "Ты ранен!"
    Quit
endif`,
    language: "sn",
    onSave: (code) => {
      console.log("💾 Сцена сохранена!")
      console.log(code)
    }
  })
}
</script>

<style>
.studio {
  height: 100vh;
  background: #1e1e1e;
  color: white;
  font-family: system-ui, sans-serif;
  display: flex;
  flex-direction: column;
}

.header {
  padding: 20px 30px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #333;
}

.version {
  color: #666;
  font-size: 14px;
}

.main-controls {
  padding: 30px;
  display: flex;
  gap: 16px;
  flex-wrap: wrap;
}

.big-btn {
  padding: 14px 32px;
  font-size: 17px;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.primary {
  background: #0066ff;
  color: white;
}

.primary:hover {
  background: #0052cc;
  transform: translateY(-2px);
}

.secondary {
  background: #252526;
  color: #ccc;
}

.secondary:hover {
  background: #333;
}
</style>