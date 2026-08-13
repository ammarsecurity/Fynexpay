<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('platforms.keysTitle') }}</h1>
        <p class="sub">{{ $t('platforms.keysSub') }}</p>
      </div>
      <RouterLink class="btn" to="/merchant/platforms">{{ $t('platforms.managePlatforms') }}</RouterLink>
    </div>

    <div v-if="revealedMerchant" class="card reveal">
      <div class="reveal-top">
        <div>
          <strong>{{ $t('platforms.merchantBearerReady') }}</strong>
          <p class="muted" style="margin:4px 0 0">{{ $t('platforms.merchantBearerOnce') }}</p>
        </div>
        <button class="btn secondary" type="button" @click="revealedMerchant = ''">{{ $t('platforms.hide') }}</button>
      </div>
      <div class="copy-box">
        <div class="key-label"><span class="mono muted">fx_merch_</span></div>
        <code class="mono value" dir="ltr">{{ revealedMerchant }}</code>
        <button class="btn" type="button" @click="copy(revealedMerchant)">{{ $t('platforms.copyKey') }}</button>
        <button class="btn secondary" type="button" @click="copy('Bearer ' + revealedMerchant)">{{ $t('platforms.copyBearer') }}</button>
      </div>
    </div>

    <div class="card">
      <div class="card-head">
        <div>
          <h3>{{ $t('platforms.merchantBearerTitle') }}</h3>
          <p class="muted" style="margin:6px 0 0">{{ $t('platforms.merchantBearerSub') }}</p>
        </div>
        <span class="badge" :class="merchantKey?.isActive ? 'ok' : 'warn'">
          {{ merchantKey?.isActive ? $t('common.enabled') : $t('platforms.merchantBearerMissing') }}
        </span>
      </div>
      <p v-if="merchantError" class="error">{{ merchantError }}</p>
      <div class="key-item" v-if="merchantKey?.isActive">
        <div>
          <div class="key-title">
            <strong>Merchant</strong>
            <span class="badge ok">Bearer</span>
          </div>
          <div class="key-meta muted">
            <span class="mono" dir="ltr">{{ merchantKey.keyPrefix }}••••••••</span>
            <span v-if="merchantKey.lastUsedAtUtc">{{ formatDate(merchantKey.lastUsedAtUtc) }}</span>
            <span v-else>{{ formatDate(merchantKey.createdAtUtc) }}</span>
          </div>
        </div>
        <button class="btn secondary" type="button" :disabled="saving" @click="regenerateMerchant">
          {{ $t('platforms.regenMerchantKey') }}
        </button>
      </div>
      <button v-else class="btn" type="button" :disabled="saving" @click="claimMerchant">
        {{ $t('platforms.claimMerchantKey') }}
      </button>
    </div>

    <div class="card">
      <div class="card-head">
        <h3>{{ $t('platforms.boundKeys') }}</h3>
        <span class="muted">{{ activeCount }} / {{ keys.length }}</span>
      </div>
      <p v-if="!keys.length" class="muted">{{ $t('platforms.noBoundKeys') }}</p>
      <div v-else class="keys-list">
        <div class="key-item" v-for="k in keys" :key="k.id">
          <div>
            <div class="key-title">
              <strong>{{ k.platformName || k.name }}</strong>
              <span class="badge" :class="k.isActive ? 'ok' : 'danger'">{{ k.isActive ? $t('common.enabled') : $t('common.disabled') }}</span>
            </div>
            <div class="key-meta muted">
              <span v-if="k.platformDomain" class="mono" dir="ltr">{{ k.platformDomain }}</span>
              <span class="mono" dir="ltr">{{ k.keyPrefix }}••••••••</span>
              <span>{{ formatDate(k.createdAtUtc) }}</span>
            </div>
          </div>
          <button v-if="k.isActive" class="btn danger" type="button" @click="revoke(k)">{{ $t('platforms.revoke') }}</button>
        </div>
      </div>
    </div>

    <div class="card">
      <h3>Webhook Secret</h3>
      <p class="muted">{{ $t('platforms.webhookHint') }}</p>
      <div class="copy-box">
        <code class="mono value" dir="ltr">{{ secret || '—' }}</code>
        <button class="btn" :disabled="!secret" type="button" @click="copy(secret)">{{ $t('platforms.copy') }}</button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useDialog } from '../../composables/useDialog'

const { t, locale } = useI18n()
const { confirm } = useDialog()
const keys = ref([])
const merchantKey = ref(null)
const revealedMerchant = ref('')
const secret = ref('')
const saving = ref(false)
const merchantError = ref('')
const activeCount = computed(() => keys.value.filter(k => k.isActive && k.merchantPlatformId).length)

function formatDate(v) {
  if (!v) return ''
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}
async function load() {
  const [k, s, m] = await Promise.all([
    api.get('/api/merchant/api-keys'),
    api.get('/api/merchant/webhook-secret'),
    api.get('/api/merchant/merchant-key')
  ])
  keys.value = (k.data || []).filter(x => x.merchantPlatformId)
  secret.value = s.data.secret
  merchantKey.value = m.data
}
async function claimMerchant() {
  merchantError.value = ''
  saving.value = true
  try {
    const { data } = await api.post('/api/merchant/merchant-key/claim')
    revealedMerchant.value = data.apiKey
    await load()
  } catch (e) {
    merchantError.value = e.response?.data?.message || t('platforms.claimFail')
  } finally {
    saving.value = false
  }
}
async function regenerateMerchant() {
  const ok = await confirm({
    variant: 'danger',
    title: t('dialog.dangerTitle'),
    message: t('platforms.regenMerchantConfirm'),
    confirmText: t('platforms.regenMerchantKey')
  })
  if (!ok) return
  merchantError.value = ''
  saving.value = true
  try {
    const { data } = await api.post('/api/merchant/merchant-key/regenerate')
    revealedMerchant.value = data.apiKey
    await load()
  } catch (e) {
    merchantError.value = e.response?.data?.message || t('platforms.claimFail')
  } finally {
    saving.value = false
  }
}
async function revoke(k) {
  const ok = await confirm({
    variant: 'danger',
    title: t('dialog.dangerTitle'),
    message: t('platforms.revokeConfirm'),
    confirmText: t('platforms.revoke')
  })
  if (!ok) return
  await api.delete(`/api/merchant/api-keys/${k.id}`)
  await load()
}
onMounted(load)
</script>

<style scoped>
.keys-list { display: grid; gap: 12px; }
.key-item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 16px;
  flex-wrap: wrap;
}
.key-title { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.key-meta { display: flex; gap: 12px; flex-wrap: wrap; margin-top: 6px; font-size: 0.85rem; }
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
  flex-wrap: wrap;
}
.value { flex: 1; overflow-x: auto; white-space: nowrap; }
.reveal {
  border-color: color-mix(in srgb, var(--brand) 28%, var(--line));
  background: var(--brand-soft);
}
.reveal-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  margin-bottom: 12px;
}
.key-label { font-weight: 800; font-size: 0.82rem; }
.error { color: var(--danger); font-weight: 700; }
</style>
