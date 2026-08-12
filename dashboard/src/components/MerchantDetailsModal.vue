<template>
  <div class="modal-root" v-if="open" @keydown.esc="close">
    <div class="modal-backdrop" @click="close"></div>
    <div class="modal-panel" role="dialog" aria-modal="true">
      <header class="modal-head">
        <div class="head-main">
          <div class="avatar" aria-hidden="true">{{ initials }}</div>
          <div class="head-copy">
            <div class="head-title-row">
              <h2>{{ detail?.businessName || $t('merchants.detailsTitle') }}</h2>
              <span v-if="detail" class="badge" :class="detail.status === 'Active' ? 'ok' : 'warn'">
                {{ $t(`status.${detail.status}`, detail.status) }}
              </span>
            </div>
            <p class="muted mono id-line">{{ merchantId }}</p>
          </div>
        </div>
        <div class="head-actions">
          <div class="seg" role="tablist">
            <button type="button" role="tab" :aria-selected="tab === 'view'" :class="{ active: tab === 'view' }" @click="tab = 'view'">
              {{ $t('merchants.tabView') }}
            </button>
            <button type="button" role="tab" :aria-selected="tab === 'edit'" :class="{ active: tab === 'edit' }" @click="tab = 'edit'">
              {{ $t('merchants.tabEdit') }}
            </button>
          </div>
          <button class="icon-btn" type="button" :aria-label="$t('common.cancel')" @click="close">×</button>
        </div>
      </header>

      <div v-if="loading" class="state-box muted">{{ $t('common.loading') }}</div>
      <div v-else-if="error" class="state-box error">{{ error }}</div>

      <div v-else-if="detail" class="modal-body">
        <template v-if="tab === 'view'">
          <div class="metrics">
            <div class="metric">
              <span>{{ $t('merchants.balance') }}</span>
              <strong>{{ money(detail.availableBalance) }}</strong>
            </div>
            <div class="metric">
              <span>{{ $t('merchants.pendingBalance') }}</span>
              <strong>{{ money(detail.pendingBalance) }}</strong>
            </div>
            <div class="metric">
              <span>{{ $t('merchants.paymentsCount') }}</span>
              <strong>{{ detail.paymentsCount }}</strong>
            </div>
            <div class="metric">
              <span>{{ $t('merchants.commissions') }}</span>
              <strong>{{ commissionSummary }}</strong>
            </div>
          </div>

          <div class="sections">
          <section class="panel">
            <div class="panel-head">
              <h3>{{ $t('merchants.kycTitle') }}</h3>
              <span class="badge" :class="kycBadgeClass">{{ kycStatusLabel }}</span>
            </div>
            <p v-if="detail.kycAdminNotes" class="muted kyc-notes">{{ detail.kycAdminNotes }}</p>
            <div class="kyc-grid">
              <a
                v-for="doc in kycDocs"
                :key="doc.key"
                class="kyc-doc"
                :href="doc.url || undefined"
                :target="doc.url ? '_blank' : undefined"
                rel="noopener"
              >
                <span>{{ doc.label }}</span>
                <img v-if="doc.url" :src="mediaUrl(doc.url)" :alt="doc.label" />
                <em v-else>{{ $t('merchants.kycMissing') }}</em>
              </a>
            </div>
            <div v-if="canReviewKyc" class="kyc-actions">
              <textarea v-model="kycNotes" rows="2" :placeholder="$t('merchants.kycNotesPh')"></textarea>
              <div class="row-actions">
                <button class="btn sm" type="button" :disabled="kycBusy" @click="reviewKyc('Approve')">
                  {{ kycBusy ? $t('common.loading') : $t('merchants.kycApprove') }}
                </button>
                <button class="btn secondary sm" type="button" :disabled="kycBusy" @click="reviewKyc('Reject')">
                  {{ $t('merchants.kycReject') }}
                </button>
              </div>
              <p v-if="kycError" class="error tiny">{{ kycError }}</p>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionBusiness') }}</h3>
              <dl class="kv">
                <div><dt>{{ $t('merchants.business') }}</dt><dd>{{ detail.businessName }}</dd></div>
                <div><dt>{{ $t('merchants.businessAr') }}</dt><dd>{{ detail.businessNameAr || '—' }}</dd></div>
                <div><dt>{{ $t('merchants.email') }}</dt><dd>{{ detail.contactEmail }}</dd></div>
                <div><dt>{{ $t('merchants.phone') }}</dt><dd>{{ detail.contactPhone || '—' }}</dd></div>
                <div><dt>{{ $t('merchants.website') }}</dt><dd>{{ detail.websiteUrl || '—' }}</dd></div>
                <div><dt>{{ $t('merchants.notes') }}</dt><dd>{{ detail.notes || '—' }}</dd></div>
              </dl>
            </section>

            <section class="panel">
              <h3>{{ $t('merchants.sectionWallet') }}</h3>
              <dl class="kv">
                <div><dt>{{ $t('merchants.lifetimeGross') }}</dt><dd>{{ money(detail.lifetimeGross) }}</dd></div>
                <div><dt>{{ $t('merchants.lifetimeFees') }}</dt><dd>{{ money(detail.lifetimeFees) }}</dd></div>
                <div><dt>{{ $t('merchants.apiKeysCount') }}</dt><dd>{{ detail.apiKeysCount }}</dd></div>
                <div><dt>{{ $t('merchants.createdAt') }}</dt><dd>{{ when(detail.createdAtUtc) }}</dd></div>
                <div><dt>{{ $t('merchants.updatedAt') }}</dt><dd>{{ when(detail.updatedAtUtc) }}</dd></div>
              </dl>
            </section>
          </div>

          <section class="panel">
            <div class="panel-head">
              <h3>{{ $t('merchants.webhookSecret') }}</h3>
              <div class="row-actions">
                <button class="btn secondary sm" type="button" @click="showSecret = !showSecret">
                  {{ showSecret ? $t('merchants.hide') : $t('merchants.show') }}
                </button>
                <button class="btn secondary sm" type="button" @click="copy(detail.webhookSecret)">{{ $t('payments.copy') }}</button>
              </div>
            </div>
            <code class="secret mono">{{ showSecret ? detail.webhookSecret : '••••••••••••••••••••••••••••••••' }}</code>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionCommissions') }}</h3>
            <div class="provider-grid">
              <div v-for="c in commissionView" :key="c.key" class="provider-card on">
                <ProviderBadge :provider="c.provider" :show-name="false" />
                <small>{{ formatPct(c.rate) }}%</small>
              </div>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionProviders') }}</h3>
            <div class="provider-grid">
              <div v-for="p in providerView" :key="p.key" class="provider-card" :class="{ on: p.on }">
                <span class="dot" />
                <ProviderBadge :provider="p.provider" :show-name="false" />
                <small>{{ p.on ? $t('common.enabled') : $t('common.disabled') }}</small>
              </div>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionOwners') }}</h3>
            <div v-if="detail.owners?.length" class="owners">
              <article v-for="o in detail.owners" :key="o.id" class="owner">
                <div class="owner-avatar">{{ (o.fullName || '?').slice(0, 1) }}</div>
                <div class="owner-meta">
                  <strong>{{ o.fullNameAr || o.fullName }}</strong>
                  <span v-if="o.fullNameAr && o.fullName" class="muted">{{ o.fullName }}</span>
                  <span class="mono">{{ o.email }}</span>
                  <span class="muted">{{ o.phone || '—' }}</span>
                </div>
                <span class="badge" :class="o.isActive ? 'ok' : 'warn'">
                  {{ o.isActive ? $t('common.enabled') : $t('common.disabled') }}
                </span>
              </article>
            </div>
            <p v-else class="muted empty">{{ $t('merchants.noOwners') }}</p>
          </section>

          <section class="danger-zone">
            <div>
              <h3>{{ $t('merchants.deleteTitle') }}</h3>
              <p>{{ $t('merchants.deleteHint') }}</p>
            </div>
            <button class="btn danger" type="button" :disabled="deleting" @click="confirmDelete">
              {{ deleting ? $t('common.loading') : $t('merchants.deleteForever') }}
            </button>
          </section>
        </template>

        <form v-else class="edit-form" @submit.prevent="save">
          <section class="panel">
            <h3>{{ $t('merchants.sectionBusiness') }}</h3>
            <div class="form-grid">
              <label class="field">
                <span>{{ $t('merchants.business') }}</span>
                <input v-model="form.businessName" required />
              </label>
              <label class="field">
                <span>{{ $t('merchants.businessAr') }}</span>
                <input v-model="form.businessNameAr" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.email') }}</span>
                <input v-model="form.contactEmail" type="email" required />
              </label>
              <label class="field">
                <span>{{ $t('merchants.phone') }}</span>
                <input v-model="form.contactPhone" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.website') }}</span>
                <input v-model="form.websiteUrl" />
              </label>
              <label class="field">
                <span>{{ $t('common.status') }}</span>
                <select v-model="form.status">
                  <option value="Active">{{ $t('status.Active') }}</option>
                  <option value="Suspended">{{ $t('status.Suspended') }}</option>
                  <option value="Pending">{{ $t('status.Pending') }}</option>
                </select>
              </label>
              <label class="field full">
                <span>{{ $t('merchants.notes') }}</span>
                <textarea v-model="form.notes" rows="3" />
              </label>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionCommissions') }}</h3>
            <p class="muted commission-hint">{{ $t('merchants.commissionsHint') }}</p>
            <div class="form-grid">
              <label class="field" v-for="c in commissionEdit" :key="c.key">
                <span class="field-logo"><ProviderBadge :provider="c.provider" :show-name="false" /></span>
                <div class="pct-input">
                  <input v-model.number="form[c.key]" type="number" min="0" max="100" step="0.1" required />
                  <span>%</span>
                </div>
              </label>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionProviders') }}</h3>
            <div class="provider-grid edit">
              <button
                v-for="p in providerEdit"
                :key="p.key"
                type="button"
                class="provider-card clickable"
                :class="{ on: form[p.key] }"
                @click="form[p.key] = !form[p.key]"
              >
                <span class="dot" />
                <ProviderBadge :provider="p.provider" :show-name="false" />
                <small>{{ form[p.key] ? $t('common.enabled') : $t('common.disabled') }}</small>
              </button>
            </div>
          </section>

          <section class="panel">
            <h3>{{ $t('merchants.sectionOwnerEdit') }}</h3>
            <div class="form-grid">
              <label class="field">
                <span>{{ $t('merchants.ownerNameAr') }}</span>
                <input v-model="form.ownerFullNameAr" dir="rtl" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.ownerName') }}</span>
                <input v-model="form.ownerFullName" dir="ltr" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.ownerEmail') }}</span>
                <input v-model="form.ownerEmail" type="email" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.ownerPhone') }}</span>
                <input v-model="form.ownerPhone" />
              </label>
              <label class="field">
                <span>{{ $t('merchants.newPassword') }}</span>
                <input
                  v-model="form.newPassword"
                  type="password"
                  autocomplete="new-password"
                  :placeholder="$t('merchants.passwordPlaceholder')"
                />
              </label>
            </div>
          </section>

          <p v-if="saveError" class="error msg">{{ saveError }}</p>
          <p v-if="saveOk" class="ok-msg msg">{{ $t('merchants.saveOk') }}</p>

          <footer class="form-footer">
            <button class="btn secondary" type="button" @click="tab = 'view'">{{ $t('common.cancel') }}</button>
            <button class="btn" type="submit" :disabled="saving">
              {{ saving ? $t('common.loading') : $t('common.save') }}
            </button>
          </footer>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, reactive, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../api'
import ProviderBadge from './ProviderBadge.vue'
import { useDialog } from '../composables/useDialog'

const props = defineProps({
  open: Boolean,
  merchantId: { type: String, default: '' }
})
const emit = defineEmits(['close', 'changed'])

const { t, locale } = useI18n()
const { confirm } = useDialog()
const detail = ref(null)
const loading = ref(false)
const error = ref('')
const tab = ref('view')
const showSecret = ref(false)
const saving = ref(false)
const deleting = ref(false)
const saveError = ref('')
const saveOk = ref(false)
const kycNotes = ref('')
const kycBusy = ref(false)
const kycError = ref('')

const kycDocs = computed(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'front', label: t('merchants.kycIdFront'), url: d.kycIdFrontUrl },
    { key: 'back', label: t('merchants.kycIdBack'), url: d.kycIdBackUrl },
    { key: 'passport', label: t('merchants.kycPassport'), url: d.kycPassportUrl }
  ]
})
const kycStatusLabel = computed(() => t(`profile.kycStatus.${detail.value?.kycStatus || 'None'}`, detail.value?.kycStatus || 'None'))
const kycBadgeClass = computed(() => {
  const s = detail.value?.kycStatus
  if (s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  if (s === 'Rejected') return 'danger'
  return ''
})
const canReviewKyc = computed(() => {
  const d = detail.value
  return d && d.kycStatus === 'Pending' && d.kycIdFrontUrl && d.kycIdBackUrl && d.kycPassportUrl
})

function mediaUrl(path) {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  return `${API_BASE}${path.startsWith('/') ? '' : '/'}${path}`
}

async function reviewKyc(action) {
  if (!props.merchantId) return
  kycBusy.value = true
  kycError.value = ''
  try {
    const { data } = await api.post(`/api/admin/merchants/${props.merchantId}/kyc/review`, {
      action,
      notes: kycNotes.value || null
    })
    if (detail.value) {
      detail.value.kycStatus = data.status
      detail.value.kycAdminNotes = data.adminNotes
      detail.value.kycReviewedAtUtc = data.reviewedAtUtc
    }
    kycNotes.value = ''
    emit('changed')
  } catch (e) {
    kycError.value = e.response?.data?.message || t('merchants.kycReviewFail')
  } finally {
    kycBusy.value = false
  }
}

const form = reactive({
  businessName: '',
  businessNameAr: '',
  contactEmail: '',
  contactPhone: '',
  websiteUrl: '',
  commissionPercent: 0,
  fibCommissionPercent: 2.5,
  zainCashCommissionPercent: 2.5,
  qiCommissionPercent: 2.5,
  superQiCommissionPercent: 2.5,
  alqasehCommissionPercent: 2.5,
  status: 'Active',
  notes: '',
  fibEnabled: true,
  zainCashEnabled: true,
  qiEnabled: true,
  superQiEnabled: true,
  alqasehEnabled: true,
  ownerFullName: '',
  ownerFullNameAr: '',
  ownerEmail: '',
  ownerPhone: '',
  newPassword: ''
})

const initials = computed(() => {
  const name = detail.value?.businessName || '?'
  return name.trim().slice(0, 1).toUpperCase()
})

const commissionView = computed(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'fib', provider: 'Fib', rate: d.fibCommissionPercent ?? d.commissionPercent },
    { key: 'zain', provider: 'ZainCash', rate: d.zainCashCommissionPercent ?? d.commissionPercent },
    { key: 'qi', provider: 'Qi', rate: d.qiCommissionPercent ?? d.commissionPercent },
    { key: 'superqi', provider: 'SuperQi', rate: d.superQiCommissionPercent ?? d.commissionPercent },
    { key: 'alqaseh', provider: 'Alqaseh', rate: d.alqasehCommissionPercent ?? d.commissionPercent }
  ]
})

const commissionSummary = computed(() => {
  const rates = commissionView.value.map((c) => Number(c.rate ?? 0))
  if (!rates.length) return '—'
  const min = Math.min(...rates)
  const max = Math.max(...rates)
  if (min === max) return `${formatPct(min)}%`
  return `${formatPct(min)}% – ${formatPct(max)}%`
})

const commissionEdit = [
  { key: 'fibCommissionPercent', provider: 'Fib' },
  { key: 'zainCashCommissionPercent', provider: 'ZainCash' },
  { key: 'qiCommissionPercent', provider: 'Qi' },
  { key: 'superQiCommissionPercent', provider: 'SuperQi' },
  { key: 'alqasehCommissionPercent', provider: 'Alqaseh' }
]

const providerView = computed(() => {
  const d = detail.value
  if (!d) return []
  return [
    { key: 'fib', provider: 'Fib', on: d.fibEnabled },
    { key: 'zain', provider: 'ZainCash', on: d.zainCashEnabled },
    { key: 'qi', provider: 'Qi', on: d.qiEnabled },
    { key: 'superqi', provider: 'SuperQi', on: d.superQiEnabled },
    { key: 'alqaseh', provider: 'Alqaseh', on: d.alqasehEnabled }
  ]
})

const providerEdit = [
  { key: 'fibEnabled', provider: 'Fib' },
  { key: 'zainCashEnabled', provider: 'ZainCash' },
  { key: 'qiEnabled', provider: 'Qi' },
  { key: 'superQiEnabled', provider: 'SuperQi' },
  { key: 'alqasehEnabled', provider: 'Alqaseh' }
]

function formatPct(v) {
  const n = Number(v ?? 0)
  return Number.isInteger(n) ? String(n) : n.toFixed(1)
}

function close() { emit('close') }

function money(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB')
}
async function copy(text) {
  try { await navigator.clipboard.writeText(text) } catch { /* ignore */ }
}

function fillForm(d) {
  const owner = d.owners?.[0]
  form.businessName = d.businessName || ''
  form.businessNameAr = d.businessNameAr || ''
  form.contactEmail = d.contactEmail || ''
  form.contactPhone = d.contactPhone || ''
  form.websiteUrl = d.websiteUrl || ''
  form.commissionPercent = d.commissionPercent ?? 0
  form.fibCommissionPercent = d.fibCommissionPercent ?? d.commissionPercent ?? 0
  form.zainCashCommissionPercent = d.zainCashCommissionPercent ?? d.commissionPercent ?? 0
  form.qiCommissionPercent = d.qiCommissionPercent ?? d.commissionPercent ?? 0
  form.superQiCommissionPercent = d.superQiCommissionPercent ?? d.commissionPercent ?? 0
  form.alqasehCommissionPercent = d.alqasehCommissionPercent ?? d.commissionPercent ?? 0
  form.status = d.status || 'Active'
  form.notes = d.notes || ''
  form.fibEnabled = !!d.fibEnabled
  form.zainCashEnabled = !!d.zainCashEnabled
  form.qiEnabled = !!d.qiEnabled
  form.superQiEnabled = !!d.superQiEnabled
  form.alqasehEnabled = !!d.alqasehEnabled
  form.ownerFullName = owner?.fullName || ''
  form.ownerFullNameAr = owner?.fullNameAr || ''
  form.ownerEmail = owner?.email || ''
  form.ownerPhone = owner?.phone || ''
  form.newPassword = ''
}

async function load() {
  if (!props.merchantId) return
  loading.value = true
  error.value = ''
  detail.value = null
  showSecret.value = false
  saveError.value = ''
  saveOk.value = false
  tab.value = 'view'
  try {
    const { data } = await api.get(`/api/admin/merchants/${props.merchantId}`)
    detail.value = data
    fillForm(data)
  } catch (e) {
    error.value = e.response?.data?.message || t('merchants.detailsFail')
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  saveError.value = ''
  saveOk.value = false
  try {
    const rates = [
      form.fibCommissionPercent,
      form.zainCashCommissionPercent,
      form.qiCommissionPercent,
      form.superQiCommissionPercent,
      form.alqasehCommissionPercent
    ].map((n) => Number(n ?? 0))
    const avg = rates.reduce((a, b) => a + b, 0) / (rates.length || 1)

    await api.patch(`/api/admin/merchants/${props.merchantId}`, {
      businessName: form.businessName,
      businessNameAr: form.businessNameAr || null,
      contactEmail: form.contactEmail,
      contactPhone: form.contactPhone || null,
      websiteUrl: form.websiteUrl || null,
      commissionPercent: Math.round(avg * 100) / 100,
      fibCommissionPercent: form.fibCommissionPercent,
      zainCashCommissionPercent: form.zainCashCommissionPercent,
      qiCommissionPercent: form.qiCommissionPercent,
      superQiCommissionPercent: form.superQiCommissionPercent,
      alqasehCommissionPercent: form.alqasehCommissionPercent,
      status: form.status,
      notes: form.notes || null,
      fibEnabled: form.fibEnabled,
      zainCashEnabled: form.zainCashEnabled,
      qiEnabled: form.qiEnabled,
      superQiEnabled: form.superQiEnabled,
      alqasehEnabled: form.alqasehEnabled,
      ownerFullName: form.ownerFullName || null,
      ownerFullNameAr: form.ownerFullNameAr || null,
      ownerEmail: form.ownerEmail || null,
      ownerPhone: form.ownerPhone || null,
      newPassword: form.newPassword || null
    })
    saveOk.value = true
    form.newPassword = ''
    emit('changed')
    await load()
    tab.value = 'view'
  } catch (e) {
    saveError.value = e.response?.data?.message || t('merchants.saveFail')
  } finally {
    saving.value = false
  }
}

async function confirmDelete() {
  const ok = await confirm({
    variant: 'danger',
    title: t('merchants.deleteTitle'),
    message: t('merchants.deleteConfirm'),
    confirmText: t('merchants.deleteForever')
  })
  if (!ok) return
  deleting.value = true
  try {
    await api.delete(`/api/admin/merchants/${props.merchantId}`)
    emit('changed')
    close()
  } catch (e) {
    error.value = e.response?.data?.message || t('merchants.deleteFail')
  } finally {
    deleting.value = false
  }
}

watch(
  () => [props.open, props.merchantId],
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
  width: min(980px, 100%);
  max-height: min(92vh, 960px);
  overflow: auto;
  background:
    radial-gradient(1200px 280px at 100% -10%, rgba(3, 24, 56, 0.12), transparent 55%),
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
.avatar {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  font-weight: 800;
  font-size: 1.15rem;
  color: #fff;
  background: linear-gradient(145deg, var(--brand), var(--brand-secondary));
  box-shadow: 0 10px 24px rgba(3, 24, 56, 0.28);
  flex-shrink: 0;
}
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
.head-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-shrink: 0;
}
.seg {
  display: inline-flex;
  padding: 4px;
  border-radius: 12px;
  background: #f1f4f9;
  border: 1px solid var(--line);
  gap: 2px;
}
.seg button {
  border: 0;
  background: transparent;
  color: var(--muted);
  font-weight: 700;
  padding: 8px 16px;
  border-radius: 10px;
  transition: 0.15s ease;
}
.seg button.active {
  background: var(--brand);
  color: #fff;
  box-shadow: 0 6px 16px rgba(3, 24, 56, 0.18);
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
}
.icon-btn:hover { background: #f8fafc; }
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
.panel-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin-bottom: 12px;
}
.panel-head h3 { margin: 0; }
.row-actions { display: flex; gap: 8px; flex-wrap: wrap; }
.btn.sm {
  padding: 7px 12px;
  font-size: 0.8rem;
  border-radius: 10px;
  box-shadow: none;
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

.secret {
  display: block;
  width: 100%;
  padding: 12px 14px;
  border-radius: 12px;
  background: #031838;
  color: #e2e8f0;
  font-size: 0.82rem;
  direction: ltr;
  text-align: left;
  overflow: auto;
}

.provider-grid {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}
.provider-card {
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 12px;
  background: #f8fafc;
  display: grid;
  gap: 4px;
  text-align: start;
  color: var(--muted);
}
.provider-card.on {
  background: var(--brand-soft);
  border-color: rgba(3, 24, 56, 0.28);
  color: var(--brand);
}
.provider-card .dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #cbd5e1;
  margin-bottom: 4px;
}
.provider-card.on .dot { background: var(--brand-secondary); }
.provider-card strong { font-size: 0.9rem; }
.provider-card small { font-size: 0.75rem; opacity: 0.85; }
.provider-card.clickable {
  cursor: pointer;
  font: inherit;
  transition: 0.15s ease;
}
.provider-card.clickable:hover {
  border-color: rgba(3, 24, 56, 0.4);
  transform: translateY(-1px);
}

.owners { display: grid; gap: 10px; }
.owner {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  border: 1px solid var(--line);
  border-radius: 14px;
  background: #f8fafc;
}
.owner-avatar {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  background: var(--brand);
  color: #fff;
  font-weight: 800;
  flex-shrink: 0;
}
.owner-meta {
  display: grid;
  gap: 2px;
  flex: 1;
  min-width: 0;
}
.owner-meta strong { color: var(--brand); }
.owner-meta .mono { font-size: 0.82rem; }
.empty { margin: 0; }

.danger-zone {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  border: 1px solid #fecaca;
  background: linear-gradient(180deg, #fff7f7, #fff);
  border-radius: 16px;
  padding: 16px 18px;
}
.danger-zone h3 {
  margin: 0 0 4px;
  color: var(--danger);
  font-size: 0.95rem;
}
.danger-zone p {
  margin: 0;
  color: var(--muted);
  font-size: 0.85rem;
  max-width: 520px;
}

.edit-form { display: grid; gap: 0; }
.form-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 4px 14px;
}
.form-grid .field {
  margin-bottom: 10px;
}
.form-grid .field > span {
  color: var(--muted);
  font-size: 0.82rem;
  font-weight: 700;
}
.form-grid .field.full { grid-column: 1 / -1; }
.commission-hint { margin: 0 0 12px; font-size: 0.85rem; }
.pct-input {
  display: flex;
  align-items: center;
  gap: 8px;
}
.pct-input input { flex: 1; }
.pct-input span {
  font-weight: 800;
  color: var(--muted);
}
.form-grid input,
.form-grid select,
.form-grid textarea {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  background: #fff;
  color: var(--ink);
  outline: none;
  transition: 0.15s ease;
  width: 100%;
}
.form-grid input:focus,
.form-grid select:focus,
.form-grid textarea:focus {
  border-color: rgba(3, 24, 56, 0.55);
  box-shadow: 0 0 0 4px rgba(3, 24, 56, 0.14);
}
.form-footer {
  position: sticky;
  bottom: 0;
  display: flex;
  justify-content: flex-end;
  gap: 10px;
  padding: 14px 0 2px;
  margin-top: 4px;
  background: linear-gradient(180deg, rgba(255,255,255,0), #fff 28%);
}
.msg { margin: 0 0 8px; font-weight: 700; }
.ok-msg { color: #15803d; }
.kyc-notes { margin: 0 0 12px; }
.kyc-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}
.kyc-doc {
  display: grid;
  gap: 8px;
  padding: 10px;
  border: 1px solid var(--line);
  border-radius: 12px;
  background: #f8fafc;
  text-decoration: none;
  color: inherit;
}
.kyc-doc span { font-size: 0.8rem; font-weight: 700; color: var(--muted); }
.kyc-doc img {
  width: 100%;
  aspect-ratio: 4 / 3;
  object-fit: cover;
  border-radius: 10px;
}
.kyc-doc em { color: var(--muted); font-style: normal; font-size: 0.85rem; }
.kyc-actions { display: grid; gap: 10px; }
.kyc-actions textarea {
  width: 100%;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 10px 12px;
  font: inherit;
  resize: vertical;
}

@media (max-width: 900px) {
  .metrics { grid-template-columns: 1fr 1fr; }
  .sections, .form-grid, .provider-grid, .kyc-grid { grid-template-columns: 1fr; }
  .modal-head { flex-direction: column; align-items: stretch; }
  .head-actions { justify-content: space-between; }
  .kv > div { grid-template-columns: 1fr; gap: 2px; }
  .danger-zone { flex-direction: column; align-items: stretch; }
}
@media (max-width: 560px) {
  .metrics { grid-template-columns: 1fr; }
  .provider-grid { grid-template-columns: 1fr 1fr; }
}
</style>
