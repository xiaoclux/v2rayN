// Converts ServiceLib/Resx/ResUI*.resx into vue-i18n JSON so the web UI reuses every
// desktop string and translation.
// Usage: node scripts/resx2json.mjs <resx-dir> <out-dir>
import { readdirSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs'
import { join } from 'node:path'

const DEFAULT_LOCALE = 'en'
const RESX_FILE_PATTERN = /^ResUI(?:\.([\w-]+))?\.resx$/
const DATA_PATTERN = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g
const XML_ENTITIES = { '&lt;': '<', '&gt;': '>', '&quot;': '"', '&apos;': "'", '&amp;': '&' }
const XML_ENTITY_PATTERN = /&(lt|gt|quot|apos|amp);/g
// vue-i18n treats these as syntax; .NET {0} placeholders map to list interpolation unchanged.
const I18N_SPECIAL_PATTERN = /[@|$]/g

const [, , resxDir, outDir] = process.argv
if (!resxDir || !outDir) {
  console.error('Usage: node scripts/resx2json.mjs <resx-dir> <out-dir>')
  process.exit(1)
}

function decode(value) {
  return value.replace(XML_ENTITY_PATTERN, (entity) => XML_ENTITIES[entity])
}

function escapeForI18n(value) {
  return value.replace(I18N_SPECIAL_PATTERN, (ch) => `{'${ch}'}`)
}

mkdirSync(outDir, { recursive: true })
const locales = []
for (const file of readdirSync(resxDir)) {
  const match = RESX_FILE_PATTERN.exec(file)
  if (!match) {
    continue
  }
  const locale = match[1] ?? DEFAULT_LOCALE
  const xml = readFileSync(join(resxDir, file), 'utf8')
  const messages = {}
  for (const [, name, value] of xml.matchAll(DATA_PATTERN)) {
    messages[name] = escapeForI18n(decode(value))
  }
  writeFileSync(join(outDir, `${locale}.json`), JSON.stringify(messages, null, 2))
  locales.push(locale)
}
writeFileSync(join(outDir, 'locales.json'), JSON.stringify(locales.sort()))
console.log(`resx2json: ${locales.length} locales -> ${outDir}`)
