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
          <span>{{ $t('auth.fullName') }}</span>
          <input v-model="form.fullName" required autocomplete="name" />
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
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useAuthStore } from '../../stores/auth'

const { t } = useI18n()
const auth = useAuthStore()

const form = reactive({
  fullName: '',
  email: '',
  phone: '',
  businessName: '',
  businessNameAr: '',
  websiteUrl: ''
})
const step = ref('form')
const challengeId = ref('')
const maskedPhone = ref('')
const otpCode = ref('')
const devCode = ref('')
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const ok = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/api/merchant/profile')
    form.fullName = data.fullName || ''
    form.email = data.email || ''
    form.phone = data.phone || ''
    form.businessName = data.businessName || ''
    form.businessNameAr = data.businessNameAr || ''
    form.websiteUrl = data.websiteUrl || ''
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.loadFail')
  } finally {
    loading.value = false
  }
}

function payload() {
  return {
    fullName: form.fullName,
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
.field {
  display: grid;
  gap: 6px;
}
.field span {
  font-size: 0.85rem;
  color: var(--muted, #64748b);
}
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
.otp-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}
.dev-otp {
  margin: 0;
  font-family: ui-monospace, monospace;
  color: #b45309;
  font-size: 0.9rem;
}
.ok-msg {
  color: #15803d;
  margin: 0;
  font-size: 0.9rem;
}
</style>
