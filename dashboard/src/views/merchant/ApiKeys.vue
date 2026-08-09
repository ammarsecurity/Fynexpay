<template>
  <div>
    <div class="head">
      <div>
        <h1>مفاتيح API</h1>
        <p class="muted">أنشئ مفتاحاً لاستدعاء `/v1` عبر ترويسة `X-Api-Key`</p>
      </div>
    </div>

    <div class="card create-card">
      <h3>إنشاء مفتاح جديد</h3>
      <div class="create-row">
        <div class="field" style="flex:1;margin:0">
          <label>اسم المفتاح</label>
          <input v-model="name" placeholder="مثلاً: Production أو Mobile App" @keyup.enter="createKey" />
        </div>
        <button class="btn" :disabled="creating || !name.trim()" @click="createKey">
          {{ creating ? 'جاري الإنشاء...' : 'إنشاء مفتاح' }}
        </button>
      </div>

      <div v-if="createdKey" class="reveal">
        <div class="reveal-top">
          <div>
            <strong>المفتاح جاهز — انسخه الآن</strong>
            <p class="muted" style="margin:4px 0 0">لن يظهر مرة أخرى بعد مغادرة الصفحة أو إنشاء مفتاح جديد.</p>
          </div>
          <button class="btn secondary" @click="dismissReveal">إخفاء</button>
        </div>

        <div class="copy-box">
          <code class="mono value">{{ createdKey }}</code>
          <button class="btn" @click="copy(createdKey, 'تم نسخ مفتاح API')">نسخ المفتاح</button>
        </div>

        <div class="copy-box subtle">
          <code class="mono value small">X-Api-Key: {{ createdKey }}</code>
          <button class="btn secondary" @click="copy(`X-Api-Key: ${createdKey}`, 'تم نسخ الترويسة')">نسخ الترويسة</button>
        </div>

        <div class="copy-box subtle">
          <code class="mono value small">{{ curlExample }}</code>
          <button class="btn secondary" @click="copy(curlExample, 'تم نسخ مثال curl')">نسخ curl</button>
        </div>
      </div>
    </div>

    <div class="card">
      <div class="row" style="justify-content:space-between;margin-bottom:8px">
        <h3 style="margin:0">المفاتيح الحالية</h3>
        <span class="muted">{{ activeCount }} نشط / {{ keys.length }} إجمالي</span>
      </div>

      <div v-if="!keys.length" class="empty">لا توجد مفاتيح بعد. أنشئ أول مفتاح أعلاه.</div>

      <div v-else class="keys-list">
        <div class="key-item" v-for="k in keys" :key="k.id">
          <div class="key-main">
            <div class="key-title">
              <strong>{{ k.name }}</strong>
              <span class="badge" :class="k.isActive ? 'ok' : 'danger'">{{ k.isActive ? 'نشط' : 'ملغى' }}</span>
            </div>
            <div class="key-meta muted">
              <span>أُنشئ: {{ formatDate(k.createdAtUtc) }}</span>
              <span v-if="k.lastUsedAtUtc">آخر استخدام: {{ formatDate(k.lastUsedAtUtc) }}</span>
              <span v-else>لم يُستخدم بعد</span>
            </div>
            <div class="copy-box compact">
              <code class="mono value">{{ k.keyPrefix }}••••••••</code>
              <button class="btn secondary" @click="copy(k.keyPrefix, 'تم نسخ بادئة المفتاح')">نسخ البادئة</button>
            </div>
          </div>
          <div class="key-actions">
            <button v-if="k.isActive" class="btn danger" @click="revoke(k)">إلغاء المفتاح</button>
          </div>
        </div>
      </div>
    </div>

    <div class="card">
      <h3>Webhook Secret</h3>
      <p class="muted">استخدمه للتحقق من توقيع `X-Fynexpay-Signature` (HMAC-SHA256)</p>
      <div class="copy-box">
        <code class="mono value">{{ secret || '—' }}</code>
        <button class="btn" :disabled="!secret" @click="copy(secret, 'تم نسخ Webhook Secret')">نسخ السر</button>
      </div>
    </div>

    <div v-if="toast" class="toast">{{ toast }}</div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { api, API_BASE } from '../../api'

const keys = ref([])
const name = ref('Production')
const createdKey = ref('')
const secret = ref('')
const creating = ref(false)
const toast = ref('')
let toastTimer = null

const activeCount = computed(() => keys.value.filter(k => k.isActive).length)
const curlExample = computed(() =>
  `curl -X POST ${API_BASE}/v1/payments \\\n  -H "X-Api-Key: ${createdKey.value}" \\\n  -H "Content-Type: application/json" \\\n  -d "{\\"amount\\":5000,\\"orderId\\":\\"ORD-1\\",\\"provider\\":\\"auto\\"}"`
)

function formatDate(v) {
  return new Date(v).toLocaleString('ar-IQ')
}

function showToast(msg) {
  toast.value = msg
  clearTimeout(toastTimer)
  toastTimer = setTimeout(() => { toast.value = '' }, 2200)
}

async function copy(text, successMsg) {
  try {
    await navigator.clipboard.writeText(text)
    showToast(successMsg)
  } catch {
    const el = document.createElement('textarea')
    el.value = text
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
    showToast(successMsg)
  }
}

async function load() {
  const [k, s] = await Promise.all([
    api.get('/api/merchant/api-keys'),
    api.get('/api/merchant/webhook-secret')
  ])
  keys.value = k.data
  secret.value = s.data.secret
}

async function createKey() {
  if (!name.value.trim()) return
  creating.value = true
  try {
    const { data } = await api.post('/api/merchant/api-keys', { name: name.value.trim() })
    createdKey.value = data.apiKey
    showToast('تم إنشاء المفتاح')
    await load()
  } catch (e) {
    showToast(e.response?.data?.message || 'فشل إنشاء المفتاح')
  } finally {
    creating.value = false
  }
}

async function revoke(k) {
  if (!confirm(`إلغاء المفتاح "${k.name}"؟ لن يعمل بعدها في أي طلب.`)) return
  await api.delete(`/api/merchant/api-keys/${k.id}`)
  showToast('تم إلغاء المفتاح')
  await load()
}

function dismissReveal() {
  createdKey.value = ''
}

onMounted(load)
</script>

<style scoped>
.head { margin-bottom: 8px; }
.create-card h3 { margin-top: 0; }
.create-row {
  display: flex;
  gap: 12px;
  align-items: end;
  flex-wrap: wrap;
}
.reveal {
  margin-top: 18px;
  padding: 16px;
  border-radius: 16px;
  border: 1px solid rgba(15, 107, 92, 0.25);
  background: rgba(15, 107, 92, 0.06);
}
.reveal-top {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: start;
  margin-bottom: 12px;
  flex-wrap: wrap;
}
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
  margin-bottom: 10px;
}
.copy-box.subtle { background: #faf7f0; }
.copy-box.compact { margin: 10px 0 0; }
.value {
  flex: 1;
  overflow-x: auto;
  white-space: nowrap;
  font-size: 0.95rem;
}
.value.small {
  white-space: pre-wrap;
  font-size: 0.82rem;
  line-height: 1.5;
}
.keys-list { display: grid; gap: 12px; margin-top: 12px; }
.key-item {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px;
  border: 1px solid var(--line);
  border-radius: 16px;
  background: #fff;
  flex-wrap: wrap;
}
.key-title {
  display: flex;
  gap: 10px;
  align-items: center;
  flex-wrap: wrap;
}
.key-meta {
  display: flex;
  gap: 14px;
  flex-wrap: wrap;
  margin-top: 6px;
  font-size: 0.88rem;
}
.key-actions { display: flex; align-items: center; }
.empty {
  padding: 18px;
  color: var(--muted);
  text-align: center;
}
.toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: var(--brand-dark);
  color: #fff;
  padding: 12px 18px;
  border-radius: 999px;
  box-shadow: var(--shadow);
  z-index: 50;
}
@media (max-width: 700px) {
  .create-row { flex-direction: column; align-items: stretch; }
  .create-row .btn { width: 100%; }
}
</style>
