<template>
  <div class="tree-header">
    <div class="search-box">
      <input 
        :value="modelValue"
        @input="updateSearch"
        placeholder="Поиск по файлам..."
        class="search-input"
        @keydown.esc="clearSearch"
      />
      <button 
        v-if="modelValue" 
        class="clear-btn" 
        @click="clearSearch"
        title="Очистить поиск"
      >
        ✕
      </button>
    </div>

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
const props = defineProps<{
  modelValue: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'refresh'): void
}>()

const updateSearch = (event: Event) => {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.value)
}

const clearSearch = () => {
  emit('update:modelValue', '')
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

.search-box {
  position: relative;
  flex: 1;
}

.search-input {
  width: 100%;
  background: #252526;
  border: 1px solid #3c3c3c;
  color: #ccc;
  padding: 5px 28px 5px 10px;
  border-radius: 4px;
  font-size: 12px;
  outline: none;
  transition: border-color 0.2s;
}

.search-input:focus {
  border-color: #FF5252;
}

.clear-btn {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  color: #888;
  font-size: 11px;
  cursor: pointer;
  padding: 2px 5px;
  border-radius: 3px;
}

.clear-btn:hover {
  color: #fff;
  background: #3c3c3c;
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