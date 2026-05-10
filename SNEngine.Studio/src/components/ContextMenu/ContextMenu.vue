<template>
  <Teleport to="body">
    <div 
      v-if="visible" 
      class="context-menu-overlay"
      @click="close"
      @contextmenu.prevent
    >
      <div 
        ref="menuRef"
        class="context-menu"
        :style="menuStyle"
        @click.stop
      >
        <div 
          v-for="(item, i) in items" 
          :key="i"
          class="menu-item"
          :class="{ 
            'is-disabled': item.disabled,
            'is-separator': item.type === 'separator'
          }"
          @click="handleClick(item)"
        >
          <!-- Разделитель -->
          <div v-if="item.type === 'separator'" class="separator"></div>

          <!-- Обычный пункт -->
          <template v-else>
            <BaseIcon 
              v-if="item.icon" 
              :name="item.icon" 
              class="item-icon"
            />
            <span class="item-label">{{ item.label }}</span>
            <span v-if="item.shortcut" class="item-shortcut">{{ item.shortcut }}</span>
          </template>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import BaseIcon from '../icons/BaseIcon.vue'

export interface ContextMenuItem {
  label?: string
  icon?: string
  shortcut?: string
  disabled?: boolean
  type?: 'separator'
  action?: () => void | Promise<void>
  children?: ContextMenuItem[] // для подменю в будущем
}

const props = defineProps<{
  items: ContextMenuItem[]
}>()

const visible = ref(false)
const menuRef = ref<HTMLElement | null>(null)
const position = ref({ x: 0, y: 0 })

const menuStyle = computed(() => ({
  left: `${position.value.x}px`,
  top: `${position.value.y}px`
}))

const show = (x: number, y: number) => {
  position.value = { x, y }
  visible.value = true

  // Автоматическое закрытие при клике вне меню
  setTimeout(() => {
    document.addEventListener('click', closeOnce, { once: true })
  }, 10)
}

const close = () => {
  visible.value = false
}

const closeOnce = () => close()

const handleClick = (item: ContextMenuItem) => {
  if (item.disabled || item.type === 'separator') return
  if (item.action) item.action()
  close()
}

defineExpose({ show, close })
</script>

<style scoped>
.context-menu-overlay {
  position: fixed;
  inset: 0;
  z-index: 99999;
  background: transparent;
}

.context-menu {
  position: absolute;
  background: #252526;
  border: 1px solid #444;
  border-radius: 6px;
  box-shadow: 0 10px 30px rgba(0, 0, 0, 0.6);
  min-width: 180px;
  padding: 4px 0;
  color: #ddd;
  font-size: 13.5px;
  overflow: hidden;
  z-index: 100000;
}

.menu-item {
  display: flex;
  align-items: center;
  padding: 8px 12px;
  gap: 10px;
  cursor: pointer;
  user-select: none;
  transition: background 0.1s;
}

.menu-item:hover:not(.is-disabled):not(.is-separator) {
  background: #FF5252;
  color: white;
}

.menu-item.is-disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.item-icon {
  width: 18px;
  height: 18px;
  flex-shrink: 0;
  color: #aaa;
}

.menu-item:hover .item-icon {
  color: white;
}

.item-label {
  flex: 1;
}

.item-shortcut {
  font-size: 12px;
  color: #777;
  margin-left: auto;
}

.separator {
  height: 1px;
  background: #444;
  margin: 4px 8px;
}
</style>