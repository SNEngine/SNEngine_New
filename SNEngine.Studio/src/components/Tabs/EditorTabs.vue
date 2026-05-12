<template>
  <div class="editor-tabs">
    <!-- Панель вкладок -->
    <div 
      class="tabs-bar" 
      v-if="tabs.length > 0"
      @wheel="handleWheel"
    >
      <div 
        v-for="tab in tabs" 
        :key="tab.id"
        class="tab"
        :class="{ active: tab.filePath === activeFilePath }"
        @click="activateTab(tab)"
      >
        <BaseIcon 
          :name="getTabIconName(tab)" 
          class="tab-icon"
        />
        <span class="tab-name">{{ tab.name }}</span>
        <span class="tab-close" @click.stop="closeTab(tab)">✕</span>
      </div>
    </div>

    <!-- Контент вкладки -->
    <div class="tab-content">
      <component 
        :is="currentComponent.component" 
        v-if="currentComponent.component"
        v-bind="currentComponent.props"
        :key="activeFilePath"
      />
      <div v-else class="empty-state">
        <span class="empty-icon">📂</span>
        <p>Выберите файл в дереве проекта</p>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'
import { useTabs } from '@/composables/useTabs'
import { useFileType } from '@/composables/useFileType'
import { getFileIcon } from '@/utils/fileIcons'

// Инициализация composables
const { tabs, activeFilePath, currentComponent, openFile, activateTab, closeTab } = useTabs()
const { getFileHandler } = useFileType()

// Открытие файла (вызывается из App.vue / DirectoryTree)
const handleOpenFile = async (filePath: string) => {
  await openFile(filePath, getFileHandler)
}

const handleWheel = (e: WheelEvent) => {
  const tabsBar = e.currentTarget as HTMLElement
  if (e.deltaY !== 0) {
    tabsBar.scrollLeft += e.deltaY
    e.preventDefault()
  }
}

const getTabIconName = (tab: any) => {
  if (tab.type === 'web') return 'html_icon'
  if (tab.type === 'image') return 'image_icon'
  if (tab.type === 'audio') return 'audio_icon'
  if (tab.type === 'code') {
    const ext = tab.filePath.split('.').pop()?.toLowerCase()
    if (ext === 'sn') return 'sn_script_icon'
    if (ext === 'cs') return 'csharp_icon'
    if (ext === 'dll') return 'dll_icon'
  }
  return 'unknown_icon'
}

// Экспонируем метод для внешнего использования
defineExpose({
  openFile: handleOpenFile
})
</script>

<style scoped>
.editor-tabs {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  background: #1e1e1e;
}

.tabs-bar {
  display: flex;
  background: #252526;
  overflow-x: auto;
  overflow-y: hidden;
  flex-shrink: 0;
  height: 35px;
}

.tabs-bar::-webkit-scrollbar {
  height: 3px;
}

.tabs-bar::-webkit-scrollbar-track {
  background: transparent;
}

.tabs-bar::-webkit-scrollbar-thumb {
  background: #3e3e3e;
}

.tabs-bar::-webkit-scrollbar-thumb:hover {
  background: #FF5252;
}

.tab {
  padding: 0 12px;
  background: #2d2d2d;
  color: #969696;
  border-right: 1px solid #1e1e1e;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  white-space: nowrap;
  user-select: none;
  flex-shrink: 0;
  height: 100%;
  font-size: 13px;
  position: relative;
  transition: background 0.2s, color 0.2s;
}

.tab:hover {
  background: #323233;
  color: #cccccc;
}

.tab.active {
  background: #1e1e1e;
  color: #FF5252;
}

.tab.active::after {
  content: '';
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  height: 1px;
  background: #FF5252;
}

.tab-icon {
  width: 16px;
  height: 16px;
  flex-shrink: 0;
}

.tab-name {
  max-width: 160px;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tab-close {
  margin-left: 6px;
  padding: 2px;
  border-radius: 3px;
  font-size: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 16px;
  height: 16px;
  opacity: 0;
  transition: all 0.2s;
}

.tab:hover .tab-close,
.tab.active .tab-close {
  opacity: 0.6;
}

.tab-close:hover {
  background: #454545;
  color: white;
  opacity: 1 !important;
}

.tab-content {
  flex: 1;
  overflow: hidden;
  background: #1e1e1e;
}

.empty-state {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #555;
}

.empty-icon {
  font-size: 40px;
  margin-bottom: 12px;
  opacity: 0.2;
}
</style>