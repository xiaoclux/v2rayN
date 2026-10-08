import { defineStore } from 'pinia'
import { ref } from 'vue'
import { api, setCsrfToken } from '../api/client'
import { API } from '../api/endpoints'
import type { MetaResponse, SessionResponse } from '../api/types'

export const useSessionStore = defineStore('session', () => {
  const isAuthenticated = ref(false)
  const meta = ref<MetaResponse | null>(null)

  /** Refreshes auth state and the CSRF token (tokens are bound to the signed-in identity). */
  async function refresh(): Promise<boolean> {
    const session = await api.get<SessionResponse>(API.session)
    setCsrfToken(session.csrfToken)
    isAuthenticated.value = session.authenticated
    return session.authenticated
  }

  async function loadMeta(): Promise<MetaResponse> {
    meta.value = await api.get<MetaResponse>(API.meta)
    return meta.value
  }

  async function login(credential: string): Promise<void> {
    await refresh()
    await api.post(API.login, { password: credential })
    await refresh()
  }

  async function logout(): Promise<void> {
    await api.post(API.logout)
    isAuthenticated.value = false
    meta.value = null
    await refresh()
  }

  function markSignedOut(): void {
    isAuthenticated.value = false
  }

  return { isAuthenticated, meta, refresh, loadMeta, login, logout, markSignedOut }
})
