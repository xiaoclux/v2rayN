const BYTE_UNITS = ['B', 'KB', 'MB', 'GB', 'TB'] as const
const BYTE_STEP = 1024
const FRACTION_DIGITS = 1

/** Human-readable byte size, e.g. 1536 -> "1.5 KB". */
export function formatBytes(value: number | null | undefined): string {
  let size = Math.max(0, value ?? 0)
  let unit = 0
  while (size >= BYTE_STEP && unit < BYTE_UNITS.length - 1) {
    size /= BYTE_STEP
    unit++
  }
  return `${unit === 0 ? size : size.toFixed(FRACTION_DIGITS)} ${BYTE_UNITS[unit]}`
}

/** Per-second rate, e.g. "1.5 KB/s". */
export function formatRate(value: number | null | undefined): string {
  return `${formatBytes(value)}/s`
}
