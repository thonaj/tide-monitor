<script setup lang="ts">
import { ref } from 'vue'
import LocationSearch from './LocationSearch.vue'
import BeachHazardAlert from './BeachHazardAlert.vue'
import TideGauge from './TideGauge.vue'
import TideChart from './TideChart.vue'
import FavoritesList from './FavoritesList.vue'
import { useTideData } from '../composables/useTideData'
import { useFavorites } from '../composables/useFavorites'
import type { Location, SavedLocation } from '../types/tide'

const { tideData, isLoading, error, fetchTideData } = useTideData()
const { favorites, addFavorite, removeFavorite, isFavorite } = useFavorites()

const currentLocation = ref<Location | null>(null)
const showFavorites = ref(true)

function onLocationSelected(location: Location) {
  currentLocation.value = location
  fetchTideData(location.latitude, location.longitude)
}

function onSelectFavorite(fav: SavedLocation) {
  currentLocation.value = {
    latitude: fav.latitude,
    longitude: fav.longitude,
    displayName: fav.displayName,
  }
  fetchTideData(fav.latitude, fav.longitude)
}

function toggleFavorite() {
  if (!currentLocation.value || !tideData.value) return

  const fav: SavedLocation = {
    id: `${currentLocation.value.latitude.toFixed(4)}_${currentLocation.value.longitude.toFixed(4)}`,
    displayName: currentLocation.value.displayName,
    latitude: currentLocation.value.latitude,
    longitude: currentLocation.value.longitude,
  }

  if (isFavorite(fav.latitude, fav.longitude)) {
    removeFavorite(fav.id)
  } else {
    addFavorite(fav)
  }
}

function isCurrentLocationFavorite(): boolean {
  if (!currentLocation.value) return false
  return isFavorite(currentLocation.value.latitude, currentLocation.value.longitude)
}

const dangerLevelIcon = ref('')
const dangerLevelTooltip = ref('')

// Watch tideData to update danger level display
import { watch } from 'vue'

watch(() => tideData.value, (data) => {
  if (!data) return
  switch (data.dangerLevel) {
    case 'Safe':
      dangerLevelIcon.value = '✅'
      dangerLevelTooltip.value = 'Tide levels are within normal range'
      break
    case 'Caution':
      dangerLevelIcon.value = '⚠️'
      dangerLevelTooltip.value = 'Tide levels are elevated. Exercise caution near the water.'
      break
    case 'Warning':
      dangerLevelIcon.value = '🔶'
      dangerLevelTooltip.value = 'Tide levels are high. Avoid water activities.'
      break
    case 'Danger':
      dangerLevelIcon.value = '🚨'
      dangerLevelTooltip.value = 'Extreme tide levels! Stay away from the shoreline.'
      break
  }
}, { immediate: true })
</script>


<template>
  <div class="dashboard">
    <header class="dashboard-header">
      <h1 class="app-title">🌊 Tide Monitor</h1>
      <p class="app-subtitle">Real-time tide and water conditions for your beach house</p>
    </header>

    <LocationSearch @location-selected="onLocationSelected" />

    <div v-if="isLoading" class="loading-state">
      <div class="spinner"></div>
      <p>Loading tide data...</p>
    </div>

    <div v-else-if="error" class="error-state">
      <p class="error-message">{{ error }}</p>
    </div>

    <div v-else-if="tideData" class="dashboard-content">
      <div class="content-header">
        <div class="location-info">
          <h2 class="location-name">{{ tideData.location.displayName }}</h2>
          <p class="location-coords">
            {{ tideData.location.latitude.toFixed(4) }}, {{ tideData.location.longitude.toFixed(4) }}
          </p>
        </div>
        <div class="header-actions">
          <span
            class="danger-badge"
            :class="'danger-' + tideData.dangerLevel.toLowerCase()"
            :title="dangerLevelTooltip"
          >
            {{ dangerLevelIcon }} {{ tideData.dangerLevel }}
          </span>
          <button
            class="favorite-toggle"
            :class="{ favorited: isCurrentLocationFavorite() }"
            @click="toggleFavorite"
            :title="isCurrentLocationFavorite() ? 'Remove from favorites' : 'Add to favorites'"
          >
            {{ isCurrentLocationFavorite() ? '★' : '☆' }}
            {{ isCurrentLocationFavorite() ? 'Saved' : 'Save' }}
          </button>
        </div>
      </div>

      <BeachHazardAlert :hazards="tideData.beachHazards" />

      <div class="dashboard-grid">
        <div class="gauge-section">
          <TideGauge :tide-data="tideData" />
        </div>
        <div class="chart-section">
          <TideChart :predictions="tideData.predictions" />
        </div>
      </div>
    </div>

    <div v-else class="welcome-state">
      <div class="welcome-icon">🏖️</div>
      <h2>Welcome to Tide Monitor</h2>
      <p>Enter a beach house address above to get started.</p>
    </div>

    <div class="sidebar-section">
      <button
        class="sidebar-toggle"
        @click="showFavorites = !showFavorites"
      >
        {{ showFavorites ? '▼' : '▶' }} Favorites ({{ favorites.length }})
      </button>
      <div v-if="showFavorites">
        <FavoritesList
          :favorites="favorites"
          @select-location="onSelectFavorite"
          @remove-favorite="removeFavorite"
        />
      </div>
    </div>
  </div>
</template>

<style scoped>
.dashboard {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem;
}

.dashboard-header {
  text-align: center;
  margin-bottom: 2rem;
}

.app-title {
  font-size: 2.5rem;
  color: #1e293b;
  margin: 0;
}

.app-subtitle {
  color: #64748b;
  margin: 0.5rem 0 0 0;
  font-size: 1.1rem;
}

.loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 4rem;
  color: #64748b;
}

.spinner {
  width: 40px;
  height: 40px;
  border: 4px solid #e2e8f0;
  border-top-color: #2563eb;
  border-radius: 50%;
  animation: spin 0.8s linear infinite;
  margin-bottom: 1rem;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

.error-state {
  text-align: center;
  padding: 2rem;
}

.error-message {
  color: #dc2626;
  background-color: #fef2f2;
  padding: 1rem;
  border-radius: 8px;
  border: 1px solid #fecaca;
}

.welcome-state {
  text-align: center;
  padding: 4rem 2rem;
  color: #64748b;
}

.welcome-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.welcome-state h2 {
  color: #1e293b;
  margin: 0 0 0.5rem 0;
}

.dashboard-content {
  margin-bottom: 2rem;
}

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 1.5rem;
}

.location-info {
  flex: 1;
}

.location-name {
  margin: 0;
  font-size: 1.3rem;
  color: #1e293b;
}

.location-coords {
  margin: 0.25rem 0 0 0;
  font-size: 0.85rem;
  color: #94a3b8;
}

.header-actions {
  display: flex;
  align-items: center;
  gap: 0.75rem;
}

.danger-badge {
  display: flex;
  align-items: center;
  gap: 0.35rem;
  padding: 0.5rem 0.85rem;
  border-radius: 8px;
  font-size: 0.85rem;
  font-weight: 700;
  letter-spacing: 0.3px;
  cursor: default;
  transition: all 0.2s;
}

.danger-safe {
  background-color: #f0fdf4;
  color: #16a34a;
  border: 1px solid #bbf7d0;
}

.danger-caution {
  background-color: #fffbeb;
  color: #d97706;
  border: 1px solid #fde68a;
}

.danger-warning {
  background-color: #fff7ed;
  color: #ea580c;
  border: 1px solid #fed7aa;
}

.danger-danger {
  background-color: #fef2f2;
  color: #dc2626;
  border: 1px solid #fecaca;
  animation: pulse-danger 2s infinite;
}

@keyframes pulse-danger {
  0%, 100% { box-shadow: 0 0 0 0 rgba(220, 38, 38, 0.3); }
  50% { box-shadow: 0 0 0 6px rgba(220, 38, 38, 0); }
}

.favorite-toggle {
  display: flex;
  align-items: center;
  gap: 0.4rem;
  padding: 0.5rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  background: white;
  font-size: 0.9rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
  color: #64748b;
}

.favorite-toggle:hover {
  border-color: #f59e0b;
  color: #f59e0b;
}

.favorite-toggle.favorited {
  border-color: #f59e0b;
  background-color: #fffbeb;
  color: #f59e0b;
}


.dashboard-grid {
  display: grid;
  grid-template-columns: 300px 1fr;
  gap: 1.5rem;
}

@media (max-width: 768px) {
  .dashboard-grid {
    grid-template-columns: 1fr;
  }
}

.gauge-section {
  min-width: 0;
}

.chart-section {
  min-width: 0;
}

.sidebar-section {
  margin-top: 1.5rem;
}

.sidebar-toggle {
  width: 100%;
  padding: 0.75rem;
  background: white;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  color: #1e293b;
  cursor: pointer;
  text-align: left;
  margin-bottom: 0.75rem;
  transition: background-color 0.2s;
}

.sidebar-toggle:hover {
  background-color: #f8fafc;
}
</style>
