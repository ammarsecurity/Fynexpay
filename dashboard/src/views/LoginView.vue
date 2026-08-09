<template>
  <div class="auth-shell">
    <aside class="auth-aside">
      <div class="brand-row brand-row--auth">
        <img src="/icon-logo-white.png" alt="" class="brand-icon brand-icon--white" />
        <div class="brand-text">Fynex<span>pay</span></div>
      </div>
      <div>
        <h1>{{ $t('auth.asideLoginTitle') }}</h1>
        <p>{{ $t('auth.asideLoginBody') }}</p>
      </div>
      <p style="opacity:.8;margin:0;font-size:.9rem">© {{ year }} Fynexpay</p>
    </aside>

    <section class="auth-panel">
      <div class="auth-card">
        <img src="/full-logo.png" alt="Fynexpay" class="auth-card-logo" />
        <div class="auth-card-top">
          <div>
            <h2>{{ $t('auth.loginTitle') }}</h2>
            <p class="muted">{{ $t('auth.loginSub') }}</p>
          </div>
          <LangSwitch />
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <div class="field"><label>{{ $t('auth.email') }}</label><input v-model="email" type="email" placeholder="name@company.iq" /></div>
        <div class="field"><label>{{ $t('auth.password') }}</label><input v-model="password" type="password" placeholder="••••••••" /></div>
        <button class="btn" :disabled="loading" @click="submit">{{ loading ? $t('auth.signingIn') : $t('auth.signIn') }}</button>
        <p class="muted" style="margin-top:16px">{{ $t('auth.noAccount') }} <RouterLink to="/register">{{ $t('auth.registerLink') }}</RouterLink></p>
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

<style scoped>
.auth-card-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 8px;
}
.auth-card-top h2 { margin: 0 0 6px; }
.auth-card-top .muted { margin: 0; }
</style>
