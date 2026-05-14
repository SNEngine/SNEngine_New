<template>
  <div class="web-toolbar">
    <!-- Режимы просмотра -->
    <WebModeTabs
      :mode="mode"
      @update:mode="emit('update:mode', $event)"
    />

    <!-- Вкладки кода (показываются только в code и split режимах) -->
    <WebCodeTabs
      v-if="mode !== 'preview'"
      :current-tab="currentTab"
      @update:current-tab="emit('update:currentTab', $event)"
    />

    <div class="spacer"></div>

    <!-- Панель действий -->
    <div class="toolbar-actions">
      <button 
        class="action-btn" 
        @click="emit('refresh')"
        title="Обновить превью"
      >
        <BaseIcon name="refresh_icon" color="#ccc" />
      </button>

      <button 
        class="action-btn" 
        :class="{ active: isFullscreen }"
        @click="emit('toggle-fullscreen')"
        title="Полноэкранное превью"
      >
        <BaseIcon 
          name="fullscreen_icon" 
          :color="isFullscreen ? '#FF5252' : '#ccc'" 
        />
      </button>

      <button class="save-btn" @click="emit('save')">
        <BaseIcon name="save_icon" color="#fff" />
        Сохранить
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import BaseIcon from '../icons/BaseIcon.vue'
import WebModeTabs from './WebModeTabs.vue'
import WebCodeTabs from './WebCodeTabs.vue'

defineProps<{
  mode: 'split' | 'code' | 'preview'
  currentTab: 'html' | 'css' | 'javascript'
  isFullscreen?: boolean
}>()

const emit = defineEmits<{
  (e: 'update:mode', mode: 'split' | 'code' | 'preview'): void
  (e: 'update:currentTab', tab: 'html' | 'css' | 'javascript'): void
  (e: 'refresh'): void
  (e: 'toggle-fullscreen'): void
  (e: 'save'): void
}>()
</script>

<style scoped>
.web-toolbar {
  height: 54px;
  background: #252526;
  border-bottom: 1px solid #333;
  display: flex;
  align-items: center;
  padding: 0 16px;
  gap: 16px;
  flex-shrink: 0;
  z-index: 10;
}

.spacer {
  flex: 1;
}

.toolbar-actions {
  display: flex;
  align-items: center;
  gap: 10px;
}

.action-btn {
  width: 34px;
  height: 34px;
  background: #333;
  border: 1px solid #444;
  color: #ccc;
  border-radius: 6px;
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: all 0.2s ease;
}

.action-btn:hover {
  background: #444;
  color: #fff;
}

.action-btn.active {
  color: #FF5252;
  border-color: #FF5252;
}

.save-btn {
  background: linear-gradient(135deg, #FF5252 0%, #d32f2f 100%);
  color: white;
  border: 1px solid rgba(255,255,255,0.1);
  padding: 8px 18px;
  border-radius: 6px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  display: flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 10px rgba(211, 47, 47, 0.3);
  transition: all 0.2s ease;
}

.save-btn:hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 14px rgba(211, 47, 47, 0.4);
}
</style>