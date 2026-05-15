<template>
  <div class="system-status">
    <div class="time">
      {{ currentTime }}
    </div>

    <div v-if="battery.exists" class="battery" :class="{ 'charging': battery.charging }">
      <BaseIcon 
        name="battery_icon" 
        class="battery-icon"
      />
      <span class="battery-level">{{ battery.level }}%</span>
    </div>

    <div v-if="battery.exists" class="divider"></div>

    <div class="date">
      {{ currentDate }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import BaseIcon from '@/components/icons/BaseIcon.vue'

const currentTime = ref('')
const currentDate = ref('')

const battery = ref({
  exists: false,
  level: 100,
  charging: false
})

// Типизация таймеров для корректной очистки
let timeUpdateTimer: ReturnType<typeof setInterval> | null = null
let batteryUpdateTimer: ReturnType<typeof setInterval> | null = null

/**
 * Обновляет текущее время и дату в соответствии с системными настройками
 */
const updateDateTime = () => {
  const now = new Date()
  
  // Обновление времени (ЧЧ:ММ)
  currentTime.value = now.toLocaleTimeString('ru-RU', {
    hour: '2-digit',
    minute: '2-digit'
  })
  
  // Обновление даты (день недели, число, месяц)
  currentDate.value = now.toLocaleDateString('ru-RU', {
    weekday: 'short',
    day: '2-digit',
    month: 'short'
  })
}

/**
 * Запрашивает статус батареи через Electron Bridge
 */
const loadBatteryStatus = async () => {
  try {
    if (window.electron?.getBatteryStatus) {
      const status = await window.electron.getBatteryStatus()
      battery.value = status
    }
  } catch (err) {
    console.warn('Не удалось получить статус батареи')
  }
}

onMounted(() => {
  // Первичная инициализация данных
  updateDateTime()
  loadBatteryStatus()

  // Каждую секунду проверяем актуальность даты и времени
  timeUpdateTimer = setInterval(updateDateTime, 1000)
  
  // Каждые 2 минуты обновляем уровень заряда батареи
  batteryUpdateTimer = setInterval(loadBatteryStatus, 120000)
})

onUnmounted(() => {
  // Очистка таймеров при размонтировании компонента для предотвращения утечек памяти
  if (timeUpdateTimer) clearInterval(timeUpdateTimer)
  if (batteryUpdateTimer) clearInterval(batteryUpdateTimer)
})
</script>

<style scoped>
.system-status {
  display: flex;
  align-items: center;
  gap: 12px;
  font-size: 13px;
  color: #ffffff;
  height: 100%;
  padding: 0 8px;
  user-select: none;
}

.time {
  font-weight: 500;
  min-width: 46px;
}

.battery {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12.5px;
}

.battery-icon {
  width: 17px;
  height: 17px;
  color: #aaaaaa;
}

.battery.charging .battery-icon {
  color: #4ade80; /* Зеленый цвет при зарядке */
}

.battery-level {
  font-weight: 500;
}

.divider {
  width: 1px;
  height: 14px;
  background: #444;
}

.date {
  opacity: 0.8;
  font-size: 12.7px;
  text-transform: lowercase;
}
</style>