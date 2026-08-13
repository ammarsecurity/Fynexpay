<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('profile.title') }}</h1>
        <p class="sub">{{ $t('profile.merchantSub') }}</p>
      </div>
    </div>

    <div class="card">
      <div v-if="step === 'form'" class="wa-hint">
        <i class="bi bi-whatsapp" aria-hidden="true"></i>
        <span>{{ $t('profile.whatsappHint') }}</span>
      </div>

      <form v-if="step === 'form'" class="profile-form" @submit.prevent="requestOtp">
        <label class="field">
          <span>{{ $t('auth.fullNameAr') }}</span>
          <input v-model="form.fullNameAr" required autocomplete="name" dir="rtl" />
        </label>
        <label class="field">
          <span>{{ $t('auth.fullNameEn') }}</span>
          <input v-model="form.fullName" required autocomplete="name" dir="ltr" />
        </label>
        <label class="field">
          <span>{{ $t('auth.email') }}</span>
          <input v-model="form.email" type="email" required dir="ltr" autocomplete="email" />
        </label>
        <label class="field">
          <span>{{ $t('auth.phone') }}</span>
          <input v-model="form.phone" required dir="ltr" autocomplete="tel" :placeholder="$t('profile.phonePh')" />
        </label>
        <label class="field">
          <span>{{ $t('auth.businessName') }}</span>
          <input v-model="form.businessName" required />
        </label>
        <label class="field">
          <span>{{ $t('auth.businessNameAr') }}</span>
          <input v-model="form.businessNameAr" />
        </label>
        <label class="field">
          <span>{{ $t('auth.website') }}</span>
          <input v-model="form.websiteUrl" dir="ltr" placeholder="https://" />
        </label>
        <p v-if="error" class="error">{{ error }}</p>
        <button class="btn" type="submit" :disabled="loading || saving">
          {{ saving ? $t('common.loading') : $t('profile.sendOtp') }}
        </button>
      </form>

      <div v-else class="otp-step">
        <p class="otp-sub">{{ $t('profile.otpSub', { phone: maskedPhone }) }}</p>
        <p v-if="devCode" class="dev-otp">DEV: {{ devCode }}</p>
        <label class="field">
          <span>{{ $t('auth.otpCode') }}</span>
          <input
            v-model="otpCode"
            class="otp-input"
            inputmode="numeric"
            maxlength="6"
            placeholder="••••••"
            dir="ltr"
          />
        </label>
        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="ok" class="ok-msg">{{ ok }}</p>
        <div class="otp-actions">
          <button class="btn" type="button" :disabled="saving || otpCode.length < 6" @click="confirmOtp">
            {{ saving ? $t('common.loading') : $t('profile.confirmSave') }}
          </button>
          <button class="btn secondary" type="button" :disabled="saving" @click="step = 'form'">
            {{ $t('profile.back') }}
          </button>
          <button class="btn secondary" type="button" :disabled="saving" @click="requestOtp">
            {{ $t('auth.resendCode') }}
          </button>
        </div>
      </div>
    </div>

    <div class="card kyc-card">
      <div class="kyc-head">
        <div>
          <h2>{{ $t('profile.bankTitle') }}</h2>
          <p class="muted">{{ $t('profile.bankSub') }}</p>
        </div>
        <span class="badge" :class="bank.isComplete ? 'ok' : 'warn'">
          {{ bank.isComplete ? $t('profile.bankReady') : $t('profile.bankMissing') }}
        </span>
      </div>
      <form class="profile-form bank-form" @submit.prevent="saveBank">
        <label class="field">
          <span>{{ $t('profile.bankName') }}</span>
          <input v-model="bank.bankName" required :placeholder="$t('profile.bankNamePh')" />
        </label>
        <label class="field">
          <span>{{ $t('profile.bankHolder') }}</span>
          <input v-model="bank.bankAccountHolder" required :placeholder="$t('profile.bankHolderPh')" />
        </label>
        <label class="field">
          <span>{{ $t('profile.bankNumber') }}</span>
          <input v-model="bank.bankAccountNumber" required dir="ltr" :placeholder="$t('profile.bankNumberPh')" />
        </label>
        <label class="field">
          <span>{{ $t('profile.bankIban') }}</span>
          <input v-model="bank.bankIban" dir="ltr" :placeholder="$t('profile.bankIbanPh')" />
        </label>
        <p v-if="bankError" class="error">{{ bankError }}</p>
        <p v-if="bankOk" class="ok-msg">{{ bankOk }}</p>
        <button class="btn" type="submit" :disabled="bankSaving">
          {{ bankSaving ? $t('common.loading') : $t('profile.bankSave') }}
        </button>
      </form>
    </div>

    <div class="card kyc-card">
      <div class="kyc-head">
        <div>
          <h2>{{ $t('profile.kycTitle') }}</h2>
          <p class="muted">{{ $t('profile.kycSub') }}</p>
        </div>
        <span class="badge" :class="kycBadgeClass">{{ kycStatusLabel }}</span>
      </div>

      <p v-if="kyc.status === 'Pending'" class="kyc-banner warn">{{ $t('profile.kycPendingHint') }}</p>
      <p v-else-if="kyc.status === 'Approved'" class="kyc-banner ok">{{ $t('profile.kycApprovedHint') }}</p>
      <p v-else-if="kyc.status === 'Rejected'" class="kyc-banner danger">
        {{ $t('profile.kycRejectedHint') }}
        <strong v-if="kyc.adminNotes">{{ kyc.adminNotes }}</strong>
      </p>

      <div class="kyc-grid">
        <article v-for="doc in docs" :key="doc.key" class="kyc-item">
          <div class="kyc-item-top">
            <strong>{{ doc.label }}</strong>
            <span class="muted tiny">{{ $t('profile.kycFormats') }}</span>
          </div>
          <div class="kyc-preview" :class="{ empty: !doc.url }">
            <img v-if="doc.url" :src="mediaUrl(doc.url)" :alt="doc.label" />
            <span v-else>{{ $t('profile.kycEmpty') }}</span>
          </div>
          <label class="upload-btn" :class="{ disabled: !kyc.canUpload || uploading === doc.key }">
            <input
              type="file"
              accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
              hidden
              :disabled="!kyc.canUpload || !!uploading"
              @change="onPick(doc.key, $event)"
            />
            {{ uploading === doc.key ? $t('common.loading') : (doc.url ? $t('profile.kycReplace') : $t('profile.kycUpload')) }}
          </label>
        </article>
      </div>
      <p v-if="kycError" class="error">{{ kycError }}</p>
      <p v-if="kycOk" class="ok-msg">{{ kycOk }}</p>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../../api'
import { useAuthStore } from '../../stores/auth'

const { t } = useI18n()
const auth = useAuthStore()

const form = reactive({
  fullName: '',
  fullNameAr: '',
  email: '',
  phone: '',
  businessName: '',
  businessNameAr: '',
  websiteUrl: ''
})
const kyc = reactive({
  status: 'None',
  idFrontUrl: '',
  idBackUrl: '',
  passportUrl: '',
  adminNotes: '',
  canUpload: true
})
const step = ref('form')
const challengeId = ref('')
const maskedPhone = ref('')
const otpCode = ref('')
const devCode = ref('')
const loading = ref(true)
const saving = ref(false)
const uploading = ref('')
const error = ref('')
const ok = ref('')
const kycError = ref('')
const kycOk = ref('')
const bank = reactive({
  bankName: '',
  bankAccountHolder: '',
  bankAccountNumber: '',
  bankIban: '',
  isComplete: false
})
const bankSaving = ref(false)
const bankError = ref('')
const bankOk = ref('')

const docs = computed(() => [
  { key: 'id-front', label: t('profile.kycIdFront'), url: kyc.idFrontUrl },
  { key: 'id-back', label: t('profile.kycIdBack'), url: kyc.idBackUrl },
  { key: 'passport', label: t('profile.kycPassport'), url: kyc.passportUrl }
])

const kycStatusLabel = computed(() => t(`profile.kycStatus.${kyc.status}`, kyc.status))
const kycBadgeClass = computed(() => {
  if (kyc.status === 'Approved') return 'ok'
  if (kyc.status === 'Pending') return 'warn'
  if (kyc.status === 'Rejected') return 'danger'
  return ''
})

function mediaUrl(path) {
  if (!path) return ''
  if (/^https?:\/\//i.test(path)) return path
  return `${API_BASE}${path.startsWith('/') ? '' : '/'}${path}`
}

function applyKyc(data) {
  kyc.status = data.status || 'None'
  kyc.idFrontUrl = data.idFrontUrl || data.kycIdFrontUrl || ''
  kyc.idBackUrl = data.idBackUrl || data.kycIdBackUrl || ''
  kyc.passportUrl = data.passportUrl || data.kycPassportUrl || ''
  kyc.adminNotes = data.adminNotes || data.kycAdminNotes || ''
  kyc.canUpload = data.canUpload === true
}

async function load() {
  loading.value = true
  error.value = ''
  try {
    const [{ data: profile }, { data: kycData }] = await Promise.all([
      api.get('/api/merchant/profile'),
      api.get('/api/merchant/kyc')
    ])
    form.fullName = profile.fullName || ''
    form.fullNameAr = profile.fullNameAr || ''
    form.email = profile.email || ''
    form.phone = profile.phone || ''
    form.businessName = profile.businessName || ''
    form.businessNameAr = profile.businessNameAr || ''
    form.websiteUrl = profile.websiteUrl || ''
    bank.bankName = profile.bankName || ''
    bank.bankAccountHolder = profile.bankAccountHolder || ''
    bank.bankAccountNumber = profile.bankAccountNumber || ''
    bank.bankIban = profile.bankIban || ''
    bank.isComplete = !!profile.hasPayoutAccount
    applyKyc(kycData)
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.loadFail')
  } finally {
    loading.value = false
  }
}

function payload() {
  return {
    fullName: form.fullName,
    fullNameAr: form.fullNameAr,
    email: form.email,
    phone: form.phone || null,
    businessName: form.businessName,
    businessNameAr: form.businessNameAr || null,
    websiteUrl: form.websiteUrl || null
  }
}

async function requestOtp() {
  saving.value = true
  error.value = ''
  ok.value = ''
  try {
    const { data } = await api.post('/api/merchant/profile/request-otp', payload())
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone || ''
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    step.value = 'otp'
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.otpFail')
  } finally {
    saving.value = false
  }
}

async function confirmOtp() {
  saving.value = true
  error.value = ''
  ok.value = ''
  try {
    const { data } = await api.post('/api/merchant/profile/confirm', {
      challengeId: challengeId.value,
      code: otpCode.value
    })
    auth.applyAuth(data)
    ok.value = t('profile.saved')
    step.value = 'form'
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.otpFail')
  } finally {
    saving.value = false
  }
}

async function saveBank() {
  bankError.value = ''
  bankOk.value = ''
  bankSaving.value = true
  try {
    const { data } = await api.put('/api/merchant/payout-account', {
      bankName: bank.bankName,
      bankAccountHolder: bank.bankAccountHolder,
      bankAccountNumber: bank.bankAccountNumber,
      bankIban: bank.bankIban || null
    })
    bank.bankName = data.bankName || ''
    bank.bankAccountHolder = data.bankAccountHolder || ''
    bank.bankAccountNumber = data.bankAccountNumber || ''
    bank.bankIban = data.bankIban || ''
    bank.isComplete = !!data.isComplete
    bankOk.value = t('profile.bankSaved')
  } catch (e) {
    bankError.value = e.response?.data?.message || t('profile.bankSaveFail')
  } finally {
    bankSaving.value = false
  }
}

async function onPick(docType, event) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  uploading.value = docType
  kycError.value = ''
  kycOk.value = ''
  try {
    const body = new FormData()
    body.append('file', file)
    const { data } = await api.post(`/api/merchant/kyc/${docType}`, body)
    applyKyc(data)
    kycOk.value = data.status === 'Pending' ? t('profile.kycSubmitted') : t('profile.kycUploaded')
  } catch (e) {
    kycError.value = e.response?.data?.message || t('profile.kycUploadFail')
  } finally {
    uploading.value = ''
  }
}

onMounted(load)
</script>

<style scoped>
.wa-hint {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 12px 14px;
  margin-bottom: 16px;
  border-radius: 12px;
  background: #ecfdf5;
  color: #166534;
  font-size: 0.92rem;
}
.wa-hint i { font-size: 1.2rem; color: #25d366; }
.profile-form,
.otp-step {
  display: grid;
  gap: 14px;
  max-width: 460px;
}
.bank-form { max-width: 560px; }
.field { display: grid; gap: 6px; }
.field span { font-size: 0.85rem; color: var(--muted, #64748b); }
.field input {
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 10px 12px;
  font: inherit;
  background: #fff;
}
.otp-input {
  letter-spacing: 0.35em;
  text-align: center;
  font-size: 1.25rem;
}
.otp-sub { margin: 0; color: #475569; }
.otp-actions { display: flex; flex-wrap: wrap; gap: 8px; }
.dev-otp {
  margin: 0;
  font-family: ui-monospace, monospace;
  color: #b45309;
  font-size: 0.9rem;
}
.ok-msg { color: #15803d; margin: 0; font-size: 0.9rem; }

.kyc-card { margin-top: 16px; }
.kyc-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 14px;
}
.kyc-head h2 { margin: 0 0 4px; font-size: 1.15rem; }
.kyc-banner {
  padding: 12px 14px;
  border-radius: 12px;
  margin: 0 0 16px;
  font-size: 0.9rem;
  font-weight: 600;
}
.kyc-banner.warn { background: #fffbeb; color: #92400e; border: 1px solid #fcd34d; }
.kyc-banner.ok { background: #ecfdf5; color: #166534; border: 1px solid #86efac; }
.kyc-banner.danger { background: #fef2f2; color: #991b1b; border: 1px solid #fecaca; }
.kyc-banner strong { display: block; margin-top: 6px; font-weight: 700; }
.kyc-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 14px;
}
.kyc-item {
  border: 1px solid var(--line, #e2e8f0);
  border-radius: 14px;
  padding: 12px;
  display: grid;
  gap: 10px;
  background: #fff;
}
.kyc-item-top { display: grid; gap: 2px; }
.tiny { font-size: 0.75rem; }
.kyc-preview {
  aspect-ratio: 4 / 3;
  border-radius: 12px;
  border: 1px dashed #cbd5e1;
  overflow: hidden;
  display: grid;
  place-items: center;
  background: #f8fafc;
  color: #94a3b8;
  font-size: 0.85rem;
}
.kyc-preview img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}
.upload-btn {
  display: inline-flex;
  justify-content: center;
  align-items: center;
  padding: 10px 12px;
  border-radius: 10px;
  background: #031838;
  color: #fff;
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
}
.upload-btn.disabled {
  opacity: 0.55;
  cursor: not-allowed;
}
@media (max-width: 900px) {
  .kyc-grid { grid-template-columns: 1fr; }
}
</style>
