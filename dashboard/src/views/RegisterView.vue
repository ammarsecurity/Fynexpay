<template>
  <div class="auth-shell">
    <aside class="auth-aside">
      <div class="brand-row brand-row--auth">
        <img src="/icon-logo-white.png" alt="" class="brand-icon brand-icon--white" />
        <div class="brand-text">Fynex<span>pay</span></div>
      </div>
      <div>
        <h1>{{ $t('auth.asideRegisterTitle') }}</h1>
        <p>{{ $t('auth.asideRegisterBody') }}</p>
      </div>
      <p style="opacity:.8;margin:0;font-size:.9rem">FIB · ZainCash · QI · SuperQi</p>
    </aside>

    <section class="auth-panel">
      <div class="auth-card">
        <img src="/full-logo.png" alt="Fynexpay" class="auth-card-logo" />
        <div class="auth-card-top">
          <div>
            <h2>{{ $t('auth.registerTitle') }}</h2>
            <p class="muted">{{ $t('auth.registerSub') }}</p>
          </div>
          <LangSwitch />
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <div class="field"><label>{{ $t('auth.fullName') }}</label><input v-model="form.fullName" /></div>
        <div class="field"><label>{{ $t('auth.businessName') }}</label><input v-model="form.businessName" placeholder="Shop Name" /></div>
        <div class="field"><label>{{ $t('auth.businessNameAr') }}</label><input v-model="form.businessNameAr" /></div>
        <div class="field"><label>{{ $t('auth.email') }}</label><input v-model="form.email" type="email" placeholder="merchant@shop.iq" /></div>
        <div class="field"><label>{{ $t('auth.phone') }}</label><input v-model="form.contactPhone" placeholder="07xxxxxxxxx" /></div>
        <div class="field"><label>{{ $t('auth.website') }}</label><input v-model="form.websiteUrl" placeholder="https://" /></div>
        <div class="field"><label>{{ $t('auth.password') }}</label><input v-model="form.password" type="password" placeholder="••••••••" /></div>
        <button class="btn" :disabled="loading" @click="submit">{{ loading ? $t('auth.creating') : $t('auth.createAccount') }}</button>
        <p class="muted" style="margin-top:16px">{{ $t('auth.hasAccount') }} <RouterLink to="/login">{{ $t('auth.loginLink') }}</RouterLink></p>
      </div>
    </section>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '../stores/auth'
import LangSwitch from '../components/LangSwitch.vue'

const auth = useAuthStore()
const router = useRouter()
const { t } = useI18n()
const error = ref('')
const loading = ref(false)
const form = reactive({
  fullName: '',
  businessName: '',
  businessNameAr: '',
  email: '',
  contactPhone: '',
  websiteUrl: '',
  password: ''
})

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.register(form)
    router.push('/merchant')
  } catch (e) {
    error.value = e.response?.data?.message || t('auth.registerFail')
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
