<script setup lang="ts">
import { computed } from 'vue'
import type { TideData } from '../types/tide'

const props = defineProps<{
  tideData: TideData
}>()

const maxLevel = computed(() => {
  if (props.tideData.predictions.length === 0) return 6
  const max = Math.max(...props.tideData.predictions.map((p) => p.waterLevel), 4)
  return Math.ceil(max * 1.2)
})

const minLevel = computed(() => {
  if (props.tideData.predictions.length === 0) return -2
  const min = Math.min(...props.tideData.predictions.map((p) => p.waterLevel), -1)
  return Math.floor(min * 1.2)
})

const range = computed(() => maxLevel.value - minLevel.value)

const fillPercentage = computed(() => {
  const level = props.tideData.currentWaterLevel
  return ((level - minLevel.value) / range.value) * 100
})

const gaugeColor = computed(() => {
  const level = props.tideData.currentWaterLevel
  const mid = (maxLevel.value + minLevel.value) / 2
  if (level > mid + range.value * 0.2) return '#dc2626' // extreme high - red
  if (level > mid + range.value * 0.1) return '#f59e0b' // above normal - amber
  if (level < mid - range.value * 0.2) return '#dc2626' // extreme low - red
  return '#2563eb' // normal - blue
})

const statusIcon = computed(() => {
  switch (props.tideData.currentStatus) {
    case 'Rising': return '↑'
    case 'Falling': return '↓'
    case 'High': return '⬆'
    case 'Low': return '⬇'
  }
})

const statusLabel = computed(() => {
  switch (props.tideData.currentStatus) {
    case 'Rising': return 'Rising'
    case 'Falling': return 'Falling'
    case 'High': return 'High Tide'
    case 'Low': return 'Low Tide'
  }
})

const waterTempDisplay = computed(() => {
  const temp = props.tideData.waterTemperature
  if (temp === null || temp === undefined) return null
  return `${temp.toFixed(1)}°F`
})
</script>

<template>
  <div class="tide-gauge">
    <h3 class="gauge-title">Current Tide Level</h3>
    <div class="gauge-container">
      <div class="gauge-labels">
        <span class="label label-high">{{ maxLevel.toFixed(1) }}'</span>
        <span class="label label-mid">{{ ((maxLevel + minLevel) / 2).toFixed(1) }}'</span>
        <span class="label label-low">{{ minLevel.toFixed(1) }}'</span>
      </div>
      <div class="gauge-track">
        <div
          class="gauge-fill"
          :style="{
            height: fillPercentage + '%',
            backgroundColor: gaugeColor,
          }"
        ></div>
      </div>
      <div class="gauge-value">
        <span class="water-level">{{ tideData.currentWaterLevel.toFixed(2) }}'</span>
        <span class="tide-status" :style="{ color: gaugeColor }">
          {{ statusIcon }} {{ statusLabel }}
        </span>
      </div>
    </div>
    <div v-if="waterTempDisplay" class="water-temp">
      <span class="temp-icon">🌡️</span>
      <span class="temp-value">{{ waterTempDisplay }}</span>
    </div>
    <div v-else class="water-temp water-temp-na" title="Water temperature sensor not available at this station">
      <span class="temp-icon">🌡️</span>
      <span class="temp-value">Water temp unavailable</span>
    </div>

  </div>
</template>

<style scoped>
.tide-gauge {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.gauge-title {
  margin: 0 0 1rem 0;
  font-size: 1.1rem;
  color: #1e293b;
}

.gauge-container {
  display: flex;
  align-items: center;
  gap: 1rem;
  height: 250px;
}

.gauge-labels {
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 100%;
  font-size: 0.8rem;
  color: #64748b;
  font-weight: 600;
}

.gauge-track {
  position: relative;
  width: 48px;
  height: 100%;
  background: linear-gradient(to top, #e0f2fe, #bfdbfe);
  border-radius: 24px;
  border: 2px solid #e2e8f0;
  overflow: hidden;
}

.gauge-fill {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  border-radius: 0 0 20px 20px;
  transition: height 0.5s ease, background-color 0.5s ease;
}

.gauge-value {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.5rem;
}

.water-level {
  font-size: 2rem;
  font-weight: 700;
  color: #1e293b;
}

.tide-status {
  font-size: 1.1rem;
  font-weight: 600;
}

.water-temp {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.4rem;
  margin-top: 1rem;
  padding-top: 1rem;
  border-top: 1px solid #e2e8f0;
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
}

.water-temp-na {
  color: #94a3b8;
  font-weight: 400;
  font-size: 0.9rem;
}

.temp-icon {
  font-size: 1.2rem;
}

.temp-value {
  letter-spacing: 0.5px;
}
</style>
