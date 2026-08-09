<template>
  <div>
    <h1>طلبات السحب</h1>
    <div class="card">
      <table>
        <thead>
          <tr><th>المبلغ</th><th>التفاصيل</th><th>الحالة</th><th>التاريخ</th><th>إجراء</th></tr>
        </thead>
        <tbody>
          <tr v-for="p in payouts" :key="p.id">
            <td>{{ format(p.amount) }}</td>
            <td>{{ p.destinationType }} — {{ p.destinationDetails }}</td>
            <td><span class="badge">{{ p.status }}</span></td>
            <td>{{ new Date(p.createdAtUtc).toLocaleString('ar-IQ') }}</td>
            <td class="row" v-if="p.status === 'Pending' || p.status === 'Approved'">
              <button class="btn" @click="review(p.id, 'approve')">موافقة</button>
              <button class="btn accent" @click="review(p.id, 'complete')">إتمام التحويل</button>
              <button class="btn danger" @click="review(p.id, 'reject')">رفض</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'
const payouts = ref([])
function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
async function load() {
  const { data } = await api.get('/api/admin/payouts')
  payouts.value = data
}
async function review(id, action) {
  await api.post(`/api/admin/payouts/${id}/review`, { action, adminNote: '' })
  await load()
}
onMounted(load)
</script>
