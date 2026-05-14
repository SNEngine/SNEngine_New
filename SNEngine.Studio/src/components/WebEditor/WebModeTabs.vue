<template>
  <div class="mode-tabs">
    <button
      :class="{ active: mode === 'split' }"
      @click="switchMode('split')"
    >
      <BaseIcon 
        name="split_icon" 
        :color="mode === 'split' ? '#FFFFFF' : '#888888'" 
      />
      Split
    </button>

    <button
      :class="{ active: mode === 'code' }"
      @click="switchMode('code')"
    >
      <BaseIcon 
        name="code_icon" 
        :color="mode === 'code' ? '#FFFFFF' : '#888888'" 
      />
      Code
    </button>

    <button
      :class="{ active: mode === 'preview' }"
      @click="switchMode('preview')"
    >
      <BaseIcon 
        name="preview_icon" 
        :color="mode === 'preview' ? '#FFFFFF' : '#888888'" 
      />
      Preview
    </button>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'

const props = defineProps<{
  mode: 'split' | 'code' | 'preview'
}>()

const emit = defineEmits<{
  (e: 'update:mode', value: 'split' | 'code' | 'preview'): void
}>()

const switchMode = (newMode: 'split' | 'code' | 'preview') => {
  if (newMode !== props.mode) {
    emit('update:mode', newMode)
  }
}
</script>

<style scoped>
.mode-tabs {
  display: flex;
  background: #1a1a1b;
  padding: 3px;
  border-radius: 8px;
  border: 1px solid #3c3c3c;
  gap: 2px;
}

.mode-tabs button {
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

.mode-tabs button:hover {
  color: #dddddd;
  background: #2d2d2d;
}

.mode-tabs button.active {
  background: #FF5252;
  color: #FFFFFF;
  box-shadow: 0 2px 8px rgba(255, 82, 82, 0.35);
}
</style>