// API paths and SSE event names; keep in sync with v2rayN.Web/Endpoints and Common/WebConsts.cs.

export const API = {
  session: '/api/auth/session',
  login: '/api/auth/login',
  logout: '/api/auth/logout',
  meta: '/api/meta',
  status: '/api/status',
  statusRouting: '/api/status/routing',
  statusServer: '/api/status/server',
  coreReload: '/api/core/reload',
  logsRecent: '/api/logs/recent',
  events: '/api/events',
  subs: '/api/subs',
  sub: (id: string) => `/api/subs/${encodeURIComponent(id)}`,
  subsCurrent: '/api/subs/current',
  subsUpdate: '/api/subs/update',
  profiles: '/api/profiles',
  profilesRemove: '/api/profiles/remove',
  profilesImport: '/api/profiles/import',
  profilesImportQr: '/api/profiles/import-qr',
  profileShare: (id: string) => `/api/profiles/${encodeURIComponent(id)}/share`,
  profileQr: (id: string) => `/api/profiles/${encodeURIComponent(id)}/qr.png`,
  speedtest: '/api/speedtest',
  speedtestStop: '/api/speedtest/stop',
  speedtestCurrent: '/api/speedtest/current',
} as const

/** ESpeedActionType names accepted by POST /api/speedtest. */
export const SPEED_ACTIONS = {
  tcping: 'Tcping',
  realping: 'Realping',
  udpTest: 'UdpTest',
  speedtest: 'Speedtest',
  mixedtest: 'Mixedtest',
  fastRealping: 'FastRealping',
} as const

export type SpeedAction = (typeof SPEED_ACTIONS)[keyof typeof SPEED_ACTIONS]

/** Upload cap enforced server-side (InputLimits.MaxUploadBytes). */
export const MAX_UPLOAD_BYTES = 10 * 1024 * 1024

export const SERVER_EVENTS = {
  log: 'log',
  toast: 'toast',
  speed: 'speed',
  statusRunning: 'status.running',
  reloadState: 'reload.state',
  profilesChanged: 'profiles.changed',
  subsChanged: 'subs.changed',
  routingChanged: 'routing.changed',
  configChanged: 'config.changed',
  speedtestResult: 'speedtest.result',
  jobProgress: 'job.progress',
  updateAvailable: 'update.available',
  clashReload: 'clash.reload',
  appStopping: 'app.stopping',
} as const

export type ServerEventName = (typeof SERVER_EVENTS)[keyof typeof SERVER_EVENTS]

export const STORAGE_KEYS = {
  locale: 'v2rayn.locale',
} as const

export const ROUTES = {
  login: '/login',
  home: '/',
} as const
