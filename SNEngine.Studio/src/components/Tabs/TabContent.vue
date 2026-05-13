<template>
  <div class="tab-content">
    <component 
      v-if="currentComponent"
      :is="currentComponent"
      v-bind="currentProps"
      ref="activeEditorRef"
      v-on="editorEvents"
    />
    
    <div v-else class="empty-state">
      <span class="empty-icon">📂</span>
      <p>Выберите файл в дереве проекта</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, type Component } from 'vue'

const props = defineProps<{
  currentComponent: Component | null
  currentProps: Record<string, any>
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'save', content: string): void
}>()

const activeEditorRef = ref<any>(null)

// Передаём события ТОЛЬКО редактируемым компонентам
const editorEvents = computed(() => {
  const name = props.currentComponent?.__name || ''
  const isEditable = ['CodeEditor', 'WebEditor'].includes(name)

  if (!isEditable) return {}

  return {
    'update:modelValue': (value: string) => emit('update:modelValue', value),
    'save': (content: string) => emit('save', content)
  }
})

defineExpose({
  activeEditorRef
})
</script>

<style scoped>
.tab-content {
  flex: 1;
  overflow: hidden;
  background: #1e1e1e;
  position: relative;
}

.empty-state {
  height: 100%;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  color: #555;
}

.empty-icon {
  font-size: 40px;
  margin-bottom: 12px;
  opacity: 0.2;
}
</style>