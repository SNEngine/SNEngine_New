<template>
  <div class="vs-dialog-footer">
    <div class="vs-name-group">
      <label>Name:</label>
      <div class="vs-name-input-wrapper">
        <input 
          :value="fileName" 
          @input="$emit('update:fileName', ($event.target as HTMLInputElement).value)"
          type="text" 
          class="vs-name-input"
          @keyup.enter="$emit('create')"
        >
        <span class="vs-extension">{{ extension }}</span>
      </div>
    </div>

    <div class="vs-buttons">
      <button class="vs-btn vs-btn-secondary" @click="$emit('cancel')">Cancel</button>
      <button 
        class="vs-btn vs-btn-primary" 
        :disabled="disabled" 
        @click="$emit('create')"
      >
        Add
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  fileName: string
  extension: string
  disabled: boolean
}>()

defineEmits<{
  (e: 'update:fileName', value: string): void
  (e: 'create'): void
  (e: 'cancel'): void
}>()
</script>

<style scoped>
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