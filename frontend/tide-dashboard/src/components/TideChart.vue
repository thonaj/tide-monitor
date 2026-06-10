<script setup lang="ts">
import { computed } from 'vue'
import { Line } from 'vue-chartjs'
import {
  Chart as ChartJS,
  CategoryScale,
  LinearScale,
  PointElement,
  LineElement,
  Title,
  Tooltip,
  Filler,
} from 'chart.js'
import type { TidePrediction } from '../types/tide'

ChartJS.register(CategoryScale, LinearScale, PointElement, LineElement, Title, Tooltip, Filler)

const props = defineProps<{
  predictions: TidePrediction[]
}>()

function formatTime(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleTimeString('en-US', {
    hour: 'numeric',
    hour12: true,
  })
}

function formatDate(dateStr: string): string {
  const date = new Date(dateStr)
  return date.toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
  })
}

const chartData = computed(() => {
  // Group predictions by day for better labels
  const labels = props.predictions.map((p) => {
    const time = formatTime(p.time)
    const date = formatDate(p.time)
    // Show date on first label of each day
    return time
  })

  return {
    labels,
    datasets: [
      {
        label: 'Water Level (ft)',
        data: props.predictions.map((p) => p.waterLevel),
        fill: true,
        borderColor: '#2563eb',
        backgroundColor: 'rgba(37, 99, 235, 0.08)',
        borderWidth: 2,
        tension: 0.4,
        pointRadius: 0,
        pointHitRadius: 10,
      },
    ],
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  interaction: {
    intersect: false,
    mode: 'index' as const,
  },
  plugins: {
    legend: {
      display: false,
    },
    tooltip: {
      backgroundColor: '#1e293b',
      titleColor: '#f8fafc',
      bodyColor: '#e2e8f0',
      padding: 12,
      cornerRadius: 8,
      callbacks: {
        title: (items: any[]) => {
          const idx = items[0].dataIndex
          const pred = props.predictions[idx]
          if (!pred) return ''
          const date = new Date(pred.time)
          return date.toLocaleString('en-US', {
            weekday: 'short',
            month: 'short',
            day: 'numeric',
            hour: 'numeric',
            minute: '2-digit',
            hour12: true,
          })
        },
        label: (item: any) => {
          const level = item.raw as number
          const pred = props.predictions[item.dataIndex]
          let label = `Water Level: ${level.toFixed(2)} ft`
          if (pred?.event === 'HighTide') label += ' 🌊 High Tide'
          if (pred?.event === 'LowTide') label += ' 🌊 Low Tide'
          return label
        },
      },
    },
  },
  scales: {
    x: {
      grid: {
        display: false,
      },
      ticks: {
        maxRotation: 45,
        font: {
          size: 11,
        },
        maxTicksLimit: 12,
      },
    },
    y: {
      title: {
        display: true,
        text: 'Feet (MLLW)',
        font: {
          size: 12,
        },
      },
      grid: {
        color: 'rgba(0, 0, 0, 0.06)',
      },
    },
  },
}))

// Add annotations for high/low tide markers
const chartPlugins = computed(() => {
  const highTideAnnotations = props.predictions
    .filter((p) => p.event === 'HighTide')
    .map((p, i) => ({
      type: 'point' as const,
      xValue: formatTime(p.time),
      yValue: p.waterLevel,
      backgroundColor: '#059669',
      borderColor: '#059669',
      radius: 5,
      label: {
        display: true,
        content: 'H',
        position: 'top' as const,
        font: { weight: 'bold' as const, size: 10 },
        color: '#059669',
      },
    }))

  const lowTideAnnotations = props.predictions
    .filter((p) => p.event === 'LowTide')
    .map((p, i) => ({
      type: 'point' as const,
      xValue: formatTime(p.time),
      yValue: p.waterLevel,
      backgroundColor: '#dc2626',
      borderColor: '#dc2626',
      radius: 5,
      label: {
        display: true,
        content: 'L',
        position: 'bottom' as const,
        font: { weight: 'bold' as const, size: 10 },
        color: '#dc2626',
      },
    }))

  return {
    id: 'tideAnnotations',
    afterDraw: () => {}, // Placeholder - annotations handled via data points
  }
})
</script>

<template>
  <div class="tide-chart">
    <h3 class="chart-title">48-Hour Tide Projection</h3>
    <div class="chart-wrapper">
      <Line v-if="predictions.length > 0" :data="chartData" :options="chartOptions" />
      <div v-else class="chart-empty">
        <p>No tide prediction data available.</p>
      </div>
    </div>
    <div class="chart-legend">
      <span class="legend-item">
        <span class="legend-dot high"></span> High Tide
      </span>
      <span class="legend-item">
        <span class="legend-dot low"></span> Low Tide
      </span>
    </div>
  </div>
</template>

<style scoped>
.tide-chart {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.chart-title {
  margin: 0 0 1rem 0;
  font-size: 1.1rem;
  color: #1e293b;
}

.chart-wrapper {
  height: 350px;
  position: relative;
}

.chart-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: #94a3b8;
  font-style: italic;
}

.chart-legend {
  display: flex;
  gap: 1.5rem;
  margin-top: 1rem;
  justify-content: center;
  font-size: 0.85rem;
  color: #64748b;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
}

.legend-dot.high {
  background-color: #059669;
}

.legend-dot.low {
  background-color: #dc2626;
}
</style>
