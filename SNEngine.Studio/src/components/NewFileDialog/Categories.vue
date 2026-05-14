<template>
  <div class="vs-categories">
    <div class="vs-categories-header">Installed</div>
    
    <div 
      v-for="cat in categories" 
      :key="cat"
      class="vs-category-item"
      :class="{ active: activeCategory === cat }"
      @click="$emit('update:activeCategory', cat)"
    >
      <BaseIcon name="folder_icon" class="vs-category-icon" />
      <span class="vs-category-name">{{ cat }}</span>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'

defineProps<{
  categories: string[]
  activeCategory: string
}>()

defineEmits<{
  (e: 'update:activeCategory', value: string): void
}>()
</script>

<style scoped>
.vs-categories {
  background: #252526;
  border-right: 1px solid #3c3c3c;
  padding: 8px 0;
}

.vs-categories-header {
  padding: 6px 12px;
  font-size: 12px;
  color: #888;
  font-weight: 600;
}

.vs-category-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  cursor: pointer;
  font-size: 13px;
}

.vs-category-item:hover {
  background: #2d2d2d;
}

.vs-category-item.active {
  background: #ff5252;   /* ← КРАСНЫЙ */
  color: #fff;
}

.vs-category-icon {
  width: 16px;
  height: 16px;
}
</style>