<template>
  <div class="tree-search">
    <div class="search-wrapper">
      <input
        :value="modelValue"
        @input="onInput"
        :placeholder="placeholder"
        class="search-input"
        @keydown.esc="clear"
      />

      <button
        v-if="modelValue && showClear"
        class="clear-btn"
        @click="clear"
        title="Очистить поиск"
      >
        ✕
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
const props = defineProps<{
  modelValue: string
  placeholder?: string
  showClear?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'clear'): void
}>()

const onInput = (event: Event) => {
  const target = event.target as HTMLInputElement
  emit('update:modelValue', target.value)
}

const clear = () => {
  emit('update:modelValue', '')
  emit('clear')
}
</script>

<style scoped>
.tree-search {
  display: block;
  width: 100%; /* Занимает всю ширину отведенного flex-контейнера */
}

.search-wrapper {
  position: relative;
  width: 100%;
  display: flex;
  align-items: center;
}

.search-input {
  width: 100%;
  height: 28px; /* Фиксированная высота для выравнивания с кнопками */
  background: #252526;
  border: 1px solid #3c3c3c;
  color: #ccc;
  padding: 0 28px 0 10px; /* Отступ справа под кнопку очистки */
  border-radius: 4px;
  font-size: 12px;
  outline: none;
  box-sizing: border-box; /* Чтобы padding не раздувал ширину */
  transition: border-color 0.2s;
}

.search-input:focus {
  border-color: #FF5252; /* Акцентный цвет при фокусе */
}

.clear-btn {
  position: absolute;
  right: 6px;
  top: 50%;
  transform: translateY(-50%);
  background: transparent;
  border: none;
  color: #888;
  font-size: 11px;
  cursor: pointer;
  padding: 2px 5px;
  border-radius: 3px;
  line-height: 1;
  display: flex;
  align-items: center;
  justify-content: center;
}

.clear-btn:hover {
  color: #fff;
  background: #3c3c3c;
}
</style>