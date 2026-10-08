// DTOs mirroring v2rayN.Web endpoint records (camelCase JSON, enums as strings).

export interface SessionResponse {
  authenticated: boolean
  csrfToken: string | null
}

export interface MetaResponse {
  version: string
  runtimeIdentifier: string
  language: string
  features: Record<string, boolean>
}

export interface RunningState {
  reloading: boolean
  profileIndexId: string | null
  profileRemarks: string | null
  coreType: string | null
  delayMs: number | null
  outboundIp: string | null
  lastReloadAt: string | null
}

export interface RoutingSummary {
  id: string
  remarks: string
  isActive: boolean
}

export interface StatusResponse {
  running: RunningState
  inboundPort: number
  allowLan: boolean
  currentRoutingId: string | null
  routings: RoutingSummary[]
}

export interface ServerSpeedItem {
  indexId: string
  proxyUp: number
  proxyDown: number
  directUp: number
  directDown: number
  todayUp: number
  todayDown: number
  totalUp: number
  totalDown: number
}

/** ServiceLib SubItem as returned by /api/subs. */
export interface SubItem {
  id: string
  remarks: string
  url: string
  moreUrl: string | null
  enabled: boolean
  userAgent: string
  requestHeaders: string | null
  sort: number
  filter: string | null
  autoUpdateInterval: number
  updateTime: number
  convertTarget: string | null
  prevProfile: string | null
  nextProfile: string | null
  preSocksPort: number | null
  memo: string | null
  customCoreType: string | null
}

export interface SubListResponse {
  currentSubId: string | null
  items: SubItem[]
}

export interface ProfileRow {
  indexId: string
  configType: string
  remarks: string
  address: string
  port: number
  network: string
  streamSecurity: string
  subid: string
  subRemarks: string
  isActive: boolean
  sort: number
  delay: number
  delayVal: string
  speedVal: string
  ipInfo: string
  todayUp: string
  todayDown: string
  totalUp: string
  totalDown: string
}

export interface SpeedtestResultEvent {
  indexId: string
  delay: string | null
  speed: string | null
  ipInfo: string | null
}

export interface SaveResponse {
  id: string | null
  message: string | null
}

export interface ImportResponse {
  count: number
}
