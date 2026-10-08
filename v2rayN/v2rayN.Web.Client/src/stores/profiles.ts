import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../api/client'
import { API, SERVER_EVENTS, type SpeedAction } from '../api/endpoints'
import type { ImportResponse, ProfileRow, SpeedtestResultEvent } from '../api/types'
import { useEventsStore } from './events'
import { useSubsStore } from './subs'
import { runAction } from '../utils/notify'

export const useProfilesStore = defineStore('profiles', () => {
  const rows = ref<ProfileRow[]>([])
  const filter = ref('')
  const isLoading = ref(false)
  let isBound = false
  // Discards responses of superseded list requests (fast tab switching / typing).
  let requestSeq = 0

  async function load(): Promise<void> {
    const subs = useSubsStore()
    const seq = ++requestSeq
    const params = new URLSearchParams({ subId: subs.currentSubId, filter: filter.value.trim() })
    isLoading.value = true
    try {
      const result = await api.get<ProfileRow[]>(`${API.profiles}?${params}`)
      if (seq === requestSeq) {
        rows.value = result
      }
    } finally {
      if (seq === requestSeq) {
        isLoading.value = false
      }
    }
  }

  function applySpeedtestResult(result: SpeedtestResultEvent): void {
    rows.value = rows.value.map((row) => {
      if (row.indexId !== result.indexId) {
        return row
      }
      return {
        ...row,
        delay: result.delay ? Number.parseInt(result.delay, 10) || row.delay : row.delay,
        delayVal: result.delay ?? row.delayVal,
        speedVal: result.speed ?? row.speedVal,
        ipInfo: result.ipInfo ?? row.ipInfo,
      }
    })
  }

  async function remove(ids: string[]): Promise<void> {
    await api.post(API.profilesRemove, { ids })
    await load()
  }

  async function activate(indexId: string): Promise<void> {
    await api.put(API.statusServer, { indexId })
    rows.value = rows.value.map((row) => ({ ...row, isActive: row.indexId === indexId }))
  }

  async function importText(text: string): Promise<number> {
    const subs = useSubsStore()
    const res = await api.post<ImportResponse>(API.profilesImport, { text, subId: subs.currentSubId })
    await load()
    return res.count
  }

  async function importQr(file: File): Promise<number> {
    const subs = useSubsStore()
    const form = new FormData()
    form.append('file', file)
    form.append('subId', subs.currentSubId)
    const res = await api.post<ImportResponse>(API.profilesImportQr, form)
    await load()
    return res.count
  }

  async function share(indexId: string): Promise<string> {
    const res = await api.get<{ uri: string }>(API.profileShare(indexId))
    return res.uri
  }

  async function speedtest(action: SpeedAction, ids: string[]): Promise<void> {
    const subs = useSubsStore()
    await api.post(API.speedtest, { action, ids, subId: subs.currentSubId })
  }

  async function stopSpeedtest(): Promise<void> {
    await api.post(API.speedtestStop)
  }

  function bindEvents(): void {
    if (isBound) {
      return
    }
    isBound = true
    const events = useEventsStore()
    events.on(SERVER_EVENTS.profilesChanged, () => runAction(load))
    events.on<SpeedtestResultEvent>(SERVER_EVENTS.speedtestResult, applySpeedtestResult)
  }

  return { rows, filter, isLoading, load, remove, activate, importText, importQr, share, speedtest, stopSpeedtest, bindEvents }
})
