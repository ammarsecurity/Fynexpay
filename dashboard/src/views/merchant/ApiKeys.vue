<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('platforms.keysTitle') }}</h1>
        <p class="sub">{{ $t('platforms.keysSub') }}</p>
      </div>
      <RouterLink class="btn" to="/merchant/platforms">{{ $t('platforms.managePlatforms') }}</RouterLink>
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
const secret = ref('')
const activeCount = computed(() => keys.value.filter(k => k.isActive && k.merchantPlatformId).length)

function formatDate(v) {
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}
async function load() {
  const [k, s] = await Promise.all([
    api.get('/api/merchant/api-keys'),
    api.get('/api/merchant/webhook-secret')
  ])
  keys.value = (k.data || []).filter(x => x.merchantPlatformId)
  secret.value = s.data.secret
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
}
.value { flex: 1; overflow-x: auto; white-space: nowrap; }
</style>
