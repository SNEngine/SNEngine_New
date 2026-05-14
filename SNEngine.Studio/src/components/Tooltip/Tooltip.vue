<template>
  <div 
    class="tooltip-wrapper"
    @mouseenter="show($event)"
    @mouseleave="hide"
  >
    <slot />

    <Teleport to="body">
      <Transition name="tooltip-pop">
        <div 
          v-if="isVisible"
          class="sn-tooltip"
          :style="tooltipStyle"
        >
          <div class="tooltip-content">
            <slot name="content">
              <div v-html="tooltipText"></div>
            </slot>
          </div>
          <div class="tooltip-arrow" />
        </div>
      </Transition>
    </Teleport>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useTooltip, type TooltipPosition } from '@/composables/useTooltip'

const props = defineProps<{
  text?: string
  position?: TooltipPosition
  delay?: number
  hideDelay?: number
  maxWidth?: number
  offset?: number
}>()

const {
  isVisible,
  tooltipText,
  coords,
  show,
  hide
} = useTooltip({
  text: props.text || '',
  position: props.position || 'bottom',
  delay: props.delay ?? 450,
  hideDelay: props.hideDelay ?? 200,
  maxWidth: props.maxWidth ?? 420,
  offset: props.offset ?? 12
})

const tooltipStyle = computed(() => ({
  left: `${coords.value.x}px`,
  top: `${coords.value.y}px`
}))
</script>

<style scoped>
.sn-tooltip {
  position: fixed;
  z-index: 99999;
  background: #1f1f1f;
  color: #eeeeee;
  font-size: 12.8px;
  line-height: 1.4;
  padding: 10px 12px;
  border-radius: 6px;
  border: 1px solid #3a3a3a;
  box-shadow: 0 8px 25px rgba(0, 0, 0, 0.7);
  pointer-events: none;
  max-width: 420px;
  /* Плавное появление через transform + opacity */
  transition: all 0.18s cubic-bezier(0.23, 1.0, 0.32, 1.0);
}

.tooltip-arrow {
  position: absolute;
  bottom: -5px;
  left: 50%;
  transform: translateX(-50%) rotate(45deg);
  width: 11px;
  height: 11px;
  background: #1f1f1f;
  border-right: 1px solid #3a3a3a;
  border-bottom: 1px solid #3a3a3a;
}

/* ==================== ПЛАВНАЯ АНИМАЦИЯ ==================== */
.tooltip-pop-enter-active,
.tooltip-pop-leave-active {
  transition: all 0.22s cubic-bezier(0.23, 1.0, 0.32, 1.0);
}

.tooltip-pop-enter-from {
  opacity: 0;
  transform: translateY(8px) scale(0.96);
}

.tooltip-pop-leave-to {
  opacity: 0;
  transform: translateY(6px) scale(0.97);
}
</style>