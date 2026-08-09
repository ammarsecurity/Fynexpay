<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('platforms.title') }}</h1>
        <p class="sub">{{ $t('platforms.merchantSub') }}</p>
      </div>
    </div>

    <div class="card">
      <h3>{{ $t('platforms.requestTitle') }}</h3>
      <form class="form-row" @submit.prevent="create">
        <label class="field">
          <span>{{ $t('platforms.name') }}</span>
          <input v-model="form.name" required :placeholder="$t('platforms.namePh')" />
        </label>
        <label class="field">
          <span>{{ $t('platforms.domain') }}</span>
          <input v-model="form.domain" required placeholder="shop.example.com" dir="ltr" />
        </label>
        <button class="btn" type="submit" :disabled="saving">{{ saving ? $t('common.loading') : $t('platforms.submit') }}</button>
      </form>
      <p v-if="error" class="error">{{ error }}</p>
    </div>

    <div v-if="revealedKey" class="card reveal">
      <div class="reveal-top">
        <div>
          <strong>{{ $t('platforms.keyReady') }}</strong>
          <p class="muted" style="margin:4px 0 0">{{ $t('platforms.keyOnce') }}</p>
        </div>
        <button class="btn secondary" type="button" @click="revealedKey = ''">{{ $t('platforms.hide') }}</button>
      </div>
      <div class="copy-box">
        <code class="mono value" dir="ltr">{{ revealedKey }}</code>
        <button class="btn" type="button" @click="copy(revealedKey)">{{ $t('platforms.copyKey') }}</button>
      </div>
    </div>

    <div class="card">
      <div class="card-head">
        <h3>{{ $t('platforms.listTitle') }}</h3>
        <span class="muted">{{ platforms.length }}</span>
      </div>
      <p v-if="!platforms.length" class="muted">{{ $t('platforms.empty') }}</p>
      <div v-else class="list">
        <article v-for="p in platforms" :key="p.id" class="item">
          <div class="item-main">
            <div class="title-row">
              <strong>{{ p.name }}</strong>
              <span class="badge" :class="statusClass(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span>
            </div>
            <div class="meta muted">
              <span class="mono" dir="ltr">{{ p.domain }}</span>
              <span v-if="p.apiKeyPrefix" class="mono" dir="ltr">{{ p.apiKeyPrefix }}••••</span>
              <span>{{ when(p.createdAtUtc) }}</span>
            </div>
            <p v-if="p.adminNotes" class="notes">{{ p.adminNotes }}</p>
          </div>
          <div class="actions">
            <button
              v-if="p.hasOneTimeApiKey"
              class="btn"
              type="button"
              @click="claim(p)"
            >{{ $t('platforms.claimKey') }}</button>
            <button
              v-if="p.status === 'Approved'"
              class="btn secondary"
              type="button"
              @click="regen(p)"
            >{{ $t('platforms.regen') }}</button>
          </div>
        </article>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useDialog } from '../../composables/useDialog'

const { t, locale } = useI18n()
const { confirm } = useDialog()
const platforms = ref([])
const saving = ref(false)
const error = ref('')
const revealedKey = ref('')
const form = reactive({ name: '', domain: '' })

function statusClass(s) {
  if (s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}

async function load() {
  const { data } = await api.get('/api/merchant/platforms')
  platforms.value = data || []
}

async function create() {
  saving.value = true
  error.value = ''
  try {
    await api.post('/api/merchant/platforms', {
      name: form.name.trim(),
      domain: form.domain.trim()
    })
    form.name = ''
    form.domain = ''
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('platforms.requestFail')
  } finally {
    saving.value = false
  }
}

async function claim(p) {
  try {
    const { data } = await api.post(`/api/merchant/platforms/${p.id}/claim-key`)
    revealedKey.value = data.apiKey
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('platforms.claimFail')
  }
}

async function regen(p) {
  const ok = await confirm({
    variant: 'danger',
    title: t('dialog.dangerTitle'),
    message: t('platforms.regenConfirm'),
    confirmText: t('platforms.regen')
  })
  if (!ok) return
  try {
    const { data } = await api.post(`/api/merchant/platforms/${p.id}/regenerate-key`)
    if (data.oneTimeApiKey) revealedKey.value = data.oneTimeApiKey
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('platforms.regenFail')
  }
}

onMounted(load)
</script>

<style scoped>
.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr auto;
  gap: 12px;
  align-items: end;
}
.form-row .field { margin: 0; display: grid; gap: 6px; }
.form-row .field span { color: var(--muted); font-size: 0.82rem; font-weight: 700; }
.form-row input {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  outline: none;
}
.form-row input:focus {
  border-color: rgba(108, 60, 236, 0.55);
  box-shadow: 0 0 0 4px rgba(108, 60, 236, 0.14);
}
.list { display: grid; gap: 12px; }
.item {
  display: flex;
  justify-content: space-between;
  gap: 14px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 16px;
  background: #fff;
  flex-wrap: wrap;
}
.title-row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.meta { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 6px; font-size: 0.85rem; }
.notes { margin: 8px 0 0; color: var(--muted); font-size: 0.85rem; }
.actions { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.reveal {
  border-color: rgba(16, 185, 129, 0.35);
  background: rgba(16, 185, 129, 0.06);
}
.reveal-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
}
.value { flex: 1; overflow-x: auto; white-space: nowrap; font-weight: 700; }
@media (max-width: 800px) {
  .form-row { grid-template-columns: 1fr; }
}
</style>
