<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="brand">Fynex<span>pay</span></div>
      <h2>تسجيل الدخول</h2>
      <p class="muted">ادخل إلى لوحة التاجر أو الإدارة</p>
      <p v-if="error" class="error">{{ error }}</p>
      <div class="field"><label>البريد</label><input v-model="email" type="email" /></div>
      <div class="field"><label>كلمة المرور</label><input v-model="password" type="password" /></div>
      <button class="btn" :disabled="loading" @click="submit">دخول</button>
      <p class="muted" style="margin-top:16px">ليس لديك حساب؟ <RouterLink to="/register">سجّل كتاجر</RouterLink></p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
const email = ref('')
const password = ref('')
const error = ref('')
const loading = ref(false)

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login(email.value, password.value)
    router.push('/')
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل تسجيل الدخول'
  } finally {
    loading.value = false
  }
}
</script>
