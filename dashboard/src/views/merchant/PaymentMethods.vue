<template>
  <div class="methods">
    <div class="hero">
      <h1>طرق الدفع</h1>
      <p class="muted">اختر المزودين الذين يظهرون لزبائنك في صفحة الدفع المستضافة. المزودون المعطّلون من المنصة لا يظهرون حتى لو فعّلتهم هنا.</p>
    </div>

    <div class="card">
      <p v-if="message" class="ok-msg">{{ message }}</p>
      <p v-if="error" class="error">{{ error }}</p>

      <div class="list" v-if="form">
        <label class="item" :class="{ off: !platformHas('Fib') }">
          <div>
            <strong>FIB</strong>
            <p class="muted">تطبيق First Iraqi Bank</p>
            <span v-if="!platformHas('Fib')" class="badge warn">غير مفعّل من المنصة</span>
          </div>
          <input v-model="form.fibEnabled" type="checkbox" :disabled="!platformHas('Fib')" />
        </label>
        <label class="item" :class="{ off: !platformHas('ZainCash') }">
          <div>
            <strong>ZainCash</strong>
            <p class="muted">محفظة زين كاش</p>
            <span v-if="!platformHas('ZainCash')" class="badge warn">غير مفعّل من المنصة</span>
          </div>
          <input v-model="form.zainCashEnabled" type="checkbox" :disabled="!platformHas('ZainCash')" />
        </label>
        <label class="item" :class="{ off: !platformHas('Qi') }">
          <div>
            <strong>QI Card</strong>
            <p class="muted">بطاقات QI والبطاقات البنكية</p>
            <span v-if="!platformHas('Qi')" class="badge warn">غير مفعّل من المنصة</span>
          </div>
          <input v-model="form.qiEnabled" type="checkbox" :disabled="!platformHas('Qi')" />
        </label>
        <label class="item" :class="{ off: !platformHas('SuperQi') }">
          <div>
            <strong>SuperQi</strong>
            <p class="muted">ادفع عبر تطبيق SuperQi (ALIPAY)</p>
            <span v-if="!platformHas('SuperQi')" class="badge warn">غير مفعّل من المنصة</span>
          </div>
          <input v-model="form.superQiEnabled" type="checkbox" :disabled="!platformHas('SuperQi')" />
        </label>
      </div>

      <div class="effective" v-if="form">
        <h3>ما يراه الزبون الآن</h3>
        <div class="chips">
          <span v-for="p in form.effectiveProviders" :key="p" class="badge ok">{{ p }}</span>
          <span v-if="!form.effectiveProviders?.length" class="badge danger">لا يوجد مزود متاح</span>
        </div>
      </div>

      <button class="btn" :disabled="saving" @click="save">{{ saving ? 'جاري الحفظ...' : 'حفظ الإعدادات' }}</button>
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { api } from '../../api'

const form = ref(null)
const saving = ref(false)
const message = ref('')
const error = ref('')

function platformHas(name) {
  return form.value?.platformEnabled?.includes(name)
}

async function load() {
  const { data } = await api.get('/api/merchant/payment-methods')
  form.value = data
}

async function save() {
  if (!form.value) return
  saving.value = true
  error.value = ''
  message.value = ''
  try {
    const { data } = await api.put('/api/merchant/payment-methods', {
      fibEnabled: form.value.fibEnabled,
      zainCashEnabled: form.value.zainCashEnabled,
      qiEnabled: form.value.qiEnabled,
      superQiEnabled: form.value.superQiEnabled
    })
    form.value = data
    message.value = 'تم حفظ طرق الدفع'
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل الحفظ'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.hero { margin-bottom: 16px; }
.list { display: grid; gap: 10px; margin: 12px 0 18px; }
.item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid var(--line);
  border-radius: 16px;
  background: #fff;
  cursor: pointer;
}
.item.off { opacity: .65; }
.item p { margin: 4px 0 0; }
.item input { width: 20px; height: 20px; }
.effective { margin-bottom: 16px; }
.effective h3 { margin: 0 0 8px; font-size: 1rem; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; }
.ok-msg { color: #0f7a45; font-weight: 600; }
</style>
