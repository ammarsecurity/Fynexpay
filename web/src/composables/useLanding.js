import { computed, onMounted, ref } from 'vue'

const apiUrl = import.meta.env.VITE_API_BASE || 'http://localhost:5080'
const content = ref(null)
const loaded = ref(false)

function unescapeUnicode(value) {
  if (typeof value !== 'string' || !value.includes('\\u')) return value
  let current = value
  for (let i = 0; i < 5 && current.includes('\\u'); i++) {
    const next = current.replace(/\\u([0-9a-fA-F]{4})/g, (_, hex) =>
      String.fromCharCode(parseInt(hex, 16)))
    if (next === current) break
    current = next
  }
  return current
}

function decodeEscapedTree(value) {
  if (typeof value === 'string') return unescapeUnicode(value)
  if (Array.isArray(value)) return value.map(decodeEscapedTree)
  if (value && typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value).map(([key, nested]) => [key, decodeEscapedTree(nested)])
    )
  }
  return value
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

async function ensureContent() {
  if (loaded.value && content.value) return content.value
  try {
    const res = await fetch(`${apiUrl}/api/landing`)
    if (!res.ok) throw new Error('landing')
    const data = await res.json()
    content.value = decodeEscapedTree({
      ar: data.ar || data.Ar,
      en: data.en || data.En
    })
  } catch {
    content.value = null
  } finally {
    loaded.value = true
  }
  return content.value
}

export function useLanding() {
  onMounted(() => {
    applyDir(locale.value)
    ensureContent()
  })

  const c = computed(() => {
    if (!content.value) return null
    return locale.value === 'en' ? content.value.en : content.value.ar
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
