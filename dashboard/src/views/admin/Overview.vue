<template>
  <div class="overview">
    <div class="page-head">
      <div>
        <h1>{{ $t('adminOverview.welcome', { name: firstName }) }}</h1>
        <p class="sub">{{ $t('adminOverview.sub') }}</p>
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
        <span class="period-chip">{{ $t('adminOverview.period14') }}</span>
        <button class="btn secondary" type="button" :disabled="loading" @click="load">
          {{ loading ? $t('common.loading') : $t('common.refresh') }}
        </button>
      </div>
    </div>

    <p class="mode-hint muted">{{ mode === 'test' ? $t('payments.modeTestHint') : $t('payments.modeLiveHint') }}</p>

    <p v-if="error" class="error">{{ error }}</p>

    <div class="grid" v-if="stats">
      <div class="stat">
        <div class="ico purple">د</div>
        <div class="label">{{ $t('adminOverview.volume') }}</div>
        <div class="value">{{ money(stats.grossVolume) }}</div>
        <div class="trend" :class="{ down: volumeDelta < 0 }">
          {{ deltaLabel(volumeDelta) }} · {{ $t('adminOverview.vsPrev7') }}
        </div>
      </div>
      <div class="stat">
        <div class="ico rose">%</div>
        <div class="label">{{ $t('adminOverview.fees') }}</div>
        <div class="value">{{ money(stats.platformFees) }}</div>
        <div class="trend">{{ $t('adminOverview.netMerchants') }}: {{ money(stats.netToMerchants) }}</div>
      </div>
      <div class="stat">
        <div class="ico green">+</div>
        <div class="label">{{ $t('adminOverview.activeMerchants') }}</div>
        <div class="value">{{ num(stats.activeMerchants) }}</div>
        <div class="trend">
          {{ $t('adminOverview.ofTotal', { n: stats.merchantsCount }) }}
          <template v-if="stats.pendingMerchants"> · {{ $t('adminOverview.pendingMerchants', { n: stats.pendingMerchants }) }}</template>
        </div>
      </div>
      <div class="stat">
        <div class="ico sky">#</div>
        <div class="label">{{ $t('adminOverview.paymentsCount') }}</div>
        <div class="value">{{ num(stats.paymentsCount) }}</div>
        <div class="trend">
          {{ $t('adminOverview.paidCount', { n: stats.paidCount }) }}
          · {{ $t('adminOverview.successRate', { n: successRate }) }}
        </div>
      </div>
    </div>

    <div class="grid secondary" v-if="stats">
      <div class="mini-card">
        <span class="mini-label">{{ $t('adminOverview.avgTicket') }}</span>
        <strong class="mono">{{ money(stats.avgTicket) }}</strong>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('adminOverview.pendingPayments') }}</span>
        <strong>{{ num(stats.pendingPayments) }}</strong>
      </div>
      <div class="mini-card">
        <span class="mini-label">{{ $t('adminOverview.failedPayments') }}</span>
        <strong>{{ num(stats.failedPayments) }}</strong>
      </div>
      <div class="mini-card" :class="{ alert: stats.pendingPayouts > 0 }">
        <span class="mini-label">{{ $t('adminOverview.payoutsQueue') }}</span>
        <strong>{{ num(stats.pendingPayouts) }}</strong>
        <RouterLink v-if="stats.pendingPayouts" class="mini-link" to="/admin/payouts">{{ $t('adminOverview.reviewPayouts') }}</RouterLink>
      </div>
    </div>

    <div class="dash-grid" v-if="stats">
      <div class="card">
        <div class="card-head">
          <div>
            <h3>{{ $t('adminOverview.volumeChart') }}</h3>
            <p class="muted chart-sub">{{ $t('adminOverview.volumeChartSub') }}</p>
          </div>
          <div class="chart-totals">
            <span>{{ $t('adminOverview.chartVol') }}: <b class="mono">{{ money(seriesTotal.volume) }}</b></span>
            <span>{{ $t('adminOverview.chartFees') }}: <b class="mono">{{ money(seriesTotal.fees) }}</b></span>
          </div>
        </div>
        <VolumeLineChart
          :series="series"
          :empty-text="$t('adminOverview.noSeries')"
          :height="220"
        />
      </div>

      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.byProvider') }}</h3>
          <RouterLink class="btn ghost" to="/admin/providers">{{ $t('nav.providers') }}</RouterLink>
        </div>
        <div v-if="providerBars.length" class="provider-list">
          <div class="provider-row" v-for="row in providerBars" :key="row.key">
            <div class="provider-top">
              <ProviderBadge :provider="row.key" :show-name="false" />
              <div class="provider-meta">
                <span class="mono">{{ money(row.amount) }}</span>
                <span class="muted">{{ $t('adminOverview.opsCount', { n: row.count }) }}</span>
              </div>
            </div>
            <div class="bar"><span :style="{ width: row.pct + '%' }"></span></div>
          </div>
        </div>
        <p v-else class="muted">{{ $t('adminOverview.noProviders') }}</p>
      </div>
    </div>

    <div class="dash-grid-3">
      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.recent') }}</h3>
          <RouterLink class="btn ghost" :to="{ path: '/admin/payments', query: { mode } }">{{ $t('common.viewAll') }}</RouterLink>
        </div>
        <div v-if="payments.length" class="recent-list">
          <article class="tx-item admin-tx" v-for="p in payments" :key="p.id">
            <ProviderBadge :provider="p.provider" :show-name="false" />
            <div class="tx-main">
              <div class="tx-title-row">
                <strong class="mono">{{ p.orderId || shortId(p.id) }}</strong>
                <span class="badge" :class="p.isTest ? 'test' : 'live'">
                  {{ p.isTest ? $t('payments.modeTest') : $t('payments.modeLive') }}
                </span>
                <span class="badge" :class="statusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
              </div>
              <div class="tx-meta">
                <span>{{ money(p.amount) }}</span>
                <span>{{ when(p.createdAtUtc) }}</span>
              </div>
            </div>
            <div class="tx-money">
              <div class="amt mono">{{ money(p.amount) }}</div>
              <div class="muted fee">{{ $t('adminOverview.feeShort') }} {{ money(p.platformFee) }}</div>
            </div>
          </article>
        </div>
        <p v-else class="muted">{{ $t('adminOverview.noPayments') }}</p>
      </div>

      <div class="card">
        <div class="card-head"><h3>{{ $t('adminOverview.distribution') }}</h3></div>
        <div v-if="statusRows.length" class="status-list">
          <div class="status-row" v-for="row in statusRows" :key="row.key">
            <div class="status-top">
              <span class="dot" :style="{ background: statusColor(row.key) }"></span>
              <span>{{ $t(`status.${row.key}`, row.key) }}</span>
              <strong>{{ row.count }}</strong>
            </div>
            <div class="bar thin"><span :style="{ width: row.pct + '%', background: statusColor(row.key) }"></span></div>
            <div class="status-amt muted mono">{{ money(row.amount) }}</div>
          </div>
        </div>
        <p v-else class="muted">{{ $t('adminOverview.noPayments') }}</p>
      </div>

      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.pendingQueue') }}</h3>
          <RouterLink class="btn ghost" to="/admin/merchants">{{ $t('nav.merchants') }}</RouterLink>
        </div>
        <div class="progress-list ops">
          <div class="progress-row">
            <div class="top">
              <span>{{ $t('adminOverview.activation') }}</span>
              <span>{{ activationPct }}%</span>
            </div>
            <div class="bar"><span :style="{ width: activationPct + '%' }"></span></div>
            <p class="hint">{{ $t('adminOverview.activationHint', { active: stats?.activeMerchants || 0, total: stats?.merchantsCount || 0 }) }}</p>
          </div>
          <div class="progress-row">
            <div class="top">
              <span>{{ $t('adminOverview.collectionRate') }}</span>
              <span>{{ successRate }}%</span>
            </div>
            <div class="bar"><span :style="{ width: successRate + '%' }"></span></div>
            <p class="hint">{{ $t('adminOverview.collectionHint', { paid: stats?.paidCount || 0, total: stats?.paymentsCount || 0 }) }}</p>
          </div>
        </div>
        <div v-if="pendingMerchants.length" class="queue-list">
          <h4>{{ $t('adminOverview.awaitingApproval') }}</h4>
          <RouterLink
            v-for="m in pendingMerchants"
            :key="m.id"
            class="queue-item"
            :to="`/admin/merchants`"
          >
            <strong>{{ m.businessName }}</strong>
            <span class="muted">{{ m.contactEmail }}</span>
            <span class="muted mono">{{ when(m.createdAtUtc) }}</span>
          </RouterLink>
        </div>
        <p v-else class="muted queue-empty">{{ $t('adminOverview.noPendingMerchants') }}</p>
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
const stats = ref(null)
const payments = ref([])
const pendingMerchants = ref([])
const loading = ref(false)
const error = ref('')
const mode = ref('live')

const firstName = computed(() => (auth.user?.fullName || 'Admin').split(/\s+/)[0])

const activationPct = computed(() => {
  if (!stats.value?.merchantsCount) return 0
  return Math.round((stats.value.activeMerchants / stats.value.merchantsCount) * 100)
})

const successRate = computed(() => {
  const total = stats.value?.paymentsCount || 0
  if (!total) return 0
  return Math.round(((stats.value.paidCount || 0) / total) * 100)
})

const series = computed(() => stats.value?.last14Days || [])

const seriesTotal = computed(() => ({
  volume: series.value.reduce((s, d) => s + Number(d.volume || 0), 0),
  fees: series.value.reduce((s, d) => s + Number(d.fees || 0), 0)
}))

const volumeDelta = computed(() => {
  const days = series.value
  if (days.length < 14) return 0
  const prev = days.slice(0, 7).reduce((s, d) => s + Number(d.volume || 0), 0)
  const curr = days.slice(7).reduce((s, d) => s + Number(d.volume || 0), 0)
  if (prev <= 0) return curr > 0 ? 100 : 0
  return Math.round(((curr - prev) / prev) * 100)
})

const providerBars = computed(() => {
  const rows = stats.value?.byProvider || []
  const max = Math.max(...rows.map((r) => Number(r.amount || 0)), 1)
  return rows.slice(0, 6).map((r) => ({
    key: r.key,
    count: r.count,
    amount: r.amount,
    pct: Math.max(4, Math.round((Number(r.amount || 0) / max) * 100))
  }))
})

const statusRows = computed(() => {
  const rows = stats.value?.byStatus || []
  const total = Math.max(rows.reduce((s, r) => s + Number(r.count || 0), 0), 1)
  return rows.map((r) => ({
    key: r.key,
    count: r.count,
    amount: r.amount,
    pct: Math.round((Number(r.count || 0) / total) * 100)
  }))
})

function money(v) {
  const loc = locale.value === 'ar' ? 'en-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(Math.round(Number(v ?? 0))) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function num(v) {
  return new Intl.NumberFormat('en-IQ').format(v ?? 0)
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
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function statusColor(key) {
  const map = {
    Paid: '#16a34a',
    Pending: '#f59e0b',
    Failed: '#ef4444',
    Declined: '#f43f5e',
    Expired: '#94a3b8',
    Cancelled: '#64748b',
    Refunded: '#0ea5e9'
  }
  return map[key] || '#031838'
}
function deltaLabel(n) {
  if (n > 0) return `+${n}%`
  if (n < 0) return `${n}%`
  return '0%'
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [s, p, m] = await Promise.all([
      api.get('/api/admin/stats', { params: { mode: mode.value } }),
      api.get('/api/admin/payments', { params: { page: 1, pageSize: 8, mode: mode.value } }),
      api.get('/api/admin/merchants', { params: { status: 'Pending', page: 1, pageSize: 5 } })
    ])
    stats.value = s.data
    payments.value = p.data?.items || []
    pendingMerchants.value = m.data?.items || []
  } catch (e) {
    error.value = e.response?.data?.message || t('adminOverview.loadFail')
  } finally {
    loading.value = false
  }
}

async function setMode(next) {
  if (mode.value === next) return
  mode.value = next
  await load()
}

onMounted(load)
</script>

<style scoped>
.head-actions { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.mode-switch {
  display: inline-flex;
  background: #f1f5f9;
  border: 1px solid var(--line);
  border-radius: 999px;
  padding: 3px;
  gap: 2px;
}
.mode-switch button {
  border: 0;
  background: transparent;
  font: inherit;
  font-weight: 700;
  font-size: 0.82rem;
  padding: 7px 14px;
  border-radius: 999px;
  cursor: pointer;
  color: var(--muted);
}
.mode-switch button.active {
  background: #fff;
  color: var(--brand-secondary);
  box-shadow: var(--shadow-sm);
}
.mode-switch button.active.test {
  color: #c2410c;
}
.mode-hint { margin: 0 0 14px; font-size: 0.88rem; }
.period-chip {
  background: var(--brand-soft);
  color: var(--brand-secondary);
  font-weight: 700;
  font-size: 0.82rem;
  padding: 8px 12px;
  border-radius: 999px;
}
.secondary { margin-top: -4px; }
.mini-card {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 16px;
  box-shadow: var(--shadow-sm);
  display: grid;
  gap: 8px;
}
.mini-card.alert { border-color: #fcd34d; background: #fffbeb; }
.mini-label { color: var(--muted); font-size: 0.85rem; font-weight: 600; }
.mini-card strong { font-size: 1.25rem; font-weight: 800; }
.mini-link { font-size: 0.82rem; font-weight: 700; color: var(--brand-secondary); }
.chart-sub { margin: 4px 0 0; font-size: 0.82rem; }
.chart-totals {
  display: grid;
  gap: 4px;
  text-align: end;
  font-size: 0.8rem;
  color: var(--muted);
}
.provider-list, .status-list, .recent-list, .queue-list { display: grid; gap: 12px; }
.provider-top, .status-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
  margin-bottom: 6px;
}
.provider-meta { display: grid; text-align: end; gap: 2px; font-size: 0.82rem; }
.status-top .dot {
  width: 10px;
  height: 10px;
  border-radius: 50%;
  display: inline-block;
}
.status-top { font-size: 0.9rem; }
.status-top strong { margin-inline-start: auto; }
.bar.thin { height: 6px; }
.status-amt { font-size: 0.78rem; margin-top: 4px; }
.admin-tx { grid-template-columns: 44px minmax(0, 1fr) auto; }
.fee { font-size: 0.75rem; }
.ops .hint { margin: 6px 0 0; color: var(--muted); font-size: 0.78rem; }
.queue-list { margin-top: 18px; }
.queue-list h4 { margin: 0 0 8px; font-size: 0.92rem; }
.queue-item {
  display: grid;
  gap: 2px;
  padding: 10px 12px;
  border: 1px solid var(--line);
  border-radius: 12px;
  text-decoration: none;
  color: inherit;
  background: #fff;
}
.queue-item:hover { border-color: #8ba3c7; background: #f5f7fb; }
.queue-empty { margin-top: 12px; }
@media (max-width: 900px) {
  .secondary { grid-template-columns: 1fr 1fr; }
}
@media (max-width: 600px) {
  .secondary { grid-template-columns: 1fr; }
  .head-actions { width: 100%; }
  .head-actions .btn { flex: 1; }
  .period-chip { flex: 1; text-align: center; }
}

</style>
