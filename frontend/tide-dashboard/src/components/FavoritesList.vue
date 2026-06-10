<script setup lang="ts">
import type { SavedLocation } from '../types/tide'

const props = defineProps<{
  favorites: SavedLocation[]
}>()

const emit = defineEmits<{
  selectLocation: [location: SavedLocation]
  removeFavorite: [id: string]
}>()
</script>

<template>
  <div class="favorites-list">
    <h3 class="favorites-title">⭐ Favorite Locations</h3>
    <div v-if="favorites.length === 0" class="favorites-empty">
      <p>No saved locations yet. Search for an address and save it as a favorite!</p>
    </div>
    <ul v-else class="favorites-items">
      <li
        v-for="fav in favorites"
        :key="fav.id"
        class="favorite-item"
      >
        <button
          class="favorite-name"
          @click="emit('selectLocation', fav)"
          :title="`View tide data for ${fav.displayName}`"
        >
          <span class="favorite-icon">📍</span>
          <span class="favorite-display-name">{{ fav.displayName }}</span>
        </button>
        <button
          class="remove-button"
          @click="emit('removeFavorite', fav.id)"
          title="Remove from favorites"
        >
          ✕
        </button>
      </li>
    </ul>
  </div>
</template>

<style scoped>
.favorites-list {
  background: white;
  border-radius: 12px;
  padding: 1.5rem;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.favorites-title {
  margin: 0 0 1rem 0;
  font-size: 1.1rem;
  color: #1e293b;
}

.favorites-empty {
  color: #94a3b8;
  font-size: 0.9rem;
  font-style: italic;
}

.favorites-items {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.favorite-item {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0.5rem 0.75rem;
  border-radius: 8px;
  transition: background-color 0.2s;
}

.favorite-item:hover {
  background-color: #f8fafc;
}

.favorite-name {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  background: none;
  border: none;
  cursor: pointer;
  text-align: left;
  flex: 1;
  padding: 0;
  font-size: 0.9rem;
  color: #1e293b;
}

.favorite-name:hover {
  color: #2563eb;
}

.favorite-icon {
  font-size: 1rem;
}

.favorite-display-name {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.remove-button {
  background: none;
  border: none;
  cursor: pointer;
  color: #94a3b8;
  font-size: 0.85rem;
  padding: 0.25rem 0.5rem;
  border-radius: 4px;
  transition: color 0.2s, background-color 0.2s;
}

.remove-button:hover {
  color: #dc2626;
  background-color: #fef2f2;
}
</style>
