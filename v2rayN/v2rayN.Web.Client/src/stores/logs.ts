import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../api/client'
import { API, SERVER_EVENTS } from '../api/endpoints'
import { useEventsStore } from './events'

// Same cap as the server ring buffer, so the panel never grows without bound.
const MAX_LINES = 2000

export const useLogsStore = defineStore('logs', () => {
  const lines = ref<string[]>([])
  const isPaused = ref(false)
  let isBound = false

  function append(batch: string[]): void {
    if (isPaused.value) {
      return
    }
    const next = lines.value.concat(batch)
    lines.value = next.length > MAX_LINES ? next.slice(next.length - MAX_LINES) : next
  }

  async function loadRecent(): Promise<void> {
    lines.value = await api.get<string[]>(API.logsRecent)
  }

  function bindEvents(): void {
    if (isBound) {
      return
    }
    isBound = true
    useEventsStore().on<string[]>(SERVER_EVENTS.log, append)
  }

  function clear(): void {
    lines.value = []
  }

  return { lines, isPaused, loadRecent, bindEvents, clear }
})
