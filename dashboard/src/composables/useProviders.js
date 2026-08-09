import { computed, onMounted, ref } from 'vue'
import { api, API_BASE } from '../api'

const catalog = ref([])
const loaded = ref(false)
let loadingPromise = null

const DEFAULT_LOGOS = {
  fib: '/providers/fib.svg',
  zaincash: '/providers/zaincash.svg',
  qi: '/providers/qi.svg',
  superqi: '/providers/superqi.svg'
}

export function defaultLogoPath(key) {
  if (!key) return ''
  return DEFAULT_LOGOS[String(key).toLowerCase()] || ''
}

export function mediaUrl(path) {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  // Local dashboard public assets
  if (path.startsWith('/providers/')) return path
  return `${API_BASE}${path.startsWith('/') ? '' : '/'}${path}`
}

async function ensureCatalog() {
  if (loaded.value) return catalog.value
  if (!loadingPromise) {
    loadingPromise = api.get('/api/providers/catalog')
      .then(({ data }) => {
        catalog.value = Array.isArray(data) ? data : []
        loaded.value = true
        return catalog.value
      })
      .catch(() => {
        catalog.value = []
        loaded.value = true
        return catalog.value
      })
      .finally(() => { loadingPromise = null })
  }
  return loadingPromise
}

export function useProviders() {
  onMounted(() => { ensureCatalog() })

  function find(key) {
    if (!key || key === 'Auto' || key === 'PendingSelection') return null
    const k = String(key).toLowerCase()
    return catalog.value.find((p) => p.key.toLowerCase() === k) || null
  }

  function logoOf(key) {
    const item = find(key)
    if (item?.logoUrl) return mediaUrl(item.logoUrl)
    return defaultLogoPath(key)
  }

  function nameOf(key) {
    return find(key)?.name || key || '—'
  }

  function refresh() {
    loaded.value = false
    return ensureCatalog()
  }

  return {
    catalog: computed(() => catalog.value),
    loaded: computed(() => loaded.value),
    find,
    logoOf,
    nameOf,
    mediaUrl,
    defaultLogoPath,
    refresh,
    ensureCatalog
  }
}
