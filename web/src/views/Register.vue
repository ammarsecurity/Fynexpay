<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-shell wide">
      <section class="auth-card">
        <div class="card-head">
          <h2>{{ step === 'otp' ? t('otpTitle') : t('registerTitle') }}</h2>
          <p class="muted">
            {{ step === 'otp' ? t('otpSub', { phone: maskedPhone }) : t('registerSub') }}
          </p>
        </div>

        <div v-if="requireOtp && step === 'form'" class="banner">{{ otpBanner }}</div>
        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="devCode" class="dev">DEV: {{ devCode }}</p>

        <form v-if="step === 'form'" @submit.prevent="submitForm">
          <div class="grid-2">
            <div class="field">
              <label>{{ t('fullName') }}</label>
              <input v-model="form.fullName" required />
            </div>
            <div class="field">
              <label>{{ t('businessName') }}</label>
              <input v-model="form.businessName" required />
            </div>
          </div>
          <div class="field">
            <label>{{ t('businessNameAr') }}</label>
            <input v-model="form.businessNameAr" />
          </div>
          <div class="grid-2">
            <div class="field">
              <label>{{ t('email') }}</label>
              <input v-model="form.email" type="email" required />
            </div>
            <div class="field" v-if="needPhone">
              <label>{{ t('phone') }}</label>
              <input v-model="form.contactPhone" class="ltr" placeholder="07xxxxxxxxx" />
            </div>
          </div>
          <div class="grid-2">
            <div class="field">
              <label>{{ t('website') }}</label>
              <input v-model="form.websiteUrl" class="ltr" placeholder="https://" />
            </div>
            <div class="field">
              <label>{{ t('password') }}</label>
              <input v-model="form.password" type="password" required />
            </div>
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading">
            {{ loading ? t('loading') : (requireOtp ? t('sendCode') : t('createAccount')) }}
          </button>
        </form>

        <div v-else class="otp-box">
          <div class="field">
            <label>{{ t('otpCode') }}</label>
            <input
              v-model="otpCode"
              class="otp-input ltr"
              inputmode="numeric"
              maxlength="6"
              placeholder="••••••"
            />
          </div>
          <button class="btn primary submit" type="button" :disabled="loading || otpCode.length < 6" @click="verifyOtp">
            {{ loading ? t('loading') : t('verify') }}
          </button>
          <button class="btn soft submit" type="button" :disabled="loading" @click="resendOtp">
            {{ t('resend') }}
          </button>
          <button class="linkish" type="button" @click="step = 'form'">{{ t('back') }}</button>
        </div>

        <p class="footer-link muted">
          {{ t('hasAccount') }}
          <RouterLink to="/login">{{ t('loginLink') }}</RouterLink>
        </p>
        <p class="secure">{{ t('secureNote') }}</p>
        <RouterLink class="back-home" to="/">{{ t('backHome') }}</RouterLink>
      </section>
    </div>
  </main>

  <footer class="site-footer" v-if="c">© {{ year }} Fynexpay — {{ c.footer }}</footer>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { handoffToDashboard, useAuthCopy } from '../composables/useAuth'

const { c, locale, dashboardUrl, year } = useLanding()
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

const form = reactive({
  fullName: '',
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

async function resendOtp() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/register/send-otp', { ...form })
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
  } finally {
    loading.value = false
  }
}
</script>
