<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('platforms.adminTitle') }}</h1>
        <p class="sub">{{ $t('platforms.adminSub') }}</p>
      </div>
    </div>

    <DataToolbar
      v-model="filters"
      :statuses="['Pending', 'Approved', 'Rejected', 'Suspended']"
      :show-dates="false"
      :search-placeholder="$t('platforms.searchPlaceholder')"
      :status-all-label="$t('platforms.allStatuses')"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <div class="card">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{{ $t('platforms.name') }}</th>
              <th>{{ $t('merchants.business') }}</th>
              <th>{{ $t('platforms.domain') }}</th>
              <th>{{ $t('common.status') }}</th>
              <th>{{ $t('platforms.createdAt') }}</th>
              <th>{{ $t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="p in items" :key="p.id">
              <td>
                <div class="plat-cell">
                  <div class="logo-frame" :class="{ empty: !p.logoUrl }">
                    <img v-if="p.logoUrl" :src="logoSrc(p.logoUrl)" :alt="p.name" width="36" height="36" />
                    <span v-else>{{ initial(p.name) }}</span>
                  </div>
                  <div class="plat-meta">
                    <strong>{{ p.name }}</strong>
                    <span v-if="p.apiKeyPrefix" class="mono muted" dir="ltr">{{ p.apiKeyPrefix }}…</span>
                  </div>
                </div>
              </td>
              <td>{{ p.merchantName || '—' }}</td>
              <td><span class="mono" dir="ltr">{{ p.domain }}</span></td>
              <td>
                <span class="badge" :class="statusClass(p.status)">
                  {{ $t(`status.${p.status}`, p.status) }}
                </span>
              </td>
              <td class="muted date-cell">{{ when(p.createdAtUtc) }}</td>
              <td>
                <div class="row-actions">
                  <button class="btn secondary" type="button" @click="openDetails(p)">{{ $t('platforms.allDetails') }}</button>
                  <button
                    v-if="p.status !== 'Approved'"
                    class="btn"
                    type="button"
                    @click="review(p, 'approve')"
                  >{{ $t('platforms.approve') }}</button>
                  <button
                    v-if="p.status === 'Approved'"
                    class="btn secondary"
                    type="button"
                    @click="review(p, 'suspend')"
                  >{{ $t('platforms.suspend') }}</button>
                  <button
                    v-if="p.status === 'Pending'"
                    class="btn danger"
                    type="button"
                    @click="review(p, 'reject')"
                  >{{ $t('platforms.reject') }}</button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p v-if="!items.length" class="muted empty">{{ $t('common.noResults') }}</p>
      <p v-if="msg" class="ok-msg">{{ msg }}</p>
      <p v-if="error" class="error">{{ error }}</p>
    </div>

    <PlatformDetailsModal
      :open="!!selectedId"
      :platform-id="selectedId"
      @close="selectedId = ''"
      @changed="load"
    />
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import PlatformDetailsModal from '../../components/PlatformDetailsModal.vue'

const { t, locale } = useI18n()
const items = ref([])
const filters = reactive({ q: '', status: '' })
const applied = reactive({ q: '', status: '' })
const msg = ref('')
const error = ref('')
const selectedId = ref('')

function openDetails(p) {
  selectedId.value = p.id
}

function statusClass(s) {
  if (s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}

function logoSrc(url) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}

function initial(name) {
  return (name || '?').trim().slice(0, 1).toUpperCase()
}

async function load() {
  const { data } = await api.get('/api/admin/platforms', {
    params: {
      q: applied.q || undefined,
      status: applied.status || undefined
    }
  })
  items.value = data || []
}

function applyFilters() {
  applied.q = filters.q
  applied.status = filters.status
  load()
}

function resetFilters() {
  filters.q = ''
  filters.status = ''
  applyFilters()
}

async function review(p, action) {
  msg.value = ''
  error.value = ''
  try {
    const { data } = await api.patch(`/api/admin/platforms/${p.id}`, {
      action,
      adminNotes: p.adminNotes || null
    })
    if (data.oneTimeApiKey) {
      msg.value = t('platforms.approvedWithKey')
      selectedId.value = p.id
    } else {
      msg.value = t('platforms.reviewOk')
    }
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('platforms.reviewFail')
  }
}

onMounted(load)
</script>

<style scoped>
.table-wrap { overflow-x: auto; }
.plat-cell {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 160px;
}
.logo-frame {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  border: 1px solid var(--line);
  display: grid;
  place-items: center;
  overflow: hidden;
  flex-shrink: 0;
  font-size: 0.8rem;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(145deg, var(--brand), var(--brand-secondary));
}
.logo-frame.empty { color: #fff; }
.logo-frame img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
  background: #fff;
}
.plat-meta {
  display: grid;
  gap: 2px;
  min-width: 0;
}
.plat-meta strong {
  color: var(--brand);
  font-size: 0.92rem;
}
.plat-meta .muted {
  font-size: 0.75rem;
}
.date-cell {
  white-space: nowrap;
  font-size: 0.88rem;
}
.row-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.row-actions .btn {
  padding: 7px 12px;
  font-size: 0.8rem;
  border-radius: 10px;
  box-shadow: none;
}
.empty { margin: 16px 0 0; }
.ok-msg {
  color: #15803d;
  font-weight: 700;
  margin: 12px 0 0;
}
</style>
