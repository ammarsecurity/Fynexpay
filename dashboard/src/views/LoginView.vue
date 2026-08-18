<template>
  <div class="auth-shell">
    <aside class="auth-aside">
      <div class="brand-row brand-row--auth">
        <img src="/icon-logo-white.png" alt="" class="brand-icon brand-icon--white" />
        <div class="brand-text">Fynex<span>pay</span></div>
      </div>

      <div class="auth-aside-copy">
        <h1>{{ $t('auth.asideLoginTitle') }}</h1>
        <p>{{ $t('auth.asideLoginBody') }}</p>
        <ul class="auth-points">
          <li>
            <i class="bi bi-shield-lock" aria-hidden="true"></i>
            <span>{{ $t('auth.pointSecure') }}</span>
          </li>
          <li>
            <i class="bi bi-wallet2" aria-hidden="true"></i>
            <span>{{ $t('auth.pointWallet') }}</span>
          </li>
          <li>
            <i class="bi bi-lightning-charge" aria-hidden="true"></i>
            <span>{{ $t('auth.pointFast') }}</span>
          </li>
        </ul>
      </div>

      <div class="auth-aside-foot">
        <span>© {{ year }} Fynexpay</span>
        <div class="auth-providers" aria-hidden="true">
          <img src="/providers/fib.svg" alt="" />
          <img src="/providers/zaincash.svg" alt="" />
          <img src="/providers/qi.svg" alt="" />
          <img src="/providers/superqi.svg" alt="" />
          <img src="/providers/alqaseh.svg" alt="" />
        </div>
      </div>
    </aside>

    <section class="auth-panel">
      <div class="auth-card">
        <img src="/full-logo.png" alt="Fynexpay" class="auth-card-logo" />
        <div class="auth-card-head">
          <div>
            <p class="step-label" v-if="requireOtp">
              {{ step === 'otp' ? $t('auth.otpStep') : $t('auth.formStep') }}
            </p>
            <h2>{{ step === 'otp' ? $t('auth.otpTitle') : $t('auth.loginTitle') }}</h2>
            <p class="muted">
              {{ step === 'otp' ? $t('auth.otpSub', { phone: maskedPhone }) : $t('auth.loginSub') }}
            </p>
          </div>
          <LangSwitch />
        </div>

        <div v-if="requireOtp && step === 'form'" class="otp-banner" :class="bannerClass">
          <i :class="bannerIcon" aria-hidden="true"></i>
          <span>{{ otpBanner }}</span>
        </div>

        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="devCode" class="dev-otp">DEV: {{ devCode }}</p>

        <form v-if="step === 'form'" @submit.prevent="submit">
          <div class="field">
            <label>{{ $t('auth.email') }}</label>
            <input v-model="email" type="email" autocomplete="email" placeholder="name@company.iq" />
          </div>
          <div class="field">
            <label>{{ $t('auth.password') }}</label>
            <input v-model="password" type="password" autocomplete="current-password" placeholder="••••••••" />
          </div>
          <p class="forgot-row">
            <a class="forgot" :href="webForgotUrl">{{ $t('auth.forgotPassword') }}</a>
          </p>
          <button class="btn" type="submit" :disabled="loading">
            <i :class="requireOtp ? 'bi bi-shield-check' : 'bi bi-box-arrow-in-left'" aria-hidden="true"></i>
            {{ loading ? $t('common.loading') : (requireOtp ? $t('auth.sendWhatsappCode') : $t('auth.signIn')) }}
          </button>
        </form>

        <div v-else class="otp-step">
          <div class="otp-badge" aria-hidden="true"><i class="bi bi-shield-lock"></i></div>
          <div class="field">
            <label>{{ $t('auth.otpCode') }}</label>
            <input v-model="otpCode" class="otp-input ltr" inputmode="numeric" maxlength="6" placeholder="••••••" />
          </div>
          <button class="btn" type="button" :disabled="loading || otpCode.length < 6" @click="verifyOtp">
            <i class="bi bi-check2-circle" aria-hidden="true"></i>
            {{ loading ? $t('common.loading') : $t('auth.verifyAndSignIn') }}
          </button>
          <button class="btn secondary resend-btn" type="button" :disabled="loading" @click="resendOtp">
            <i class="bi bi-arrow-clockwise" aria-hidden="true"></i>
            {{ $t('auth.resendCode') }}
          </button>
          <button class="linkish" type="button" @click="backToForm">{{ $t('auth.backToForm') }}</button>
        </div>

        <p class="muted auth-footer-link">
          {{ $t('auth.noAccount') }}
          <RouterLink to="/register">{{ $t('auth.registerLink') }}</RouterLink>
        </p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { api } from '../api'
import { useAuthStore } from '../stores/auth'
import LangSwitch from '../components/LangSwitch.vue'

const auth = useAuthStore()
const router = useRouter()
const { t } = useI18n()
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
const year = new Date().getFullYear()
const webForgotUrl = `${(import.meta.env.VITE_WEB_URL || 'https://fynexpay.net').replace(/\/$/, '')}/forgot-password`

const otpBanner = computed(() => {
  if (needPhone.value && needEmailChannel.value) return t('auth.loginOtpBothRequired')
  if (needEmailChannel.value) return t('auth.loginOtpEmailRequired')
  return t('auth.loginOtpRequired')
})

const bannerClass = computed(() => {
  if (needPhone.value && !needEmailChannel.value) return 'wa'
  if (!needPhone.value && needEmailChannel.value) return 'mail'
  return 'both'
})

const bannerIcon = computed(() => {
  if (needPhone.value && !needEmailChannel.value) return 'bi bi-whatsapp'
  if (!needPhone.value && needEmailChannel.value) return 'bi bi-envelope-fill'
  return 'bi bi-shield-lock-fill'
})

onMounted(async () => {
  try {
    const { data } = await api.get('/api/auth/register/policy')
    needPhone.value = data.whatsAppEnabled !== false
    needEmailChannel.value = !!data.emailEnabled
    if (data.channel === 'Email') needPhone.value = false
  } catch {
    /* channels optional for the OTP step */
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
    await auth.login(email.value, password.value)
    router.push('/')
  } catch (e) {
    if (e.response?.data?.code === 'otp_required') {
      requireOtp.value = true
      await sendOtp()
      return
    }
    error.value = e.response?.data?.message || t('auth.loginFail')
  } finally {
    loading.value = false
  }
}

async function sendOtp() {
  const data = await auth.sendLoginOtp(email.value, password.value)
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
    await auth.verifyLoginOtp(challengeId.value, otpCode.value)
    router.push('/')
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.otpFail')
  } finally {
    loading.value = false
  }
}

async function resendOtp() {
  error.value = ''
  loading.value = true
  try {
    await sendOtp()
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.otpFail')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.forgot-row {
  display: flex;
  justify-content: flex-end;
  margin: -4px 0 12px;
}
.forgot {
  color: var(--brand-secondary);
  font-weight: 700;
  font-size: 0.88rem;
  text-decoration: none;
}
.forgot:hover { text-decoration: underline; }
.step-label {
  margin: 0 0 6px;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.04em;
  color: var(--brand-secondary);
}
.otp-banner {
  display: flex;
  align-items: center;
  gap: 10px;
  border-radius: 12px;
  padding: 12px 14px;
  font-weight: 700;
  font-size: 0.9rem;
  margin-bottom: 16px;
}
.otp-banner.wa {
  background: rgba(37, 211, 102, 0.1);
  color: #0f7a3f;
  border: 1px solid rgba(37, 211, 102, 0.22);
}
.otp-banner.mail {
  background: rgba(3, 24, 56, 0.08);
  color: #031838;
  border: 1px solid rgba(3, 24, 56, 0.18);
}
.otp-banner.both {
  background: #f8fafc;
  color: #334155;
  border: 1px solid var(--line);
}
.otp-banner i { font-size: 1.15rem; }
.otp-step { text-align: center; }
.otp-badge {
  width: 56px;
  height: 56px;
  margin: 0 auto 16px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  background: var(--brand-soft);
  color: var(--brand-secondary);
  font-size: 1.4rem;
}
.otp-input {
  letter-spacing: 0.35em;
  text-align: center;
  font-size: 1.35rem;
  font-weight: 800;
}
.ltr { direction: ltr; text-align: left; }
.otp-input.ltr { text-align: center; }
.resend-btn {
  width: 100%;
  justify-content: center;
  margin-top: 10px;
}
.linkish {
  display: inline-block;
  margin: 16px auto 0;
  border: 0;
  background: transparent;
  color: var(--brand-secondary);
  font: inherit;
  font-weight: 700;
  cursor: pointer;
}
.dev-otp {
  background: #fef9c3;
  border: 1px solid #fde68a;
  border-radius: 12px;
  padding: 8px 12px;
  font-weight: 700;
  margin-bottom: 12px;
  text-align: center;
}
</style>
