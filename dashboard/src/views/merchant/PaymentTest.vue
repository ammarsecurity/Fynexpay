<template>
  <div class="tester">
    <div class="hero">
      <h1>تجربة الدفع</h1>
      <p class="muted">أنشئ دفعة بالمبلغ ونوع الخدمة فقط — الزبون يختار المزود من صفحة Fynexpay المستضافة.</p>
    </div>

    <div class="layout">
      <div class="card form-card">
        <h2>إنشاء دفعة تجريبية</h2>
        <p class="muted" v-if="merchantStatus && merchantStatus !== 'Active'">
          حسابك حالياً <span class="badge warn">{{ merchantStatus }}</span> — يجب تفعيله من الأدمن قبل الاختبار.
        </p>

        <div class="field">
          <label>المبلغ (د.ع)</label>
          <input v-model.number="form.amount" type="number" min="250" step="250" />
        </div>
        <div class="field">
          <label>نوع الخدمة</label>
          <input v-model="form.serviceType" placeholder="مثال: اشتراك شهري / شحن رصيد" />
        </div>
        <div class="field">
          <label>رقم الطلب orderId (اختياري)</label>
          <input v-model="form.orderId" placeholder="يُولَّد تلقائياً إن تُرك فارغاً" />
        </div>
        <div class="field">
          <label>callbackUrl (اختياري)</label>
          <input v-model="form.callbackUrl" placeholder="https://yoursite.com/hooks/fynexpay" />
        </div>
        <div class="field">
          <label>successUrl (اختياري)</label>
          <input v-model="form.successUrl" placeholder="https://yoursite.com/success" />
        </div>
        <div class="field">
          <label>failureUrl (اختياري)</label>
          <input v-model="form.failureUrl" placeholder="https://yoursite.com/fail" />
        </div>

        <p class="muted tip">المزودون الظاهرون للزبون تُضبط من <RouterLink to="/merchant/payment-methods">طرق الدفع</RouterLink>.</p>

        <p v-if="error" class="error">{{ error }}</p>
        <button class="btn" :disabled="loading || merchantStatus !== 'Active'" @click="createPayment">
          {{ loading ? 'جاري الإنشاء...' : 'إنشاء دفعة' }}
        </button>
      </div>

      <div class="card result-card" v-if="payment">
        <div class="row between">
          <h2 style="margin:0">نتيجة الدفعة</h2>
          <span class="badge" :class="statusClass(payment.status)">{{ payment.status }}</span>
        </div>

        <div class="meta">
          <div><span class="muted">Payment ID</span><div class="mono">{{ payment.id }}</div></div>
          <div><span class="muted">Order</span><div>{{ payment.orderId }}</div></div>
          <div><span class="muted">الخدمة</span><div>{{ payment.description }}</div></div>
          <div><span class="muted">المزود</span><div>{{ payment.provider }}</div></div>
          <div><span class="muted">المبلغ</span><div>{{ format(payment.amount) }}</div></div>
          <div><span class="muted">الصافي</span><div>{{ format(payment.netAmount) }}</div></div>
        </div>

        <div v-if="payment.availableProviders?.length" class="chips">
          <span class="muted">متاح للزبون:</span>
          <span v-for="p in payment.availableProviders" :key="p" class="badge">{{ p }}</span>
        </div>

        <div class="actions">
          <a
            v-if="payment.checkoutUrl"
            class="btn"
            :href="payment.checkoutUrl"
            target="_blank"
            rel="noopener"
          >فتح صفحة الدفع</a>
          <button class="btn secondary" @click="refreshStatus">تحديث الحالة</button>
        </div>

        <div class="steps-mini" v-if="payment.status === 'Pending'">
          <h3>الخطوات التالية</h3>
          <ol>
            <li>افتح صفحة الدفع المستضافة.</li>
            <li>اختر المزود (QI / ZainCash / FIB حسب إعداداتك).</li>
            <li>أكمل الدفع لدى المزود ثم حدّث الحالة هنا.</li>
          </ol>
        </div>

        <div class="success-box" v-if="payment.status === 'Paid'">
          تم الدفع بنجاح. الصافي {{ format(payment.netAmount) }} أُضيف لمحفظتك.
          <div class="row" style="margin-top:10px">
            <RouterLink class="btn secondary" to="/merchant">عرض المحفظة</RouterLink>
            <RouterLink class="btn secondary" to="/merchant/payments">كل المدفوعات</RouterLink>
          </div>
        </div>
      </div>

      <div class="card result-card empty" v-else>
        <h2>بانتظار إنشاء دفعة</h2>
        <p class="muted">املأ المبلغ ونوع الخدمة ثم افتح صفحة الدفع كما يفعل الزبون.</p>
        <RouterLink class="btn secondary" to="/merchant/docs">فتح دليل الربط</RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, reactive, ref } from 'vue'
import { api } from '../../api'

const form = reactive({
  amount: 5000,
  serviceType: 'اشتراك شهري',
  orderId: '',
  callbackUrl: '',
  successUrl: 'https://example.com/success',
  failureUrl: 'https://example.com/fail'
})

const payment = ref(null)
const loading = ref(false)
const error = ref('')
const merchantStatus = ref('')
let pollTimer = null

function format(v) {
  return new Intl.NumberFormat('ar-IQ').format(v ?? 0) + ' د.ع'
}
function statusClass(s) {
  if (s === 'Paid') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

async function loadMe() {
  const { data } = await api.get('/api/merchant/me')
  merchantStatus.value = data.status
}

async function createPayment() {
  error.value = ''
  loading.value = true
  try {
    const { data } = await api.post('/api/merchant/test-payments', {
      amount: form.amount,
      currency: 'IQD',
      orderId: form.orderId || null,
      serviceType: form.serviceType,
      description: form.serviceType,
      callbackUrl: form.callbackUrl || null,
      successUrl: form.successUrl || null,
      failureUrl: form.failureUrl || null
    })
    payment.value = data
    startPolling()
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل إنشاء الدفعة'
  } finally {
    loading.value = false
  }
}

async function refreshStatus() {
  if (!payment.value) return
  const { data } = await api.get(`/api/merchant/test-payments/${payment.value.id}`)
  payment.value = data
  if (data.status !== 'Pending') stopPolling()
}

function startPolling() {
  stopPolling()
  pollTimer = setInterval(refreshStatus, 4000)
}

function stopPolling() {
  if (pollTimer) {
    clearInterval(pollTimer)
    pollTimer = null
  }
}

onMounted(loadMe)
onUnmounted(stopPolling)
</script>

<style scoped>
.hero { margin-bottom: 16px; }
.layout {
  display: grid;
  grid-template-columns: minmax(280px, 380px) 1fr;
  gap: 18px;
  align-items: start;
}
.form-card h2, .result-card h2 { margin-top: 0; }
.row.between { justify-content: space-between; align-items: center; }
.meta {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(140px, 1fr));
  gap: 12px;
  margin: 16px 0;
}
.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  margin-bottom: 14px;
}
.chips { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 12px; }
.tip { font-size: 0.92rem; }
.steps-mini {
  margin-top: 16px;
  padding: 12px 14px;
  border-radius: 14px;
  background: rgba(15, 107, 92, 0.06);
}
.steps-mini h3 { margin: 0 0 8px; font-size: 1rem; }
.steps-mini ol { margin: 0; padding-right: 18px; color: var(--muted); line-height: 1.8; }
.success-box {
  margin-top: 14px;
  padding: 14px;
  border-radius: 14px;
  background: rgba(15, 122, 69, 0.1);
  border: 1px solid rgba(15, 122, 69, 0.2);
  font-weight: 600;
}
.empty {
  min-height: 280px;
  display: grid;
  align-content: center;
  gap: 10px;
}
@media (max-width: 900px) {
  .layout { grid-template-columns: 1fr; }
}
</style>
