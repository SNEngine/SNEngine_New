<template>
  <div class="directory-tree">
    <!-- Только поисковая строка -->
    <div class="tree-header">
      <div class="search-box">
        <input 
          v-model="searchQuery"
          placeholder="Поиск по файлам..."
          class="search-input"
        />
      </div>
    </div>

    <div v-if="loading" class="loading">Загрузка директории...</div>

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
            @file-click="handleFileClick"
          />
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
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const items = ref<TreeItem[]>([])
const loading = ref(true)
const searchQuery = ref('')
const activePath = ref(props.activePath || '')

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
.directory-tree {
  height: 100%;
  background: #161616;
  border: 1px solid #333;
  border-radius: 6px;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

/* Шапка только с поиском */
.tree-header {
  padding: 8px 12px;
  border-bottom: 1px solid #333;
  flex-shrink: 0;
}

.search-box {
  width: 100%;
}

.search-input {
  width: 100%;
  background: #252526;
  border: 1px solid #444;
  color: #ddd;
  padding: 7px 12px;
  border-radius: 6px;
  font-size: 13.5px;
  outline: none;
  transition: all 0.2s;
}

.search-input:focus {
  border-color: #FF5252;
  box-shadow: 0 0 0 2px rgba(255, 82, 82, 0.25);
}

/* Остальные стили без изменений */
.tree-content {
  flex: 1;
  overflow-y: auto;
  padding: 4px 0;
  scrollbar-width: thin;
  scrollbar-color: #FF5252 #1e1e1e;
}

.tree-content::-webkit-scrollbar { width: 6px; }
.tree-content::-webkit-scrollbar-thumb {
  background: #FF5252;
  border-radius: 3px;
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

.tree-node:hover { background: #252526; }
.tree-node.is-active { background: #2a2a3a; border-left: 3px solid #FF5252; }

.node-icon { margin-right: 8px; flex-shrink: 0; width: 18px; height: 18px; }
.node-name { color: #eeeeee; line-height: 1.4; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.tree-node.is-folder .node-name { color: #ffffff; font-weight: 500; }

.tree-children {
  padding-left: 26px;
  border-left: 1px solid #333;
  margin-left: 10px;
}

.loading { padding: 20px; color: #666; font-style: italic; }
</style>