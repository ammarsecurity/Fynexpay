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
        <div class="auth-providers">
          <span>FIB</span>
          <span>ZainCash</span>
          <span>QI</span>
          <span>SuperQi</span>
        </div>
      </div>
    </aside>

    <section class="auth-panel">
      <div class="auth-card">
        <img src="/full-logo.png" alt="Fynexpay" class="auth-card-logo" />
        <div class="auth-card-head">
          <div>
            <h2>{{ $t('auth.loginTitle') }}</h2>
            <p class="muted">{{ $t('auth.loginSub') }}</p>
          </div>
          <LangSwitch />
        </div>

        <p v-if="error" class="error">{{ error }}</p>

        <form @submit.prevent="submit">
          <div class="field">
            <label>{{ $t('auth.email') }}</label>
            <input v-model="email" type="email" autocomplete="email" placeholder="name@company.iq" />
          </div>
          <div class="field">
            <label>{{ $t('auth.password') }}</label>
            <input v-model="password" type="password" autocomplete="current-password" placeholder="••••••••" />
          </div>
          <button class="btn" type="submit" :disabled="loading">
            <i class="bi bi-box-arrow-in-left" aria-hidden="true"></i>
            {{ loading ? $t('auth.signingIn') : $t('auth.signIn') }}
          </button>
        </form>

        <p class="muted auth-footer-link">
          {{ $t('auth.noAccount') }}
          <RouterLink to="/register">{{ $t('auth.registerLink') }}</RouterLink>
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
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../stores/auth'
import LangSwitch from '../components/LangSwitch.vue'

const auth = useAuthStore()
const router = useRouter()
const { t } = useI18n()
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)
const year = new Date().getFullYear()

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    router.push('/')
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.loginFail')
  } finally {
    loading.value = false
  }
}
</script>
