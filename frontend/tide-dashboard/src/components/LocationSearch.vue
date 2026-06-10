<script setup lang="ts">
import { ref } from 'vue'
import { geocodeAddress } from '../services/tideApi'
import type { Location } from '../types/tide'

const emit = defineEmits<{
  locationSelected: [location: Location]
}>()

const address = ref('')
const isSearching = ref(false)
const searchError = ref<string | null>(null)

async function handleSearch() {
  if (!address.value.trim()) return

  isSearching.value = true
  searchError.value = null

  try {
    const location = await geocodeAddress(address.value)
    emit('locationSelected', location)
  } catch (e: any) {
    searchError.value = e?.response?.data || 'Could not find that address. Please try again.'
  } finally {
    isSearching.value = false
  }
}

function handleKeydown(e: KeyboardEvent) {
  if (e.key === 'Enter') {
    handleSearch()
  }
}
</script>

<template>
  <div class="location-search">
    <div class="search-input-group">
      <input
        v-model="address"
        type="text"
        placeholder="Enter a beach house address (e.g., 807 Ocean Drive, Emerald Isle, NC)"
        :disabled="isSearching"
        @keydown="handleKeydown"
        class="search-input"
      />
      <button
        @click="handleSearch"
        :disabled="isSearching || !address.trim()"
        class="search-button"
      >
        {{ isSearching ? 'Searching...' : 'Search' }}
      </button>
    </div>
    <p v-if="searchError" class="search-error">{{ searchError }}</p>
  </div>
</template>

<style scoped>
.location-search {
  margin-bottom: 1.5rem;
}

.search-input-group {
  display: flex;
  gap: 0.5rem;
}

.search-input {
  flex: 1;
  padding: 0.75rem 1rem;
  border: 2px solid #e2e8f0;
  border-radius: 8px;
  font-size: 1rem;
  transition: border-color 0.2s;
  outline: none;
}

.search-input:focus {
  border-color: #2563eb;
}

.search-input:disabled {
  background-color: #f1f5f9;
}

.search-button {
  padding: 0.75rem 1.5rem;
  background-color: #2563eb;
  color: white;
  border: none;
  border-radius: 8px;
  font-size: 1rem;
  font-weight: 600;
  cursor: pointer;
  transition: background-color 0.2s;
  white-space: nowrap;
}

.search-button:hover:not(:disabled) {
  background-color: #1d4ed8;
}

.search-button:disabled {
  background-color: #93c5fd;
  cursor: not-allowed;
}

.search-error {
  color: #dc2626;
  font-size: 0.875rem;
  margin-top: 0.5rem;
}
</style>
