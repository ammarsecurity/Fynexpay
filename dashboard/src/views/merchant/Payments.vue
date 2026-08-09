<template>
  <div>
    <h1>المدفوعات</h1>
    <p class="muted">عمليات الدفع عبر Fynexpay</p>
    <div class="card">
      <table>
        <thead>
          <tr>
            <th>الطلب</th><th>المبلغ</th><th>الصافي</th><th>المزود</th><th>الحالة</th><th>التاريخ</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in payments" :key="p.id">
            <td>{{ p.orderId }}</td>
            <td>{{ format(p.amount) }}</td>
            <td>{{ format(p.netAmount) }}</td>
            <td>{{ p.provider }}</td>
            <td><span class="badge" :class="badge(p.status)">{{ p.status }}</span></td>
            <td>{{ formatDate(p.createdAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
      <p v-if="!payments.length" class="muted">لا توجد مدفوعات.</p>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'

const payments = ref([])
function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
function formatDate(v) { return new Date(v).toLocaleString('ar-IQ') }
function badge(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

onMounted(async () => {
  const { data } = await api.get('/api/merchant/payments')
  payments.value = data
})
</script>
