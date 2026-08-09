<template>
  <div class="tester">
    <div class="page-head">
      <div>
        <h1>{{ $t('testPay.title') }}</h1>
        <p class="sub">{{ $t('testPay.subtitle') }}</p>
      </div>
      <div class="row">
        <RouterLink class="btn secondary" to="/merchant/platforms">{{ $t('nav.platforms') }}</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/docs">{{ $t('nav.docs') }}</RouterLink>
      </div>
    </div>

    <section v-if="!approvedPlatforms.length" class="card empty-platform">
      <div>
        <h2>{{ $t('testPay.needPlatformTitle') }}</h2>
        <p class="muted">{{ $t('testPay.needPlatformBody') }}</p>
      </div>
      <RouterLink class="btn" to="/merchant/platforms">{{ $t('testPay.addPlatform') }}</RouterLink>
    </section>

    <div v-else class="layout">
      <section class="card form-card">
        <div class="card-head">
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

        <div v-if="selectedPlatform" class="platform-meta">
          <div>
            <span class="muted">{{ $t('platforms.domain') }}</span>
            <strong class="mono" dir="ltr">{{ selectedPlatform.domain }}</strong>
          </div>
          <div v-if="selectedPlatform.apiKeyPrefix">
            <span class="muted">{{ $t('testPay.keyPrefix') }}</span>
            <strong class="mono" dir="ltr">{{ selectedPlatform.apiKeyPrefix }}••••</strong>
          </div>
        </div>

        <label class="field">
          <span>{{ $t('testPay.amount') }}</span>
          <input v-model.number="form.amount" type="number" min="250" step="250" />
        </label>
        <label class="field">
          <span>{{ $t('testPay.serviceType') }}</span>
          <input v-model="form.serviceType" :placeholder="$t('testPay.servicePh')" />
        </label>
        <label class="field">
          <span>{{ $t('testPay.orderId') }}</span>
          <input v-model="form.orderId" :placeholder="$t('testPay.orderPh')" />
        </label>
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
          <input v-model="form.failureUrl" dir="ltr" :placeholder="urlPh('/fail')" />
        </label>

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

      <section class="card result-card" v-if="payment">
        <div class="result-head">
          <div>
            <h2>{{ $t('testPay.resultTitle') }}</h2>
            <p v-if="payment.merchantPlatformId" class="muted mono" dir="ltr">
              platform: {{ payment.merchantPlatformId }}
            </p>
          </div>
          <span class="badge" :class="statusClass(payment.status)">
            {{ $t(`status.${payment.status}`, payment.status) }}
          </span>
        </div>

        <div class="meta">
          <div><span>{{ $t('testPay.paymentId') }}</span><strong class="mono">{{ payment.id }}</strong></div>
          <div><span>{{ $t('testPay.order') }}</span><strong>{{ payment.orderId }}</strong></div>
          <div><span>{{ $t('testPay.service') }}</span><strong>{{ payment.description }}</strong></div>
          <div><span>{{ $t('common.provider') }}</span><strong>{{ payment.provider }}</strong></div>
          <div><span>{{ $t('common.amount') }}</span><strong>{{ format(payment.amount) }}</strong></div>
          <div><span>{{ $t('testPay.net') }}</span><strong>{{ format(payment.netAmount) }}</strong></div>
        </div>

        <div v-if="payment.availableProviders?.length" class="chips">
          <span class="muted">{{ $t('testPay.available') }}</span>
          <span v-for="p in payment.availableProviders" :key="p" class="badge">{{ p }}</span>
        </div>

        <div class="actions">
          <a
            v-if="payment.checkoutUrl"
            class="btn"
            :href="payment.checkoutUrl"
            target="_blank"
            rel="noopener"
          >{{ $t('testPay.openCheckout') }}</a>
          <button class="btn secondary" type="button" @click="refreshStatus">{{ $t('testPay.refresh') }}</button>
        </div>

        <div class="steps-mini" v-if="payment.status === 'Pending' || payment.status === 'PendingSelection'">
          <h3>{{ $t('testPay.nextTitle') }}</h3>
          <ol>
            <li>{{ $t('testPay.next1') }}</li>
            <li>{{ $t('testPay.next2') }}</li>
            <li>{{ $t('testPay.next3') }}</li>
          </ol>
        </div>

        <div class="success-box" v-if="payment.status === 'Paid'">
          {{ $t('testPay.paidMsg', { amount: format(payment.netAmount) }) }}
          <div class="row" style="margin-top:10px;gap:8px;flex-wrap:wrap">
            <RouterLink class="btn secondary" to="/merchant">{{ $t('nav.overview') }}</RouterLink>
            <RouterLink class="btn secondary" to="/merchant/payments">{{ $t('nav.payments') }}</RouterLink>
          </div>
        </div>
      </section>

      <section class="card result-card empty" v-else>
        <div class="empty-ico" aria-hidden="true">⇢</div>
        <h2>{{ $t('testPay.waitTitle') }}</h2>
        <p class="muted">{{ $t('testPay.waitBody') }}</p>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'

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
let pollTimer = null

const approvedPlatforms = computed(() => (platforms.value || []).filter(p => p.status === 'Approved'))
const selectedPlatform = computed(() => approvedPlatforms.value.find(p => p.id === form.platformId) || null)

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function urlPh(path) {
  const d = selectedPlatform.value?.domain || 'shop.example.com'
  return `https://${d}${path}`
}

function fillUrlsFromPlatform(p) {
  if (!p) return
  const base = `https://${p.domain}`
  if (!form.callbackUrl) form.callbackUrl = `${base}/hooks/fynexpay`
  if (!form.successUrl) form.successUrl = `${base}/success`
  if (!form.failureUrl) form.failureUrl = `${base}/fail`
}

function onPlatformChange() {
  const p = selectedPlatform.value
  if (!p) return
  form.callbackUrl = `https://${p.domain}/hooks/fynexpay`
  form.successUrl = `https://${p.domain}/success`
  form.failureUrl = `https://${p.domain}/fail`
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
onUnmounted(stopPolling)
</script>

<style scoped>
.empty-platform {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
  background:
    radial-gradient(700px 200px at 100% 0%, rgba(108, 60, 236, 0.12), transparent 55%),
    #fff;
}
.empty-platform h2 { margin: 0 0 6px; color: var(--brand); }
.empty-platform .muted { margin: 0; }

.layout {
  display: grid;
  grid-template-columns: minmax(300px, 400px) 1fr;
  gap: 18px;
  align-items: start;
}
.form-card h2, .result-card h2 { margin: 0; color: var(--brand); }
.card-head {
  display: flex;
  justify-content: space-between;
  gap: 10px;
  align-items: center;
  margin-bottom: 14px;
}
.warn-box {
  margin: 0 0 12px;
  padding: 10px 12px;
  border-radius: 12px;
  background: var(--warn-soft);
  color: #92400e;
  font-weight: 700;
  font-size: 0.9rem;
}
.field {
  display: grid;
  gap: 6px;
  margin-bottom: 12px;
}
.field > span {
  color: var(--muted);
  font-size: 0.82rem;
  font-weight: 700;
}
.field input, .field select {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 11px 12px;
  outline: none;
  background: #fff;
  color: var(--ink);
  transition: 0.15s ease;
}
.field input:focus, .field select:focus {
  border-color: rgba(108, 60, 236, 0.55);
  box-shadow: 0 0 0 4px rgba(108, 60, 236, 0.14);
}
.platform-meta {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 10px;
  margin: 0 0 14px;
  padding: 12px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid var(--line);
}
.platform-meta span {
  display: block;
  font-size: 0.75rem;
  margin-bottom: 4px;
}
.platform-meta strong { color: var(--brand); font-size: 0.88rem; }
.tip { font-size: 0.88rem; margin: 4px 0 14px; }
.btn.full { width: 100%; justify-content: center; }

.result-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 8px;
}
.result-head .muted { margin: 4px 0 0; font-size: 0.78rem; }
.meta {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 10px;
  margin: 14px 0;
}
.meta > div {
  padding: 12px;
  border-radius: 14px;
  background: #f8fafc;
  border: 1px solid var(--line);
  display: grid;
  gap: 4px;
}
.meta span {
  color: var(--muted);
  font-size: 0.75rem;
  font-weight: 700;
}
.meta strong {
  color: var(--brand);
  font-size: 0.92rem;
  word-break: break-word;
}
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 14px;
}
.chips {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  margin-bottom: 12px;
}
.steps-mini {
  margin-top: 8px;
  padding: 14px;
  border-radius: 16px;
  background: var(--brand-soft);
}
.steps-mini h3 { margin: 0 0 8px; font-size: 0.95rem; color: var(--brand); }
.steps-mini ol {
  margin: 0;
  padding-inline-start: 18px;
  color: var(--muted);
  font-weight: 600;
  line-height: 1.8;
}
.success-box {
  margin-top: 14px;
  padding: 14px;
  border-radius: 16px;
  background: var(--ok-soft);
  border: 1px solid rgba(16, 185, 129, 0.25);
  font-weight: 700;
  color: #047857;
}
.empty {
  min-height: 320px;
  display: grid;
  place-content: center;
  gap: 8px;
  text-align: center;
  background:
    radial-gradient(500px 180px at 50% 0%, rgba(108, 60, 236, 0.08), transparent 60%),
    #fff;
}
.empty-ico {
  width: 52px;
  height: 52px;
  margin: 0 auto 6px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  background: var(--brand-soft);
  color: var(--brand-secondary);
  font-size: 1.4rem;
  font-weight: 800;
}
.empty h2 { margin: 0; color: var(--brand); }
.empty .muted { margin: 0; max-width: 280px; }
@media (max-width: 900px) {
  .layout { grid-template-columns: 1fr; }
  .platform-meta, .meta { grid-template-columns: 1fr; }
}
</style>
