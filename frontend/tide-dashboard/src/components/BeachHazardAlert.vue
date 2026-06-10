<script setup lang="ts">
import { computed, ref } from 'vue'
import type { BeachHazard } from '../types/tide'

const props = defineProps<{
  hazards: BeachHazard[]
}>()

const severityOrder: Record<string, number> = {
  'Extreme': 0,
  'Severe': 1,
  'Moderate': 2,
  'Minor': 3,
  'Unknown': 4,
}

const sortedHazards = computed(() => {
  return [...props.hazards].sort((a, b) => {
    const aOrder = severityOrder[a.severity] ?? 99
    const bOrder = severityOrder[b.severity] ?? 99
    return aOrder - bOrder
  })
})

function getSeverityClass(severity: string): string {
  switch (severity) {
    case 'Extreme': return 'severity-extreme'
    case 'Severe': return 'severity-severe'
    case 'Moderate': return 'severity-moderate'
    case 'Minor': return 'severity-minor'
    default: return 'severity-unknown'
  }
}

function getSeverityIcon(severity: string): string {
  switch (severity) {
    case 'Extreme': return '🔴'
    case 'Severe': return '🟠'
    case 'Moderate': return '🟡'
    case 'Minor': return '🔵'
    default: return '⚪'
  }
}

function getEventIcon(eventType: string): string {
  const type = eventType.toLowerCase()
  if (type.includes('rip current')) return '🌊'
  if (type.includes('high surf')) return '🏄'
  if (type.includes('coastal flood') || type.includes('lakeshore flood')) return '🌊'
  if (type.includes('hurricane') || type.includes('tropical storm')) return '🌀'
  if (type.includes('storm surge')) return '🌊'
  if (type.includes('beach hazard')) return '⚠️'
  if (type.includes('thunderstorm')) return '⛈️'
  if (type.includes('tornado')) return '🌪️'
  return '⚠️'
}

function formatDate(dateStr: string | null): string {
  if (!dateStr) return 'N/A'
  const date = new Date(dateStr)
  return date.toLocaleString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
    hour12: true,
  })
}

function truncateDescription(text: string, maxLength = 200): string {
  if (text.length <= maxLength) return text
  return text.substring(0, maxLength) + '...'
}

const expandedHazardIds = ref<string[]>([])

function toggleExpand(id: string) {
  const index = expandedHazardIds.value.indexOf(id)
  if (index >= 0) {
    expandedHazardIds.value.splice(index, 1)
  } else {
    expandedHazardIds.value.push(id)
  }
}

function isExpanded(id: string): boolean {
  return expandedHazardIds.value.includes(id)
}
</script>

<template>
  <div v-if="hazards.length > 0" class="beach-hazards">
    <div class="hazards-header">
      <span class="hazards-icon">⚠️</span>
      <h3 class="hazards-title">Beach Hazard Alerts ({{ hazards.length }})</h3>
    </div>
    <div class="hazards-list">
      <div
        v-for="hazard in sortedHazards"
        :key="hazard.id"
        class="hazard-card"
        :class="getSeverityClass(hazard.severity)"
      >
        <div class="hazard-header" @click="toggleExpand(hazard.id)">
          <span class="hazard-severity-icon">{{ getSeverityIcon(hazard.severity) }}</span>
          <span class="hazard-event-icon">{{ getEventIcon(hazard.eventType) }}</span>
          <div class="hazard-info">
            <span class="hazard-event-type">{{ hazard.eventType }}</span>
            <span class="hazard-headline">{{ hazard.headline }}</span>
          </div>
          <span class="hazard-expand">{{ isExpanded(hazard.id) ? '▲' : '▼' }}</span>
        </div>
        <div v-if="isExpanded(hazard.id)" class="hazard-body">
          <div class="hazard-meta">
            <span class="meta-item">
              <strong>Severity:</strong>
              <span class="severity-badge" :class="getSeverityClass(hazard.severity)">
                {{ getSeverityIcon(hazard.severity) }} {{ hazard.severity }}
              </span>
            </span>
            <span class="meta-item">
              <strong>Effective:</strong> {{ formatDate(hazard.effective) }}
            </span>
            <span class="meta-item">
              <strong>Expires:</strong> {{ formatDate(hazard.expires) }}
            </span>
            <span class="meta-item" v-if="hazard.area">
              <strong>Area:</strong> {{ hazard.area }}
            </span>
          </div>
          <p class="hazard-description">{{ hazard.description }}</p>
          <div v-if="hazard.instruction" class="hazard-instruction">
            <strong>Instructions:</strong>
            <p>{{ hazard.instruction }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.beach-hazards {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
  margin-bottom: 1.5rem;
}

.hazards-header {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-bottom: 1rem;
}

.hazards-icon {
  font-size: 1.3rem;
}

.hazards-title {
  margin: 0;
  font-size: 1.1rem;
  color: #1e293b;
}

.hazards-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.hazard-card {
  border-radius: 10px;
  border: 1px solid #e2e8f0;
  overflow: hidden;
  transition: box-shadow 0.2s;
}

.hazard-card:hover {
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.08);
}

.hazard-card.severity-extreme {
  border-left: 4px solid #dc2626;
}

.hazard-card.severity-severe {
  border-left: 4px solid #ea580c;
}

.hazard-card.severity-moderate {
  border-left: 4px solid #f59e0b;
}

.hazard-card.severity-minor {
  border-left: 4px solid #3b82f6;
}

.hazard-card.severity-unknown {
  border-left: 4px solid #94a3b8;
}

.hazard-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.85rem 1rem;
  cursor: pointer;
  user-select: none;
  transition: background-color 0.15s;
}

.hazard-header:hover {
  background-color: #f8fafc;
}

.hazard-severity-icon,
.hazard-event-icon {
  font-size: 1.2rem;
  flex-shrink: 0;
}

.hazard-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.hazard-event-type {
  font-size: 0.8rem;
  font-weight: 700;
  color: #64748b;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.hazard-headline {
  font-size: 0.95rem;
  font-weight: 600;
  color: #1e293b;
  line-height: 1.3;
}

.hazard-expand {
  font-size: 0.75rem;
  color: #94a3b8;
  flex-shrink: 0;
}

.hazard-body {
  padding: 0 1rem 1rem 1rem;
  border-top: 1px solid #f1f5f9;
}

.hazard-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 0.75rem;
  padding: 0.75rem 0;
  font-size: 0.85rem;
  color: #475569;
}

.meta-item {
  display: flex;
  align-items: center;
  gap: 0.35rem;
}

.severity-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.15rem 0.5rem;
  border-radius: 4px;
  font-size: 0.8rem;
  font-weight: 600;
}

.severity-badge.severity-extreme {
  background-color: #fef2f2;
  color: #dc2626;
}

.severity-badge.severity-severe {
  background-color: #fff7ed;
  color: #ea580c;
}

.severity-badge.severity-moderate {
  background-color: #fffbeb;
  color: #d97706;
}

.severity-badge.severity-minor {
  background-color: #eff6ff;
  color: #2563eb;
}

.hazard-description {
  font-size: 0.9rem;
  line-height: 1.6;
  color: #334155;
  margin: 0.5rem 0;
  white-space: pre-line;
}

.hazard-instruction {
  margin-top: 0.75rem;
  padding: 0.75rem;
  background-color: #fefce8;
  border: 1px solid #fef08a;
  border-radius: 8px;
  font-size: 0.9rem;
  line-height: 1.5;
  color: #713f12;
}

.hazard-instruction p {
  margin: 0.25rem 0 0 0;
  white-space: pre-line;
}
</style>
