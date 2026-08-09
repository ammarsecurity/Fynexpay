<template>
  <div>
    <h1>لوحة الإدارة</h1>
    <p class="muted">ملخص منصة Fynexpay</p>
    <div class="grid" v-if="stats">
      <div class="stat"><div class="label">التجار</div><div class="value">{{ stats.merchantsCount }}</div></div>
      <div class="stat"><div class="label">نشطون</div><div class="value">{{ stats.activeMerchants }}</div></div>
      <div class="stat"><div class="label">المدفوعات</div><div class="value">{{ stats.paymentsCount }}</div></div>
      <div class="stat"><div class="label">حجم العمليات</div><div class="value">{{ format(stats.grossVolume) }}</div></div>
      <div class="stat"><div class="label">إيراد العمولة</div><div class="value">{{ format(stats.platformFees) }}</div></div>
      <div class="stat"><div class="label">سحوبات معلّقة</div><div class="value">{{ stats.pendingPayouts }}</div></div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'
const stats = ref(null)
function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
onMounted(async () => {
  const { data } = await api.get('/api/admin/stats')
  stats.value = data
})
</script>
