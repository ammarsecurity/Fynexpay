<template>
  <div>
    <h1>طلبات السحب</h1>
    <div class="card">
      <div class="field"><label>المبلغ (د.ع)</label><input v-model.number="amount" type="number" /></div>
      <div class="field"><label>طريقة التحويل</label>
        <select v-model="destinationType">
          <option>BankTransfer</option>
          <option>FibWallet</option>
          <option>ZainCash</option>
        </select>
      </div>
      <div class="field"><label>تفاصيل الحساب</label><textarea v-model="destinationDetails" rows="3" /></div>
      <p v-if="error" class="error">{{ error }}</p>
      <button class="btn" @click="create">طلب سحب</button>
    </div>
    <div class="card">
      <table>
        <thead><tr><th>المبلغ</th><th>الوجهة</th><th>الحالة</th><th>التاريخ</th></tr></thead>
        <tbody>
          <tr v-for="p in payouts" :key="p.id">
            <td>{{ format(p.amount) }}</td>
            <td>{{ p.destinationType }} — {{ p.destinationDetails }}</td>
            <td><span class="badge" :class="badge(p.status)">{{ p.status }}</span></td>
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

const payouts = ref([])
const amount = ref(10000)
const destinationType = ref('BankTransfer')
const destinationDetails = ref('')
const error = ref('')

function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
function badge(s) {
  if (s === 'Completed' || s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

async function load() {
  const { data } = await api.get('/api/merchant/payouts')
  payouts.value = data
}

async function create() {
  error.value = ''
  try {
    await api.post('/api/merchant/payouts', { amount: amount.value, destinationType: destinationType.value, destinationDetails: destinationDetails.value })
    await load()
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل الطلب'
  }
}

onMounted(load)
</script>
