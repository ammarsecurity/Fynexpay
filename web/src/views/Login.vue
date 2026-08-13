<template>
  <SiteNav />

  <main class="auth-page">
    <div class="auth-shell">
      <section class="auth-card">
        <div class="card-head">
          <h2>{{ t('loginTitle') }}</h2>
          <p class="muted">{{ t('loginSub') }}</p>
        </div>

        <p v-if="error" class="error">{{ error }}</p>

        <form @submit.prevent="submit">
          <div class="field">
            <label>{{ t('email') }}</label>
            <input v-model="email" type="email" autocomplete="email" placeholder="name@company.iq" required />
          </div>
          <div class="field">
            <label>{{ t('password') }}</label>
            <input v-model="password" type="password" autocomplete="current-password" placeholder="••••••••" required />
          </div>
          <div class="forgot-row">
            <RouterLink to="/forgot" class="forgot-link">{{ t('forgotPassword') }}</RouterLink>
          </div>
          <button class="btn primary submit" type="submit" :disabled="loading">
            {{ loading ? t('signingIn') : t('signIn') }}
          </button>
        </form>

        <p class="footer-link muted">
          {{ t('noAccount') }}
          <RouterLink to="/register">{{ t('registerLink') }}</RouterLink>
        </p>
        <p class="secure">{{ t('secureNote') }}</p>
        <RouterLink class="back-home" to="/">{{ t('backHome') }}</RouterLink>
      </section>
    </div>
  </main>

  <SiteFooter />
</template>

<script setup>
import { ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { api } from '../api'
import { useLanding } from '../composables/useLanding'
import { handoffToDashboard, useAuthCopy } from '../composables/useAuth'

const { locale, dashboardUrl } = useLanding()
const { t } = useAuthCopy(locale)

const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/auth/login', {
      email: email.value,
      password: password.value
    })
    handoffToDashboard(dashboardUrl, data)
  } catch (e) {
    error.value = e.response?.data?.message || t('loginFail')
    loading.value = false
  }
}
</script>
