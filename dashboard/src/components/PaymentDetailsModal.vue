<template>
  <div class="modal-root" v-if="open" @keydown.esc="close">
    <div class="modal-backdrop" @click="close"></div>
    <div class="modal-panel" role="dialog" aria-modal="true">
      <div class="modal-head">
        <div>
          <h2>{{ $t('payments.detailsTitle') }}</h2>
          <p class="muted mono">{{ payment?.id }}</p>
        </div>
        <button class="btn secondary" type="button" @click="close">{{ $t('common.cancel') }}</button>
      </div>

      <div v-if="loading" class="muted">{{ $t('common.loading') }}</div>
      <div v-else-if="error" class="error">{{ error }}</div>
      <div v-else-if="payment" class="modal-body">
        <div class="detail-grid">
          <div class="detail-card">
            <h3>{{ $t('payments.sectionOverview') }}</h3>
            <dl>
              <div><dt>{{ $t('payments.id') }}</dt><dd class="mono">{{ payment.id }}</dd></div>
              <div><dt>{{ $t('payments.order') }}</dt><dd class="mono">{{ payment.orderId || '—' }}</dd></div>
              <div><dt>{{ $t('common.status') }}</dt>
                <dd><span class="badge" :class="statusClass(payment.status)">{{ $t(`status.${payment.status}`, payment.status) }}</span></dd>
              </div>
              <div><dt>{{ $t('payments.mode') }}</dt>
                <dd>
                  <span class="badge" :class="payment.isTest ? 'test' : 'live'">
                    {{ payment.isTest ? $t('payments.modeTest') : $t('payments.modeLive') }}
                  </span>
                </dd>
              </div>
              <div><dt>{{ $t('common.provider') }}</dt><dd><ProviderBadge :provider="payment.provider" /></dd></div>
              <div v-if="payment.merchantName"><dt>{{ $t('payments.merchant') }}</dt><dd>{{ payment.merchantName }}</dd></div>
              <div v-if="payment.merchantId"><dt>{{ $t('payments.merchantId') }}</dt><dd class="mono">{{ payment.merchantId }}</dd></div>
              <div><dt>{{ $t('payments.description') }}</dt><dd>{{ payment.description || '—' }}</dd></div>
              <div><dt>{{ $t('payments.providerPaymentId') }}</dt><dd class="mono">{{ payment.providerPaymentId || '—' }}</dd></div>
              <div><dt>{{ $t('payments.idempotency') }}</dt><dd class="mono">{{ payment.idempotencyKey || '—' }}</dd></div>
              <div><dt>{{ $t('payments.ledgerApplied') }}</dt><dd>{{ payment.ledgerApplied ? $t('common.enabled') : $t('common.disabled') }}</dd></div>
            </dl>
          </div>

          <div class="detail-card">
            <h3>{{ $t('payments.sectionMoney') }}</h3>
            <dl>
              <div><dt>{{ $t('common.amount') }}</dt><dd class="strong">{{ money(payment.amount) }}</dd></div>
              <div><dt>{{ $t('payments.fee') }}</dt><dd>{{ money(payment.platformFee) }}</dd></div>
              <div><dt>{{ $t('payments.net') }}</dt><dd>{{ money(payment.netAmount) }}</dd></div>
              <div><dt>{{ $t('payments.currency') }}</dt><dd>{{ payment.currency }}</dd></div>
            </dl>
          </div>

          <div class="detail-card">
            <h3>{{ $t('payments.sectionDates') }}</h3>
            <dl>
              <div><dt>{{ $t('payments.createdAt') }}</dt><dd>{{ when(payment.createdAtUtc) }}</dd></div>
              <div><dt>{{ $t('payments.updatedAt') }}</dt><dd>{{ when(payment.updatedAtUtc) }}</dd></div>
              <div><dt>{{ $t('payments.paidAt') }}</dt><dd>{{ when(payment.paidAtUtc) }}</dd></div>
              <div><dt>{{ $t('payments.expiredAt') }}</dt><dd>{{ when(payment.expiredAtUtc) }}</dd></div>
            </dl>
          </div>
        </div>

        <div class="detail-card">
          <h3>{{ $t('payments.sectionLinks') }}</h3>
          <div class="link-list">
            <div class="link-row" v-for="item in links" :key="item.label">
              <div>
                <strong>{{ item.label }}</strong>
                <a v-if="item.url" class="mono link" :href="item.url" target="_blank" rel="noopener">{{ item.url }}</a>
                <span v-else class="muted">—</span>
              </div>
              <button v-if="item.url" class="btn secondary" type="button" @click="copy(item.url)">{{ $t('payments.copy') }}</button>
            </div>
          </div>
        </div>

        <div class="detail-card" v-if="payment.qrCode || payment.readableCode">
          <h3>{{ $t('payments.sectionCodes') }}</h3>
          <dl>
            <div v-if="payment.readableCode"><dt>{{ $t('payments.readableCode') }}</dt><dd class="mono">{{ payment.readableCode }}</dd></div>
            <div v-if="payment.qrCode"><dt>{{ $t('payments.qrCode') }}</dt><dd class="mono wrap">{{ payment.qrCode }}</dd></div>
          </dl>
        </div>

        <div class="detail-card" v-if="payment.failureReason">
          <h3>{{ $t('payments.failureReason') }}</h3>
          <p class="error" style="margin:0">{{ payment.failureReason }}</p>
        </div>

        <div class="detail-card" v-if="payment.availableProviders?.length">
          <h3>{{ $t('payments.availableProviders') }}</h3>
          <div class="chips">
            <ProviderBadge v-for="p in payment.availableProviders" :key="p" :provider="p" />
          </div>
        </div>

        <div class="detail-card" v-if="payment.providerRawResponse">
          <h3>{{ $t('payments.providerRaw') }}</h3>
          <pre class="raw">{{ pretty(payment.providerRawResponse) }}</pre>
        </div>

        <div class="detail-card">
          <h3>{{ $t('payments.events') }} ({{ payment.events?.length || 0 }})</h3>
          <div v-if="payment.events?.length" class="events">
            <article v-for="e in payment.events" :key="e.id" class="event">
              <div class="event-top">
                <strong>{{ e.eventType }}</strong>
                <span class="badge">{{ e.source }}</span>
                <span class="muted">{{ when(e.createdAtUtc) }}</span>
              </div>
              <pre class="raw">{{ pretty(e.payload) }}</pre>
            </article>
          </div>
          <p v-else class="muted">{{ $t('payments.noEvents') }}</p>
        </div>

        <div class="detail-card">
          <h3>{{ $t('payments.rawJson') }}</h3>
          <pre class="raw">{{ pretty(payment) }}</pre>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../api'
import ProviderBadge from './ProviderBadge.vue'

const props = defineProps({
  open: Boolean,
  paymentId: { type: String, default: '' },
  endpoint: { type: String, required: true }
})
const emit = defineEmits(['close'])

const { t, locale } = useI18n()
const payment = ref(null)
const loading = ref(false)
const error = ref('')

const links = computed(() => {
  const p = payment.value
  if (!p) return []
  return [
    { label: t('payments.linkCheckout'), url: p.checkoutUrl },
    { label: t('payments.linkProvider'), url: p.providerCheckoutUrl },
    { label: t('payments.linkReturn'), url: p.platformReturnUrl },
    { label: t('payments.linkSuccess'), url: p.successUrl },
    { label: t('payments.linkFailure'), url: p.failureUrl },
    { label: t('payments.linkCallback'), url: p.callbackUrl }
  ]
})

function close() { emit('close') }

function money(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function pretty(v) {
  if (v == null) return ''
  if (typeof v === 'string') {
    try { return JSON.stringify(JSON.parse(v), null, 2) } catch { return v }
  }
  try { return JSON.stringify(v, null, 2) } catch { return String(v) }
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}

watch(
  () => [props.open, props.paymentId],
  async ([open, id]) => {
    if (!open || !id) return
    loading.value = true
    error.value = ''
    payment.value = null
    try {
      const { data } = await api.get(`${props.endpoint}/${id}`)
      payment.value = data
    } catch (e) {
      error.value = e.response?.data?.message || t('payments.detailsFail')
    } finally {
      loading.value = false
    }
  },
  { immediate: true }
)
</script>

<style scoped>
.modal-root {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: grid;
  place-items: center;
  padding: 24px;
}
.modal-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(3, 24, 56, 0.45);
  backdrop-filter: blur(4px);
}
.modal-panel {
  position: relative;
  width: min(920px, 100%);
  max-height: min(90vh, 920px);
  overflow: auto;
  background: #fff;
  border-radius: 20px;
  border: 1px solid var(--line);
  box-shadow: 0 24px 60px rgba(3, 24, 56, 0.22);
  padding: 22px;
}
.modal-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 16px;
  position: sticky;
  top: 0;
  background: #fff;
  z-index: 1;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--line);
}
.modal-head h2 { margin: 0 0 4px; }
.modal-head .muted { margin: 0; font-size: 0.82rem; }
.detail-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
  margin-bottom: 12px;
}
.detail-card {
  border: 1px solid var(--line);
  border-radius: 16px;
  padding: 14px;
  margin-bottom: 12px;
  background: #fafbff;
}
.detail-card h3 {
  margin: 0 0 12px;
  font-size: 0.95rem;
  color: var(--brand);
}
dl { margin: 0; display: grid; gap: 10px; }
dl > div {
  display: grid;
  gap: 2px;
}
dt {
  color: var(--muted);
  font-size: 0.78rem;
  font-weight: 700;
}
dd { margin: 0; font-weight: 600; word-break: break-word; }
dd.strong { color: var(--brand-secondary); font-size: 1.05rem; }
.link-list { display: grid; gap: 10px; }
.link-row {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  padding: 10px 12px;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 12px;
}
.link { display: block; margin-top: 4px; color: var(--brand-secondary); font-size: 0.82rem; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; }
.raw {
  margin: 0;
  background: #031838;
  color: #e2e8f0;
  border-radius: 12px;
  padding: 12px;
  overflow: auto;
  font-size: 0.78rem;
  direction: ltr;
  text-align: left;
  max-height: 260px;
}
.events { display: grid; gap: 10px; }
.event {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 10px;
}
.event-top {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  align-items: center;
  margin-bottom: 8px;
}
.wrap { white-space: pre-wrap; word-break: break-all; }
@media (max-width: 900px) {
  .detail-grid { grid-template-columns: 1fr; }
  .link-row { flex-direction: column; }
}
</style>
