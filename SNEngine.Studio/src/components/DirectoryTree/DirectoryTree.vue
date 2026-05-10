<template>
  <div class="directory-tree-container">
    <div class="tree-header">
      <div class="search-box">
        <input 
          v-model="searchQuery"
          placeholder="Поиск по файлам..."
          class="search-input"
        />
      </div>
    </div>

    <div class="tree-viewport">
      <div v-if="loading && isRoot" class="loading">Загрузка директории...</div>
      
      <div class="tree-content">
        <div 
          v-for="item in filteredItems" 
          :key="item.path"
          class="tree-item"
        >
          <div 
            class="tree-node"
            :class="{ 
              'is-folder': item.isFolder, 
              'is-open': item.isOpen,
              'is-active': activePath === item.path 
            }"
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
              :active-path="activePath"
              :is-sub-tree="true"
              @file-click="handleFileClick"
            />
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
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
  activePath?: string
  isSubTree?: boolean // Флаг, чтобы отличать вложенные компоненты
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const items = ref<TreeItem[]>([])
const loading = ref(true)
const searchQuery = ref('')
const activePath = ref(props.activePath || '')

const isRoot = computed(() => !props.isSubTree)

const filteredItems = computed(() => {
  if (!searchQuery.value.trim()) return items.value
  const q = searchQuery.value.toLowerCase().trim()
  return items.value.filter(item => 
    item.name.toLowerCase().includes(q)
  )
})

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
    activePath.value = item.path
    emit('file-click', item.path)
    return
  }
  item.isOpen = !item.isOpen
}

const handleFileClick = (path: string) => {
  activePath.value = path
  emit('file-click', path)
}

onMounted(() => {
  loadDirectory()
})
</script>

<style scoped>
/* Главный контейнер */
.directory-tree-container {
  height: 100%;
  width: 100%;
  display: flex;
  flex-direction: column;
  background: #161616;
  color: #eeeeee;
  box-sizing: border-box;
}

/* Шапка с поиском — всегда сверху */
.tree-header {
  padding: 12px;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
}

.search-box {
  width: 100%;
}

.search-input {
  width: 100%;
  box-sizing: border-box;
  background: #252526;
  border: 1px solid #444;
  color: #ddd;
  padding: 8px 12px;
  border-radius: 6px;
  font-size: 13.5px;
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus {
  border-color: #FF5252;
}

/* Область прокрутки */
.tree-viewport {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

.tree-viewport::-webkit-scrollbar {
  width: 6px;
}

.tree-viewport::-webkit-scrollbar-thumb {
  background: #FF5252;
  border-radius: 3px;
}

.tree-content {
  padding: 4px 0;
}

.tree-node {
  display: flex;
  align-items: center;
  padding: 6px 12px;
  cursor: pointer;
  transition: background 0.1s;
  min-height: 30px;
  user-select: none;
}

.tree-node:hover {
  background: #252526;
}

.tree-node.is-active {
  background: #2a2a3a;
  border-left: 3px solid #FF5252;
}

.node-icon {
  margin-right: 10px;
  flex-shrink: 0;
  width: 18px;
  height: 18px;
}

.node-name {
  line-height: 1.4;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  font-size: 14px;
}

.tree-node.is-folder .node-name {
  font-weight: 500;
}

/* Отступ для вложенных элементов */
.tree-children {
  margin-left: 14px;
  padding-left: 10px;
  border-left: 1px solid #333;
}

.loading {
  padding: 16px;
  color: #666;
  font-size: 13px;
}

/* Убираем лишние рамки и фон у вложенных компонентов, 
   чтобы они не создавали эффект "матрешки" */
.tree-children :deep(.directory-tree-container) {
  background: transparent;
  border: none;
  height: auto;
}
.tree-children :deep(.tree-header) {
  display: none; /* Прячем поиск во вложенных папках */
}
</style>