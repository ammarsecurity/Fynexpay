<template>
  <div>
    <h1>التجار</h1>
    <div class="card">
      <table>
        <thead>
          <tr>
            <th>النشاط</th><th>البريد</th><th>الحالة</th><th>العمولة</th><th>الرصيد</th><th>إجراء</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in merchants" :key="m.id">
            <td>{{ m.businessName }}</td>
            <td>{{ m.contactEmail }}</td>
            <td><span class="badge" :class="m.status === 'Active' ? 'ok' : 'warn'">{{ m.status }}</span></td>
            <td>
              <input style="width:80px" type="number" step="0.1" v-model.number="m.commissionPercent" />
            </td>
            <td>{{ format(m.availableBalance) }}</td>
            <td class="row">
              <button class="btn" @click="save(m, 'Active')">تفعيل</button>
              <button class="btn secondary" @click="save(m)">حفظ العمولة</button>
              <button class="btn danger" @click="save(m, 'Suspended')">تعليق</button>
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
const merchants = ref([])
function format(v) { return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع' }
async function load() {
  const { data } = await api.get('/api/admin/merchants')
  merchants.value = data
}
async function save(m, status) {
  await api.patch(`/api/admin/merchants/${m.id}`, {
    status: status || undefined,
    commissionPercent: m.commissionPercent
  })
  await load()
}
onMounted(load)
</script>
