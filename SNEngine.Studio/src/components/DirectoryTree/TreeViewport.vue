<template>
  <div class="tree-viewport">
    <div v-if="loading && items.length === 0" class="loading">Загрузка...</div>

    <div class="tree-content">
      <TreeNode
        v-for="item in items"
        :key="item.path"
        :item="item"
        :active-path="activePath"
        :search-query="searchQuery"
        :is-selected="treeSelection?.isSelected?.(item.path) ?? false"
        :drag-handlers="dragHandlers"
        :tree-drag="treeDrag"
        :on-select="onSelect"
        @toggle="$emit('toggle', $event)"
        @file-click="$emit('file-click', $event)"
        @contextmenu="(e, item) => $emit('contextmenu', e, item)"
        @select="$emit('select', $event)"
        @internal-drop="$emit('internal-drop', $event)"
      />
    </div>
  </div>
</template>

<script setup lang="ts">
import TreeNode from './TreeNode.vue'

const props = defineProps<{
  loading?: boolean
  items: any[]
  activePath?: string
  searchQuery?: string
  dragHandlers?: any
  treeDrag?: any
  onSelect?: (payload: any, event?: MouseEvent) => void
  treeSelection?: any   // for is-selected checks (from provide/inject or passed)
}>()

defineEmits<{
  (e: 'toggle', item: any): void
  (e: 'file-click', path: string): void
  (e: 'contextmenu', event: MouseEvent, item?: any): void
  (e: 'select', payload: any): void
  (e: 'internal-drop', payload: any): void
}>()
</script>

<style scoped>
.tree-viewport {
  flex: 1;
  overflow-y: auto;
  overflow-x: hidden;
}

.loading {
  padding: 15px;
  color: #666;
  font-size: 12px;
  text-align: center;
}

.tree-viewport::-webkit-scrollbar {
  width: 4px;
}

.tree-viewport::-webkit-scrollbar-thumb {
  background: #333;
}

.tree-viewport::-webkit-scrollbar-thumb:hover {
  background: #444;
}

.tree-content {
  /* Можно добавить стили при необходимости */
}
</style>
