<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('platforms.keysTitle') }}</h1>
        <p class="sub">{{ $t('platforms.keysSub') }}</p>
      </div>
      <RouterLink class="btn" to="/merchant/platforms">{{ $t('platforms.managePlatforms') }}</RouterLink>
    </div>

    <p class="hint-banner">{{ $t('platforms.keysStayVisible') }}</p>

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

      <KeyCopyBox
        v-if="merchantKey?.apiKey"
        :value="merchantKey.apiKey"
        :hint="'fx_merch_'"
        :copy-label="$t('platforms.copyKey')"
      >
        <button class="btn secondary" type="button" :disabled="!merchantKey.apiKey" @click="copyBearer">
          {{ $t('platforms.copyBearer') }}
        </button>
      </KeyCopyBox>
      <p v-else-if="merchantKey?.isActive" class="muted missing">{{ $t('platforms.fullKeyMissing') }}</p>

      <div class="optional-actions">
        <button
          v-if="!merchantKey?.isActive"
          class="btn"
          type="button"
          :disabled="saving"
          @click="claimMerchant"
        >
          {{ $t('platforms.claimMerchantKey') }}
        </button>
        <template v-else>
          <button
            class="btn ghost"
            type="button"
            :disabled="saving"
            @click="regenerateMerchant"
          >
            {{ $t('platforms.regenMerchantKey') }}
          </button>
          <span class="muted regen-hint">{{ $t('platforms.regenHint') }}</span>
        </template>
      </div>
    </div>

    <div class="card">
      <div class="card-head">
        <h3>{{ $t('platforms.boundKeys') }}</h3>
        <span class="muted">{{ approvedPlatforms.length }}</span>
      </div>
      <p v-if="!approvedPlatforms.length" class="muted">{{ $t('platforms.noBoundKeys') }}</p>
      <p v-if="keysError" class="error">{{ keysError }}</p>
      <div v-else class="keys-list">
        <div class="key-item" v-for="p in approvedPlatforms" :key="p.id">
          <div class="key-body">
            <div class="key-title">
              <strong>{{ p.name }}</strong>
              <span class="badge ok">{{ $t(`status.${p.status}`, p.status) }}</span>
            </div>
            <p v-if="p.domain" class="muted domain mono" dir="ltr">{{ p.domain }}</p>
            <KeyCopyBox
              v-if="p.oneTimeApiKey"
              :value="p.oneTimeApiKey"
              :label="$t('platforms.liveKey')"
              hint="fx_live_"
              :copy-label="$t('platforms.copyKey')"
            />
            <KeyCopyBox
              v-if="p.oneTimeTestApiKey"
              :value="p.oneTimeTestApiKey"
              :label="$t('platforms.testKey')"
              hint="fx_test_"
              :copy-label="$t('platforms.copyKey')"
            />
            <p v-if="!p.oneTimeApiKey && !p.oneTimeTestApiKey" class="muted missing">
              {{ $t('platforms.fullKeyMissing') }}
            </p>
            <div class="optional-actions">
              <button
                v-if="!p.oneTimeApiKey && !p.oneTimeTestApiKey"
                class="btn"
                type="button"
                :disabled="savingId === p.id"
                @click="regenPlatform(p, true)"
              >{{ $t('platforms.generateKeys') }}</button>
              <template v-else>
                <button
                  class="btn ghost"
                  type="button"
                  :disabled="savingId === p.id"
                  @click="regenPlatform(p, false)"
                >{{ $t('platforms.regen') }}</button>
                <span class="muted regen-hint">{{ $t('platforms.regenHint') }}</span>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <h3>Webhook Secret</h3>
      <p class="muted">{{ $t('platforms.webhookHint') }}</p>
      <KeyCopyBox :value="secret" :copy-label="$t('platforms.copy')" />
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useDialog } from '../../composables/useDialog'
import KeyCopyBox from '../../components/KeyCopyBox.vue'

const { t } = useI18n()
const { confirm } = useDialog()
const platforms = ref([])
const merchantKey = ref(null)
const secret = ref('')
const saving = ref(false)
const savingId = ref('')
const merchantError = ref('')
const keysError = ref('')
const approvedPlatforms = computed(() => platforms.value.filter(p => p.status === 'Approved'))

async function copyBearer() {
  const value = merchantKey.value?.apiKey
  if (!value) return
  try { await navigator.clipboard.writeText('Bearer ' + value) } catch { /* ignore */ }
}

async function load() {
  keysError.value = ''
  const [s, m, p] = await Promise.all([
    api.get('/api/merchant/webhook-secret'),
    api.get('/api/merchant/merchant-key'),
    api.get('/api/merchant/platforms')
  ])
  secret.value = s.data.secret
  merchantKey.value = m.data
  platforms.value = p.data || []
}

async function claimMerchant() {
  merchantError.value = ''
  saving.value = true
  try {
    const { data } = await api.post('/api/merchant/merchant-key/claim')
    merchantKey.value = { ...merchantKey.value, apiKey: data.apiKey, isActive: true, canClaim: false, keyPrefix: data.keyPrefix }
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
    await api.post('/api/merchant/merchant-key/regenerate')
    await load()
  } catch (e) {
    merchantError.value = e.response?.data?.message || t('platforms.claimFail')
  } finally {
    saving.value = false
  }
}

async function regenPlatform(p, isCreate = false) {
  const ok = await confirm({
    variant: isCreate ? 'default' : 'danger',
    title: isCreate ? t('platforms.generateKeys') : t('dialog.dangerTitle'),
    message: isCreate ? t('platforms.generateConfirm') : t('platforms.regenConfirm'),
    confirmText: isCreate ? t('platforms.generateKeys') : t('platforms.regen')
  })
  if (!ok) return
  keysError.value = ''
  savingId.value = p.id
  try {
    const { data } = await api.post(`/api/merchant/platforms/${p.id}/regenerate-key`)
    const idx = platforms.value.findIndex((x) => x.id === p.id)
    if (idx >= 0) platforms.value[idx] = data
    else await load()
  } catch (e) {
    keysError.value = e.response?.data?.message || t('platforms.regenFail')
  } finally {
    savingId.value = ''
  }
}

onMounted(load)
</script>

<style scoped>
.hint-banner {
  margin: 0 0 16px;
  color: var(--muted);
  font-weight: 600;
  line-height: 1.55;
}
.keys-list { display: grid; gap: 12px; }
.key-item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 16px;
  flex-wrap: wrap;
  align-items: flex-start;
}
.key-body { flex: 1; min-width: 0; display: grid; gap: 10px; }
.key-title { display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
.domain { margin: 0; font-size: 0.85rem; }
.card > .optional-actions { margin-top: 14px; }
.optional-actions {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 10px;
  margin-top: 4px;
}
.regen-hint { font-size: 0.82rem; }
.missing { margin: 8px 0 0; }
.error { color: var(--danger); font-weight: 700; }
@media (max-width: 600px) {
  .optional-actions .btn { width: 100%; min-height: 44px; }
}
</style>
