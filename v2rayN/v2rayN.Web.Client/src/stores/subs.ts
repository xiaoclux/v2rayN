import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api } from '../api/client'
import { API, SERVER_EVENTS } from '../api/endpoints'
import type { SaveResponse, SubItem, SubListResponse } from '../api/types'
import { useEventsStore } from './events'
import { runAction } from '../utils/notify'

/** Value of the "all profiles" tab; matches the desktop's empty SubIndexId. */
export const ALL_SUBS_ID = ''

export const useSubsStore = defineStore('subs', () => {
  const items = ref<SubItem[]>([])
  const currentSubId = ref<string>(ALL_SUBS_ID)
  let isBound = false

  async function load(): Promise<void> {
    const res = await api.get<SubListResponse>(API.subs)
    items.value = res.items
    const exists = res.currentSubId && res.items.some((s) => s.id === res.currentSubId)
    currentSubId.value = exists ? (res.currentSubId as string) : ALL_SUBS_ID
  }

  async function selectSub(subId: string): Promise<void> {
    currentSubId.value = subId
    await api.put(API.subsCurrent, { subId })
  }

  async function save(sub: SubItem): Promise<SaveResponse> {
    const res = sub.id ? await api.put<SaveResponse>(API.sub(sub.id), sub) : await api.post<SaveResponse>(API.subs, sub)
    await load()
    return res
  }

  async function remove(id: string): Promise<void> {
    await api.del(API.sub(id))
    await load()
  }

  async function update(subId: string, isViaProxy: boolean): Promise<void> {
    await api.post(API.subsUpdate, { subId, viaProxy: isViaProxy })
  }

  function bindEvents(): void {
    if (isBound) {
      return
    }
    isBound = true
    useEventsStore().on(SERVER_EVENTS.subsChanged, () => runAction(load))
  }

  return { items, currentSubId, load, selectSub, save, remove, update, bindEvents }
})

/** A blank subscription with the same defaults as the desktop SubItem. */
export function newSubItem(): SubItem {
  return {
    id: '',
    remarks: '',
    url: '',
    moreUrl: null,
    enabled: true,
    userAgent: '',
    requestHeaders: null,
    sort: 0,
    filter: null,
    autoUpdateInterval: 0,
    updateTime: 0,
    convertTarget: null,
    prevProfile: null,
    nextProfile: null,
    preSocksPort: null,
    memo: null,
    customCoreType: null,
  }
}
