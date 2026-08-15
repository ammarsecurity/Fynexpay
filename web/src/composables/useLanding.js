import { computed, onMounted, ref } from 'vue'
import { siteCopy } from '../i18n/site'

const apiUrl = import.meta.env.VITE_API_BASE || 'http://localhost:5080'
const content = ref(null)
let inflight = null

function looksEscaped(value) {
  return typeof value === 'string' && /\\u[0-9a-fA-F]{4}/.test(value)
}

function unescapeUnicode(value) {
  if (typeof value !== 'string') return value
  let current = value
  for (let i = 0; i < 6; i++) {
    if (!looksEscaped(current)) break
    try {
      const parsed = JSON.parse(`"${current.replace(/"/g, '\\"')}"`)
      if (typeof parsed === 'string' && parsed !== current) {
        current = parsed
        continue
      }
    } catch {
      /* use regex fallback */
    }
    const next = current.replace(/\\u([0-9a-fA-F]{4})/g, (_, hex) =>
      String.fromCharCode(parseInt(hex, 16)))
    if (next === current) break
    current = next
  }
  return current
}

function decodeEscapedTree(value) {
  if (typeof value === 'string') {
    const trimmed = value.trim()
    if (
      (trimmed.startsWith('{') && trimmed.endsWith('}')) ||
      (trimmed.startsWith('[') && trimmed.endsWith(']'))
    ) {
      try {
        return decodeEscapedTree(JSON.parse(trimmed))
      } catch {
        /* keep as string */
      }
    }
    return unescapeUnicode(value)
  }
  if (Array.isArray(value)) return value.map(decodeEscapedTree)
  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value).map(([key, nested]) => [key, decodeEscapedTree(nested)])
    )
  }
  return value
}

function mergeLocale(cms, fallback) {
  const decoded = decodeEscapedTree(cms)
  if (!decoded || typeof decoded !== 'object' || Array.isArray(decoded)) {
    return { ...fallback }
  }
  const out = { ...fallback, ...decoded }
  for (const [key, val] of Object.entries(out)) {
    if (typeof val === 'string' && looksEscaped(val) && fallback[key]) {
      out[key] = fallback[key]
    }
  }
  return out
}

const saved = localStorage.getItem('fx_web_locale')
const locale = ref(saved === 'en' || saved === 'ar' ? saved : 'ar')

function applyDir(loc) {
  document.documentElement.lang = loc
  document.documentElement.dir = loc === 'ar' ? 'rtl' : 'ltr'
}

export function setLocale(loc) {
  locale.value = loc
  localStorage.setItem('fx_web_locale', loc)
  applyDir(loc)
}

function ensureContent() {
  if (content.value) {
    content.value = decodeEscapedTree(content.value)
    return Promise.resolve(content.value)
  }
  if (inflight) return inflight

  inflight = fetch(`${apiUrl}/api/landing`, { cache: 'no-store' })
    .then(async (res) => {
      if (!res.ok) throw new Error('landing')
      const data = await res.json()
      content.value = decodeEscapedTree({
        ar: data.ar || data.Ar,
        en: data.en || data.En
      })
      return content.value
    })
    .catch(() => {
      content.value = content.value || null
      return content.value
    })
    .finally(() => {
      inflight = null
    })

  return inflight
}

ensureContent()

export function useLanding() {
  applyDir(locale.value)
  ensureContent()

  onMounted(() => {
    applyDir(locale.value)
    ensureContent()
  })

  const c = computed(() => {
    const loc = locale.value === 'en' ? 'en' : 'ar'
    const fallback = siteCopy[loc] || {}
    if (!content.value) return fallback
    const cms = loc === 'en' ? content.value.en : content.value.ar
    return mergeLocale(cms, fallback)
  })

  return {
    apiUrl,
    dashboardUrl: import.meta.env.VITE_DASHBOARD_URL || 'http://localhost:5173',
    locale,
    c,
    setLocale,
    ensureContent,
    year: new Date().getFullYear()
  }
}
