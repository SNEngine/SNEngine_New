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

    <div class="directory-test">
      <DirectoryTree 
        base-path="C:/Users/Siphome/Desktop/testBuild"
        @file-click="handleFileClick"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import { useCodeEditor } from "./composables/useCodeEditor"
import DirectoryTree from "./components/DirectoryTree/DirectoryTree.vue"

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

const handleFileClick = (filePath: string) => {
  console.log("📂 Выбран файл:", filePath)
}
</script>

<style>
/* Глобальные стили для удаления системного скролла */
html, body {
  margin: 0;
  padding: 0;
  height: 100vh;
  width: 100vw;
  overflow: hidden; /* Убирает белый скроллбар окна */
  background: #1e1e1e;
}

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

/* Контейнер дерева */
.directory-test {
  padding: 0 30px 20px 30px;
  flex: 1;
  overflow: hidden; /* Скролл будет внутри DirectoryTree.vue */
  display: flex;
  flex-direction: column;
}

.directory-test h2 {
  margin-bottom: 16px;
  color: #ccc;
}
</style>