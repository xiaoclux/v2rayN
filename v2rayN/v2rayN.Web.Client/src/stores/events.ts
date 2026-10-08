import { defineStore } from 'pinia'
import { ref } from 'vue'
import { API, SERVER_EVENTS, type ServerEventName } from '../api/endpoints'

type Handler = (data: unknown) => void

/**
 * Owns the single EventSource connection to /api/events and dispatches server events to
 * the stores that registered for them. EventSource reconnects automatically.
 */
export const useEventsStore = defineStore('events', () => {
  const isConnected = ref(false)
  const handlers = new Map<ServerEventName, Set<Handler>>()
  let source: EventSource | null = null

  function dispatch(type: ServerEventName, raw: string): void {
    let data: unknown
    try {
      data = JSON.parse(raw)
    } catch (err) {
      throw new Error(`Malformed server event '${type}': ${String(err)}`)
    }
    handlers.get(type)?.forEach((handler) => handler(data))
  }

  function connect(): void {
    if (source) {
      return
    }
    source = new EventSource(API.events, { withCredentials: true })
    source.onopen = () => {
      isConnected.value = true
    }
    source.onerror = () => {
      isConnected.value = false
    }
    for (const type of Object.values(SERVER_EVENTS)) {
      source.addEventListener(type, (evt) => dispatch(type, (evt as MessageEvent<string>).data))
    }
  }

  function disconnect(): void {
    source?.close()
    source = null
    isConnected.value = false
  }

  /** Registers a handler; returns a function that removes it. */
  function on<T>(type: ServerEventName, handler: (data: T) => void): () => void {
    const set = handlers.get(type) ?? new Set<Handler>()
    set.add(handler as Handler)
    handlers.set(type, set)
    return () => set.delete(handler as Handler)
  }

  return { isConnected, connect, disconnect, on }
})
