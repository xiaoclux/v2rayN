import { createI18n } from 'vue-i18n'
import { STORAGE_KEYS } from '../api/endpoints'
import en from './generated/en.json'
import webEn from './web/en.json'

export const DEFAULT_LOCALE = 'en'

// Every resx translation except the default is loaded on demand.
const loaders = import.meta.glob<{ default: Messages }>('./generated/*.json')
// Web-only strings (login, connection state) that have no desktop resx equivalent.
const webLoaders = import.meta.glob<{ default: Messages }>('./web/*.json')

type Messages = Record<string, string>
const initialMessages: Record<string, Messages> = { [DEFAULT_LOCALE]: { ...en, ...webEn } }
const initialLocale: string = DEFAULT_LOCALE

export const i18n = createI18n({
  legacy: false,
  locale: initialLocale,
  fallbackLocale: initialLocale,
  messages: initialMessages,
  missingWarn: false,
  fallbackWarn: false,
})

export const AVAILABLE_LOCALES = Object.keys(loaders)
  .map((path) => path.replace('./generated/', '').replace('.json', ''))
  .filter((locale) => locale !== 'locales')

function readStoredLocale(): string | null {
  try {
    return localStorage.getItem(STORAGE_KEYS.locale)
  } catch {
    // Storage may be blocked (private mode); fall back to the server language.
    return null
  }
}

export function storeLocale(locale: string): void {
  try {
    localStorage.setItem(STORAGE_KEYS.locale, locale)
  } catch {
    // Not persisting the choice is acceptable; it still applies to this session.
  }
}

/** Switches the UI language, loading its messages if needed. Unknown locales fall back to English. */
export async function setLocale(requested: string): Promise<void> {
  const locale = AVAILABLE_LOCALES.includes(requested) ? requested : DEFAULT_LOCALE
  if (!i18n.global.availableLocales.includes(locale)) {
    const loader = loaders[`./generated/${locale}.json`]
    const webLoader = webLoaders[`./web/${locale}.json`]
    const messages = loader ? (await loader()).default : {}
    const webMessages = webLoader ? (await webLoader()).default : {}
    i18n.global.setLocaleMessage(locale, { ...messages, ...webMessages })
  }
  i18n.global.locale.value = locale
  document.documentElement.lang = locale
}

/** Picks the stored locale, else the server's configured language. */
export async function initLocale(serverLanguage: string | null): Promise<void> {
  await setLocale(readStoredLocale() ?? serverLanguage ?? DEFAULT_LOCALE)
}
