<template>
  <div class="studio">
    <div class="header">
      <h1>SNEngine Studio</h1>
      <div class="version">v0.0.1-dev</div>
    </div>

    <div class="main-controls">
      <button @click="openSceneEditor" class="big-btn secondary">
        📝 Открыть Редактор Сцены (.sn)
      </button>
    </div>

    <!-- Тест BaseIcon -->
    <div class="icons-test">
      <h2>Тест иконок (BaseIcon)</h2>
      <div class="icon-grid">
        <BaseIcon name="folder_icon" color="#FF5252" />
        <BaseIcon name="html_icon" color="#FF9800" />
        <BaseIcon name="css_icon" color="#2196F3" />
        <BaseIcon name="image_icon" color="#4CAF50" />
        <BaseIcon name="sn_script_icon" color="#E91E63" />
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { useCodeEditor } from "./composables/useCodeEditor"
import BaseIcon from "./components/icons/BaseIcon.vue"

const { openEditor } = useCodeEditor()

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

.secondary {
  background: #252526;
  color: #ccc;
}

.secondary:hover {
  background: #333;
}

/* Тест иконок */
.icons-test {
  padding: 30px;
}

.icons-test h2 {
  margin-bottom: 20px;
  color: #ccc;
}

.icon-grid {
  display: flex;
  gap: 32px;
  flex-wrap: wrap;
}
</style>