<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('payments.adminTitle') }}</h1>
      </div>
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
    </div>

    <p class="mode-hint muted">{{ mode === 'test' ? $t('payments.modeTestHint') : $t('payments.modeLiveHint') }}</p>

    <DataToolbar
      v-model="filters"
      :statuses="['Paid', 'Pending', 'Failed', 'Cancelled', 'Expired']"
      :search-placeholder="$t('payments.searchPlaceholder')"
      :status-all-label="$t('payments.allStatuses')"
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
              <th>{{ $t('payments.mode') }}</th>
              <th>{{ $t('common.provider') }}</th>
              <th>{{ $t('common.amount') }}</th>
              <th>{{ $t('payments.fee') }}</th>
              <th>{{ $t('payments.order') }}</th>
              <th>{{ $t('payments.sectionCustomer') }}</th>
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
              <td>
                <span class="badge" :class="p.isTest ? 'test' : 'live'">
                  {{ p.isTest ? $t('payments.modeTest') : $t('payments.modeLive') }}
                </span>
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
                <div class="order-cell">
                  <strong class="mono ltr">{{ p.customerPhone || '—' }}</strong>
                  <span class="muted">{{ p.customerPhoneVerifiedAtUtc ? $t('payments.customerVerified') : $t('payments.customerUnverified') }}</span>
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
import { useRoute } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import PaginationBar from '../../components/PaginationBar.vue'
import ProviderBadge from '../../components/ProviderBadge.vue'
import PaymentDetailsModal from '../../components/PaymentDetailsModal.vue'

const { locale } = useI18n()
const route = useRoute()
const payments = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const selectedId = ref('')
const mode = ref(route.query.mode === 'test' ? 'test' : 'live')
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

function setMode(next) {
  if (mode.value === next) return
  mode.value = next
  page.value = 1
  load()
}

async function load() {
  const { data } = await api.get('/api/admin/payments', {
    params: {
      page: page.value,
      pageSize: pageSize.value,
      mode: mode.value,
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
.page-head {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 8px;
}
.page-head h1 { margin: 0; }
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
  font-size: 0.86rem;
  padding: 9px 16px;
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
.badge.test { background: #fff7ed; color: #c2410c; border: 1px solid #fdba74; }
.badge.live { background: #ecfdf5; color: #047857; border: 1px solid #6ee7b7; }
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
.payments-table tr:hover td { background: rgba(3, 24, 56, 0.03); }
.date-cell { display: grid; gap: 2px; min-width: 110px; }
.date-cell .muted { font-size: 0.78rem; }
.order-cell { display: grid; gap: 2px; min-width: 140px; }
.order-cell .muted { font-size: 0.75rem; }
.ltr { direction: ltr; text-align: start; unicode-bidi: isolate; }
.money { font-weight: 700; white-space: nowrap; font-variant-numeric: tabular-nums; }
.muted-money { color: var(--muted); font-weight: 600; }
.details-btn { padding: 8px 12px; font-size: 0.82rem; white-space: nowrap; }
.table-card :deep(.pagination) { padding: 12px 16px 16px; margin-top: 0; }
.table-card > .muted { padding: 16px; }
@media (max-width: 600px) {
  .mode-switch { width: 100%; display: flex; }
  .mode-switch button { flex: 1; min-height: 44px; }
  .details-btn { min-height: 40px; }
  .payments-table th,
  .payments-table td { padding: 10px 12px; }
}
</style>
