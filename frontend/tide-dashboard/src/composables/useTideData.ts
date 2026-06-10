import { ref } from 'vue'
import type { TideData } from '../types/tide'
import { getTideData } from '../services/tideApi'

export function useTideData() {
  const tideData = ref<TideData | null>(null)
  const isLoading = ref(false)
  const error = ref<string | null>(null)

  async function fetchTideData(lat: number, lng: number) {
    isLoading.value = true
    error.value = null

    try {
      const data = await getTideData(lat, lng)
      tideData.value = data
    } catch (e: any) {
      error.value = e?.response?.data || e.message || 'Failed to load tide data'
    } finally {
      isLoading.value = false
    }
  }

  return {
    tideData,
    isLoading,
    error,
    fetchTideData,
  }
}
