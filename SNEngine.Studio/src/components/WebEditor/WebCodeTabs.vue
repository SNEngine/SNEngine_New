<template>
  <div class="code-tabs">
    <button
      :class="{ active: currentTab === 'html' }"
      @click="switchTab('html')"
    >
      <BaseIcon 
        name="html_icon" 
        :color="currentTab === 'html' ? '#FFFFFF' : '#888888'" 
      />
      HTML
    </button>

    <button
      :class="{ active: currentTab === 'css' }"
      @click="switchTab('css')"
    >
      <BaseIcon 
        name="css_icon" 
        :color="currentTab === 'css' ? '#FFFFFF' : '#888888'" 
      />
      CSS
    </button>

    <button
      :class="{ active: currentTab === 'javascript' }"
      @click="switchTab('javascript')"
    >
      <BaseIcon 
        name="js_icon" 
        :color="currentTab === 'javascript' ? '#FFFFFF' : '#888888'" 
      />
      JS
    </button>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  currentTab: 'html' | 'css' | 'javascript'
}>()

const emit = defineEmits<{
  (e: 'update:currentTab', value: 'html' | 'css' | 'javascript'): void
}>()

const switchTab = (newTab: 'html' | 'css' | 'javascript') => {
  if (newTab !== props.currentTab) {
    emit('update:currentTab', newTab)
  }
}
</script>

<style scoped>
.code-tabs {
  display: flex;
  background: #1a1a1b;
  padding: 3px;
  border-radius: 8px;
  border: 1px solid #3c3c3c;
  gap: 2px;
}

.code-tabs button {
  padding: 6px 16px;
  background: transparent;
  color: #888888;
  border: none;
  border-radius: 6px;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  transition: all 0.2s ease;
  white-space: nowrap;
}

.code-tabs button:hover {
  color: #dddddd;
  background: #2d2d2d;
}

.code-tabs button.active {
  background: #FF5252;
  color: #FFFFFF;
  box-shadow: 0 2px 8px rgba(255, 82, 82, 0.35);
}
</style>