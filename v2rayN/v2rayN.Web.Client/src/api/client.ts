// Thin fetch wrapper: sends the session cookie, attaches the CSRF token to unsafe
// requests and turns 401 into a redirect to the login page.

const CSRF_HEADER = 'X-CSRF-TOKEN'
const UNSAFE_METHODS = new Set(['POST', 'PUT', 'PATCH', 'DELETE'])
const HTTP_NO_CONTENT = 204
const HTTP_UNAUTHORIZED = 401

let csrfToken: string | null = null
let onUnauthorized: (() => void) | null = null

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
  }
}

export function setCsrfToken(token: string | null): void {
  csrfToken = token
}

export function setUnauthorizedHandler(handler: () => void): void {
  onUnauthorized = handler
}

interface ProblemDetails {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function errorMessage(res: Response): Promise<string> {
  try {
    const problem = (await res.json()) as ProblemDetails
    const fieldErrors = problem.errors ? Object.values(problem.errors).flat().join('; ') : ''
    return problem.detail || fieldErrors || problem.title || res.statusText
  } catch {
    return res.statusText
  }
}

export async function request<T>(method: string, url: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = {}
  let payload: BodyInit | undefined
  if (body instanceof FormData) {
    payload = body
  } else if (body !== undefined) {
    headers['Content-Type'] = 'application/json'
    payload = JSON.stringify(body)
  }
  if (UNSAFE_METHODS.has(method) && csrfToken) {
    headers[CSRF_HEADER] = csrfToken
  }

  const res = await fetch(url, { method, headers, body: payload, credentials: 'same-origin' })
  if (res.status === HTTP_UNAUTHORIZED && !url.startsWith('/api/auth/')) {
    onUnauthorized?.()
  }
  if (!res.ok) {
    throw new ApiError(res.status, await errorMessage(res))
  }
  if (res.status === HTTP_NO_CONTENT || res.headers.get('Content-Length') === '0') {
    return undefined as T
  }
  const contentType = res.headers.get('Content-Type') ?? ''
  return (contentType.includes('json') ? await res.json() : await res.text()) as T
}

export const api = {
  get: <T>(url: string) => request<T>('GET', url),
  post: <T = void>(url: string, body?: unknown) => request<T>('POST', url, body),
  put: <T = void>(url: string, body?: unknown) => request<T>('PUT', url, body),
  del: <T = void>(url: string, body?: unknown) => request<T>('DELETE', url, body),
}
