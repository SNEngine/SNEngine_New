<template>
  <div class="vs-preview">
    <div class="vs-preview-header">
      Type: <span class="vs-type-badge">{{ template?.type || getFallbackType() }}</span>
    </div>
    
    <div class="vs-preview-content" v-if="template">
      <div class="vs-preview-title">{{ template.name }}</div>
      <div class="vs-preview-desc">{{ template.description }}</div>
      
      <div class="vs-preview-code" v-if="template.content">
        <pre><code>{{ previewCode }}</code></pre>
      </div>
    </div>
    
    <div class="vs-preview-empty" v-else>
      Select a template to see details
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import type { ScriptTemplate } from '../../composables/useFileTemplates'

const props = defineProps<{
  template: ScriptTemplate | null
}>()

const getFallbackType = (): string => {
  if (!props.template) return ''
  
  const map: Record<string, string> = {
    html: 'HTML',
    sn: 'SN Script',
    csharp: 'C#',
    file: 'Text File',
    scene: 'SN Scene'
  }
  
  return map[props.template.icon] || 'Template'
}

const previewCode = computed(() => {
  if (!props.template?.content) return ''
  
  const code = props.template.content.trim()
  if (code.length > 220) {
    return code.slice(0, 220) + '\n...'
  }
  return code
})
</script>

<style scoped>
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
  display: flex;
  align-items: center;
  gap: 6px;
}

.vs-type-badge {
  color: #ff4f4f;
  font-weight: 600;
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
  margin-bottom: 10px;
}

.vs-preview-code {
  background: #161616;
  border-radius: 4px;
  padding: 10px;
  font-size: 11px;
  color: #d4d4d4;
  border: 1px solid #333;
  overflow: hidden;
}

.vs-preview-code pre {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-all;
}

.vs-preview-empty {
  color: #666;
  font-size: 12px;
  margin-top: 40px;
  text-align: center;
}
</style>