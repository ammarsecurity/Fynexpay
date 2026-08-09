<template>
  <div class="auth-wrap">
    <div class="auth-card">
      <div class="brand">Fynex<span>pay</span></div>
      <h2>تسجيل تاجر</h2>
      <p class="muted">أنشئ حساباً واحصل على محفظة وAPI للدفع</p>
      <p v-if="error" class="error">{{ error }}</p>
      <div class="field"><label>الاسم الكامل</label><input v-model="form.fullName" /></div>
      <div class="field"><label>اسم النشاط</label><input v-model="form.businessName" /></div>
      <div class="field"><label>الاسم بالعربي (اختياري)</label><input v-model="form.businessNameAr" /></div>
      <div class="field"><label>البريد</label><input v-model="form.email" type="email" /></div>
      <div class="field"><label>الهاتف</label><input v-model="form.contactPhone" /></div>
      <div class="field"><label>الموقع</label><input v-model="form.websiteUrl" /></div>
      <div class="field"><label>كلمة المرور</label><input v-model="form.password" type="password" /></div>
      <button class="btn" :disabled="loading" @click="submit">إنشاء الحساب</button>
      <p class="muted" style="margin-top:16px">لديك حساب؟ <RouterLink to="/login">تسجيل الدخول</RouterLink></p>
    </div>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const router = useRouter()
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
    error.value = e.response?.data?.message || 'فشل التسجيل'
  } finally {
    loading.value = false
  }
}
</script>
