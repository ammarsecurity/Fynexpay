<template>
  <div>
    <h1>مزودو الدفع</h1>
    <p class="muted">عدّل بيانات الاعتماد لكل بيئة، وبدّل بين Test و Production.</p>

    <div class="card" v-if="settings">
      <div class="row" style="justify-content:space-between;margin-bottom:8px">
        <div>
          <h3 style="margin:0">البيئة النشطة</h3>
          <p class="muted" style="margin:6px 0 0">المدفوعات الجديدة تستخدم إعدادات هذه البيئة</p>
        </div>
        <div class="env-toggle">
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Test' ? '' : 'secondary'"
            @click="switchEnv('Test')"
          >Test</button>
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Production' ? 'accent' : 'secondary'"
            @click="switchEnv('Production')"
          >Production</button>
        </div>
      </div>
      <p>
        الحالة الحالية:
        <span class="badge" :class="settings.activeEnvironment === 'Production' ? 'warn' : 'ok'">
          {{ settings.activeEnvironment }}
        </span>
      </p>
      <label class="row" style="gap:8px;margin-top:12px">
        <input type="checkbox" v-model="settings.useMockWhenMissingCredentials" />
        استخدام Mock عند غياب بيانات الاعتماد
      </label>
    </div>

    <p v-if="message" class="badge ok" style="margin-bottom:12px">{{ message }}</p>
    <p v-if="error" class="error">{{ error }}</p>

    <div v-if="settings" class="provider-list">
      <div class="card" v-for="item in providerCards" :key="item.key">
        <div class="row" style="justify-content:space-between">
          <div>
            <h3 style="margin:0">{{ item.title }}</h3>
            <p class="muted" style="margin:6px 0 0">الأولوية {{ settings[item.key].priority }}</p>
          </div>
          <label class="row" style="gap:8px">
            <input type="checkbox" v-model="settings[item.key].enabled" />
            مفعّل
          </label>
        </div>

        <div class="field" style="margin-top:14px;max-width:160px">
          <label>الأولوية</label>
          <input type="number" v-model.number="settings[item.key].priority" />
        </div>

        <div class="tabs">
          <button
            class="tab"
            :class="{ active: editEnv[item.key] === 'Test' }"
            @click="editEnv[item.key] = 'Test'"
          >بيانات Test</button>
          <button
            class="tab"
            :class="{ active: editEnv[item.key] === 'Production' }"
            @click="editEnv[item.key] = 'Production'"
          >بيانات Production</button>
        </div>

        <div class="creds-grid">
          <div class="field" v-for="f in item.fields" :key="f.key">
            <label>{{ f.label }}</label>
            <input
              :type="f.secret ? 'password' : 'text'"
              v-model="currentCreds(item.key)[f.key]"
              :placeholder="f.placeholder || ''"
              autocomplete="off"
            />
          </div>
        </div>
      </div>
    </div>

    <div class="row" style="margin-top:8px" v-if="settings">
      <button class="btn" :disabled="saving" @click="save">حفظ الإعدادات</button>
      <button class="btn accent" :disabled="saving" @click="loadDemo">تحميل بيانات Sandbox الرسمية</button>
      <button class="btn secondary" :disabled="saving" @click="load">إعادة تحميل</button>
    </div>
    <p class="muted" style="margin-top:10px" v-if="settings">
      QI / SuperQi و ZainCash يملكون بيانات تيست عامة. SuperQi يعمل عبر QI Gate (طريقة ALIPAY).
      FIB يحتاج تسجيل sandbox من البنك (support@fib-payment.com).
      زبون اختبار ZainCash غالباً: <span class="mono">9647802999569</span> / PIN <span class="mono">1111</span> / OTP <span class="mono">111111</span>
      <br />مرجع SuperQi:
      <a href="https://developers-gate.qi.iq/docs/category/pay-with-superqi" target="_blank" rel="noopener">developers-gate.qi.iq</a>
    </p>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { api } from '../../api'

const settings = ref(null)
const saving = ref(false)
const message = ref('')
const error = ref('')
const editEnv = reactive({
  fib: 'Test',
  zainCash: 'Test',
  qi: 'Test',
  superQi: 'Test'
})

const providerCards = [
  {
    key: 'fib',
    title: 'FIB',
    fields: [
      { key: 'authUrl', label: 'Auth URL' },
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'clientId', label: 'Client ID' },
      { key: 'clientSecret', label: 'Client Secret', secret: true }
    ]
  },
  {
    key: 'zainCash',
    title: 'ZainCash',
    fields: [
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'authUrl', label: 'Auth URL' },
      { key: 'merchantId', label: 'Merchant ID' },
      { key: 'msisdn', label: 'MSISDN' },
      { key: 'secret', label: 'Secret', secret: true },
      { key: 'clientId', label: 'Client ID (اختياري)' },
      { key: 'clientSecret', label: 'Client Secret (اختياري)', secret: true }
    ]
  },
  {
    key: 'qi',
    title: 'QI Gate (بطاقات)',
    fields: [
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'username', label: 'Username' },
      { key: 'password', label: 'Password', secret: true },
      { key: 'terminalId', label: 'Terminal ID' }
    ]
  },
  {
    key: 'superQi',
    title: 'SuperQi (Pay with SuperQi)',
    fields: [
      { key: 'baseUrl', label: 'Base URL (QI Gate)' },
      { key: 'username', label: 'Username' },
      { key: 'password', label: 'Password', secret: true },
      { key: 'terminalId', label: 'Terminal ID' }
    ]
  }
]

function currentCreds(key) {
  const bundle = settings.value[key]
  return editEnv[key] === 'Production' ? bundle.production : bundle.test
}

async function load() {
  error.value = ''
  const { data } = await api.get('/api/admin/providers')
  settings.value = data
  editEnv.fib = data.activeEnvironment === 'Production' ? 'Production' : 'Test'
  editEnv.zainCash = editEnv.fib
  editEnv.qi = editEnv.fib
  editEnv.superQi = editEnv.fib
}

async function save() {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.put('/api/admin/providers', settings.value)
    settings.value = data
    message.value = 'تم حفظ إعدادات المزودين'
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل الحفظ'
  } finally {
    saving.value = false
  }
}

async function switchEnv(environment) {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    // احفظ التعديلات الحالية أولاً ثم بدّل البيئة
    await api.put('/api/admin/providers', settings.value)
    const { data } = await api.post('/api/admin/providers/environment', { environment })
    settings.value = data
    editEnv.fib = environment
    editEnv.zainCash = environment
    editEnv.qi = environment
    editEnv.superQi = environment
    message.value = `تم التبديل إلى ${environment}`
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل تبديل البيئة'
  } finally {
    saving.value = false
  }
}

async function loadDemo() {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.post('/api/admin/providers/load-demo')
    settings.value = data
    editEnv.fib = 'Test'
    editEnv.zainCash = 'Test'
    editEnv.qi = 'Test'
    editEnv.superQi = 'Test'
    message.value = 'تم تحميل بيانات Sandbox الرسمية (QI + SuperQi + ZainCash). FIB يحتاج credentials من البنك.'
  } catch (e) {
    error.value = e.response?.data?.message || 'فشل تحميل الديمو'
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.env-toggle { display: flex; gap: 8px; }
.tabs {
  display: flex;
  gap: 8px;
  margin: 16px 0 12px;
}
.tab {
  border: 1px solid var(--line);
  background: transparent;
  border-radius: 999px;
  padding: 8px 14px;
  color: var(--muted);
}
.tab.active {
  background: rgba(15, 107, 92, 0.12);
  color: var(--brand-dark);
  border-color: transparent;
  font-weight: 600;
}
.creds-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
}
.provider-list { display: grid; gap: 16px; }
</style>
