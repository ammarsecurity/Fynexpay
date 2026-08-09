<template>
  <div class="page">
    <div class="shell">
      <aside class="sidebar">
        <div class="brand">Fynex<span>pay</span></div>
        <p class="muted" style="margin:0 0 18px;font-size:0.9rem">{{ auth.user?.fullName }}</p>
        <nav class="nav" v-if="auth.isMerchant">
          <RouterLink to="/merchant">نظرة عامة</RouterLink>
          <RouterLink to="/merchant/docs">دليل الربط</RouterLink>
          <RouterLink to="/merchant/test">تجربة الدفع</RouterLink>
          <RouterLink to="/merchant/payment-methods">طرق الدفع</RouterLink>
          <RouterLink to="/merchant/payments">المدفوعات</RouterLink>
          <RouterLink to="/merchant/keys">مفاتيح API</RouterLink>
          <RouterLink to="/merchant/payouts">السحب</RouterLink>
        </nav>
        <nav class="nav" v-if="auth.isAdmin">
          <RouterLink to="/admin">لوحة الإدارة</RouterLink>
          <RouterLink to="/admin/merchants">التجار</RouterLink>
          <RouterLink to="/admin/payments">كل المدفوعات</RouterLink>
          <RouterLink to="/admin/payouts">طلبات السحب</RouterLink>
          <RouterLink to="/admin/providers">المزودون</RouterLink>
        </nav>
        <div style="margin-top:24px">
          <button class="btn secondary" @click="logout">تسجيل الخروج</button>
        </div>
      </aside>
      <main class="main">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'

const auth = useAuthStore()
const router = useRouter()
function logout() {
  auth.logout()
  router.push('/login')
}
</script>
