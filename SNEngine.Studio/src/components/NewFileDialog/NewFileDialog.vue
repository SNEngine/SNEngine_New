<template>
  <Teleport to="body">
    <div v-if="visible" class="vs-dialog-overlay" @click.self="close">
      <div class="vs-dialog" @click.stop>
        
        <div class="vs-dialog-header">
          <div class="vs-title">Add New Item - SNEngine.Studio</div>
          <button class="vs-close-btn" @click="close">✕</button>
        </div>

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

/* Body */
.vs-dialog-body {
  display: grid;
  grid-template-columns: 180px 1fr 280px;
  height: 420px;
  background: #1e1e1e;
}

/* Categories */
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
  background: #ff5252;
  color: #fff;
}

.vs-category-icon {
  font-size: 14px;
}

/* Templates */
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
  font-size: 22px;
  width: 28px;
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

/* Preview */
.vs-preview {
  background: #1e1e1e;
  padding: 12px;
  display: flex;
  flex-direction: column;
}

.vs-preview-header {
  font-size: 11px;
  color: #888;
  margin-bottom: 8px;
}

.vs-preview-title {
  font-size: 14px;
  font-weight: 600;
  color: #ff4f4f;
  margin-bottom: 6px;
}

.vs-preview-desc {
  font-size: 12px;
  color: #ccc;
  line-height: 1.4;
}

.vs-preview-example {
  margin-top: 12px;
  background: #161616;
  padding: 8px;
  border-radius: 4px;
  font-size: 11px;
  color: #888;
  overflow: hidden;
}

.vs-preview-empty {
  color: #666;
  font-size: 12px;
  margin-top: 40px;
  text-align: center;
}

/* Footer */
.vs-dialog-footer {
  background: #252526;
  padding: 12px 16px;
  border-top: 1px solid #3c3c3c;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.vs-name-group {
  display: flex;
  align-items: center;
  gap: 8px;
}

.vs-name-group label {
  font-size: 12px;
  color: #888;
}

.vs-name-input-wrapper {
  display: flex;
  align-items: center;
}

.vs-name-input {
  background: #1e1e1e;
  border: 1px solid #3c3c3c;
  color: #fff;
  padding: 6px 10px;
  width: 220px;
  font-size: 13px;
}

.vs-extension {
  background: #2d2d2d;
  padding: 6px 10px;
  font-size: 13px;
  color: #888;
  border: 1px solid #3c3c3c;
  border-left: none;
}

.vs-buttons {
  display: flex;
  gap: 8px;
}

.vs-btn {
  padding: 6px 20px;
  font-size: 13px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.vs-btn-secondary {
  background: #3c3c3c;
  color: #fff;
}

.vs-btn-primary {
  background: #ff5252;
  color: #fff;
  font-weight: 500;
}

.vs-btn:disabled {
  background: #555;
  cursor: not-allowed;
}
</style>