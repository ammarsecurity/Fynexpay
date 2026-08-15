<template>
  <div class="nx-page" v-if="order">
    <header class="nx-page-head">
      <h1>{{ t('demoChoose') }}</h1>
      <p>{{ t('demoOrder') }} {{ order.id }} · {{ money(order.total) }} {{ t('demoIq') }}</p>
    </header>

    <div class="nx-pay">
      <div class="nx-methods">
        <p v-if="!loaded" class="hint">{{ t('demoProcessing') }}</p>
        <p v-else-if="!methods.length" class="hint">{{ t('demoNoMethods') }}</p>
        <button
          v-for="m in methods"
          :key="m.id"
          class="nx-method"
          :class="{ on: method === m.id }"
          type="button"
          @click="method = m.id"
        >
          <img :src="m.logo" :alt="m.label" />
          <span>{{ m.label }}</span>
          <i class="bi bi-check-circle-fill" v-if="method === m.id"></i>
        </button>
      </div>

      <aside class="nx-sum">
        <h2>FynexPay</h2>
        <p class="hint">{{ t('demoPaySecure') }}</p>
        <div class="total"><span>{{ t('demoTotal') }}</span><b>{{ money(order.total) }} {{ t('demoIq') }}</b></div>
        <p v-if="busy" class="hint">{{ t('demoProcessing') }}</p>
        <button class="nx-btn full" type="button" :disabled="!method || busy" @click="finish('paid')">
          {{ t('demoSimOk') }}
        </button>
        <button class="nx-ghost" type="button" :disabled="!method || busy" @click="finish('failed')">
          {{ t('demoSimFail') }}
        </button>
      </aside>
    </div>
  </div>
  <div v-else class="nx-empty">
    <RouterLink class="nx-btn" to="/demo">{{ t('demoShop') }}</RouterLink>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const SYSTEM_KEYS = new Set(['fib', 'zaincash', 'qi', 'superqi', 'alqaseh'])
const DEFAULT_LOGOS = {
  fib: '/providers/fib.svg',
  zaincash: '/providers/zaincash.svg',
  qi: '/providers/qi.svg',
  superqi: '/providers/superqi.svg',
  alqaseh: '/providers/alqaseh.svg'
}

const { t, apiUrl } = useSiteCopy()
const { getOrder, setStatus, clearCart, money } = useDemoStore()
const route = useRoute()
const router = useRouter()
const order = ref(getOrder(route.params.id))
const method = ref(order.value?.method || '')
const methods = ref([])
const loaded = ref(false)
const busy = ref(false)

function logoOf(item) {
  const key = String(item.key || '').toLowerCase()
  const path = item.logoUrl || DEFAULT_LOGOS[key] || ''
  if (!path) return DEFAULT_LOGOS[key] || ''
  if (/^https?:\/\//i.test(path)) return path
  if (path.startsWith('/providers/')) return path
  return `${apiUrl}${path.startsWith('/') ? '' : '/'}${path}`
}

async function loadMethods() {
  try {
    const res = await fetch(`${apiUrl}/api/providers/catalog`)
    const data = res.ok ? await res.json() : []
    methods.value = (Array.isArray(data) ? data : [])
      .filter((item) => item?.enabled && SYSTEM_KEYS.has(String(item.key || '').toLowerCase()))
      .sort((a, b) => (a.priority ?? 0) - (b.priority ?? 0))
      .map((item) => ({
        id: String(item.key),
        label: item.name || item.key,
        logo: logoOf(item)
      }))
    if (!methods.value.some((m) => m.id === method.value)) {
      method.value = methods.value[0]?.id || ''
    }
  } catch {
    methods.value = []
  } finally {
    loaded.value = true
  }
}

onMounted(loadMethods)

function finish(status) {
  if (!method.value || !order.value) return
  busy.value = true
  order.value.method = method.value
  setTimeout(() => {
    setStatus(order.value.id, status)
    if (status === 'paid') clearCart()
    router.replace(status === 'paid' ? `/demo/success/${order.value.id}` : `/demo/failed/${order.value.id}`)
  }, 700)
}
</script>
