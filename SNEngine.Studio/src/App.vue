<template>
  <div class="studio">
    <h1>SNEngine Studio</h1>
    <button @click="openTestEditor" class="big-btn">
      Открыть Редактор Сцены
    </button>

    <CodeEditorModal />
  </div>
</template>

<script setup lang="ts">
import { useCodeEditor } from "./composables/useCodeEditor"
import CodeEditorModal from "./components/CodeEditor/CodeEditorModal.vue"

const { openEditor } = useCodeEditor()

const openTestEditor = () => {
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
      console.log("💾 Сохранено!")
      console.log(code)
    }
  })
}
</script>