<template>
  <div>
    <h1>{{ $t('payouts.title') }}</h1>

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
            <th>{{ $t('payouts.details') }}</th>
            <th>{{ $t('common.status') }}</th>
            <th>{{ $t('common.date') }}</th>
            <th>{{ $t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in payouts" :key="p.id">
            <td>{{ format(p.amount) }}</td>
            <td>{{ p.destinationType }} — {{ p.destinationDetails }}</td>
            <td><span class="badge">{{ $t(`status.${p.status}`, p.status) }}</span></td>
            <td>{{ formatDate(p.createdAtUtc) }}</td>
            <td class="row" v-if="p.status === 'Pending' || p.status === 'Approved'">
              <button class="btn" @click="review(p.id, 'approve')">{{ $t('payouts.approve') }}</button>
              <button class="btn accent" @click="review(p.id, 'complete')">{{ $t('payouts.complete') }}</button>
              <button class="btn danger" @click="review(p.id, 'reject')">{{ $t('payouts.reject') }}</button>
            </td>
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

const { locale } = useI18n()
const payouts = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const filters = reactive({ q: '', status: '', from: '', to: '' })
const applied = reactive({ q: '', status: '', from: '', to: '' })

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function formatDate(v) {
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}

async function load() {
  const { data } = await api.get('/api/admin/payouts', {
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

async function review(id, action) {
  await api.post(`/api/admin/payouts/${id}/review`, { action, adminNote: '' })
  await load()
}

onMounted(load)
</script>
