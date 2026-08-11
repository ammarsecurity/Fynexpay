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

    <div v-if="revealedKey || revealedTestKey" class="card reveal">
      <div class="reveal-top">
        <div>
          <strong>{{ $t('platforms.keyReady') }}</strong>
          <p class="muted" style="margin:4px 0 0">{{ $t('platforms.keyOnce') }}</p>
        </div>
        <button class="btn secondary" type="button" @click="clearRevealed">{{ $t('platforms.hide') }}</button>
      </div>
      <div v-if="revealedKey" class="copy-box">
        <div class="key-label">{{ $t('platforms.liveKey') }} <span class="mono muted">fx_live_</span></div>
        <code class="mono value" dir="ltr">{{ revealedKey }}</code>
        <button class="btn" type="button" @click="copy(revealedKey)">{{ $t('platforms.copyKey') }}</button>
      </div>
      <div v-if="revealedTestKey" class="copy-box" style="margin-top:12px">
        <div class="key-label">{{ $t('platforms.testKey') }} <span class="mono muted">fx_test_</span></div>
        <code class="mono value" dir="ltr">{{ revealedTestKey }}</code>
        <button class="btn secondary" type="button" @click="copy(revealedTestKey)">{{ $t('platforms.copyKey') }}</button>
      </div>
    </div>

    <div class="card">
      <div class="card-head">
        <h3>{{ $t('platforms.listTitle') }}</h3>
        <span class="muted">{{ platforms.length }}</span>
      </div>
      <p v-if="!platforms.length" class="muted">{{ $t('platforms.empty') }}</p>
      <p v-if="editMsg" class="ok-msg">{{ editMsg }}</p>
      <div v-if="platforms.length" class="list">
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
              <span v-if="p.apiKeyPrefix" class="mono" dir="ltr">live {{ p.apiKeyPrefix }}••••</span>
              <span v-if="p.testApiKeyPrefix" class="mono" dir="ltr">test {{ p.testApiKeyPrefix }}••••</span>
              <span>{{ when(p.createdAtUtc) }}</span>
            </div>
            <p class="logo-hint">{{ $t('platforms.logoHint') }}</p>
            <p v-if="p.adminNotes" class="notes">{{ p.adminNotes }}</p>
            <p v-if="logoError[p.id]" class="error tiny">{{ logoError[p.id] }}</p>
          </div>
          <div class="actions">
            <button class="btn secondary" type="button" @click="startEdit(p)">{{ $t('platforms.edit') }}</button>
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

          <div v-if="editingId === p.id" class="edit-panel">
            <p class="edit-hint">{{ $t('platforms.editHint') }}</p>
            <form class="edit-form" @submit.prevent="saveEdit(p)">
              <label class="field">
                <span>{{ $t('platforms.name') }}</span>
                <input v-model="editForm.name" required :placeholder="$t('platforms.namePh')" />
              </label>
              <label class="field">
                <span>{{ $t('platforms.domain') }}</span>
                <input v-model="editForm.domain" required placeholder="shop.example.com" dir="ltr" class="ltr" />
              </label>
              <div class="edit-actions">
                <button class="btn secondary" type="button" :disabled="saving" @click="cancelEdit">{{ $t('common.cancel') }}</button>
                <button class="btn" type="submit" :disabled="saving">
                  {{ saving ? $t('common.loading') : $t('platforms.saveEdit') }}
                </button>
              </div>
            </form>
            <p v-if="editError" class="error tiny">{{ editError }}</p>
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
const revealedTestKey = ref('')
const uploadingId = ref('')
const logoError = reactive({})
const form = reactive({ name: '', domain: '' })
const editingId = ref('')
const editForm = reactive({ name: '', domain: '' })
const editError = ref('')
const editMsg = ref('')

function startEdit(p) {
  editingId.value = p.id
  editForm.name = p.name || ''
  editForm.domain = p.domain || ''
  editError.value = ''
  error.value = ''
}

function cancelEdit() {
  editingId.value = ''
  editError.value = ''
}

async function saveEdit(p) {
  editError.value = ''
  error.value = ''
  const name = editForm.name.trim()
  const domain = editForm.domain.trim()
  if (!name || !domain) {
    editError.value = t('platforms.editRequired')
    return
  }

  const needsReapproval = ['Approved', 'Suspended', 'Rejected'].includes(p.status)
  if (needsReapproval) {
    const ok = await confirm({
      title: t('platforms.edit'),
      message: t('platforms.editConfirm'),
      confirmText: t('platforms.saveEdit')
    })
    if (!ok) return
  }

  saving.value = true
  try {
    await api.patch(`/api/merchant/platforms/${p.id}`, { name, domain })
    editingId.value = ''
    await load()
    error.value = ''
    editMsg.value = needsReapproval ? t('platforms.editPending') : t('platforms.editOk')
  } catch (e) {
    editError.value = e.response?.data?.message || t('platforms.editFail')
  } finally {
    saving.value = false
  }
}

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

function clearRevealed() {
  revealedKey.value = ''
  revealedTestKey.value = ''
}

async function claim(p) {
  try {
    const { data } = await api.post(`/api/merchant/platforms/${p.id}/claim-key`)
    revealedKey.value = data.apiKey || data.liveApiKey || ''
    revealedTestKey.value = data.testApiKey || ''
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
    if (data.oneTimeTestApiKey) revealedTestKey.value = data.oneTimeTestApiKey
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
  border-color: rgba(3, 24, 56, 0.55);
  box-shadow: 0 0 0 4px rgba(3, 24, 56, 0.14);
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
  color: #031838;
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
.edit-panel {
  flex: 1 1 100%;
  width: 100%;
  margin-top: 4px;
  padding-top: 14px;
  border-top: 1px dashed var(--line);
}
.edit-hint {
  margin: 0 0 12px;
  color: var(--muted);
  font-size: 0.85rem;
  line-height: 1.55;
  font-weight: 600;
}
.edit-form {
  display: grid;
  grid-template-columns: 1fr 1fr auto;
  gap: 12px;
  align-items: end;
}
.edit-form .field { margin: 0; display: grid; gap: 6px; }
.edit-form .field span { color: var(--muted); font-size: 0.82rem; font-weight: 700; }
.edit-form input {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  outline: none;
  width: 100%;
}
.edit-form input:focus {
  border-color: rgba(3, 24, 56, 0.55);
  box-shadow: 0 0 0 4px rgba(3, 24, 56, 0.14);
}
.edit-form .ltr { direction: ltr; text-align: left; }
.edit-actions { display: flex; gap: 8px; flex-wrap: wrap; }
.ok-msg {
  color: #15803d;
  font-weight: 700;
  margin: 0 0 12px;
}
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
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
}
.key-label {
  flex: 1 0 100%;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--muted);
}
.value { flex: 1; overflow-x: auto; white-space: nowrap; font-weight: 700; }
@media (max-width: 800px) {
  .form-row, .edit-form { grid-template-columns: 1fr; }
}
</style>
