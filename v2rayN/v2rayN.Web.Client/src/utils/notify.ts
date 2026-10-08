import { ElMessage } from 'element-plus'
import { ApiError } from '../api/client'

const HTTP_UNAUTHORIZED = 401

/** Shows a user-facing message for a failed action. 401 is handled by the router redirect. */
export function reportError(err: unknown): void {
  if (err instanceof ApiError && err.status === HTTP_UNAUTHORIZED) {
    return
  }
  const message = err instanceof Error ? err.message : String(err)
  ElMessage.error({ message, grouping: true })
}

/** Runs an async UI action and reports any failure instead of leaving a floating promise. */
export function runAction(action: () => Promise<unknown>): void {
  action().catch(reportError)
}
