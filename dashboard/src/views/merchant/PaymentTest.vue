<template>
  <div class="tester">
    <header class="hero">
      <div class="hero-text">
        <span class="sandbox-pill">{{ $t('testPay.sandboxBadge') }}</span>
        <h1>{{ $t('testPay.title') }}</h1>
        <p>{{ $t('testPay.subtitle') }}</p>
        <p class="sandbox-hint">{{ $t('testPay.sandboxHint') }}</p>
      </div>
      <div class="hero-actions">
        <RouterLink class="btn secondary" to="/merchant/platforms">{{ $t('nav.platforms') }}</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/docs">{{ $t('nav.docs') }}</RouterLink>
      </div>
      <ol v-if="approvedPlatforms.length" class="flow" aria-label="payment flow">
        <li><span>1</span>{{ $t('testPay.flowCreate') }}</li>
        <li><span>2</span>{{ $t('testPay.flowCheckout') }}</li>
        <li><span>3</span>{{ $t('testPay.flowPay') }}</li>
        <li><span>4</span>{{ $t('testPay.flowDone') }}</li>
      </ol>
    </header>

    <section v-if="!approvedPlatforms.length" class="card empty-platform">
      <div>
        <h2>{{ $t('testPay.needPlatformTitle') }}</h2>
        <p class="muted">{{ $t('testPay.needPlatformBody') }}</p>
      </div>
      <RouterLink class="btn" to="/merchant/platforms">{{ $t('testPay.addPlatform') }}</RouterLink>
    </section>

    <template v-else>
      <div class="workspace">
        <section class="panel form-panel">
          <div class="panel-head">
            <h2>{{ $t('testPay.formTitle') }}</h2>
            <span v-if="merchantStatus" class="badge" :class="merchantStatus === 'Active' ? 'ok' : 'warn'">
              {{ $t(`status.${merchantStatus}`, merchantStatus) }}
            </span>
          </div>

          <p v-if="merchantStatus && merchantStatus !== 'Active'" class="warn-box">
            {{ $t('testPay.merchantInactive') }}
          </p>

          <label class="field">
            <span>{{ $t('testPay.platform') }}</span>
            <select v-model="form.platformId" @change="onPlatformChange">
              <option disabled value="">{{ $t('testPay.platformPh') }}</option>
              <option v-for="p in approvedPlatforms" :key="p.id" :value="p.id">
                {{ p.name }} — {{ p.domain }}
              </option>
            </select>
          </label>

          <div v-if="selectedPlatform" class="platform-chip">
            <img
              v-if="selectedPlatform.logoUrl"
              class="plat-logo"
              :src="logoSrc(selectedPlatform.logoUrl)"
              alt=""
              width="36"
              height="36"
            />
            <div class="plat-meta">
              <strong>{{ selectedPlatform.name }}</strong>
              <span class="mono" dir="ltr">{{ selectedPlatform.domain }}</span>
            </div>
            <span v-if="selectedPlatform.testApiKeyPrefix || selectedPlatform.apiKeyPrefix" class="mono key-pill" dir="ltr">
              {{ selectedPlatform.testApiKeyPrefix || selectedPlatform.apiKeyPrefix }}••••
            </span>
          </div>

          <div class="field-grid">
            <label class="field">
              <span>{{ $t('testPay.amount') }}</span>
              <input v-model.number="form.amount" type="number" min="250" step="250" />
            </label>
            <label class="field">
              <span>{{ $t('testPay.serviceType') }}</span>
              <input v-model="form.serviceType" :placeholder="$t('testPay.servicePh')" />
            </label>
          </div>

          <label class="field">
            <span>{{ $t('testPay.orderId') }}</span>
            <input v-model="form.orderId" :placeholder="$t('testPay.orderPh')" dir="ltr" />
          </label>

          <button class="urls-toggle" type="button" @click="showUrls = !showUrls">
            <span>{{ $t('testPay.urlsTitle') }}</span>
            <em>{{ showUrls ? '−' : '+' }}</em>
          </button>
          <div v-show="showUrls" class="urls-block">
            <label class="field">
              <span>callbackUrl</span>
              <input v-model="form.callbackUrl" dir="ltr" :placeholder="urlPh('/hooks/fynexpay')" />
            </label>
            <label class="field">
              <span>successUrl</span>
              <input v-model="form.successUrl" dir="ltr" :placeholder="urlPh('/success')" />
            </label>
            <label class="field">
              <span>failureUrl</span>
              <input v-model="form.failureUrl" dir="ltr" :placeholder="urlPh('/failed')" />
            </label>
          </div>

          <p class="muted tip">
            {{ $t('testPay.methodsHint') }}
            <RouterLink to="/merchant/payment-methods">{{ $t('nav.paymentMethods') }}</RouterLink>
          </p>

          <p v-if="error" class="error">{{ error }}</p>
          <button
            class="btn full"
            type="button"
            :disabled="loading || merchantStatus !== 'Active' || !form.platformId"
            @click="createPayment"
          >
            {{ loading ? $t('common.loading') : $t('testPay.create') }}
          </button>
        </section>

        <section class="panel preview-panel">
          <div class="panel-head">
            <div>
              <h2>{{ payment ? $t('testPay.resultTitle') : $t('testPay.previewTitle') }}</h2>
              <p class="muted tiny">{{ payment ? '' : $t('testPay.previewLive') }}</p>
            </div>
            <span v-if="payment" class="badge" :class="statusClass(payment.status)">
              {{ $t(`status.${payment.status}`, payment.status) }}
            </span>
          </div>

          <div class="receipt">
            <div class="receipt-hero">
              <span>{{ $t('common.amount') }}</span>
              <strong>{{ format(payment ? payment.amount : amount) }}</strong>
              <small>{{ payment?.description || serviceType }}</small>
            </div>
            <div class="receipt-rows">
              <div>
                <span>{{ $t('testPay.previewPlatform') }}</span>
                <strong>{{ selectedPlatform?.name || '—' }}</strong>
              </div>
              <div>
                <span>{{ $t('testPay.previewDomain') }}</span>
                <strong class="mono" dir="ltr">{{ selectedPlatform?.domain || '—' }}</strong>
              </div>
              <div>
                <span>{{ $t('testPay.previewOrder') }}</span>
                <strong class="mono" dir="ltr">{{ payment?.orderId || orderId }}</strong>
              </div>
              <div v-if="payment">
                <span>{{ $t('testPay.paymentId') }}</span>
                <strong class="mono" dir="ltr">{{ payment.id }}</strong>
              </div>
              <div v-if="payment">
                <span>{{ $t('common.provider') }}</span>
                <strong>{{ payment.provider }}</strong>
              </div>
              <div v-if="payment">
                <span>{{ $t('testPay.net') }}</span>
                <strong>{{ format(payment.netAmount) }}</strong>
              </div>
            </div>

            <div v-if="payment?.availableProviders?.length" class="chips">
              <span class="muted">{{ $t('testPay.available') }}</span>
              <span v-for="p in payment.availableProviders" :key="p" class="badge">{{ p }}</span>
            </div>

            <div v-if="payment" class="actions">
              <a
                v-if="payment.checkoutUrl"
                class="btn"
                :href="payment.checkoutUrl"
                target="_blank"
                rel="noopener"
              >{{ $t('testPay.openCheckout') }}</a>
              <button class="btn secondary" type="button" @click="refreshStatus">{{ $t('testPay.refresh') }}</button>
            </div>

            <div v-else class="preview-hint">
              <p>{{ $t('testPay.waitBody') }}</p>
            </div>

            <div class="success-box" v-if="payment?.status === 'Paid'">
              {{ $t('testPay.paidMsg', { amount: format(payment.netAmount) }) }}
            </div>
          </div>
        </section>
      </div>

      <section class="code-shell">
        <aside class="code-side">
          <h2>{{ $t('testPay.codeTitle') }}</h2>
          <p class="muted">{{ $t('testPay.codeSub') }}</p>
          <ul class="flow-mini">
            <li>{{ $t('testPay.codeFlow') }}</li>
            <li>{{ $t('testPay.codeKeyHint') }}</li>
          </ul>
          <div class="tabs" role="tablist">
            <button
              v-for="tab in tabs"
              :key="tab.id"
              type="button"
              class="tab"
              :class="{ active: codeTab === tab.id }"
              @click="codeTab = tab.id"
            >{{ tab.label }}</button>
          </div>
          <button class="btn full" type="button" :disabled="!activeCode" @click="copyCode">
            {{ copied ? $t('testPay.copied') : $t('testPay.copyCode') }}
          </button>
        </aside>
        <div class="code-main">
          <div class="code-bar">
            <span class="mono" dir="ltr">{{ codeTabLabel }}</span>
            <span class="mono dim" dir="ltr">{{ API_BASE }}/v1/payments</span>
          </div>
          <pre v-if="activeCode" class="code mono" dir="ltr">{{ activeCode }}</pre>
          <p v-else class="muted pad">{{ $t('testPay.codeNeedPlatform') }}</p>
        </div>
      </section>
    </template>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../../api'

const { t, locale } = useI18n()

const form = reactive({
  platformId: '',
  amount: 5000,
  serviceType: '',
  orderId: '',
  callbackUrl: '',
  successUrl: '',
  failureUrl: ''
})

const platforms = ref([])
const payment = ref(null)
const loading = ref(false)
const error = ref('')
const merchantStatus = ref('')
const codeTab = ref('curl')
const copied = ref(false)
const showUrls = ref(false)
let pollTimer = null
let copyTimer = null

const approvedPlatforms = computed(() => (platforms.value || []).filter(p => p.status === 'Approved'))
const selectedPlatform = computed(() => approvedPlatforms.value.find(p => p.id === form.platformId) || null)

const tabs = computed(() => [
  { id: 'curl', label: t('testPay.codeTabCurl') },
  { id: 'node', label: t('testPay.codeTabNode') },
  { id: 'php', label: t('testPay.codeTabPhp') },
  { id: 'status', label: t('testPay.codeTabStatus') }
])
const codeTabLabel = computed(() => tabs.value.find(x => x.id === codeTab.value)?.label || 'cURL')

const domain = computed(() => selectedPlatform.value?.domain || 'shop.example.com')
const origin = computed(() => {
  const d = domain.value
  return d.startsWith('localhost') || d.startsWith('127.0.0.1') ? `http://${d}` : `https://${d}`
})
const apiKeyPlaceholder = computed(() => {
  const prefix = selectedPlatform.value?.testApiKeyPrefix || selectedPlatform.value?.apiKeyPrefix
  return prefix ? `${prefix}…YOUR_FULL_KEY` : 'fx_test_YOUR_FULL_KEY'
})
const amount = computed(() => Number(form.amount) > 0 ? Number(form.amount) : 5000)
const serviceType = computed(() => (form.serviceType || t('testPay.serviceDefault')).trim())
const orderId = computed(() => (form.orderId || '').trim() || 'ORD-1001')
const callbackUrl = computed(() => (form.callbackUrl || `${origin.value}/hooks/fynexpay`).trim())
const successUrl = computed(() => (form.successUrl || `${origin.value}/success`).trim())
const failureUrl = computed(() => (form.failureUrl || `${origin.value}/fail`).trim())
const paymentIdSample = computed(() => payment.value?.id || 'PAYMENT_ID')
const idempotencyKey = computed(() => `order-${orderId.value}`)

const bodyJson = computed(() => ({
  amount: amount.value,
  serviceType: serviceType.value,
  orderId: orderId.value,
  callbackUrl: callbackUrl.value,
  successUrl: successUrl.value,
  failureUrl: failureUrl.value
}))

const bodyPretty = computed(() => JSON.stringify(bodyJson.value, null, 2))

const curlCode = computed(() => {
  if (!selectedPlatform.value) return ''
  const escaped = bodyPretty.value.replace(/'/g, "'\\''")
  return `curl -X POST ${API_BASE}/v1/payments \\
  -H "Authorization: Bearer YOUR_MERCHANT_BEARER" \\
  -H "X-Api-Key: ${apiKeyPlaceholder.value}" \\
  -H "Content-Type: application/json" \\
  -H "X-Idempotency-Key: ${idempotencyKey.value}" \\
  -H "Origin: ${origin.value}" \\
  -d '${escaped}'`
})

const nodeCode = computed(() => {
  if (!selectedPlatform.value) return ''
  return `const res = await fetch("${API_BASE}/v1/payments", {
  method: "POST",
  headers: {
    "Authorization": "Bearer YOUR_MERCHANT_BEARER",
    "X-Api-Key": "${apiKeyPlaceholder.value}",
    "Content-Type": "application/json",
    "X-Idempotency-Key": "${idempotencyKey.value}",
    "Origin": "${origin.value}"
  },
  body: JSON.stringify(${bodyPretty.value.replace(/\n/g, '\n  ')})
});

const payment = await res.json();
// Redirect customer:
// window.location = payment.checkoutUrl;
console.log(payment.id, payment.checkoutUrl, payment.status);`
})

const phpCode = computed(() => {
  if (!selectedPlatform.value) return ''
  const payload = bodyPretty.value.replace(/\$/g, '\\$')
  return `<?php
$payload = <<<'JSON'
${payload}
JSON;

$ch = curl_init('${API_BASE}/v1/payments');
curl_setopt_array($ch, [
  CURLOPT_POST => true,
  CURLOPT_RETURNTRANSFER => true,
  CURLOPT_HTTPHEADER => [
    'Authorization: Bearer YOUR_MERCHANT_BEARER',
    'X-Api-Key: ${apiKeyPlaceholder.value}',
    'Content-Type: application/json',
    'X-Idempotency-Key: ${idempotencyKey.value}',
    'Origin: ${origin.value}',
  ],
  CURLOPT_POSTFIELDS => $payload,
]);

$response = curl_exec($ch);
curl_close($ch);
$payment = json_decode($response, true);
// Redirect customer to $payment['checkoutUrl'];`
})

const statusCode = computed(() => {
  if (!selectedPlatform.value) return ''
  return `curl ${API_BASE}/v1/payments/${paymentIdSample.value} \\
  -H "Authorization: Bearer YOUR_MERCHANT_BEARER" \\
  -H "X-Api-Key: ${apiKeyPlaceholder.value}" \\
  -H "Origin: ${origin.value}"

# Node
const statusRes = await fetch("${API_BASE}/v1/payments/${paymentIdSample.value}", {
  headers: {
    "Authorization": "Bearer YOUR_MERCHANT_BEARER",
    "X-Api-Key": "${apiKeyPlaceholder.value}",
    "Origin": "${origin.value}"
  }
});
const status = await statusRes.json();
// status.status === "Paid" | "Pending" | ...`
})

const activeCode = computed(() => {
  switch (codeTab.value) {
    case 'node': return nodeCode.value
    case 'php': return phpCode.value
    case 'status': return statusCode.value
    default: return curlCode.value
  }
})

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function logoSrc(url) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}
function urlPh(path) {
  const d = selectedPlatform.value?.domain || 'shop.example.com'
  const proto = d.startsWith('localhost') || d.startsWith('127.0.0.1') ? 'http' : 'https'
  return `${proto}://${d}${path}`
}

function fillUrlsFromPlatform(p) {
  if (!p) return
  const proto = p.domain.startsWith('localhost') || p.domain.startsWith('127.0.0.1') ? 'http' : 'https'
  const base = `${proto}://${p.domain}`
  if (!form.callbackUrl) form.callbackUrl = `${base}/hooks/fynexpay`
  if (!form.successUrl) form.successUrl = `${base}/success`
  if (!form.failureUrl) form.failureUrl = `${base}/failed`
}

function onPlatformChange() {
  const p = selectedPlatform.value
  if (!p) return
  const proto = p.domain.startsWith('localhost') || p.domain.startsWith('127.0.0.1') ? 'http' : 'https'
  const base = `${proto}://${p.domain}`
  form.callbackUrl = `${base}/hooks/fynexpay`
  form.successUrl = `${base}/success`
  form.failureUrl = `${base}/failed`
}

async function copyCode() {
  if (!activeCode.value) return
  try {
    await navigator.clipboard.writeText(activeCode.value)
    copied.value = true
    clearTimeout(copyTimer)
    copyTimer = setTimeout(() => { copied.value = false }, 1800)
  } catch { /* ignore */ }
}

async function load() {
  const [me, plats] = await Promise.all([
    api.get('/api/merchant/me'),
    api.get('/api/merchant/platforms')
  ])
  merchantStatus.value = me.data.status
  platforms.value = plats.data || []
  if (!form.serviceType) form.serviceType = t('testPay.serviceDefault')
  if (approvedPlatforms.value.length && !form.platformId) {
    form.platformId = approvedPlatforms.value[0].id
    fillUrlsFromPlatform(approvedPlatforms.value[0])
  }
}

async function createPayment() {
  error.value = ''
  if (!form.platformId) {
    error.value = t('testPay.needPlatformSelect')
    return
  }
  loading.value = true
  try {
    const { data } = await api.post('/api/merchant/test-payments', {
      amount: form.amount,
      currency: 'IQD',
      orderId: form.orderId || null,
      serviceType: form.serviceType,
      description: form.serviceType,
      callbackUrl: form.callbackUrl || null,
      successUrl: form.successUrl || null,
      failureUrl: form.failureUrl || null,
      merchantPlatformId: form.platformId
    })
    payment.value = data
    startPolling()
  } catch (e) {
    error.value = e.response?.data?.message || t('testPay.createFail')
  } finally {
    loading.value = false
  }
}

async function refreshStatus() {
  if (!payment.value) return
  const { data } = await api.get(`/api/merchant/test-payments/${payment.value.id}`)
  payment.value = data
  if (data.status !== 'Pending' && data.status !== 'PendingSelection') stopPolling()
}

function startPolling() {
  stopPolling()
  pollTimer = setInterval(refreshStatus, 4000)
}
function stopPolling() {
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }
}

onMounted(load)
onUnmounted(() => {
  stopPolling()
  clearTimeout(copyTimer)
})
</script>

<style scoped>
.tester { display: grid; gap: 18px; }

.hero {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 14px 18px;
  padding: 22px 24px;
  border-radius: 22px;
  background:
    radial-gradient(720px 220px at 100% 0%, rgba(3, 24, 56, 0.16), transparent 55%),
    linear-gradient(135deg, #031838 0%, #0a2a5c 55%, #1a1460 100%);
  color: #fff;
  box-shadow: var(--shadow);
}
.hero-text h1 {
  margin: 0;
  font-size: clamp(1.4rem, 2vw, 1.85rem);
  font-weight: 800;
  letter-spacing: -0.02em;
}
.sandbox-pill {
  display: inline-flex;
  margin-bottom: 10px;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.14);
  color: #fff;
  font-size: 0.82rem;
  font-weight: 800;
}
.sandbox-hint {
  margin: 8px 0 0 !important;
  color: rgba(255, 255, 255, 0.88) !important;
  font-weight: 600;
  font-size: 0.9rem !important;
}
.hero-text p {
  margin: 8px 0 0;
  max-width: 52ch;
  color: rgba(255, 255, 255, 0.78);
  line-height: 1.55;
  font-size: 0.92rem;
}
.hero-actions { display: flex; gap: 8px; flex-wrap: wrap; align-items: start; }
.hero-actions .btn.secondary {
  background: rgba(255, 255, 255, 0.1);
  border-color: rgba(255, 255, 255, 0.22);
  color: #fff;
}
.hero-actions .btn.secondary:hover { background: rgba(255, 255, 255, 0.18); }
.flow {
  grid-column: 1 / -1;
  list-style: none;
  margin: 0;
  padding: 12px;
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 8px;
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: 16px;
}
.flow li {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.82rem;
  font-weight: 700;
  color: rgba(255, 255, 255, 0.9);
}
.flow span {
  width: 24px;
  height: 24px;
  border-radius: 999px;
  display: grid;
  place-items: center;
  background: rgba(255, 255, 255, 0.18);
  color: #fff;
  font-size: 0.75rem;
  flex-shrink: 0;
}

.empty-platform {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
}
.empty-platform h2 { margin: 0 0 6px; color: var(--brand); }
.empty-platform .muted { margin: 0; }

.workspace {
  display: grid;
  grid-template-columns: minmax(300px, 420px) 1fr;
  gap: 16px;
  align-items: stretch;
}
.panel {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: 22px;
  padding: 18px;
  box-shadow: var(--shadow-sm);
}
.panel-head {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: flex-start;
  margin-bottom: 14px;
}
.panel-head h2 {
  margin: 0;
  font-size: 1.05rem;
  color: var(--brand);
}
.tiny { margin: 4px 0 0; font-size: 0.78rem; }

.warn-box {
  margin: 0 0 12px;
  padding: 10px 12px;
  border-radius: 12px;
  background: var(--warn-soft);
  color: #92400e;
  font-weight: 700;
  font-size: 0.9rem;
}

.field { display: grid; gap: 6px; margin-bottom: 12px; }
.field > span {
  color: var(--muted);
  font-size: 0.78rem;
  font-weight: 700;
}
.field input, .field select {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 11px 12px;
  outline: none;
  background: #fff;
  color: var(--ink);
}
.field input:focus, .field select:focus {
  border-color: rgba(3, 24, 56, 0.55);
  box-shadow: 0 0 0 4px rgba(3, 24, 56, 0.14);
}
.field-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}

.platform-chip {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 12px;
  margin-bottom: 14px;
  border-radius: 14px;
  background: #f4f6fb;
  border: 1px solid var(--line);
}
.plat-logo {
  width: 36px;
  height: 36px;
  object-fit: contain;
  border-radius: 10px;
  background: #fff;
  border: 1px solid var(--line);
}
.plat-meta { flex: 1; min-width: 0; display: grid; gap: 2px; }
.plat-meta strong { font-size: 0.9rem; color: var(--brand); }
.plat-meta span { font-size: 0.78rem; color: var(--muted); }
.key-pill {
  font-size: 0.72rem;
  font-weight: 700;
  padding: 6px 8px;
  border-radius: 999px;
  background: #031838;
  color: #e2e8f0;
}

.urls-toggle {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
  border: 1px dashed var(--line);
  background: #fafbff;
  border-radius: 12px;
  padding: 10px 12px;
  margin-bottom: 10px;
  font-weight: 700;
  color: var(--brand);
}
.urls-toggle em {
  font-style: normal;
  width: 22px;
  height: 22px;
  border-radius: 999px;
  background: var(--brand-soft);
  display: grid;
  place-items: center;
  color: var(--brand-secondary);
}
.urls-block {
  padding: 10px 12px 2px;
  margin-bottom: 8px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid var(--line);
}
.tip { font-size: 0.84rem; margin: 4px 0 14px; }
.btn.full { width: 100%; justify-content: center; }

.preview-panel {
  background:
    radial-gradient(500px 180px at 100% 0%, rgba(3, 24, 56, 0.08), transparent 55%),
    #fff;
  display: flex;
  flex-direction: column;
}
.receipt {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 14px;
}
.receipt-hero {
  padding: 22px 20px;
  border-radius: 18px;
  background: linear-gradient(145deg, #031838, #05204a 70%, #031838);
  color: #fff;
  display: grid;
  gap: 4px;
}
.receipt-hero span {
  font-size: 0.78rem;
  font-weight: 700;
  opacity: 0.75;
}
.receipt-hero strong {
  font-size: clamp(1.6rem, 3vw, 2.1rem);
  font-weight: 800;
  letter-spacing: -0.02em;
}
.receipt-hero small {
  opacity: 0.8;
  font-size: 0.88rem;
}
.receipt-rows {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
}
.receipt-rows > div {
  padding: 12px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid var(--line);
  display: grid;
  gap: 4px;
  min-width: 0;
}
.receipt-rows span {
  color: var(--muted);
  font-size: 0.72rem;
  font-weight: 700;
}
.receipt-rows strong {
  color: var(--brand);
  font-size: 0.88rem;
  word-break: break-word;
}
.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
}
.actions { display: flex; flex-wrap: wrap; gap: 10px; }
.preview-hint {
  margin-top: auto;
  padding: 14px 16px;
  border-radius: 14px;
  background: var(--brand-soft);
  color: var(--muted);
  font-weight: 600;
  font-size: 0.9rem;
  line-height: 1.55;
}
.preview-hint p { margin: 0; }
.success-box {
  padding: 14px;
  border-radius: 14px;
  background: var(--ok-soft);
  border: 1px solid rgba(16, 185, 129, 0.25);
  font-weight: 700;
  color: #047857;
}

.code-shell {
  display: grid;
  grid-template-columns: minmax(220px, 280px) 1fr;
  gap: 0;
  border: 1px solid var(--line);
  border-radius: 22px;
  overflow: hidden;
  background: #fff;
  box-shadow: var(--shadow-sm);
  min-height: 360px;
}
.code-side {
  padding: 18px 16px;
  background: #f4f6fb;
  border-inline-end: 1px solid var(--line);
  display: flex;
  flex-direction: column;
  gap: 10px;
}
.code-side h2 {
  margin: 0;
  font-size: 1.02rem;
  color: var(--brand);
}
.code-side .muted {
  margin: 0;
  font-size: 0.82rem;
  line-height: 1.5;
}
.flow-mini {
  margin: 0;
  padding-inline-start: 16px;
  color: var(--muted);
  font-size: 0.8rem;
  font-weight: 600;
  line-height: 1.65;
}
.tabs {
  display: grid;
  gap: 6px;
  margin-top: 4px;
}
.tab {
  border: 1px solid var(--line);
  background: #fff;
  color: var(--muted);
  border-radius: 12px;
  padding: 9px 12px;
  font: inherit;
  font-weight: 700;
  font-size: 0.82rem;
  cursor: pointer;
  text-align: start;
}
.tab.active {
  background: #031838;
  border-color: #031838;
  color: #fff;
}
.code-side .btn { margin-top: auto; }

.code-main {
  display: flex;
  flex-direction: column;
  min-width: 0;
  background: #021225;
}
.code-bar {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  flex-wrap: wrap;
  padding: 10px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  color: #94a3b8;
  font-size: 0.75rem;
  font-weight: 700;
}
.code-bar .dim { opacity: 0.65; }
.code {
  margin: 0;
  flex: 1;
  padding: 16px 18px;
  overflow: auto;
  color: #e2e8f0;
  font-size: 0.82rem;
  line-height: 1.6;
  white-space: pre-wrap;
  max-height: none;
  min-height: 280px;
}
.pad { padding: 18px; color: #94a3b8; }

@media (max-width: 980px) {
  .workspace, .code-shell { grid-template-columns: 1fr; }
  .code-side { border-inline-end: 0; border-bottom: 1px solid var(--line); }
  .flow { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 640px) {
  .hero { grid-template-columns: 1fr; }
  .field-grid, .receipt-rows, .flow { grid-template-columns: 1fr; }
}
</style>
