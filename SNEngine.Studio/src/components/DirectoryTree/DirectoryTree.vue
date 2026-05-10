<template>
  <div class="directory-tree-container">
    <div class="tree-header" v-if="isRoot">
      <div class="search-box">
        <input 
          v-model="searchQuery"
          placeholder="Поиск по файлам..."
          class="search-input"
        />
      </div>
      <button class="refresh-btn" @click="manualRefresh" title="Обновить">⟳</button>
    </div>

    <div 
      class="tree-viewport" 
      @contextmenu.prevent="onContextMenu($event, null)"
      @click="selectedItem = null"
    >
      <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>
      
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
              'is-active': activePath === item.path || (selectedItem && selectedItem.path === item.path)
            }"
            @click.stop="toggleItem(item)"
            @contextmenu.stop.prevent="onContextMenu($event, item)"
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

    <ContextMenu ref="contextMenuRef" :items="menuItems" />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, computed, watch } from 'vue'
import { getFileIcon } from '@/utils/fileIcons'
import { lastUpdate } from '@/utils/watcherState'
import { useMessageBox } from '@/composables/useMessageBox'
import { useInputBox } from '@/composables/useInputBox'
import BaseIcon from '../icons/BaseIcon.vue'
import ContextMenu from '@/components/ContextMenu/ContextMenu.vue'

interface TreeItem {
  name: string
  path: string
  isFolder: boolean
  isOpen: boolean
  children?: any[]
}

const props = defineProps<{
  basePath: string
  activePath?: string
  isSubTree?: boolean
}>()

const emit = defineEmits<{
  (e: 'file-click', path: string): void
}>()

const items = ref<TreeItem[]>([])
const loading = ref(true)
const searchQuery = ref('')
const isRoot = computed(() => !props.isSubTree)

const { showMessageBox } = useMessageBox()
const { showInputBox } = useInputBox()

const contextMenuRef = ref<any>(null)
const selectedItem = ref<TreeItem | null>(null)

const menuItems = computed(() => {
  const common = [
    { label: 'Создать файл', icon: 'file_icon', action: () => handleCreate(false) },
    { label: 'Создать папку', icon: 'folder_icon', action: () => handleCreate(true) },
  ]
  if (selectedItem.value) {
    return [
      ...common,
      { type: 'separator' },
      { label: 'Переименовать', action: handleRename },
      { label: 'Удалить', icon: 'delete_icon', action: handleDelete }
    ]
  }
  return common
})

const loadDirectory = async () => {
  loading.value = true
  try {
    const result = await (window as any).electron.readDirectory(props.basePath)
    const openedPaths = new Set(items.value.filter(i => i.isOpen).map(i => i.path))
    
    items.value = result.map((item: any) => ({
      ...item,
      isOpen: openedPaths.has(item.path)
    })).sort((a: any, b: any) => {
      if (a.isFolder && !b.isFolder) return -1
      if (!a.isFolder && b.isFolder) return 1
      return a.name.localeCompare(b.name)
    })
  } catch (err) {
    console.error('Directory read error:', err)
  } finally {
    loading.value = false
  }
}

const onContextMenu = (e: MouseEvent, item: TreeItem | null) => {
  selectedItem.value = item
  contextMenuRef.value?.show(e.clientX, e.clientY)
}

const handleCreate = async (isFolder: boolean) => {
  let targetDir = props.basePath
  if (selectedItem.value) {
    targetDir = selectedItem.value.isFolder ? selectedItem.value.path : props.basePath
  }
  
  const name = await showInputBox({
    title: isFolder ? 'Новая папка' : 'Новый файл',
    message: `Создание в: ${targetDir.split(/[\\/]/).pop() || 'Корень'}`,
    placeholder: isFolder ? 'Имя папки' : 'new_file.sn'
  })
  
  if (name) {
    const fullPath = `${targetDir}/${name}`
    try {
      if (isFolder) {
        await (window as any).electron.createDirectory(fullPath)
      } else {
        await (window as any).electron.createFile(fullPath)
      }
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ title: 'Ошибка', message: 'Не удалось создать объект', icon: 'error' })
    }
  }
}

const handleRename = async () => {
  if (!selectedItem.value) return
  const newName = await showInputBox({
    title: 'Переименование',
    message: `Введите новое имя для ${selectedItem.value.name}:`,
    value: selectedItem.value.name
  })
  if (newName && newName !== selectedItem.value.name) {
    try {
      await (window as any).electron.renameItem(selectedItem.value.path, newName)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ title: 'Ошибка', message: 'Ошибка переименования', icon: 'error' })
    }
  }
}

const handleDelete = async () => {
  if (!selectedItem.value) return
  const result = await showMessageBox({
    title: 'Удаление',
    message: `Вы действительно хотите удалить ${selectedItem.value.name}?`,
    type: 'yesno',
    icon: 'warning'
  })
  if (result === 'yes') {
    try {
      await (window as any).electron.deleteItem(selectedItem.value.path)
      lastUpdate.value = Date.now()
    } catch (err) {
      await showMessageBox({ title: 'Ошибка', message: 'Не удалось удалить', icon: 'error' })
    }
  }
}

const toggleItem = (item: TreeItem) => {
  selectedItem.value = item
  if (!item.isFolder) {
    emit('file-click', item.path)
    return
  }
  item.isOpen = !item.isOpen
}

const handleFileClick = (path: string) => emit('file-click', path)
const manualRefresh = () => { lastUpdate.value = Date.now() }

const filteredItems = computed(() => {
  if (!searchQuery.value.trim()) return items.value
  const q = searchQuery.value.toLowerCase().trim()
  return items.value.filter(item => item.name.toLowerCase().includes(q))
})

watch(lastUpdate, () => loadDirectory())

let watcherCleanup: (() => void) | null = null
const setupWatcher = () => {
  if (!isRoot.value) return
  const handler = () => { lastUpdate.value = Date.now() }
  (window as any).electron.onFileChange(handler)
  watcherCleanup = () => {
    (window as any).electron.offFileChange(handler)
    ;(window as any).electron.stopWatcher()
  }
  ;(window as any).electron.startWatcher(props.basePath)
}

onMounted(() => {
  loadDirectory()
  if (isRoot.value) setupWatcher()
})

onUnmounted(() => { if (watcherCleanup) watcherCleanup() })

watch(() => props.basePath, (newPath) => {
  loadDirectory()
  if (isRoot.value) (window as any).electron.startWatcher(newPath)
})
</script>

<style scoped>
/* Убираем лишние высоты, чтобы контейнер подстраивался под контент, 
   но в корне занимал всё доступное пространство */
.directory-tree-container {
  display: flex;
  flex-direction: column;
  background: #161616;
  color: #eeeeee;
  user-select: none;
  width: 100%;
}

/* Ограничиваем высоту только для корневого контейнера */
:not(.tree-children) > .directory-tree-container {
  height: 100%;
}

.tree-header {
  padding: 8px 10px;
  border-bottom: 1px solid #2a2a2a;
  display: flex;
  gap: 6px;
  flex-shrink: 0;
}

.search-input {
  width: 100%;
  background: #252526;
  border: 1px solid #3c3c3c;
  color: #ccc;
  padding: 4px 8px;
  border-radius: 3px;
  font-size: 12px;
  outline: none;
}

.refresh-btn {
  background: transparent;
  border: none;
  color: #888;
  cursor: pointer;
  padding: 0 4px;
}

.tree-viewport {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

/* Элемент дерева */
/* В секции <style scoped> найдите этот блок: */
.tree-node {
  display: flex;
  align-items: center;
  padding: 4px 10px; /* Было 2px 10px, увеличили вертикальный отступ для комфортного клика */
  cursor: pointer;
  font-size: 13px;
  white-space: nowrap;
  transition: background 0.1s;
}

.tree-node:hover { background: #2a2d2e; }
.tree-node.is-active { background: #37373d; color: #FF5252; }

.node-icon { 
  margin-right: 6px; 
  width: 16px; 
  height: 16px;
  flex-shrink: 0; 
}

/* Вложенность */
.tree-children {
  /* Вложенные списки не должны иметь своей высоты 100% */
  height: auto; 
  margin-left: 12px;
  border-left: 1px solid #2d2d2d;
}

/* Убираем лишние отступы у вложенных контейнеров */
.tree-children :deep(.directory-tree-container) {
  height: auto;
}

.loading { 
  padding: 15px; 
  color: #666; 
  font-size: 12px; 
  text-align: center; 
}

/* Скроллбар */
.tree-viewport::-webkit-scrollbar { width: 4px; }
.tree-viewport::-webkit-scrollbar-thumb { background: #333; }
.tree-viewport::-webkit-scrollbar-thumb:hover { background: #444; }
</style>