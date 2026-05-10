<template>
  <Teleport to="body">
    <div v-if="visible" class="inputbox-overlay" @click.self="handleCancel">
      <div class="inputbox">
        <div class="inputbox-header">
          <span class="title">{{ title }}</span>
        </div>
        
        <div class="inputbox-content">
          <p class="message" v-if="message">{{ message }}</p>
          
          <input 
            ref="inputRef"
            v-model="inputValue"
            class="input-field"
            :placeholder="placeholder"
            @keyup.enter="handleConfirm"
            @keyup.esc="handleCancel"
          />
        </div>

        <div class="inputbox-footer">
          <button class="msg-btn secondary" @click="handleCancel">Отмена</button>
          <button class="msg-btn primary" @click="handleConfirm">ОК</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, nextTick } from 'vue'

export interface InputBoxOptions {
  title?: string
  message?: string
  value?: string
  placeholder?: string
}

const visible = ref(false)
const title = ref('Ввод данных')
const message = ref('')
const inputValue = ref('')
const placeholder = ref('')

let resolvePromise: (value: string | null) => void

const show = (options: InputBoxOptions) => {
  title.value = options.title || 'Ввод данных'
  message.value = options.message || ''
  inputValue.value = options.value || ''
  placeholder.value = options.placeholder || ''
  visible.value = true

  nextTick(() => {
    inputRef.value?.focus()
    inputRef.value?.select()
  })

  return new Promise<string | null>((resolve) => {
    resolvePromise = resolve
  })
}

const inputRef = ref<HTMLInputElement | null>(null)

const handleConfirm = () => {
  visible.value = false
  resolvePromise(inputValue.value)
}

const handleCancel = () => {
  visible.value = false
  resolvePromise(null)
}

defineExpose({ show })
</script>

<style scoped>
.inputbox-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.8);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 20000;
}

.inputbox {
  width: 400px;
  background: #1e1e1e;
  border: 1px solid #3a3a3a;
  border-radius: 12px;
  box-shadow: 0 25px 80px rgba(0, 0, 0, 0.85);
  overflow: hidden;
}

.inputbox-header {
  background: #252526;
  padding: 16px 20px;
  border-bottom: 1px solid #333;
  font-weight: 600;
  color: #FF5252;
  font-size: 15.5px;
}

.inputbox-content {
  padding: 24px 20px 16px;
  /* Гарантируем, что контент не распирает родителя */
  display: flex;
  flex-direction: column;
}

.message {
  margin-bottom: 12px;
  color: #bbbbbb;
  font-size: 14.5px;
}

.input-field {
  width: 100%;
  /* КРИТИЧЕСКИЙ ПАРАМЕТР: чтобы padding входил в 100% ширины */
  box-sizing: border-box; 
  
  background: #2a2a2a;
  border: 1px solid #555;
  color: #ffffff;
  padding: 12px 14px;
  border-radius: 8px;
  font-size: 15px;
  outline: none;
  transition: all 0.2s ease;
}

.input-field:focus {
  border-color: #FF5252;
  background: #333;
  box-shadow: 0 0 0 3px rgba(255, 82, 82, 0.2);
}

.inputbox-footer {
  padding: 12px 20px 20px;
  display: flex;
  justify-content: flex-end;
  gap: 12px;
}

.msg-btn {
  padding: 8px 20px;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border: none;
  transition: all 0.2s;
}

.msg-btn.primary {
  background: #FF5252;
  color: white;
}

.msg-btn.primary:hover {
  background: #ff6b6b;
}

.msg-btn.secondary {
  background: #333;
  color: #ccc;
}

.msg-btn.secondary:hover {
  background: #444;
  color: white;
}
</style>