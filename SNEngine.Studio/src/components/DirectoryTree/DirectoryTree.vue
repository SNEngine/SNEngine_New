<template>
  <div class="directory-tree-container">
    <div class="directory-tree">

      <div v-if="loading" class="loading">Загрузка директории...</div>

      <div class="tree-content">
        <div 
          v-for="item in items" 
          :key="item.path"
          class="tree-item"
        >
          <div 
            class="tree-node"
            :class="{ 'is-folder': item.isFolder, 'is-open': item.isOpen }"
            @click="toggleItem(item)"
          >
            <BaseIcon 
              :name="item.isFolder ? 'folder_icon' : getFileIcon(item.name)" 
              :color="item.isFolder ? '#FFCA28' : '#FF5252'"
              class="node-icon"
            />
            <span class="node-name">{{ item.name }}</span>
          </div>

          <div v-if="item.isFolder && item.isOpen" class="tree-children">
            <DirectoryTree 
              :base-path="item.path" 
              @file-click="emitFileClick"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { getFileIcon } from '@/utils/fileIcons'
import BaseIcon from '../icons/BaseIcon.vue'

interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen: boolean
}

const props = defineProps<{
  basePath: string
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const items = ref<TreeItem[]>([])
const loading = ref(true)

const loadDirectory = async () => {
  try {
    const result = await (window as any).electron.readDirectory(props.basePath)
    items.value = result.sort((a: any, b: any) => {
      if (a.isFolder && !b.isFolder) return -1
      if (!a.isFolder && b.isFolder) return 1
      return a.name.localeCompare(b.name)
    })
  } catch (err) {
    console.error('Не удалось прочитать папку:', err)
  } finally {
    loading.value = false
  }
}

const toggleItem = (item: TreeItem) => {
  if (!item.isFolder) {
    emit('file-click', item.path)
    return
  }
  item.isOpen = !item.isOpen
}

const emitFileClick = (path: string) => {
  emit('file-click', path)
}

onMounted(() => {
  loadDirectory()
})
</script>

<style scoped>
.directory-tree-container {
  height: 100%;
  overflow: hidden; /* Убирает внешние скроллбары контейнера */
}

.directory-tree {
  height: 100%;
  background: #161616;
  border: 1px solid #333;
  border-radius: 6px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.tree-header {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 14px;
  color: #FF5252;
  font-weight: 600;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
}

.header-icon {
  width: 20px;
  height: 20px;
}

.tree-content {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
  
  /* Скрытие стандартного скроллбара для Firefox */
  scrollbar-width: thin;
  scrollbar-color: #FF5252 #1e1e1e;
}

/* Стилизация кастомного скроллбара для Chrome/Electron */
.tree-content::-webkit-scrollbar {
  width: 6px;
}

.tree-content::-webkit-scrollbar-track {
  background: #161616;
}

.tree-content::-webkit-scrollbar-thumb {
  background: #FF5252;
  border-radius: 3px;
}

.tree-content::-webkit-scrollbar-thumb:hover {
  background: #FF1744;
}

/* Принудительное скрытие системных полос, если они пробиваются */
.tree-content {
  -ms-overflow-style: none; /* IE/Edge */
}

.tree-node {
  display: flex;
  align-items: center;
  padding: 6px 12px;
  border-radius: 4px;
  cursor: pointer;
  transition: all 0.1s ease;
  min-height: 30px;
}

.tree-node:hover {
  background: #252526;
}

.node-icon {
  margin-right: 8px;
  flex-shrink: 0;
  width: 18px;
  height: 18px;
}

.node-name {
  color: #eeeeee;
  line-height: 1.4;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.tree-node.is-folder .node-name {
  color: #ffffff;
  font-weight: 500;
}

.tree-children {
  padding-left: 26px;
  border-left: 1px solid #333;
  margin-left: 10px;
}

.loading {
  padding: 20px;
  color: #666;
  font-style: italic;
}
</style>