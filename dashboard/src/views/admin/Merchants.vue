<template>
  <div>
    <h1>{{ $t('merchants.title') }}</h1>

    <DataToolbar
      v-model="filters"
      :statuses="['Active', 'Suspended', 'Pending']"
      :show-dates="false"
      :search-placeholder="$t('merchants.searchPlaceholder')"
      :status-all-label="$t('merchants.allStatuses')"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <div class="card">
      <table>
        <thead>
          <tr>
            <th>{{ $t('merchants.business') }}</th>
            <th>{{ $t('merchants.email') }}</th>
            <th>{{ $t('common.status') }}</th>
            <th>{{ $t('merchants.commission') }}</th>
            <th>{{ $t('merchants.balance') }}</th>
            <th>{{ $t('common.actions') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in merchants" :key="m.id">
            <td>{{ m.businessName }}</td>
            <td>{{ m.contactEmail }}</td>
            <td><span class="badge" :class="m.status === 'Active' ? 'ok' : 'warn'">{{ $t(`status.${m.status}`, m.status) }}</span></td>
            <td>
              <label class="commission-cell">
                <input class="input-compact" type="number" step="0.1" min="0" max="100" v-model.number="m.commissionPercent" />
                <span class="suffix">%</span>
              </label>
            </td>
            <td>{{ format(m.availableBalance) }}</td>
            <td class="row">
              <button class="btn secondary" @click="openDetails(m)">{{ $t('merchants.allDetails') }}</button>
              <button class="btn" @click="save(m, 'Active')">{{ $t('merchants.activate') }}</button>
              <button class="btn secondary" @click="save(m)">{{ $t('merchants.saveCommission') }}</button>
              <button class="btn danger" @click="save(m, 'Suspended')">{{ $t('merchants.suspend') }}</button>
            </td>
          </tr>
        </tbody>
      </table>
      <p v-if="!merchants.length" class="muted">{{ $t('common.noResults') }}</p>
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        @change="load"
      />
    </div>

    <MerchantDetailsModal
      :open="!!selectedId"
      :merchant-id="selectedId"
      @close="selectedId = ''"
      @changed="load"
    />
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import MerchantDetailsModal from '../../components/MerchantDetailsModal.vue'
import PaginationBar from '../../components/PaginationBar.vue'

const { locale } = useI18n()
const merchants = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const filters = reactive({ q: '', status: '' })
const applied = reactive({ q: '', status: '' })
const selectedId = ref('')

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}

async function load() {
  const { data } = await api.get('/api/admin/merchants', {
    params: {
      page: page.value,
      pageSize: pageSize.value,
      q: applied.q || undefined,
      status: applied.status || undefined
    }
  })
  merchants.value = data.items || []
  total.value = data.total || 0
}

function applyFilters() {
  applied.q = filters.q
  applied.status = filters.status
  page.value = 1
  load()
}

function resetFilters() {
  filters.q = ''
  filters.status = ''
  applyFilters()
}

function openDetails(m) {
  selectedId.value = m.id
}

async function save(m, status) {
  await api.patch(`/api/admin/merchants/${m.id}`, {
    status: status || undefined,
    commissionPercent: m.commissionPercent
  })
  await load()
}

onMounted(load)
</script>
