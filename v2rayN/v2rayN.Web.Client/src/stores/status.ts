import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../api/client'
import { API, SERVER_EVENTS } from '../api/endpoints'
import type { RunningState, ServerSpeedItem, StatusResponse } from '../api/types'
import { useEventsStore } from './events'
import { runAction } from '../utils/notify'

export const useStatusStore = defineStore('status', () => {
  const status = ref<StatusResponse | null>(null)
  const speed = ref<ServerSpeedItem | null>(null)
  let isBound = false

  async function load(): Promise<void> {
    status.value = await api.get<StatusResponse>(API.status)
  }

  function bindEvents(): void {
    if (isBound) {
      return
    }
    isBound = true
    const events = useEventsStore()
    events.on<RunningState>(SERVER_EVENTS.statusRunning, (running) => {
      if (status.value) {
        status.value = { ...status.value, running }
      }
    })
    events.on<ServerSpeedItem>(SERVER_EVENTS.speed, (item) => {
      speed.value = item
    })
    events.on(SERVER_EVENTS.routingChanged, () => {
      runAction(load)
    })
  }

  async function reload(): Promise<void> {
    await api.post(API.coreReload)
  }

  async function setRouting(routingId: string): Promise<void> {
    await api.put(API.statusRouting, { routingId })
    await load()
  }

  return { status, speed, load, bindEvents, reload, setRouting }
})
