<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-shell">
      <section class="auth-card">
        <div class="card-head">
          <h2>{{ heading }}</h2>
          <p class="muted">{{ subheading }}</p>
        </div>

        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="devCode" class="dev">DEV: {{ devCode }}</p>

        <form v-if="step === 'phone'" @submit.prevent="sendOtp">
          <div class="field">
            <label>{{ t('phone') }}</label>
            <input
              v-model="phone"
              class="ltr"
              type="tel"
              inputmode="numeric"
              autocomplete="tel"
              placeholder="07xxxxxxxxx"
              required
            />
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading">
            {{ loading ? t('loading') : t('sendWhatsappOtp') }}
          </button>
        </form>

        <form v-else @submit.prevent="resetPassword">
          <div class="field">
            <label>{{ t('otpCode') }}</label>
            <input
              v-model="otpCode"
              class="otp-input ltr"
              inputmode="numeric"
              maxlength="6"
              placeholder="••••••"
              required
            />
          </div>
          <div class="field">
            <label>{{ t('newPassword') }}</label>
            <input
              v-model="newPassword"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              required
            />
            <small class="hint">{{ t('passwordHint') }}</small>
          </div>
          <div class="field">
            <label>{{ t('confirmPassword') }}</label>
            <input
              v-model="confirmPassword"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              required
            />
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading || otpCode.length < 6">
            {{ loading ? t('loading') : t('saveNewPassword') }}
          </button>
          <button class="btn soft submit" type="button" :disabled="loading" @click="sendOtp">
            {{ t('resend') }}
          </button>
          <button class="linkish" type="button" @click="backToPhone">{{ t('changePhone') }}</button>
        </form>

        <p class="footer-link muted">
          {{ t('rememberPassword') }}
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
import { computed, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { handoffToDashboard, useAuthCopy } from '../composables/useAuth'

const { c, locale, dashboardUrl, year } = useLanding()
const { t } = useAuthCopy(locale)

const step = ref('phone')
const phone = ref('')
const otpCode = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const challengeId = ref('')
const maskedPhone = ref('')
const devCode = ref('')
const error = ref('')
const loading = ref(false)

const heading = computed(() => (step.value === 'phone' ? t('forgotTitle') : t('resetTitle')))
const subheading = computed(() =>
  step.value === 'phone' ? t('forgotSub') : t('resetSub', { phone: maskedPhone.value || phone.value })
)

function backToPhone() {
  step.value = 'phone'
  error.value = ''
  otpCode.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
}

async function sendOtp() {
  error.value = ''
  if (!phone.value.trim()) {
    error.value = t('phoneRequired')
    return
  }
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/forgot-password/send-otp', {
      phone: phone.value.trim()
    })
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone || data.maskedDestination || phone.value
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    step.value = 'reset'
  } catch (e) {
    error.value = e.response?.data?.message || t('forgotFail')
  } finally {
    loading.value = false
  }
}

async function resetPassword() {
  error.value = ''
  if (otpCode.value.trim().length < 6) {
    error.value = t('otpFail')
    return
  }
  if (newPassword.value !== confirmPassword.value) {
    error.value = t('passwordMismatch')
    return
  }
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/forgot-password/reset', {
      challengeId: challengeId.value,
      code: otpCode.value.trim(),
      newPassword: newPassword.value
    })
    handoffToDashboard(dashboardUrl, data)
  } catch (e) {
    error.value = e.response?.data?.message || t('resetFail')
    loading.value = false
  }
}
</script>
