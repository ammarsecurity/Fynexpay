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
      <p v-if="!items.length" class="muted">{{ $t('common.noResults') }}</p>
      <div v-else class="list">
        <article v-for="p in items" :key="p.id" class="item">
          <div class="main">
            <div class="title-row">
              <strong>{{ p.name }}</strong>
              <span class="badge" :class="statusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
            </div>
            <div class="meta muted">
              <span>{{ p.merchantName || '—' }}</span>
              <span class="mono" dir="ltr">{{ p.domain }}</span>
              <span>{{ when(p.createdAtUtc) }}</span>
            </div>
            <label class="field notes-field">
              <span>{{ $t('platforms.adminNotes') }}</span>
              <input v-model="notes[p.id]" :placeholder="$t('platforms.adminNotesPh')" />
            </label>
            <p v-if="p.oneTimeApiKey" class="key-hint mono" dir="ltr">API: {{ p.oneTimeApiKey }}</p>
          </div>
          <div class="actions">
            <button class="btn" type="button" @click="review(p, 'approve')">{{ $t('platforms.approve') }}</button>
            <button class="btn secondary" type="button" @click="review(p, 'suspend')">{{ $t('platforms.suspend') }}</button>
            <button class="btn danger" type="button" @click="review(p, 'reject')">{{ $t('platforms.reject') }}</button>
          </div>
        </article>
      </div>
      <p v-if="msg" class="ok-msg">{{ msg }}</p>
      <p v-if="error" class="error">{{ error }}</p>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'

const { t, locale } = useI18n()
const items = ref([])
const notes = reactive({})
const filters = reactive({ q: '', status: 'Pending' })
const applied = reactive({ q: '', status: 'Pending' })
const msg = ref('')
const error = ref('')

function statusClass(s) {
  if (s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}

async function load() {
  const { data } = await api.get('/api/admin/platforms', {
    params: {
      q: applied.q || undefined,
      status: applied.status || undefined
    }
  })
  items.value = data || []
  for (const p of items.value) {
    if (notes[p.id] == null) notes[p.id] = p.adminNotes || ''
  }
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
      adminNotes: notes[p.id] || null
    })
    if (data.oneTimeApiKey) {
      msg.value = t('platforms.approvedWithKey')
      p.oneTimeApiKey = data.oneTimeApiKey
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
.list { display: grid; gap: 12px; }
.item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 16px;
  background: #fafbff;
  flex-wrap: wrap;
}
.title-row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.meta { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 6px; font-size: 0.85rem; }
.notes-field { margin: 10px 0 0; max-width: 420px; }
.notes-field span { font-size: 0.78rem; color: var(--muted); font-weight: 700; }
.notes-field input {
  margin-top: 4px;
  width: 100%;
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 9px 12px;
}
.actions { display: flex; gap: 8px; flex-wrap: wrap; align-items: start; }
.key-hint {
  margin: 8px 0 0;
  padding: 8px 10px;
  background: #021225;
  color: #e2e8f0;
  border-radius: 10px;
  font-size: 0.8rem;
  overflow: auto;
}
.ok-msg { color: #15803d; font-weight: 700; }
</style>
