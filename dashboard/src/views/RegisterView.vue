<template>
  <div class="auth-shell">
    <aside class="auth-aside">
      <div class="brand-row brand-row--auth">
        <img src="/icon-logo-white.png" alt="" class="brand-icon brand-icon--white" />
        <div class="brand-text">Fynex<span>pay</span></div>
      </div>

      <div class="auth-aside-copy">
        <h1>{{ $t('auth.asideRegisterTitle') }}</h1>
        <p>{{ $t('auth.asideRegisterBody') }}</p>
        <ul class="auth-points">
          <li>
            <i class="bi bi-shop" aria-hidden="true"></i>
            <span>{{ $t('auth.pointMerchant') }}</span>
          </li>
          <li>
            <i class="bi bi-key" aria-hidden="true"></i>
            <span>{{ $t('auth.pointApi') }}</span>
          </li>
          <li>
            <i class="bi bi-phone" aria-hidden="true"></i>
            <span>{{ $t('auth.pointCheckout') }}</span>
          </li>
        </ul>
      </div>

      <div class="auth-aside-foot">
        <span>{{ $t('auth.providersLabel') }}</span>
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
      <div class="auth-card auth-card--wide">
        <img src="/full-logo.png" alt="Fynexpay" class="auth-card-logo" />
        <div class="auth-card-head">
          <div>
            <h2>{{ step === 'otp' ? $t('auth.otpTitle') : $t('auth.registerTitle') }}</h2>
            <p class="muted">
              {{ step === 'otp' ? $t('auth.otpSub', { phone: maskedPhone }) : $t('auth.registerSub') }}
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

        <template v-if="step === 'form'">
          <form @submit.prevent="submitForm">
            <div class="auth-fields-2">
              <div class="field"><label>{{ $t('auth.fullName') }}</label><input v-model="form.fullName" required /></div>
              <div class="field"><label>{{ $t('auth.businessName') }}</label><input v-model="form.businessName" placeholder="Shop Name" required /></div>
            </div>
            <div class="field"><label>{{ $t('auth.businessNameAr') }}</label><input v-model="form.businessNameAr" /></div>
            <div class="auth-fields-2">
              <div class="field"><label>{{ $t('auth.email') }}</label><input v-model="form.email" type="email" placeholder="merchant@shop.iq" required /></div>
              <div class="field" v-if="needPhone"><label>{{ $t('auth.phone') }}</label><input v-model="form.contactPhone" placeholder="07xxxxxxxxx" class="ltr" /></div>
            </div>
            <div class="auth-fields-2">
              <div class="field"><label>{{ $t('auth.website') }}</label><input v-model="form.websiteUrl" placeholder="https://" class="ltr" /></div>
              <div class="field"><label>{{ $t('auth.password') }}</label><input v-model="form.password" type="password" placeholder="••••••••" required /></div>
            </div>
            <button class="btn" type="submit" :disabled="loading">
              <i :class="requireOtp ? 'bi bi-shield-check' : 'bi bi-person-plus'" aria-hidden="true"></i>
              {{ loading ? $t('common.loading') : (requireOtp ? $t('auth.sendWhatsappCode') : $t('auth.createAccount')) }}
            </button>
          </form>
        </template>

        <template v-else>
          <div class="otp-step">
            <div class="otp-badge" aria-hidden="true"><i class="bi bi-shield-lock"></i></div>
            <div class="field">
              <label>{{ $t('auth.otpCode') }}</label>
              <input v-model="otpCode" class="otp-input ltr" inputmode="numeric" maxlength="6" placeholder="••••••" />
            </div>
            <button class="btn" :disabled="loading || otpCode.length < 6" @click="verifyOtp">
              <i class="bi bi-check2-circle" aria-hidden="true"></i>
              {{ loading ? $t('common.loading') : $t('auth.verifyAndCreate') }}
            </button>
            <button class="btn secondary resend-btn" type="button" :disabled="loading" @click="resendOtp">
              <i class="bi bi-arrow-clockwise" aria-hidden="true"></i>
              {{ $t('auth.resendCode') }}
            </button>
            <button class="linkish" type="button" @click="step = 'form'">{{ $t('auth.backToForm') }}</button>
          </div>
        </template>

        <p class="muted auth-footer-link">
          {{ $t('auth.hasAccount') }}
          <RouterLink to="/login">{{ $t('auth.loginLink') }}</RouterLink>
        </p>
        <p class="auth-secure-note">
          <i class="bi bi-lock-fill" aria-hidden="true"></i>
          {{ $t('auth.secureNote') }}
        </p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { api } from '../api'
import { useAuthStore } from '../stores/auth'
import LangSwitch from '../components/LangSwitch.vue'

const auth = useAuthStore()
const router = useRouter()
const { t } = useI18n()
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

const otpBanner = computed(() => {
  if (needPhone.value && needEmailChannel.value) return t('auth.otpBothRequired')
  if (needEmailChannel.value) return t('auth.otpEmailRequired')
  return t('auth.whatsappRequired')
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

const form = reactive({
  fullName: '',
  businessName: '',
  businessNameAr: '',
  email: '',
  contactPhone: '',
  websiteUrl: '',
  password: ''
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
      await auth.register(form)
      router.push('/merchant')
      return
    }
    const { data } = await api.post('/api/auth/register/send-otp', form)
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
    step.value = 'otp'
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.registerFail')
  } finally {
    loading.value = false
  }
}

async function verifyOtp() {
  error.value = ''
  loading.value = true
  try {
    await auth.verifyRegisterOtp(challengeId.value, otpCode.value)
    router.push('/merchant')
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
    const { data } = await api.post('/api/auth/register/send-otp', form)
    challengeId.value = data.challengeId
    maskedPhone.value = data.maskedPhone
    devCode.value = data.devCode || ''
    otpCode.value = data.devCode || ''
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.otpFail')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
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
