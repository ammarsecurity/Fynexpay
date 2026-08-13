<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-stage">
      <AuthAside :title="t('sideLoginTitle')" :body="t('sideLoginBody')" :t="t" />

      <section class="auth-card">
        <div class="card-head">
          <p class="step-label" v-if="requireOtp">
            {{ step === 'otp' ? t('otpStep') : t('formStep') }}
          </p>
          <h2>{{ step === 'otp' ? t('otpTitle') : t('loginTitle') }}</h2>
          <p class="muted">
            {{ step === 'otp' ? t('otpSub', { phone: maskedPhone }) : t('loginSub') }}
          </p>
        </div>

        <div v-if="requireOtp && step === 'form'" class="banner">{{ loginOtpBanner }}</div>
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <p v-if="devCode" class="dev">DEV: {{ devCode }}</p>

        <form v-if="step === 'form'" @submit.prevent="submit">
          <div class="field">
            <label for="login-email">{{ t('email') }}</label>
            <input
              id="login-email"
              v-model="email"
              type="email"
              autocomplete="email"
              placeholder="name@company.iq"
              required
            />
          </div>
          <div class="field">
            <label for="login-password">{{ t('password') }}</label>
            <input
              id="login-password"
              v-model="password"
              type="password"
              autocomplete="current-password"
              placeholder="••••••••"
              required
            />
          </div>
          <div class="forgot-row">
            <RouterLink to="/forgot" class="forgot-link">{{ t('forgotPassword') }}</RouterLink>
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading">
            {{ loading ? t('loading') : (requireOtp ? t('sendCode') : t('signIn')) }}
          </button>
        </form>

        <div v-else class="otp-box">
          <div class="field">
            <label for="login-otp">{{ t('otpCode') }}</label>
            <input
              id="login-otp"
              v-model="otpCode"
              class="otp-input ltr"
              inputmode="numeric"
              maxlength="6"
              placeholder="••••••"
            />
          </div>
          <button class="btn primary submit" type="button" :disabled="loading || otpCode.length < 6" @click="verifyOtp">
            {{ loading ? t('loading') : t('verifyLogin') }}
          </button>
          <button class="btn soft submit" type="button" :disabled="loading" @click="resendOtp">
            {{ t('resend') }}
          </button>
          <button class="linkish" type="button" @click="backToForm">{{ t('back') }}</button>
        </div>

        <p class="footer-link muted">
          {{ t('noAccount') }}
          <RouterLink to="/register">{{ t('registerLink') }}</RouterLink>
        </p>
      </section>
    </div>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import AuthAside from '../components/AuthAside.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { handoffToDashboard, useAuthCopy } from '../composables/useAuth'

const { locale, dashboardUrl } = useLanding()
const { t } = useAuthCopy(locale)

const email = ref('')
const password = ref('')
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

const loginOtpBanner = computed(() => {
  if (needPhone.value && needEmailChannel.value) return t('loginBothRequired')
  if (needEmailChannel.value) return t('loginEmailRequired')
  return t('loginWhatsappRequired')
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

function backToForm() {
  step.value = 'form'
  error.value = ''
  otpCode.value = ''
  devCode.value = ''
}

async function submit() {
  error.value = ''
  loading.value = true
  try {
    if (!requireOtp.value) {
      const { data } = await api.post('/api/auth/login', {
        email: email.value,
        password: password.value
      })
      handoffToDashboard(dashboardUrl, data)
      return
    }
    await sendOtp()
  } catch (e) {
    error.value = e.response?.data?.message || t('loginFail')
  } finally {
    loading.value = false
  }
}

async function sendOtp() {
  const { data } = await api.post('/api/auth/login/send-otp', {
    email: email.value,
    password: password.value
  })
  challengeId.value = data.challengeId
  maskedPhone.value = data.maskedPhone
  devCode.value = data.devCode || ''
  otpCode.value = data.devCode || ''
  step.value = 'otp'
}

async function verifyOtp() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/login/verify-otp', {
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
    await sendOtp()
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
  } finally {
    loading.value = false
  }
}
</script>
