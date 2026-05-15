<template>
  <Teleport to="body">
    <div v-if="visible" class="vs-dialog-overlay" @click.self="close">
      <div class="vs-dialog" @click.stop>
        
        <!-- Header -->
        <div class="vs-dialog-header">
          <div class="vs-title">Add New Item - SNEngine.Studio</div>
          <button class="vs-close-btn" @click="close">✕</button>
        </div>

        <!-- Body -->
        <div class="vs-dialog-body">
          <Categories 
            :categories="categories" 
            :activeCategory="activeCategory"
            @update:activeCategory="activeCategory = $event"
          />
          
          <TemplatesList 
            :filteredTemplates="filteredTemplates" 
            :selectedId="selected?.id || null"
            @select="selectTemplate"
          />
          
          <PreviewPane :template="selected" />
        </div>

        <!-- Footer -->
        <DialogFooter 
          :fileName="fileName"
          :extension="selected?.extension || ''"
          :disabled="!selected || !fileName.trim()"
          @update:fileName="fileName = $event"
          @create="createFile"
          @cancel="close"
        />

      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useFileTemplates } from '../../composables/useFileTemplates'

import Categories from './Categories.vue'
import TemplatesList from './TemplatesList.vue'
import PreviewPane from './PreviewPane.vue'
import DialogFooter from './DialogFooter.vue'

const { templates, loadTemplates } = useFileTemplates()

const visible = defineModel<boolean>('visible', { default: false })
const emit = defineEmits(['create'])

const activeCategory = ref('All')
const selected = ref<any>(null)
const fileName = ref('')

const categories = computed(() => {
  const cats = new Set(templates.value.map(t => t.category))
  return ['All', ...Array.from(cats).sort()]
})

const filteredTemplates = computed(() => {
  if (activeCategory.value === 'All') return templates.value
  return templates.value.filter(t => t.category === activeCategory.value)
})

const selectTemplate = (template: any) => {
  selected.value = template
  fileName.value = template.defaultName || 'NewFile'
}

const createFile = () => {
  if (!selected.value || !fileName.value.trim()) return

  const finalName = fileName.value.trim() + selected.value.extension
  const content = selected.value.content.replace(/\${name}/gi, fileName.value.trim())

  emit('create', {
    name: finalName,
    content: content,
    templateId: selected.value.id
  })

  close()
}

const close = () => {
  visible.value = false
  selected.value = null
  fileName.value = ''
}

onMounted(() => {
  loadTemplates()
})
</script>

<style scoped>
/* === Только стили самого диалога === */
.vs-dialog-overlay {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0, 0, 0, 0.8);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 10000;
}

.vs-dialog {
  width: 920px;
  background: #1e1e1e;
  border: 1px solid #3c3c3c;
  border-radius: 6px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.6);
  overflow: hidden;
  color: #d4d4d4;
  font-family: 'Segoe UI', system-ui, sans-serif;
}

/* Header */
.vs-dialog-header {
  background: #252526;
  padding: 8px 12px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border-bottom: 1px solid #3c3c3c;
}

.vs-title {
  font-size: 13px;
  color: #ff5252;
  font-weight: 600;
}

.vs-close-btn {
  background: none;
  border: none;
  color: #888;
  font-size: 16px;
  cursor: pointer;
  padding: 2px 8px;
}

.vs-close-btn:hover {
  color: #fff;
}

/* Body (сетка 3 колонки) */
.vs-dialog-body {
  display: grid;
  grid-template-columns: 180px 1fr 280px;
  height: 420px;
  background: #1e1e1e;
}
</style>