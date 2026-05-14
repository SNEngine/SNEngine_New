<template>
  <div class="vs-templates">
    <div class="vs-templates-header">
      <div class="vs-sort">Sort by: <strong>Default</strong></div>
    </div>

    <div class="vs-templates-list">
      <div
        v-for="template in filteredTemplates"
        :key="template.id"
        class="vs-template-item"
        :class="{ selected: selectedId === template.id }"
        @click="$emit('select', template)"
      >
        <BaseIcon :name="getIconName(template.icon)" class="vs-template-icon" />
        <div class="vs-template-info">
          <div class="vs-template-name">{{ template.name }}</div>
          <div class="vs-template-desc">{{ template.description }}</div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'
import type { ScriptTemplate } from '../../composables/useFileTemplates'

defineProps<{
  filteredTemplates: ScriptTemplate[]
  selectedId: string | null
}>()

defineEmits<{
  (e: 'select', template: ScriptTemplate): void
}>()

const getIconName = (icon: string): string => {
  const map: Record<string, string> = {
    file: 'file_icon',
    sn: 'sn_script_icon',
    html: 'html_icon',
    csharp: 'csharp_icon',
    scene: 'file_icon'
  }
  return map[icon] || 'unknown_icon'
}
</script>

<style scoped>
.vs-templates {
  background: #1e1e1e;
  border-right: 1px solid #3c3c3c;
  display: flex;
  flex-direction: column;
}

.vs-templates-header {
  padding: 6px 10px;
  background: #252526;
  font-size: 12px;
  color: #888;
}

.vs-templates-list {
  flex: 1;
  overflow-y: auto;
}

.vs-template-item {
  display: flex;
  gap: 10px;
  padding: 8px 10px;
  cursor: pointer;
  border-bottom: 1px solid #2d2d2d;
}

.vs-template-item:hover {
  background: #2d2d2d;
}

.vs-template-item.selected {
  background: #ff5252;
}

.vs-template-icon {
  width: 22px;
  height: 22px;
  flex-shrink: 0;
}

.vs-template-name {
  font-weight: 500;
  font-size: 13px;
}

.vs-template-desc {
  font-size: 11px;
  color: #888;
  margin-top: 2px;
}
</style>