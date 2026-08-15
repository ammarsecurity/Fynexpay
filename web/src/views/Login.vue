<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-stage">
      <AuthAside :title="t('sideLoginTitle')" :body="t('sideLoginBody')" :t="t" />

      <section class="auth-card">
        <AuthPending
          v-if="pending"
          :title="t('pendingLoginTitle')"
          :lead="t('pendingLoginLead')"
          :body="t('pendingLoginBody')"
          :t="t"
        />
        <div class="card-head" v-else>
          <p class="step-label" v-if="requireOtp">
            {{ step === 'otp' ? t('otpStep') : t('formStep') }}
          </p>
          <h2>{{ step === 'otp' ? t('otpTitle') : t('loginTitle') }}</h2>
          <p class="muted">
            {{ step === 'otp' ? t('otpSub', { phone: maskedPhone }) : t('loginSub') }}
          </p>
        </div>

        <template v-if="!pending">
        <div v-if="requireOtp && step === 'form'" class="banner">{{ loginOtpBanner }}</div>
        <p v-if="sessionNotice" class="error" role="status">{{ sessionNotice }}</p>
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
            {{ loading ? t('loading') : t('verifyLogin') }}
          </button>
          <button class="linkish" type="button" @click="backToForm">{{ t('back') }}</button>
        </div>

        <p class="footer-link muted">
          {{ t('noAccount') }}
          <RouterLink to="/register">{{ t('registerLink') }}</RouterLink>
        </p>
        </template>
      </section>
    </div>
  </main>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import SiteNav from '../components/SiteNav.vue'
import AuthAside from '../components/AuthAside.vue'
import AuthPending from '../components/AuthPending.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { completeAuth, useAuthCopy } from '../composables/useAuth'
import { useOtpResend } from '../composables/useOtpResend'

const { locale, dashboardUrl } = useLanding()
const { t } = useAuthCopy(locale)
const route = useRoute()
const sessionNotice = computed(() => (route.query.session === 'expired' ? t('sessionExpired') : ''))

const email = ref('')
const password = ref('')
const error = ref('')
const pending = ref(route.query.pending === '1')
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
  resetCooldown()
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
      completeAuth(dashboardUrl, data, () => { pending.value = true })
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
  startCooldown()
}

async function verifyOtp() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/login/verify-otp', {
      challengeId: challengeId.value,
      code: otpCode.value
    })
    completeAuth(dashboardUrl, data, () => { pending.value = true })
    loading.value = false
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
    loading.value = false
  }
}

async function resendOtp() {
  if (!canResend.value || resending.value) return
  error.value = ''
  resending.value = true
  try {
    await sendOtp()
  } catch (e) {
    error.value = e.response?.data?.message || t('otpFail')
  } finally {
    resending.value = false
  }
}
</script>
