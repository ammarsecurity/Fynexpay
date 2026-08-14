<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-stage wide">
      <AuthAside :title="t('sideRegisterTitle')" :body="t('sideRegisterBody')" :t="t" />

      <section class="auth-card">
        <div class="card-head">
          <p class="step-label" v-if="requireOtp">
            {{ step === 'otp' ? t('otpStep') : t('formStep') }}
          </p>
          <h2>{{ step === 'otp' ? t('otpTitle') : t('registerTitle') }}</h2>
          <p class="muted">
            {{ step === 'otp' ? t('otpSub', { phone: maskedPhone }) : t('registerSub') }}
          </p>
        </div>

        <div v-if="requireOtp && step === 'form'" class="banner">{{ otpBanner }}</div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <p v-if="devCode" class="dev">DEV: {{ devCode }}</p>

        <form v-if="step === 'form'" @submit.prevent="submitForm">
          <p class="group-label">{{ t('groupIdentity') }}</p>
          <div class="grid-2">
            <div class="field">
              <label for="reg-name-ar">{{ t('fullNameAr') }}</label>
              <input id="reg-name-ar" v-model="form.fullNameAr" required dir="rtl" />
            </div>
            <div class="field">
              <label for="reg-name-en">{{ t('fullNameEn') }}</label>
              <input id="reg-name-en" v-model="form.fullName" required dir="ltr" />
            </div>
          </div>

          <p class="group-label">{{ t('groupBusiness') }}</p>
          <div class="grid-2">
            <div class="field">
              <label for="reg-biz">{{ t('businessName') }}</label>
              <input id="reg-biz" v-model="form.businessName" required />
            </div>
            <div class="field">
              <label for="reg-biz-ar">{{ t('businessNameAr') }}</label>
              <input id="reg-biz-ar" v-model="form.businessNameAr" />
            </div>
          </div>

          <p class="group-label">{{ t('groupAccess') }}</p>
          <div class="grid-2">
            <div class="field">
              <label for="reg-email">{{ t('email') }}</label>
              <input id="reg-email" v-model="form.email" type="email" required />
            </div>
            <div class="field" v-if="needPhone">
              <label for="reg-phone">{{ t('phone') }}</label>
              <input id="reg-phone" v-model="form.contactPhone" class="ltr" placeholder="07xxxxxxxxx" />
            </div>
          </div>
          <div class="grid-2">
            <div class="field">
              <label for="reg-web">{{ t('website') }}</label>
              <input id="reg-web" v-model="form.websiteUrl" class="ltr" placeholder="https://" />
            </div>
            <div class="field">
              <label for="reg-pass">{{ t('password') }}</label>
              <input id="reg-pass" v-model="form.password" type="password" required />
            </div>
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading">
            {{ loading ? t('loading') : (requireOtp ? t('sendCode') : t('createAccount')) }}
          </button>
        </form>

        <div v-else class="otp-box">
          <div class="field">
            <label for="reg-otp">{{ t('otpCode') }}</label>
            <input
              id="reg-otp"
              v-model="otpCode"
              class="otp-input ltr"
              inputmode="numeric"
              maxlength="6"
              placeholder="••••••"
            />
          </div>
          <p class="otp-resend">
            <span>{{ t('noCode') }}</span>
            <button
              class="otp-resend-btn"
              type="button"
              :disabled="resending || !canResend"
              @click="resendOtp"
            >
              {{ canResend ? t('resend') : t('resendIn', { time: clock }) }}
            </button>
          </p>
          <button class="btn primary submit" type="button" :disabled="loading || otpCode.length < 6" @click="verifyOtp">
            {{ loading ? t('loading') : t('verify') }}
          </button>
          <button class="linkish" type="button" @click="backToForm">{{ t('back') }}</button>
        </div>

        <p class="footer-link muted">
          {{ t('hasAccount') }}
          <RouterLink to="/login">{{ t('loginLink') }}</RouterLink>
        </p>
      </section>
    </div>
  </main>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import AuthAside from '../components/AuthAside.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { handoffToDashboard, useAuthCopy } from '../composables/useAuth'
import { useOtpResend } from '../composables/useOtpResend'

const { locale, dashboardUrl } = useLanding()
const { t } = useAuthCopy(locale)

const error = ref('')
const loading = ref(false)
const requireOtp = ref(false)
const needPhone = ref(true)
const needEmailChannel = ref(false)
const step = ref('form')
const challengeId = ref('')
const maskedPhone = ref('')
const otpCode = ref('')
const devCode = ref('')
const resending = ref(false)
const { canResend, clock, startCooldown, resetCooldown } = useOtpResend()

const form = reactive({
  fullName: '',
  fullNameAr: '',
  businessName: '',
  businessNameAr: '',
  email: '',
  contactPhone: '',
  websiteUrl: '',
  password: ''
})

const otpBanner = computed(() => {
  if (needPhone.value && needEmailChannel.value) return t('bothRequired')
  if (needEmailChannel.value) return t('emailRequired')
  return t('whatsappRequired')
})

onMounted(async () => {
  try {
    const { data } = await api.get('/api/auth/register/policy')
    requireOtp.value = !!data.requireWhatsAppOtp
    needPhone.value = data.whatsAppEnabled !== false
    needEmailChannel.value = !!data.emailEnabled
    if (data.channel === 'Email') needPhone.value = false
  } catch {
    requireOtp.value = false
  }
})

async function submitForm() {
  error.value = ''
  loading.value = true
  try {
    if (!requireOtp.value) {
      const { data } = await api.post('/api/auth/register', { ...form })
      handoffToDashboard(dashboardUrl, data)
      return
    }
    const { data } = await api.post('/api/auth/register/send-otp', { ...form })
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    step.value = 'otp'
    startCooldown()
  } catch (e) {
    error.value = e.response?.data?.message || t('registerFail')
  } finally {
    loading.value = false
  }
}

async function verifyOtp() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/register/verify-otp', {
      challengeId: challengeId.value,
      code: otpCode.value
    })
    handoffToDashboard(dashboardUrl, data)
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
    loading.value = false
  }
}

function backToForm() {
  step.value = 'form'
  resetCooldown()
}

async function resendOtp() {
  if (!canResend.value || resending.value) return
  error.value = ''
  resending.value = true
  try {
    const { data } = await api.post('/api/auth/register/send-otp', { ...form })
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    startCooldown()
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
  } finally {
    resending.value = false
  }
}
</script>
