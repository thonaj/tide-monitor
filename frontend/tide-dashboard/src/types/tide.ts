export interface TideData {
  location: Location
  timestamp: string
  currentWaterLevel: number
  currentStatus: TideStatus
  waterTemperature: number | null
  dangerLevel: DangerLevel
  predictions: TidePrediction[]
  beachHazards: BeachHazard[]
}

export interface TidePrediction {
  time: string
  waterLevel: number
  event: TideEvent | null
}

export interface Location {
  latitude: number
  longitude: number
  displayName: string
}

export type TideStatus = 'Rising' | 'Falling' | 'High' | 'Low'
export type TideEvent = 'HighTide' | 'LowTide'
export type DangerLevel = 'Safe' | 'Caution' | 'Warning' | 'Danger'

export interface BeachHazard {
  id: string
  headline: string
  description: string
  severity: string
  eventType: string
  effective: string | null
  expires: string | null
  instruction: string
  area: string
}

export interface SavedLocation {
  id: string
  displayName: string
  latitude: number
  longitude: number
}
