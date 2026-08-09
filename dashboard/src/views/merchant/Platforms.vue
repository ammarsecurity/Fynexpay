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
          <div class="logo-col">
            <div class="logo-frame" :class="{ empty: !p.logoUrl }">
              <img v-if="p.logoUrl" :src="logoSrc(p.logoUrl)" :alt="p.name" width="72" height="72" />
              <span v-else class="logo-ph">{{ initials(p.name) }}</span>
            </div>
            <label class="upload-btn">
              <input
                type="file"
                accept="image/png,.png"
                hidden
                :disabled="uploadingId === p.id"
                @change="onLogoPick(p, $event)"
              />
              {{ uploadingId === p.id ? $t('common.loading') : $t('platforms.uploadLogo') }}
            </label>
            <button
              v-if="p.logoUrl"
              class="link-btn"
              type="button"
              :disabled="uploadingId === p.id"
              @click="removeLogo(p)"
            >{{ $t('platforms.removeLogo') }}</button>
          </div>

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
            <p class="logo-hint">{{ $t('platforms.logoHint') }}</p>
            <p v-if="p.adminNotes" class="notes">{{ p.adminNotes }}</p>
            <p v-if="logoError[p.id]" class="error tiny">{{ logoError[p.id] }}</p>
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
import { api, API_BASE } from '../../api'
import { useDialog } from '../../composables/useDialog'

const { t, locale } = useI18n()
const { confirm } = useDialog()
const platforms = ref([])
const saving = ref(false)
const error = ref('')
const revealedKey = ref('')
const uploadingId = ref('')
const logoError = reactive({})
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
function logoSrc(url) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}
function initials(name) {
  const parts = String(name || '').trim().split(/\s+/).filter(Boolean)
  if (!parts.length) return '?'
  return parts.slice(0, 2).map((p) => p[0]).join('').toUpperCase()
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}

function readImageMeta(file) {
  return new Promise((resolve, reject) => {
    const url = URL.createObjectURL(file)
    const img = new Image()
    img.onload = () => {
      URL.revokeObjectURL(url)
      resolve({ width: img.naturalWidth, height: img.naturalHeight })
    }
    img.onerror = () => {
      URL.revokeObjectURL(url)
      reject(new Error('bad-image'))
    }
    img.src = url
  })
}

async function validateLogoClient(file) {
  if (!file) throw new Error(t('platforms.logoRequired'))
  if (file.type !== 'image/png' && !file.name.toLowerCase().endsWith('.png')) {
    throw new Error(t('platforms.logoPngOnly'))
  }
  if (file.size > 1_500_000) throw new Error(t('platforms.logoTooLarge'))
  const { width, height } = await readImageMeta(file)
  if (width !== 500 || height !== 500) throw new Error(t('platforms.logoSize'))
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

async function onLogoPick(p, event) {
  const input = event.target
  const file = input?.files?.[0]
  logoError[p.id] = ''
  if (!file) return
  uploadingId.value = p.id
  try {
    await validateLogoClient(file)
    const body = new FormData()
    body.append('file', file)
    const { data } = await api.post(`/api/merchant/platforms/${p.id}/logo`, body)
    const idx = platforms.value.findIndex((x) => x.id === p.id)
    if (idx >= 0) platforms.value[idx] = data
  } catch (e) {
    logoError[p.id] = e.response?.data?.message || e.message || t('platforms.logoFail')
  } finally {
    uploadingId.value = ''
    if (input) input.value = ''
  }
}

async function removeLogo(p) {
  const ok = await confirm({
    title: t('platforms.removeLogo'),
    message: t('platforms.removeLogoConfirm'),
    confirmText: t('platforms.removeLogo')
  })
  if (!ok) return
  uploadingId.value = p.id
  logoError[p.id] = ''
  try {
    const { data } = await api.delete(`/api/merchant/platforms/${p.id}/logo`)
    const idx = platforms.value.findIndex((x) => x.id === p.id)
    if (idx >= 0) platforms.value[idx] = data
  } catch (e) {
    logoError[p.id] = e.response?.data?.message || t('platforms.logoFail')
  } finally {
    uploadingId.value = ''
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
  align-items: flex-start;
}
.logo-col {
  display: grid;
  gap: 8px;
  justify-items: center;
  width: 96px;
  flex-shrink: 0;
}
.logo-frame {
  width: 72px;
  height: 72px;
  border-radius: 18px;
  border: 1px solid var(--line);
  display: grid;
  place-items: center;
  overflow: hidden;
  background:
    linear-gradient(45deg, #eceff5 25%, transparent 25%),
    linear-gradient(-45deg, #eceff5 25%, transparent 25%),
    linear-gradient(45deg, transparent 75%, #eceff5 75%),
    linear-gradient(-45deg, transparent 75%, #eceff5 75%);
  background-size: 14px 14px;
  background-position: 0 0, 0 7px, 7px -7px, -7px 0;
  background-color: #fff;
}
.logo-frame.empty { background: #f3f5fb; }
.logo-frame img {
  width: 100%;
  height: 100%;
  object-fit: contain;
  display: block;
}
.logo-ph {
  font-weight: 800;
  color: #6c3cec;
  font-size: 0.95rem;
}
.upload-btn {
  font-size: 0.72rem;
  font-weight: 700;
  color: #fff;
  background: #031838;
  border-radius: 999px;
  padding: 7px 10px;
  cursor: pointer;
  text-align: center;
  width: 100%;
}
.upload-btn:has(input:disabled) { opacity: 0.6; cursor: wait; }
.link-btn {
  border: 0;
  background: transparent;
  color: #b42318;
  font-size: 0.72rem;
  font-weight: 700;
  cursor: pointer;
  padding: 0;
}
.item-main { flex: 1; min-width: 200px; }
.title-row { display: flex; gap: 8px; align-items: center; flex-wrap: wrap; }
.meta { display: flex; flex-wrap: wrap; gap: 10px; margin-top: 6px; font-size: 0.85rem; }
.logo-hint { margin: 8px 0 0; color: var(--muted); font-size: 0.78rem; line-height: 1.45; }
.notes { margin: 8px 0 0; color: var(--muted); font-size: 0.85rem; }
.error.tiny { margin: 6px 0 0; font-size: 0.8rem; }
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
