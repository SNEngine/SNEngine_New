<template>
  <div class="tree-header">
    <TreeSearch
      v-model="modelValue"
      placeholder="Поиск по файлам..."
      :show-clear="true"
      @clear="onClear"
    />

    <div class="sort-control" ref="sortControl">
      <button 
        class="sort-btn" 
        @click="toggleSortMenu"
        title="Сортировка"
      >
        ↕ {{ sortLabel }}
      </button>

      <div v-if="showSortMenu" class="sort-menu">
        <div class="sort-option" @click="setField('name')">
          <span class="dot" v-show="sortField === 'name'">•</span>
          Имя
        </div>
        <div class="sort-option" @click="setField('modified')">
          <span class="dot" v-show="sortField === 'modified'">•</span>
          Дата изменения
        </div>
        <div class="sort-option" @click="setField('type')">
          <span class="dot" v-show="sortField === 'type'">•</span>
          Тип
        </div>

        <div class="sort-divider"></div>

        <div class="sort-option" @click="setOrder('asc')">
          <span class="dot" v-show="sortOrder === 'asc'">•</span>
          По возрастанию
        </div>
        <div class="sort-option" @click="setOrder('desc')">
          <span class="dot" v-show="sortOrder === 'desc'">•</span>
          По убыванию
        </div>
      </div>
    </div>

    <button 
      class="refresh-btn" 
      @click="$emit('refresh')"
      title="Обновить (F5)"
    >
      <i class="refresh-icon">↻</i>
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import TreeSearch from './TreeSearch.vue'

// Use defineModel for proper v-model support inside the component
const modelValue = defineModel<string>('modelValue')
const sortField = defineModel<'name' | 'modified' | 'type'>('sortField')
const sortOrder = defineModel<'asc' | 'desc'>('sortOrder')

const emit = defineEmits<{
  (e: 'refresh'): void
}>()

const sortControl = ref<HTMLElement | null>(null)
const showSortMenu = ref(false)

const sortLabel = computed(() => {
  const labels = { name: 'Имя', modified: 'Дата', type: 'Тип' }
  return labels[sortField.value ?? 'name']
})

const toggleSortMenu = () => {
  showSortMenu.value = !showSortMenu.value
}

const setField = (field: 'name' | 'modified' | 'type') => {
  sortField.value = field
  showSortMenu.value = false
}

const setOrder = (order: 'asc' | 'desc') => {
  sortOrder.value = order
  showSortMenu.value = false
}

const onClear = () => {
  modelValue.value = ''
}

const closeMenu = (e: MouseEvent) => {
  if (sortControl.value && !sortControl.value.contains(e.target as Node)) {
    showSortMenu.value = false
  }
}

onMounted(() => window.addEventListener('click', closeMenu))
onUnmounted(() => window.removeEventListener('click', closeMenu))
</script>

<style scoped>
.tree-header {
  padding: 8px 10px;
  border-bottom: 1px solid #2a2a2a;
  display: flex;
  gap: 8px;
  flex-shrink: 0;
  align-items: center;
  position: relative;
  width: 100%;
  box-sizing: border-box;
}

:deep(.tree-search) {
  flex: 1;
  min-width: 0;
}

.sort-control {
  position: relative;
  flex-shrink: 0;
}

.sort-btn {
  background: transparent;
  border: 1px solid #3c3c3c;
  color: #888;
  cursor: pointer;
  padding: 0 10px;
  border-radius: 4px;
  font-size: 12px;
  white-space: nowrap;
  transition: all 0.2s;
  display: flex;
  align-items: center;
  height: 28px;
}

.sort-btn:hover {
  color: #fff;
  border-color: #FF5252;
  background: #2a2a2a;
}

.sort-menu {
  position: absolute;
  top: calc(100% + 6px);
  right: 0;
  background: #252526;
  border: 1px solid #454545;
  border-radius: 4px;
  padding: 4px 0;
  z-index: 1000;
  min-width: 180px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
}

.sort-option {
  padding: 6px 14px 6px 30px; /* Отступ слева под точку */
  font-size: 13px;
  color: #cccccc;
  cursor: pointer;
  display: flex;
  align-items: center;
  position: relative;
  transition: all 0.1s ease;
}

.sort-option:hover {
  background: #37373d;
  color: #ff5252;
}

.dot {
  position: absolute;
  left: 10px;
  font-size: 18px;
  line-height: 1;
  color: #ff5252;
}

.sort-divider {
  height: 1px;
  background: #3c3c3c;
  margin: 4px 0;
}

.refresh-btn {
  background: transparent;
  border: 1px solid #3c3c3c;
  color: #888;
  cursor: pointer;
  padding: 0;
  width: 28px;
  height: 28px;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s;
  flex-shrink: 0;
}

.refresh-btn:hover {
  color: #fff;
  border-color: #FF5252;
  background: #2a2a2a;
}

.refresh-icon {
  font-style: normal;
  font-size: 16px;
  line-height: 1;
}
</style>