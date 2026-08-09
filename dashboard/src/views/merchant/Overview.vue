<template>
  <div>
    <h1>نظرة عامة</h1>
    <p class="muted">محفظتك وحالة الحساب</p>
    <div v-if="merchant" class="grid">
      <div class="stat"><div class="label">الرصيد المتاح</div><div class="value">{{ format(wallet?.availableBalance) }}</div></div>
      <div class="stat"><div class="label">الرصيد المعلّق</div><div class="value">{{ format(wallet?.pendingBalance) }}</div></div>
      <div class="stat"><div class="label">إجمالي المقبوضات</div><div class="value">{{ format(wallet?.lifetimeGross) }}</div></div>
      <div class="stat"><div class="label">عمولة المنصة</div><div class="value">{{ merchant.commissionPercent }}%</div></div>
    </div>
    <div class="card" v-if="merchant">
      <h3>{{ merchant.businessName }}</h3>
      <p class="muted">الحالة: <span class="badge" :class="statusClass(merchant.status)">{{ merchant.status }}</span></p>
      <p class="muted" v-if="merchant.status === 'Pending'">بانتظار موافقة الإدارة لتفعيل حسابك واستخدام API.</p>
      <div class="row" style="margin-top:12px">
        <RouterLink class="btn" to="/merchant/docs">دليل ربط الدفع</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/keys">مفاتيح API</RouterLink>
      </div>
    </div>
    <div class="card">
      <h3>آخر حركات المحفظة</h3>
      <table v-if="wallet?.recentEntries?.length">
        <thead><tr><th>الوصف</th><th>النوع</th><th>المبلغ</th><th>الرصيد</th></tr></thead>
        <tbody>
          <tr v-for="e in wallet.recentEntries" :key="e.id">
            <td>{{ e.description }}</td>
            <td>{{ e.type }}</td>
            <td>{{ format(e.amount) }}</td>
            <td>{{ format(e.balanceAfter) }}</td>
          </tr>
        </tbody>
      </table>
      <p v-else class="muted">لا توجد حركات بعد.</p>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'

const merchant = ref(null)
const wallet = ref(null)

function format(v) {
  return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع'
}
function statusClass(s) {
  if (s === 'Active') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

onMounted(async () => {
  const [m, w] = await Promise.all([
    api.get('/api/merchant/me'),
    api.get('/api/merchant/wallet')
  ])
  merchant.value = m.data
  wallet.value = w.data
})
</script>
