import axios from 'axios'
import type { TideData, Location } from '../types/tide'

// Using Vite proxy - requests go to /api which is proxied to the backend
const apiClient = axios.create({
  baseURL: '/api',
  timeout: 15000,
})

export async function getTideData(lat: number, lng: number): Promise<TideData> {
  const response = await apiClient.get<TideData>('/tide', {
    params: { lat, lng },
  })
  return response.data
}

export async function geocodeAddress(address: string): Promise<Location> {
  const response = await apiClient.get<Location>('/location/geocode', {
    params: { address },
  })
  return response.data
}
