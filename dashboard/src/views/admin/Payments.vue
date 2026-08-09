<template>
  <div>
    <h1>{{ $t('payments.adminTitle') }}</h1>

    <DataToolbar
      v-model="filters"
      :statuses="['Paid', 'Pending', 'Failed', 'Cancelled', 'Expired']"
      :providers="providerOptions"
      :search-placeholder="$t('payments.searchPlaceholder')"
      :status-all-label="$t('payments.allStatuses')"
      :provider-all-label="$t('payments.allProviders')"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <div class="card table-card">
      <div class="table-wrap">
        <table class="payments-table">
          <thead>
            <tr>
              <th>{{ $t('common.date') }}</th>
              <th>{{ $t('common.status') }}</th>
              <th>{{ $t('common.provider') }}</th>
              <th>{{ $t('common.amount') }}</th>
              <th>{{ $t('payments.fee') }}</th>
              <th>{{ $t('payments.order') }}</th>
              <th>{{ $t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in payments" :key="p.id">
              <td class="date-cell">
                <strong>{{ formatDay(p.createdAtUtc) }}</strong>
                <span class="muted">{{ formatTime(p.createdAtUtc) }}</span>
              </td>
              <td>
                <span class="badge" :class="statusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
              </td>
              <td><ProviderBadge :provider="p.provider" /></td>
              <td class="money">{{ format(p.amount) }}</td>
              <td class="money muted-money">{{ format(p.platformFee) }}</td>
              <td>
                <div class="order-cell">
                  <strong class="mono">{{ shortOrder(p.orderId) }}</strong>
                  <span class="muted mono">{{ p.id.slice(0, 8) }}</span>
                </div>
              </td>
              <td>
                <button class="btn secondary details-btn" type="button" @click="openDetails(p.id)">
                  {{ $t('payments.allDetails') }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="!payments.length" class="muted">{{ $t('payments.empty') }}</p>
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        @change="load"
      />
    </div>

    <PaymentDetailsModal
      :open="!!selectedId"
      :payment-id="selectedId"
      endpoint="/api/admin/payments"
      @close="selectedId = ''"
    />
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import PaginationBar from '../../components/PaginationBar.vue'
import ProviderBadge from '../../components/ProviderBadge.vue'
import PaymentDetailsModal from '../../components/PaymentDetailsModal.vue'

const { locale } = useI18n()
const payments = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const selectedId = ref('')
const providerOptions = ['Fib', 'ZainCash', 'Qi', 'SuperQi']
const filters = reactive({ q: '', status: '', provider: '', from: '', to: '' })
const applied = reactive({ q: '', status: '', provider: '', from: '', to: '' })

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function formatDay(v) {
  return new Date(v).toLocaleDateString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
function formatTime(v) {
  return new Date(v).toLocaleTimeString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', { hour: '2-digit', minute: '2-digit' })
}
function shortOrder(orderId) {
  if (!orderId) return '—'
  return orderId.length > 22 ? `${orderId.slice(0, 18)}…` : orderId
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending' || s === 'PendingSelection') return 'warn'
  return 'danger'
}
function openDetails(id) { selectedId.value = id }

async function load() {
  const { data } = await api.get('/api/admin/payments', {
    params: {
      page: page.value,
      pageSize: pageSize.value,
      q: applied.q || undefined,
      status: applied.status || undefined,
      provider: applied.provider || undefined,
      from: applied.from || undefined,
      to: applied.to || undefined
    }
  })
  payments.value = data.items || []
  total.value = data.total || 0
}

function applyFilters() {
  Object.assign(applied, filters)
  page.value = 1
  load()
}

function resetFilters() {
  filters.q = ''
  filters.status = ''
  filters.provider = ''
  filters.from = ''
  filters.to = ''
  applyFilters()
}

onMounted(load)
</script>

<style scoped>
.table-card { padding: 0; overflow: hidden; }
.table-wrap { overflow-x: auto; }
.payments-table { width: 100%; border-collapse: collapse; }
.payments-table th {
  background: #f8fafc;
  padding: 12px 14px;
  font-size: 0.78rem;
  color: var(--muted);
  border-bottom: 1px solid var(--line);
  white-space: nowrap;
}
.payments-table td {
  padding: 12px 14px;
  border-bottom: 1px solid var(--line);
  vertical-align: middle;
}
.payments-table tr:hover td { background: rgba(108, 60, 236, 0.03); }
.date-cell { display: grid; gap: 2px; min-width: 110px; }
.date-cell .muted { font-size: 0.78rem; }
.order-cell { display: grid; gap: 2px; min-width: 140px; }
.order-cell .muted { font-size: 0.75rem; }
.money { font-weight: 700; white-space: nowrap; font-variant-numeric: tabular-nums; }
.muted-money { color: var(--muted); font-weight: 600; }
.details-btn { padding: 8px 12px; font-size: 0.82rem; white-space: nowrap; }
.table-card :deep(.pagination) { padding: 12px 16px 16px; margin-top: 0; }
.table-card > .muted { padding: 16px; }
</style>
