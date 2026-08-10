<template>
  <div class="modal-root" v-if="open" @keydown.esc="close">
    <div class="modal-backdrop" @click="close"></div>
    <div class="modal-panel" role="dialog" aria-modal="true" :aria-label="$t('platforms.detailsTitle')">
      <header class="modal-head">
        <div class="head-main">
          <div class="logo-frame" :class="{ empty: !detail?.logoUrl }">
            <img v-if="detail?.logoUrl" :src="logoSrc(detail.logoUrl)" :alt="detail.name" width="48" height="48" />
            <span v-else>{{ initials }}</span>
          </div>
          <div class="head-copy">
            <div class="head-title-row">
              <h2>{{ detail?.name || $t('platforms.detailsTitle') }}</h2>
              <span v-if="detail" class="badge" :class="statusClass(detail.status)">
                {{ $t(`status.${detail.status}`, detail.status) }}
              </span>
            </div>
            <p class="muted mono id-line" dir="ltr">{{ detail?.domain || platformId }}</p>
          </div>
        </div>
        <button class="icon-btn" type="button" :aria-label="$t('common.cancel')" @click="close">×</button>
      </header>

      <div v-if="loading" class="state-box muted">{{ $t('common.loading') }}</div>
      <div v-else-if="error" class="state-box error">{{ error }}</div>

      <div v-else-if="detail" class="modal-body">
        <div class="metrics">
          <div class="metric">
            <span>{{ $t('platforms.paymentsCount') }}</span>
            <strong>{{ detail.paymentsCount }}</strong>
          </div>
          <div class="metric">
            <span>{{ $t('platforms.paymentsVolume') }}</span>
            <strong>{{ money(detail.paymentsVolume) }}</strong>
          </div>
          <div class="metric">
            <span>{{ $t('platforms.apiKeyStatus') }}</span>
            <strong>{{ detail.apiKeyIsActive ? $t('common.enabled') : $t('common.disabled') }}</strong>
          </div>
          <div class="metric">
            <span>{{ $t('platforms.hasPendingKey') }}</span>
            <strong>{{ detail.hasOneTimeApiKey ? $t('common.yes') : $t('common.no') }}</strong>
          </div>
        </div>

        <div class="sections">
          <section class="panel">
            <h3>{{ $t('platforms.sectionPlatform') }}</h3>
            <dl class="kv">
              <div><dt>{{ $t('platforms.name') }}</dt><dd>{{ detail.name }}</dd></div>
              <div><dt>{{ $t('platforms.domain') }}</dt><dd class="mono" dir="ltr">{{ detail.domain }}</dd></div>
              <div><dt>{{ $t('common.status') }}</dt><dd>{{ $t(`status.${detail.status}`, detail.status) }}</dd></div>
              <div><dt>{{ $t('platforms.createdAt') }}</dt><dd>{{ when(detail.createdAtUtc) }}</dd></div>
              <div><dt>{{ $t('platforms.updatedAt') }}</dt><dd>{{ when(detail.updatedAtUtc) }}</dd></div>
              <div><dt>{{ $t('platforms.reviewedAt') }}</dt><dd>{{ when(detail.reviewedAtUtc) }}</dd></div>
              <div><dt>{{ $t('platforms.reviewedBy') }}</dt><dd>{{ detail.reviewedByName || '—' }}</dd></div>
              <div><dt>{{ $t('platforms.adminNotes') }}</dt><dd>{{ detail.adminNotes || '—' }}</dd></div>
            </dl>
          </section>

          <section class="panel">
            <h3>{{ $t('platforms.sectionMerchant') }}</h3>
            <dl class="kv">
              <div><dt>{{ $t('merchants.business') }}</dt><dd>{{ detail.merchantName || '—' }}</dd></div>
              <div><dt>{{ $t('merchants.email') }}</dt><dd>{{ detail.merchantEmail || '—' }}</dd></div>
              <div><dt>{{ $t('merchants.phone') }}</dt><dd>{{ detail.merchantPhone || '—' }}</dd></div>
              <div>
                <dt>{{ $t('platforms.merchantStatus') }}</dt>
                <dd>
                  <span v-if="detail.merchantStatus" class="badge" :class="detail.merchantStatus === 'Active' ? 'ok' : 'warn'">
                    {{ $t(`status.${detail.merchantStatus}`, detail.merchantStatus) }}
                  </span>
                  <template v-else>—</template>
                </dd>
              </div>
              <div>
                <dt>{{ $t('platforms.merchantId') }}</dt>
                <dd class="mono id-break">{{ detail.merchantId }}</dd>
              </div>
            </dl>
            <RouterLink class="btn secondary sm merchant-link" :to="`/admin/merchants`">
              {{ $t('platforms.openMerchants') }}
            </RouterLink>
          </section>
        </div>

        <section class="panel">
          <h3>{{ $t('platforms.sectionApiKey') }}</h3>
          <dl class="kv">
            <div><dt>{{ $t('platforms.keyPrefix') }}</dt><dd class="mono" dir="ltr">{{ detail.apiKeyPrefix || '—' }}</dd></div>
            <div><dt>{{ $t('platforms.apiKeyStatus') }}</dt><dd>{{ detail.apiKeyIsActive ? $t('common.enabled') : $t('common.disabled') }}</dd></div>
            <div><dt>{{ $t('platforms.keyCreatedAt') }}</dt><dd>{{ when(detail.apiKeyCreatedAtUtc) }}</dd></div>
            <div><dt>{{ $t('platforms.platformId') }}</dt><dd class="mono id-break">{{ detail.id }}</dd></div>
          </dl>
        </section>

        <section class="panel review-panel">
          <h3>{{ $t('platforms.reviewActions') }}</h3>
          <label class="field">
            <span>{{ $t('platforms.adminNotes') }}</span>
            <textarea v-model="notes" rows="3" :placeholder="$t('platforms.adminNotesPh')" />
          </label>
          <p v-if="reviewMsg" class="ok-msg">{{ reviewMsg }}</p>
          <p v-if="reviewError" class="error">{{ reviewError }}</p>
          <div class="review-actions">
            <button class="btn" type="button" :disabled="reviewing" @click="review('approve')">{{ $t('platforms.approve') }}</button>
            <button class="btn secondary" type="button" :disabled="reviewing" @click="review('suspend')">{{ $t('platforms.suspend') }}</button>
            <button class="btn danger" type="button" :disabled="reviewing" @click="review('reject')">{{ $t('platforms.reject') }}</button>
          </div>
          <p v-if="oneTimeKey" class="key-hint mono" dir="ltr">API: {{ oneTimeKey }}</p>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../api'

const props = defineProps({
  open: Boolean,
  platformId: { type: String, default: '' }
})
const emit = defineEmits(['close', 'changed'])

const { t, locale } = useI18n()
const detail = ref(null)
const loading = ref(false)
const error = ref('')
const notes = ref('')
const reviewing = ref(false)
const reviewMsg = ref('')
const reviewError = ref('')
const oneTimeKey = ref('')

const initials = computed(() => {
  const n = detail.value?.name || '?'
  return n.trim().slice(0, 1).toUpperCase()
})

function statusClass(s) {
  if (s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
function money(v) {
  return new Intl.NumberFormat(locale.value === 'ar' ? 'ar-IQ' : 'en-IQ', {
    style: 'currency',
    currency: 'IQD',
    maximumFractionDigits: 0
  }).format(Number(v || 0))
}
function logoSrc(url) {
  if (!url) return ''
  if (url.startsWith('http')) return url
  return `${API_BASE}${url}`
}
function close() {
  emit('close')
}

async function load() {
  if (!props.platformId) return
  loading.value = true
  error.value = ''
  reviewMsg.value = ''
  reviewError.value = ''
  oneTimeKey.value = ''
  try {
    const { data } = await api.get(`/api/admin/platforms/${props.platformId}`)
    detail.value = data
    notes.value = data.adminNotes || ''
  } catch (e) {
    detail.value = null
    error.value = e.response?.data?.message || t('platforms.detailsFail')
  } finally {
    loading.value = false
  }
}

async function review(action) {
  reviewing.value = true
  reviewMsg.value = ''
  reviewError.value = ''
  try {
    const { data } = await api.patch(`/api/admin/platforms/${props.platformId}`, {
      action,
      adminNotes: notes.value || null
    })
    if (data.oneTimeApiKey) {
      oneTimeKey.value = data.oneTimeApiKey
      reviewMsg.value = t('platforms.approvedWithKey')
    } else {
      reviewMsg.value = t('platforms.reviewOk')
    }
    await load()
    emit('changed')
  } catch (e) {
    reviewError.value = e.response?.data?.message || t('platforms.reviewFail')
  } finally {
    reviewing.value = false
  }
}

watch(
  () => [props.open, props.platformId],
  ([open]) => {
    if (open) load()
  },
  { immediate: true }
)
</script>

<style scoped>
.modal-root {
  position: fixed;
  inset: 0;
  z-index: 80;
  display: grid;
  place-items: center;
  padding: 20px;
}
.modal-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(3, 24, 56, 0.52);
  backdrop-filter: blur(6px);
}
.modal-panel {
  position: relative;
  width: min(920px, 100%);
  max-height: min(92vh, 960px);
  overflow: auto;
  background:
    radial-gradient(1200px 280px at 100% -10%, rgba(108, 60, 236, 0.12), transparent 55%),
    #fff;
  border-radius: 22px;
  border: 1px solid var(--line);
  box-shadow: 0 28px 80px rgba(3, 24, 56, 0.28);
  display: flex;
  flex-direction: column;
}
.modal-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  padding: 18px 22px;
  border-bottom: 1px solid var(--line);
  position: sticky;
  top: 0;
  z-index: 2;
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(8px);
}
.head-main {
  display: flex;
  align-items: center;
  gap: 14px;
  min-width: 0;
}
.logo-frame {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  border: 1px solid var(--line);
  display: grid;
  place-items: center;
  overflow: hidden;
  flex-shrink: 0;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(145deg, var(--brand), var(--brand-secondary));
}
.logo-frame.empty { color: #fff; }
.logo-frame img { width: 100%; height: 100%; object-fit: contain; background: #fff; }
.head-copy { min-width: 0; }
.head-title-row {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px;
}
.head-title-row h2 {
  margin: 0;
  font-size: 1.2rem;
  letter-spacing: -0.02em;
  color: var(--brand);
}
.id-line {
  margin: 4px 0 0;
  font-size: 0.78rem;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 420px;
}
.icon-btn {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  border: 1px solid var(--line);
  background: #fff;
  color: var(--brand);
  font-size: 1.4rem;
  line-height: 1;
  display: grid;
  place-items: center;
  flex-shrink: 0;
}
.modal-body { padding: 20px 22px 22px; }
.state-box { padding: 40px 22px; text-align: center; }

.metrics {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-bottom: 16px;
}
.metric {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 16px;
  padding: 14px 16px;
  box-shadow: var(--shadow-sm);
  display: grid;
  gap: 6px;
}
.metric span {
  color: var(--muted);
  font-size: 0.78rem;
  font-weight: 700;
}
.metric strong {
  color: var(--brand);
  font-size: 1.05rem;
  font-variant-numeric: tabular-nums;
}

.sections {
  display: grid;
  grid-template-columns: 1.1fr 0.9fr;
  gap: 12px;
  margin-bottom: 12px;
}
.panel {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 16px;
  padding: 16px 18px;
  margin-bottom: 12px;
  box-shadow: var(--shadow-sm);
}
.panel h3 {
  margin: 0 0 14px;
  font-size: 0.92rem;
  color: var(--brand);
  font-weight: 800;
}
.kv {
  margin: 0;
  display: grid;
  gap: 10px;
}
.kv > div {
  display: grid;
  grid-template-columns: 120px 1fr;
  gap: 10px;
  align-items: start;
  padding-bottom: 10px;
  border-bottom: 1px dashed var(--line);
}
.kv > div:last-child {
  border-bottom: 0;
  padding-bottom: 0;
}
dt {
  margin: 0;
  color: var(--muted);
  font-size: 0.78rem;
  font-weight: 700;
}
dd {
  margin: 0;
  font-weight: 700;
  color: var(--ink);
  word-break: break-word;
}
.id-break { font-size: 0.78rem; word-break: break-all; }
.merchant-link {
  margin-top: 14px;
  display: inline-flex;
}
.btn.sm {
  padding: 7px 12px;
  font-size: 0.8rem;
  border-radius: 10px;
  box-shadow: none;
}

.review-panel .field { margin: 0 0 12px; }
.review-panel textarea {
  width: 100%;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  font: inherit;
  resize: vertical;
  margin-top: 6px;
}
.review-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.key-hint {
  margin: 12px 0 0;
  padding: 8px 10px;
  background: #021225;
  color: #e2e8f0;
  border-radius: 10px;
  font-size: 0.8rem;
  overflow: auto;
}
.ok-msg { color: #15803d; font-weight: 700; margin: 0 0 10px; }

@media (max-width: 900px) {
  .metrics { grid-template-columns: 1fr 1fr; }
  .sections { grid-template-columns: 1fr; }
  .kv > div { grid-template-columns: 1fr; gap: 2px; }
}
@media (max-width: 560px) {
  .metrics { grid-template-columns: 1fr; }
}
</style>
