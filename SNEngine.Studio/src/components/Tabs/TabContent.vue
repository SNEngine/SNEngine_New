<template>
  <div class="tab-content">
    <!-- Удалённый файл -->
    <DeletedFile 
      v-if="isDeleted"
      :filePath="currentProps.filePath || ''"
      @close-tab="emitCloseTab"
    />

    <!-- Пустое состояние -->
    <div v-else-if="!currentComponent" class="empty-state">
      <span class="empty-icon">📂</span>
      <p>Выберите файл в дереве проекта</p>
    </div>

    <!-- Обычный контент -->
    <component 
      v-else
      :is="currentComponent"
      v-bind="currentProps"
      ref="activeEditorRef"
      v-on="editorEvents"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import DeletedFile from '../DeletedFile/DeletedFile.vue'

const props = defineProps<{
  currentComponent: any
  currentProps: Record<string, any>
  isEditable?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'save'): void
  (e: 'close-tab'): void
}>()

const activeEditorRef = ref<any>(null)

const isDeleted = computed(() => props.currentProps?.isDeleted === true)

const emitCloseTab = () => {
  emit('close-tab')
}

const shouldHandleEvents = computed(() => {
  if (props.isEditable !== undefined) return props.isEditable
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

defineExpose({ activeEditorRef })
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