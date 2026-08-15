<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-stage">
      <AuthAside :title="t('sideForgotTitle')" :body="t('sideForgotBody')" :t="t" />

      <section class="auth-card">
        <AuthPending
          v-if="pending"
          :title="t('pendingLoginTitle')"
          :lead="t('pendingLoginLead')"
          :body="t('pendingLoginBody')"
          :t="t"
        />
        <div class="card-head" v-else>
          <h2>{{ heading }}</h2>
          <p class="muted">{{ subheading }}</p>
        </div>

        <template v-if="!pending">
        <p v-if="error" class="error" role="alert">{{ error }}</p>
        <p v-if="devCode" class="dev">DEV: {{ devCode }}</p>

        <form v-if="step === 'phone'" @submit.prevent="sendOtp()">
          <div class="field">
            <label for="forgot-phone">{{ t('phone') }}</label>
            <input
              id="forgot-phone"
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
            <label for="forgot-otp">{{ t('otpCode') }}</label>
            <input
              id="forgot-otp"
              v-model="otpCode"
              class="otp-input ltr"
              inputmode="numeric"
              maxlength="6"
              placeholder="••••••"
              required
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
          <div class="field">
            <label for="forgot-pass">{{ t('newPassword') }}</label>
            <input
              id="forgot-pass"
              v-model="newPassword"
              type="password"
              autocomplete="new-password"
              placeholder="••••••••"
              required
            />
            <small class="hint">{{ t('passwordHint') }}</small>
          </div>
          <div class="field">
            <label for="forgot-confirm">{{ t('confirmPassword') }}</label>
            <input
              id="forgot-confirm"
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
          <button class="linkish" type="button" @click="backToPhone">{{ t('changePhone') }}</button>
        </form>

        <p class="footer-link muted">
          {{ t('rememberPassword') }}
          <RouterLink to="/login">{{ t('loginLink') }}</RouterLink>
        </p>
        <p class="secure">{{ t('secureNote') }}</p>
        </template>
      </section>
    </div>
  </main>
</template>

<script setup>
import { computed, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import AuthAside from '../components/AuthAside.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import AuthPending from '../components/AuthPending.vue'
import { completeAuth, useAuthCopy } from '../composables/useAuth'
import { useOtpResend } from '../composables/useOtpResend'

const { locale, dashboardUrl } = useLanding()
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
const pending = ref(false)
const loading = ref(false)
const resending = ref(false)
const { canResend, clock, startCooldown, resetCooldown } = useOtpResend()

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
  resetCooldown()
}

async function resendOtp() {
  if (!canResend.value || resending.value) return
  resending.value = true
  try {
    await sendOtp({ silent: true })
  } finally {
    resending.value = false
  }
}

async function sendOtp({ silent = false } = {}) {
  error.value = ''
  if (!phone.value.trim()) {
    error.value = t('phoneRequired')
    return
  }
  if (!silent) loading.value = true
  try {
    const { data } = await api.post('/api/auth/forgot-password/send-otp', {
      phone: phone.value.trim()
    })
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone || data.maskedDestination || phone.value
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    step.value = 'reset'
    startCooldown()
  } catch (e) {
    error.value = e.response?.data?.message || t('forgotFail')
  } finally {
    if (!silent) loading.value = false
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
    completeAuth(dashboardUrl, data, () => { pending.value = true })
    loading.value = false
  } catch (e) {
    error.value = e.response?.data?.message || t('resetFail')
    loading.value = false
  }
}
</script>
