<template>
  <div>
    <h1>كل المدفوعات</h1>
    <div class="card">
      <table>
        <thead>
          <tr><th>المعرف</th><th>الطلب</th><th>المبلغ</th><th>العمولة</th><th>المزود</th><th>الحالة</th><th>التاريخ</th></tr>
        </thead>
        <tbody>
          <tr v-for="p in payments" :key="p.id">
            <td class="mono">{{ p.id.slice(0, 8) }}</td>
            <td>{{ p.orderId }}</td>
            <td>{{ format(p.amount) }}</td>
            <td>{{ format(p.platformFee) }}</td>
            <td>{{ p.provider }}</td>
            <td><span class="badge" :class="p.status === 'Paid' ? 'ok' : 'warn'">{{ p.status }}</span></td>
            <td>{{ new Date(p.createdAtUtc).toLocaleString('ar-IQ') }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'
const payments = ref([])
function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
onMounted(async () => {
  const { data } = await api.get('/api/admin/payments')
  payments.value = data
})
</script>
