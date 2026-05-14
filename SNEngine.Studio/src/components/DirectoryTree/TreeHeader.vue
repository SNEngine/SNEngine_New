<template>
  <div class="tree-header">
    <TreeSearch
      v-model="searchQuery"
      placeholder="Поиск по файлам..."
      :show-clear="true"
      @clear="onClear"
    />

    <button 
      class="refresh-btn" 
      @click="$emit('refresh')"
      title="Обновить (F5)"
    >
      ⟳
    </button>
  </div>
</template>

<script setup lang="ts">
import { ref, watch } from 'vue'
import TreeSearch from './TreeSearch.vue'

const props = defineProps<{
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'refresh'): void
}>()

const searchQuery = ref(props.modelValue)

// Синхронизация v-model
watch(() => props.modelValue, (newVal) => {
  searchQuery.value = newVal
})

watch(searchQuery, (newVal) => {
  emit('update:modelValue', newVal)
})

const onClear = () => {
  searchQuery.value = ''
}
</script>

<style scoped>
.tree-header {
  padding: 8px 10px;
  border-bottom: 1px solid #2a2a2a;
  display: flex;
  gap: 6px;
  flex-shrink: 0;
  align-items: center;
}

.refresh-btn {
  background: transparent;
  border: 1px solid #3c3c3c;
  color: #888;
  cursor: pointer;
  padding: 5px 8px;
  border-radius: 4px;
  font-size: 14px;
  line-height: 1;
  transition: all 0.2s;
  flex-shrink: 0;
}

.refresh-btn:hover {
  color: #fff;
  border-color: #FF5252;
  background: #2a2a2a;
}
</style>