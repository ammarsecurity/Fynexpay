<template>
  <div>
    <h1>{{ $t('payouts.title') }}</h1>
    <div class="card">
      <div class="field"><label>{{ $t('payouts.amountLabel') }}</label><input v-model.number="amount" type="number" class="input-control" /></div>
      <div class="field"><label>{{ $t('payouts.method') }}</label>
        <select v-model="destinationType">
          <option>BankTransfer</option>
          <option>Wallet</option>
          <option>Other</option>
        </select>
      </div>
      <div class="field"><label>{{ $t('payouts.accountDetails') }}</label><textarea v-model="destinationDetails" rows="3" /></div>
      <p v-if="error" class="error">{{ error }}</p>
      <button class="btn" @click="create">{{ $t('payouts.create') }}</button>
    </div>

    <DataToolbar
      v-model="filters"
      :statuses="['Pending', 'Approved', 'Completed', 'Rejected']"
      :search-placeholder="$t('payouts.searchPlaceholder')"
      :status-all-label="$t('payouts.allStatuses')"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <div class="card">
      <table>
        <thead>
          <tr>
            <th>{{ $t('common.amount') }}</th>
            <th>{{ $t('payouts.destination') }}</th>
            <th>{{ $t('common.status') }}</th>
            <th>{{ $t('common.date') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in payouts" :key="p.id">
            <td>{{ format(p.amount) }}</td>
            <td>{{ p.destinationType }} — {{ p.destinationDetails }}</td>
            <td><span class="badge" :class="badge(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span></td>
            <td>{{ formatDate(p.createdAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="!payouts.length" class="muted">{{ $t('common.noResults') }}</p>
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        @change="load"
      />
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import PaginationBar from '../../components/PaginationBar.vue'

const { t, locale } = useI18n()
const payouts = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const amount = ref(10000)
const destinationType = ref('BankTransfer')
const destinationDetails = ref('')
const error = ref('')
const filters = reactive({ q: '', status: '', from: '', to: '' })
const applied = reactive({ q: '', status: '', from: '', to: '' })

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function formatDate(v) {
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
function badge(s) {
  if (s === 'Completed' || s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

async function load() {
  const { data } = await api.get('/api/merchant/payouts', {
    params: {
      page: page.value,
      pageSize: pageSize.value,
      q: applied.q || undefined,
      status: applied.status || undefined,
      from: applied.from || undefined,
      to: applied.to || undefined
    }
  })
  payouts.value = data.items || []
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
  filters.from = ''
  filters.to = ''
  applyFilters()
}

async function create() {
  error.value = ''
  try {
    await api.post('/api/merchant/payouts', {
      amount: amount.value,
      destinationType: destinationType.value,
      destinationDetails: destinationDetails.value
    })
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('payouts.fail')
  }
}

onMounted(load)
</script>
