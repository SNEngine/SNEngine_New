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
  isEditable?: boolean   // ← Новый пропс
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'save'): void
}>()

const activeEditorRef = ref<any>(null)

// Определяем, нужно ли подключать события сохранения
const shouldHandleEvents = computed(() => {
  // Приоритет: используем явный пропс, если передан
  if (props.isEditable !== undefined) return props.isEditable

  // Fallback по имени компонента
  const name = props.currentComponent?.__name || ''
  return ['CodeEditor', 'WebEditor'].includes(name)
})

const editorEvents = computed(() => {
  if (!shouldHandleEvents.value) return {}

  return {
    'update:modelValue': (value: string) => emit('update:modelValue', value),
    'save': () => emit('save')
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