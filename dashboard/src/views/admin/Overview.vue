<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('adminOverview.welcome', { name: firstName }) }}</h1>
        <p class="sub">{{ $t('adminOverview.sub') }}</p>
      </div>
      <button class="btn secondary" type="button">{{ $t('adminOverview.last30') }}</button>
    </div>

    <div class="grid" v-if="stats">
      <div class="stat">
        <div class="ico purple">$</div>
        <div class="label">{{ $t('adminOverview.volume') }}</div>
        <div class="value">{{ format(stats.grossVolume) }}</div>
        <div class="trend">{{ $t('adminOverview.trend') }}</div>
      </div>
      <div class="stat">
        <div class="ico green">+</div>
        <div class="label">{{ $t('adminOverview.activeMerchants') }}</div>
        <div class="value">{{ stats.activeMerchants }}</div>
        <div class="trend">{{ $t('adminOverview.ofTotal', { n: stats.merchantsCount }) }}</div>
      </div>
      <div class="stat">
        <div class="ico sky">#</div>
        <div class="label">{{ $t('adminOverview.paymentsCount') }}</div>
        <div class="value">{{ stats.paymentsCount }}</div>
        <div class="trend">{{ $t('adminOverview.registered') }}</div>
      </div>
      <div class="stat">
        <div class="ico rose">%</div>
        <div class="label">{{ $t('adminOverview.fees') }}</div>
        <div class="value">{{ format(stats.platformFees) }}</div>
        <div class="trend down" v-if="stats.pendingPayouts">{{ $t('adminOverview.pendingPayouts', { n: stats.pendingPayouts }) }}</div>
        <div class="trend" v-else>{{ $t('adminOverview.noPending') }}</div>
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.revenue') }}</h3>
          <span class="badge">{{ $t('adminOverview.commissions') }}</span>
        </div>
        <div class="chart-line">
          <svg viewBox="0 0 400 180" preserveAspectRatio="none">
            <defs>
              <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#6c3cec" stop-opacity="0.35" />
                <stop offset="100%" stop-color="#6c3cec" stop-opacity="0" />
              </linearGradient>
            </defs>
            <path d="M0 140 C40 120, 70 150, 110 100 S180 40, 220 70 S300 130, 340 60 S380 40, 400 55 L400 180 L0 180 Z" fill="url(#g)" />
            <path d="M0 140 C40 120, 70 150, 110 100 S180 40, 220 70 S300 130, 340 60 S380 40, 400 55" fill="none" stroke="#6c3cec" stroke-width="3" stroke-linecap="round" />
          </svg>
        </div>
      </div>

      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.activity') }}</h3>
          <span class="badge ok">Live</span>
        </div>
        <div class="bars-chart">
          <i style="height:35%"></i>
          <i style="height:55%"></i>
          <i style="height:42%"></i>
          <i style="height:78%"></i>
          <i style="height:60%"></i>
          <i style="height:90%"></i>
          <i style="height:48%"></i>
          <i style="height:70%"></i>
        </div>
      </div>
    </div>

    <div class="dash-grid-3">
      <div class="card">
        <div class="card-head">
          <h3>{{ $t('adminOverview.recent') }}</h3>
          <RouterLink class="btn ghost" to="/admin/payments">{{ $t('common.viewAll') }}</RouterLink>
        </div>
        <div v-if="payments.length">
          <div class="tx-item" v-for="p in payments.slice(0, 5)" :key="p.id">
            <ProviderBadge :provider="p.provider" :show-name="false" />
            <div>
              <strong>{{ p.orderId }}</strong>
              <div class="muted" style="font-size:0.82rem">{{ p.provider }}</div>
            </div>
            <div class="amt mono">{{ format(p.amount) }}</div>
            <span class="badge" :class="statusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
          </div>
        </div>
        <p v-else class="muted">{{ $t('adminOverview.noPayments') }}</p>
      </div>

      <div class="card">
        <div class="card-head"><h3>{{ $t('adminOverview.distribution') }}</h3></div>
        <div class="donut-wrap">
          <div class="donut"><strong>{{ stats?.paymentsCount || 0 }}</strong></div>
          <div class="legend">
            <div><i style="background:#6c3cec"></i> Paid</div>
            <div><i style="background:#22d3ee"></i> Pending</div>
            <div><i style="background:#f59e0b"></i> Failed</div>
            <div><i style="background:#e2e8f0"></i> Other</div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-head"><h3>{{ $t('adminOverview.priorities') }}</h3></div>
        <div class="progress-list">
          <div class="progress-row">
            <div class="top"><span>{{ $t('adminOverview.activation') }}</span><span>{{ activationPct }}%</span></div>
            <div class="bar"><span :style="{ width: activationPct + '%' }"></span></div>
          </div>
          <div class="progress-row">
            <div class="top"><span>{{ $t('adminOverview.providerReady') }}</span><span>90%</span></div>
            <div class="bar"><span style="width:90%"></span></div>
          </div>
          <div class="progress-row">
            <div class="top"><span>{{ $t('adminOverview.payoutProcessing') }}</span><span>{{ payoutHealth }}%</span></div>
            <div class="bar"><span :style="{ width: payoutHealth + '%' }"></span></div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { api } from '../../api'
import { useAuthStore } from '../../stores/auth'
import ProviderBadge from '../../components/ProviderBadge.vue'

const auth = useAuthStore()
const stats = ref(null)
const payments = ref([])

const firstName = computed(() => (auth.user?.fullName || 'Admin').split(/\s+/)[0])
const activationPct = computed(() => {
  if (!stats.value?.merchantsCount) return 0
  return Math.round((stats.value.activeMerchants / stats.value.merchantsCount) * 100)
})
const payoutHealth = computed(() => {
  const pending = stats.value?.pendingPayouts || 0
  return Math.max(20, 100 - pending * 15)
})

function format(v) {
  return new Intl.NumberFormat('en-IQ').format(v ?? 0) + ' د.ع'
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}

onMounted(async () => {
  const [s, p] = await Promise.all([
    api.get('/api/admin/stats'),
    api.get('/api/admin/payments', { params: { page: 1, pageSize: 5 } })
  ])
  stats.value = s.data
  payments.value = p.data?.items || []
})
</script>
