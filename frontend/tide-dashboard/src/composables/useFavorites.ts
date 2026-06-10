import { ref, onMounted } from 'vue'
import type { SavedLocation } from '../types/tide'

const STORAGE_KEY = 'tide-favorites'

export function useFavorites() {
  const favorites = ref<SavedLocation[]>([])

  function loadFavorites() {
    try {
      const stored = localStorage.getItem(STORAGE_KEY)
      favorites.value = stored ? JSON.parse(stored) : []
    } catch {
      favorites.value = []
    }
  }

  function saveFavorites() {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(favorites.value))
  }

  function addFavorite(location: SavedLocation) {
    const exists = favorites.value.some((f) => f.id === location.id)
    if (!exists) {
      favorites.value.push(location)
      saveFavorites()
    }
  }

  function removeFavorite(id: string) {
    favorites.value = favorites.value.filter((f) => f.id !== id)
    saveFavorites()
  }

  function isFavorite(lat: number, lng: number): boolean {
    return favorites.value.some(
      (f) => Math.abs(f.latitude - lat) < 0.001 && Math.abs(f.longitude - lng) < 0.001
    )
  }

  onMounted(loadFavorites)

  return {
    favorites,
    addFavorite,
    removeFavorite,
    isFavorite,
    loadFavorites,
  }
}
