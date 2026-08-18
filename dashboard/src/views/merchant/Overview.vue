<template>
  <div class="overview">
    <div class="page-head">
      <div>
        <h1>{{ $t('merchantOverview.welcome', { name: firstName }) }}</h1>
        <p class="sub">{{ $t('merchantOverview.sub') }}</p>
      </div>
      <div class="head-actions">
        <div class="mode-switch" role="tablist" :aria-label="$t('payments.modeLabel')">
          <button
            type="button"
            role="tab"
            :aria-selected="mode === 'live'"
            :class="{ active: mode === 'live' }"
            @click="setMode('live')"
          >{{ $t('payments.modeLive') }}</button>
          <button
            type="button"
            role="tab"
            :aria-selected="mode === 'test'"
            :class="{ active: mode === 'test', test: true }"
            @click="setMode('test')"
          >{{ $t('payments.modeTest') }}</button>
        </div>
        <span v-if="merchant" class="badge" :class="statusClass(merchant.status)">
          {{ $t(`status.${merchant.status}`, merchant.status) }}
        </span>
        <RouterLink class="btn" to="/merchant/test">
          <i class="bi bi-plus-lg" aria-hidden="true"></i>
          {{ $t('merchantOverview.newPayment') }}
        </RouterLink>
      </div>
    </div>

    <p class="mode-hint muted">{{ mode === 'test' ? $t('payments.modeTestHint') : $t('payments.modeLiveHint') }}</p>

    <p v-if="error" class="error">{{ error }}</p>
    <p v-if="merchant?.status === 'Pending'" class="banner warn">
      {{ $t('merchantOverview.pendingBanner') }}
    </p>

    <div class="grid" v-if="wallet">
      <div class="stat">
        <div class="ico purple"><i class="bi bi-wallet2" aria-hidden="true"></i></div>
        <div class="label">{{ $t('merchantOverview.available') }}</div>
        <div class="value">{{ money(wallet.availableBalance) }}</div>
        <div class="trend">{{ $t('merchantOverview.readyWithdraw') }}</div>
      </div>
      <div class="stat">
        <div class="ico amber"><i class="bi bi-hourglass-split" aria-hidden="true"></i></div>
        <div class="label">{{ $t('merchantOverview.pending') }}</div>
        <div class="value">{{ money(wallet.pendingBalance) }}</div>
        <div class="trend">{{ $t('merchantOverview.settling') }}</div>
      </div>
      <div class="stat">
        <div class="ico green"><i class="bi bi-graph-up-arrow" aria-hidden="true"></i></div>
        <div class="label">{{ $t('merchantOverview.lifetimeGross') }}</div>
        <div class="value">{{ money(overviewGross) }}</div>
        <div class="trend">{{ $t('merchantOverview.paidOps', { n: paidCount }) }}</div>
      </div>
      <div class="stat">
        <div class="ico sky"><i class="bi bi-percent" aria-hidden="true"></i></div>
        <div class="label">{{ $t('merchantOverview.commission') }}</div>
        <div class="value">{{ commissionRange }}</div>
        <div class="trend">{{ $t('merchantOverview.commissionByProvider') }}</div>
      </div>
    </div>

    <div class="card commission-card" v-if="commissionRows.length">
      <div class="card-head">
        <h3>{{ $t('merchantOverview.commissionTitle') }}</h3>
        <span class="muted">{{ $t('merchantOverview.commissionSub') }}</span>
      </div>
      <div class="commission-grid">
        <div v-for="c in commissionRows" :key="c.key" class="commission-item">
          <ProviderBadge :provider="c.provider" :show-name="false" />
          <strong class="mono">{{ formatPct(c.rate) }}%</strong>
        </div>
      </div>
    </div>

    <div class="grid secondary" v-if="wallet">
      <div class="mini-card">
        <span class="mini-label">{{ $t('merchantOverview.lifetimeFees') }}</span>
        <strong class="mono">{{ money(overviewFees) }}</strong>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('merchantOverview.netLifetime') }}</span>
        <strong class="mono">{{ money(overviewNet) }}</strong>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('merchantOverview.successRate') }}</span>
        <strong>{{ successRate }}%</strong>
        <span class="muted mini-hint">{{ $t('merchantOverview.ofPayments', { n: paymentsTotal }) }}</span>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('merchantOverview.platforms') }}</span>
        <strong>{{ approvedPlatforms }}/{{ platforms.length }}</strong>
        <span class="muted mini-hint">{{ $t('merchantOverview.platformsApproved') }}</span>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('merchantOverview.methodsLive') }}</span>
        <strong>{{ effectiveMethods.length }}</strong>
        <div class="chips tiny" v-if="effectiveMethods.length">
          <ProviderBadge v-for="p in effectiveMethods" :key="p" :provider="p" :show-name="false" size="sm" />
        </div>
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-head">
          <div>
            <h3>{{ merchant?.businessName || $t('merchantOverview.storeFallback') }}</h3>
            <p class="muted chart-sub">{{ merchant?.businessNameAr || merchant?.contactEmail }}</p>
          </div>
          <div class="chart-totals">
            <span class="badge" :class="mode === 'test' ? 'test' : 'live'">
              {{ mode === 'test' ? $t('payments.modeTest') : $t('payments.modeLive') }}
            </span>
            <span>{{ $t('merchantOverview.last14Vol') }}: <b class="mono">{{ money(seriesTotal) }}</b></span>
            <span>{{ $t('merchantOverview.last14Count', { n: seriesCount }) }}</span>
          </div>
        </div>
        <VolumeLineChart
          :series="series"
          :empty-text="$t('merchantOverview.noSeries')"
          :height="200"
        />
      </div>

      <div class="card">
        <div class="card-head">
          <div>
            <h3>{{ $t('merchantOverview.readiness') }}</h3>
            <p class="muted chart-sub">{{ $t('merchantOverview.readinessSub', { n: readinessPct }) }}</p>
          </div>
          <strong class="ready-pct">{{ readinessPct }}%</strong>
        </div>
        <div class="progress-list">
          <div class="progress-row" v-for="step in readiness" :key="step.key">
            <div class="top">
              <span>{{ step.label }}</span>
              <span :class="step.done ? 'ok-text' : 'warn-text'">{{ step.done ? $t('merchantOverview.done') : $t('merchantOverview.todo') }}</span>
            </div>
            <div class="bar"><span :style="{ width: step.done ? '100%' : '28%' }"></span></div>
            <p class="hint">{{ step.hint }}</p>
          </div>
        </div>
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-head">
          <h3>{{ $t('merchantOverview.recentPayments') }}</h3>
          <RouterLink class="btn ghost" to="/merchant/payments">{{ $t('common.viewAll') }}</RouterLink>
        </div>
        <div v-if="payments.length" class="recent-list">
          <article class="tx-item" v-for="p in payments.slice(0, 6)" :key="p.id">
            <ProviderBadge :provider="p.provider" :show-name="false" />
            <div class="tx-main">
              <div class="tx-title-row">
                <strong class="mono">{{ p.orderId || shortId(p.id) }}</strong>
                <span class="badge" :class="p.isTest ? 'test' : 'live'">
                  {{ p.isTest ? $t('payments.modeTest') : $t('payments.modeLive') }}
                </span>
                <span class="badge" :class="payStatusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
              </div>
              <div class="tx-meta">
                <span>{{ money(p.amount) }}</span>
                <span>{{ when(p.createdAtUtc) }}</span>
              </div>
            </div>
            <div class="tx-money">
              <div class="amt mono">{{ money(p.amount) }}</div>
              <div class="muted fee">{{ $t('merchantOverview.netShort') }} {{ money(p.netAmount) }}</div>
            </div>
          </article>
        </div>
        <p v-else class="muted">{{ $t('merchantOverview.noPayments') }}</p>
      </div>

      <div class="card ledger-card">
        <div class="card-head">
          <div>
            <h3>{{ $t('merchantOverview.ledgerTitle') }}</h3>
            <p class="muted ledger-sub">{{ $t('merchantOverview.ledgerSub') }}</p>
          </div>
          <RouterLink class="btn ghost" to="/merchant/payouts">{{ $t('nav.payouts') }}</RouterLink>
        </div>
        <div v-if="wallet?.recentEntries?.length" class="ledger-list">
          <article class="tx-item" v-for="e in wallet.recentEntries.slice(0, 6)" :key="e.id">
            <div class="tx-ico" :class="entryTone(e)">{{ entryIcon(e) }}</div>
            <div class="tx-main">
              <div class="tx-title-row">
                <strong>{{ entryTitle(e) }}</strong>
                <span class="tx-chip" :class="entryTone(e)">{{ entryTypeLabel(e.type) }}</span>
              </div>
              <div class="tx-meta">
                <span v-if="entryRef(e)" class="mono">{{ entryRef(e) }}</span>
                <span>{{ when(e.createdAtUtc) }}</span>
              </div>
            </div>
            <div class="tx-money">
              <div class="amt mono" :class="entryTone(e)">{{ signedMoney(e.amount) }}</div>
              <div class="tx-balance muted">{{ $t('merchantOverview.balanceAfter') }} {{ money(e.balanceAfter) }}</div>
            </div>
          </article>
        </div>
        <p v-else class="muted">{{ $t('merchantOverview.ledgerEmpty') }}</p>
      </div>
    </div>

    <div class="card actions-card">
      <div class="card-head">
        <h3>{{ $t('merchantOverview.shortcuts') }}</h3>
      </div>
      <div class="action-grid">
        <RouterLink class="action" to="/merchant/test">
          <strong>{{ $t('merchantOverview.actTest') }}</strong>
          <span>{{ $t('merchantOverview.actTestHint') }}</span>
        </RouterLink>
        <RouterLink class="action" to="/merchant/platforms">
          <strong>{{ $t('merchantOverview.actPlatforms') }}</strong>
          <span>{{ $t('merchantOverview.actPlatformsHint') }}</span>
        </RouterLink>
        <RouterLink class="action" to="/merchant/payment-methods">
          <strong>{{ $t('merchantOverview.actMethods') }}</strong>
          <span>{{ $t('merchantOverview.actMethodsHint') }}</span>
        </RouterLink>
        <RouterLink class="action" to="/merchant/docs">
          <strong>{{ $t('merchantOverview.actDocs') }}</strong>
          <span>{{ $t('merchantOverview.actDocsHint') }}</span>
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useAuthStore } from '../../stores/auth'
import ProviderBadge from '../../components/ProviderBadge.vue'
import VolumeLineChart from '../../components/VolumeLineChart.vue'

const { t, locale } = useI18n()
const auth = useAuthStore()

const merchant = ref(null)
const wallet = ref(null)
const payments = ref([])
const paymentsTotal = ref(0)
const platforms = ref([])
const methods = ref(null)
const keys = ref([])
const error = ref('')
const mode = ref('live')

const firstName = computed(() => (auth.user?.fullName || merchant.value?.businessName || 'تاجر').split(/\s+/)[0])
const paidCount = computed(() => payments.value.filter((p) => p.status === 'Paid').length)
const overviewGross = computed(() => {
  if (mode.value === 'test') {
    return payments.value.filter((p) => p.status === 'Paid').reduce((s, p) => s + Number(p.amount || 0), 0)
  }
  return Number(wallet.value?.lifetimeGross || 0)
})
const overviewFees = computed(() => {
  if (mode.value === 'test') {
    return payments.value.filter((p) => p.status === 'Paid').reduce((s, p) => s + Number(p.platformFee || 0), 0)
  }
  return Number(wallet.value?.lifetimeFees || 0)
})
const overviewNet = computed(() => overviewGross.value - overviewFees.value)

const commissionRows = computed(() => {
  const m = merchant.value
  if (!m) return []
  // Only providers enabled by the main platform admin (same source as Payment Methods).
  const allowed = new Set((methods.value?.platformEnabled || []).map((p) => String(p)))
  if (!allowed.size) return []
  return [
    { key: 'fib', provider: 'Fib', rate: m.fibCommissionPercent ?? m.commissionPercent },
    { key: 'zain', provider: 'ZainCash', rate: m.zainCashCommissionPercent ?? m.commissionPercent },
    { key: 'qi', provider: 'Qi', rate: m.qiCommissionPercent ?? m.commissionPercent },
    { key: 'super', provider: 'SuperQi', rate: m.superQiCommissionPercent ?? m.commissionPercent },
    { key: 'alqaseh', provider: 'Alqaseh', rate: m.alqasehCommissionPercent ?? m.commissionPercent }
  ].filter((c) => allowed.has(c.provider))
})
const commissionRange = computed(() => {
  const rates = commissionRows.value.map((c) => Number(c.rate ?? 0))
  if (!rates.length) return '—'
  const min = Math.min(...rates)
  const max = Math.max(...rates)
  if (min === max) return `${formatPct(min)}%`
  return `${formatPct(min)}–${formatPct(max)}%`
})

function formatPct(v) {
  const n = Number(v ?? 0)
  return Number.isInteger(n) ? String(n) : n.toFixed(1)
}
const successRate = computed(() => {
  if (!paymentsTotal.value) return 0
  const paid = payments.value.filter((p) => p.status === 'Paid').length
  // Approximate from loaded page when total is larger — prefer paid/total if we have full sample
  const samplePaid = payments.value.filter((p) => p.status === 'Paid').length
  const sampleTotal = payments.value.length || 1
  if (payments.value.length >= paymentsTotal.value) {
    return Math.round((samplePaid / sampleTotal) * 100)
  }
  // Use ratio from sample as estimate when we only have a page
  return Math.round((samplePaid / sampleTotal) * 100)
})
const approvedPlatforms = computed(() => platforms.value.filter((p) => p.status === 'Approved').length)
const effectiveMethods = computed(() => methods.value?.effectiveProviders || [])
const hasKey = computed(() =>
  (keys.value?.length || 0) > 0
  || platforms.value.some((p) => p.apiKeyId || p.apiKeyPrefix || p.hasOneTimeApiKey)
)

const series = computed(() => buildDailySeries(payments.value.filter((p) => p.status === 'Paid')))
const seriesTotal = computed(() => series.value.reduce((s, d) => s + d.volume, 0))
const seriesCount = computed(() => series.value.reduce((s, d) => s + d.count, 0))

const readiness = computed(() => {
  const active = merchant.value?.status === 'Active'
  const plat = approvedPlatforms.value > 0
  const key = hasKey.value
  const meth = effectiveMethods.value.length > 0
  const paid = paidCount.value > 0 || (mode.value === 'live' && Number(wallet.value?.lifetimeGross || 0) > 0)
  return [
    {
      key: 'account',
      done: active,
      label: t('merchantOverview.stepAccount'),
      hint: active ? t('merchantOverview.stepAccountOk') : t('merchantOverview.stepAccountWait')
    },
    {
      key: 'platform',
      done: plat,
      label: t('merchantOverview.stepPlatform'),
      hint: plat
        ? t('merchantOverview.stepPlatformOk', { n: approvedPlatforms.value })
        : t('merchantOverview.stepPlatformWait')
    },
    {
      key: 'key',
      done: key,
      label: t('merchantOverview.stepKey'),
      hint: key ? t('merchantOverview.stepKeyOk') : t('merchantOverview.stepKeyWait')
    },
    {
      key: 'methods',
      done: meth,
      label: t('merchantOverview.stepMethods'),
      hint: meth
        ? t('merchantOverview.stepMethodsOk', { n: effectiveMethods.value.length })
        : t('merchantOverview.stepMethodsWait')
    },
    {
      key: 'paid',
      done: paid,
      label: t('merchantOverview.stepPaid'),
      hint: paid ? t('merchantOverview.stepPaidOk') : t('merchantOverview.stepPaidWait')
    }
  ]
})

const readinessPct = computed(() => {
  const steps = readiness.value
  if (!steps.length) return 0
  return Math.round((steps.filter((s) => s.done).length / steps.length) * 100)
})

function money(v) {
  return new Intl.NumberFormat('en-IQ').format(Math.round(Number(v ?? 0))) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function signedMoney(v) {
  const n = Number(v ?? 0)
  const sign = n > 0 ? '+' : n < 0 ? '−' : ''
  return sign + money(Math.abs(n))
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}
function shortId(id) {
  return String(id || '').replace(/-/g, '').slice(0, 8).toUpperCase()
}
function statusClass(s) {
  if (s === 'Active') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}
function payStatusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function entryTypeLabel(type) {
  return t(`merchantOverview.ledgerTypes.${type}`, type || '—')
}
function entryTone(e) {
  const n = Number(e?.amount ?? 0)
  if (n > 0) return 'credit'
  if (n < 0) return 'debit'
  return 'neutral'
}
function entryIcon(e) {
  const tone = entryTone(e)
  if (tone === 'credit') return '↑'
  if (tone === 'debit') return '↓'
  return '•'
}
function entryRef(e) {
  if (e.paymentId) return `#${shortId(e.paymentId)}`
  if (e.payoutRequestId) return `#${shortId(e.payoutRequestId)}`
  return ''
}
function entryTitle(e) {
  const label = entryTypeLabel(e.type)
  if (label && label !== e.type) return label
  return String(e.description || label || '—').slice(0, 80)
}

function buildDailySeries(paidPayments) {
  const from = new Date()
  from.setUTCHours(0, 0, 0, 0)
  from.setUTCDate(from.getUTCDate() - 13)
  const map = new Map()
  for (let i = 0; i < 14; i++) {
    const d = new Date(from)
    d.setUTCDate(from.getUTCDate() + i)
    const key = d.toISOString().slice(0, 10)
    map.set(key, { date: key, count: 0, volume: 0, fees: 0 })
  }
  for (const p of paidPayments) {
    const when = p.paidAtUtc || p.createdAtUtc
    if (!when) continue
    const key = new Date(when).toISOString().slice(0, 10)
    if (!map.has(key)) continue
    const row = map.get(key)
    row.count += 1
    row.volume += Number(p.amount || 0)
    row.fees += Number(p.platformFee || 0)
  }
  return [...map.values()]
}

async function load() {
  error.value = ''
  try {
    const [m, w, pay, plat, meth, k] = await Promise.all([
      api.get('/api/merchant/me'),
      api.get('/api/merchant/wallet'),
      api.get('/api/merchant/payments', { params: { page: 1, pageSize: 200, mode: mode.value } }),
      api.get('/api/merchant/platforms'),
      api.get('/api/merchant/payment-methods'),
      api.get('/api/merchant/api-keys')
    ])
    merchant.value = m.data
    wallet.value = w.data
    payments.value = pay.data?.items || []
    paymentsTotal.value = pay.data?.total ?? payments.value.length
    platforms.value = Array.isArray(plat.data) ? plat.data : (plat.data?.items || [])
    methods.value = meth.data
    keys.value = Array.isArray(k.data) ? k.data : (k.data?.items || [])
  } catch (e) {
    error.value = e.response?.data?.message || t('merchantOverview.loadFail')
  }
}

async function setMode(next) {
  if (mode.value === next) return
  mode.value = next
  error.value = ''
  try {
    const { data } = await api.get('/api/merchant/payments', {
      params: { page: 1, pageSize: 200, mode: mode.value }
    })
    payments.value = data?.items || []
    paymentsTotal.value = data?.total ?? payments.value.length
  } catch (e) {
    error.value = e.response?.data?.message || t('merchantOverview.loadFail')
  }
}

onMounted(load)
</script>

<style scoped>
.head-actions { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.mode-switch {
  display: inline-flex;
  padding: 4px;
  border: 1px solid var(--line);
  border-radius: 14px;
  background: #f8fafc;
  gap: 4px;
}
.mode-switch button {
  border: 0;
  background: transparent;
  color: var(--muted);
  font: inherit;
  font-weight: 700;
  font-size: 0.82rem;
  padding: 8px 14px;
  border-radius: 10px;
  cursor: pointer;
}
.mode-switch button.active {
  background: #031838;
  color: #fff;
}
.mode-switch button.active.test {
  background: #b45309;
  color: #fff;
}
.mode-hint { margin: 0 0 14px; font-size: 0.88rem; }
.banner.warn {
  background: #fffbeb;
  border: 1px solid #fcd34d;
  color: #92400e;
  padding: 12px 16px;
  border-radius: 14px;
  margin-bottom: 16px;
  font-weight: 600;
}
.commission-card { margin-bottom: 16px; }
.commission-grid {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 10px;
}
.commission-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 8px;
  padding: 12px;
  border: 1px solid var(--line);
  border-radius: 12px;
  background: #fff;
}
.secondary { margin-top: -4px; }
.mini-card {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 16px;
  box-shadow: var(--shadow-sm);
  display: grid;
  gap: 6px;
}
.mini-label { color: var(--muted); font-size: 0.85rem; font-weight: 600; }
.mini-card strong { font-size: 1.2rem; font-weight: 800; }
.mini-hint { font-size: 0.78rem; }
.chips.tiny { display: flex; flex-wrap: wrap; gap: 6px; margin-top: 4px; }
.chart-sub { margin: 4px 0 0; font-size: 0.82rem; }
.chart-totals {
  display: grid;
  gap: 4px;
  text-align: end;
  font-size: 0.8rem;
  color: var(--muted);
  justify-items: end;
}
.chart-totals .badge { justify-self: end; }
.ready-pct {
  font-size: 1.4rem;
  color: var(--brand-secondary);
}
.hint { margin: 6px 0 0; color: var(--muted); font-size: 0.78rem; }
.ok-text { color: var(--ok); font-weight: 700; font-size: 0.82rem; }
.warn-text { color: var(--warn); font-weight: 700; font-size: 0.82rem; }
.fee { font-size: 0.75rem; }
.action-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
}
.action {
  display: grid;
  gap: 6px;
  padding: 16px;
  border: 1px solid var(--line);
  border-radius: 14px;
  text-decoration: none;
  color: inherit;
  background: #fff;
  transition: 0.15s ease;
}
.action:hover {
  border-color: #8ba3c7;
  background: #f5f7fb;
  box-shadow: var(--shadow-sm);
}
.action strong { color: var(--brand); }
.action span { color: var(--muted); font-size: 0.82rem; line-height: 1.5; }
@media (max-width: 1100px) {
  .action-grid, .commission-grid { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 900px) {
  .secondary { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 600px) {
  .action-grid, .secondary, .commission-grid { grid-template-columns: 1fr; }
  .head-actions { width: 100%; }
  .head-actions .btn { flex: 1; min-height: 44px; }
  .mode-switch { width: 100%; display: flex; }
  .mode-switch button { flex: 1; min-height: 44px; }
}
</style>
