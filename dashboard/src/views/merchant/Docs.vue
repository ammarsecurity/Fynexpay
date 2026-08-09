<template>
  <div class="docs">
    <div class="docs-hero">
      <h1>دليل ربط الدفع</h1>
      <p class="muted">اتبع الخطوات التالية لربط موقعك أو تطبيقك مع Fynexpay خلال دقائق.</p>
    </div>

    <div class="steps">
      <div class="step" v-for="(s, i) in steps" :key="i">
        <div class="num">{{ i + 1 }}</div>
        <div>
          <h3>{{ s.title }}</h3>
          <p class="muted">{{ s.text }}</p>
          <RouterLink v-if="s.link" class="btn secondary" :to="s.link">{{ s.linkText }}</RouterLink>
        </div>
      </div>
    </div>

    <div class="card">
      <h2>كيف تتم العملية؟</h2>
      <div class="flow">
        <div class="flow-item">تطبيقك يطلب إنشاء دفعة (مبلغ + نوع خدمة)</div>
        <div class="arrow">←</div>
        <div class="flow-item">Fynexpay يرجع صفحة دفع مستضافة</div>
        <div class="arrow">←</div>
        <div class="flow-item">الزبون يختار المزود ويدفع</div>
        <div class="arrow">←</div>
        <div class="flow-item">نشعرك بالنتيجة + نضيف الصافي لمحفظتك</div>
      </div>
    </div>

    <div class="card">
      <div class="row between">
        <h2>1) أنشئ دفعة</h2>
        <button class="btn secondary" @click="copy(createExample, 'تم نسخ مثال إنشاء الدفعة')">نسخ المثال</button>
      </div>
      <p class="muted">أرسل طلباً من سيرفرك (لا تضع المفتاح داخل التطبيق للموبايل مباشرة).</p>
      <pre class="code mono" dir="ltr">{{ createExample }}</pre>

      <h3>الحقول المهمة</h3>
      <table>
        <thead>
          <tr><th>الحقل</th><th>مطلوب؟</th><th>الشرح</th></tr>
        </thead>
        <tbody>
          <tr><td class="mono">amount</td><td>نعم</td><td>المبلغ بالدينار العراقي (الحد الأدنى 250)</td></tr>
          <tr><td class="mono">serviceType</td><td>نعم</td><td>نوع الخدمة أو الوصف الذي يظهر للزبون (أو <code>description</code>)</td></tr>
          <tr><td class="mono">orderId</td><td>لا</td><td>رقم الطلب عندك — يُولَّد تلقائياً إن لم تُرسله</td></tr>
          <tr><td class="mono">callbackUrl</td><td>اختياري</td><td>رابط على سيرفرك نستدعيه عند تغيّر الحالة</td></tr>
          <tr><td class="mono">successUrl</td><td>اختياري</td><td>صفحة العودة بعد نجاح الدفع</td></tr>
          <tr><td class="mono">failureUrl</td><td>اختياري</td><td>صفحة العودة عند الفشل</td></tr>
        </tbody>
      </table>

      <h3>ماذا يرجع الرد؟</h3>
      <pre class="code mono" dir="ltr">{{ createResponse }}</pre>
      <p class="muted">وجّه الزبون إلى <code>checkoutUrl</code> — صفحة Fynexpay حيث يختار طريقة الدفع.</p>
    </div>

    <div class="card">
      <div class="row between">
        <h2>2) تحقق من حالة الدفعة</h2>
        <button class="btn secondary" @click="copy(statusExample, 'تم نسخ مثال الاستعلام')">نسخ المثال</button>
      </div>
      <p class="muted">حتى مع الـ webhook، يفضّل دائماً التأكد من السيرفر قبل تسليم الطلب.</p>
      <pre class="code mono" dir="ltr">{{ statusExample }}</pre>
      <p>الحالات: <span class="badge">Pending</span> <span class="badge ok">Paid</span> <span class="badge danger">Failed</span> <span class="badge warn">Cancelled</span></p>
    </div>

    <div class="card">
      <div class="row between">
        <h2>3) استقبل إشعار Webhook</h2>
        <button class="btn secondary" @click="copy(secret || '', secret ? 'تم نسخ Webhook Secret' : 'لا يوجد سر')">نسخ السر</button>
      </div>
      <p class="muted">عند تغيّر الحالة نرسل POST إلى <code>callbackUrl</code> مع توقيع HMAC.</p>
      <ul class="bullets">
        <li>الترويسة: <code>X-Fynexpay-Signature</code></li>
        <li>الخوارزمية: HMAC-SHA256 على جسم الطلب (raw body) باستخدام Webhook Secret</li>
        <li>قارن التوقيع ثم حدّث طلبك فقط إذا الحالة <code>Paid</code></li>
      </ul>
      <div class="copy-box" v-if="secret">
        <code class="mono">{{ secret }}</code>
        <RouterLink class="btn secondary" to="/merchant/keys">إدارة المفاتيح</RouterLink>
      </div>
      <pre class="code mono" dir="ltr">{{ webhookExample }}</pre>
    </div>

    <div class="card">
      <h2>اختيار مزود الدفع</h2>
      <p class="muted">التاجر لا يختار المزود عند الإنشاء. الزبون يختار من صفحة الدفع حسب ما فعّلته في <RouterLink to="/merchant/payment-methods">طرق الدفع</RouterLink> (متقاطع مع مزودي المنصة).</p>
      <div class="providers">
        <div class="provider">
          <h3>FIB</h3>
          <p class="muted">دفع عبر تطبيق FIB.</p>
        </div>
        <div class="provider">
          <h3>ZainCash</h3>
          <p class="muted">محفظة زين كاش.</p>
        </div>
        <div class="provider">
          <h3>QI</h3>
          <p class="muted">بطاقات QI والبطاقات البنكية.</p>
        </div>
        <div class="provider">
          <h3>SuperQi</h3>
          <p class="muted">محفظة SuperQi عبر QI Gate (ALIPAY).</p>
        </div>
      </div>
    </div>

    <div class="card tip">
      <h2>نصائح سريعة</h2>
      <ul class="bullets">
        <li>استخدم المفتاح من السيرفر فقط، ولا تعرضه في الفرونتاند.</li>
        <li>أرسل <code>X-Idempotency-Key</code> عند إنشاء الدفعة لتجنب التكرار عند إعادة المحاولة.</li>
        <li>بعد <code>Paid</code> يُضاف صافي المبلغ لمحفظتك ناقص عمولة المنصة.</li>
        <li>للتجربة بدون مزود حقيقي: المنصة تدعم Mock عبر الأدمن عند غياب credentials.</li>
      </ul>
      <div class="row" style="margin-top:14px;gap:10px;flex-wrap:wrap">
        <a class="btn" :href="swaggerUrl" target="_blank" rel="noopener">فتح Swagger</a>
        <RouterLink class="btn accent" to="/merchant/test">تجربة الدفع الآن</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/keys">إنشاء مفتاح API</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/payments">عرض المدفوعات</RouterLink>
      </div>
    </div>

    <div v-if="toast" class="toast">{{ toast }}</div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { api, API_BASE } from '../../api'

const secret = ref('')
const toast = ref('')
let timer = null

const swaggerUrl = `${API_BASE}/swagger`
const steps = [
  { title: 'فعّل حسابك', text: 'بعد التسجيل ينتظر حسابك موافقة الإدارة. عند التفعيل يظهر رصيدك ويمكنك استخدام API.', link: '/merchant', linkText: 'نظرة عامة' },
  { title: 'أنشئ مفتاح API', text: 'من صفحة المفاتيح انسخ المفتاح فوراً واحفظه في السيرفر.', link: '/merchant/keys', linkText: 'المفاتيح' },
  { title: 'أنشئ دفعة من سيرفرك', text: 'استدعِ POST /v1/payments بالمبلغ ونوع الخدمة ثم وجّه الزبون لـ checkoutUrl.', link: null },
  { title: 'أكد النتيجة', text: 'اعتمد على webhook + استعلام الحالة قبل تسليم المنتج أو الخدمة.', link: '/merchant/payments', linkText: 'المدفوعات' }
]

const createExample = computed(() => `curl -X POST ${API_BASE}/v1/payments \\
  -H "X-Api-Key: YOUR_API_KEY" \\
  -H "Content-Type: application/json" \\
  -H "X-Idempotency-Key: order-1001" \\
  -d '{
    "amount": 5000,
    "serviceType": "اشتراك شهري",
    "callbackUrl": "https://yoursite.com/hooks/fynexpay",
    "successUrl": "https://yoursite.com/success"
  }'`)

const createResponse = `{
  "id": "5bac8b83-....",
  "orderId": "ORD-....",
  "amount": 5000,
  "status": "Pending",
  "provider": "PendingSelection",
  "description": "اشتراك شهري",
  "checkoutUrl": "http://localhost:5080/checkout/....",
  "availableProviders": ["Qi", "ZainCash", "Fib"],
  "platformFee": 125,
  "netAmount": 4875
}`

const statusExample = computed(() => `curl ${API_BASE}/v1/payments/PAYMENT_ID \\
  -H "X-Api-Key: YOUR_API_KEY"`)

const webhookExample = `{
  "id": "5bac8b83-....",
  "orderId": "ORD-1001",
  "amount": 5000,
  "currency": "IQD",
  "status": "Paid",
  "provider": "Fib",
  "platformFee": 125,
  "netAmount": 4875,
  "paidAtUtc": "2026-08-09T13:40:00Z"
}`

function showToast(msg) {
  toast.value = msg
  clearTimeout(timer)
  timer = setTimeout(() => { toast.value = '' }, 2000)
}

async function copy(text, okMsg) {
  if (!text) return
  try {
    await navigator.clipboard.writeText(text)
  } catch {
    const el = document.createElement('textarea')
    el.value = text
    document.body.appendChild(el)
    el.select()
    document.execCommand('copy')
    document.body.removeChild(el)
  }
  showToast(okMsg)
}

onMounted(async () => {
  try {
    const { data } = await api.get('/api/merchant/webhook-secret')
    secret.value = data.secret
  } catch {
    secret.value = ''
  }
})
</script>

<style scoped>
.docs-hero { margin-bottom: 18px; }
.docs-hero h1 { margin-bottom: 8px; }
.steps {
  display: grid;
  gap: 12px;
  margin-bottom: 18px;
}
.step {
  display: grid;
  grid-template-columns: 44px 1fr;
  gap: 14px;
  align-items: start;
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 16px;
}
.num {
  width: 44px;
  height: 44px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: rgba(15, 107, 92, 0.12);
  color: var(--brand-dark);
  font-weight: 700;
}
.step h3 { margin: 0 0 6px; }
.flow {
  display: flex;
  flex-wrap: wrap;
  gap: 10px;
  align-items: center;
}
.flow-item {
  background: rgba(15, 107, 92, 0.08);
  border-radius: 12px;
  padding: 10px 14px;
  font-weight: 600;
}
.arrow { color: var(--muted); }
.row.between { justify-content: space-between; align-items: center; margin-bottom: 8px; }
.card h2 { margin-top: 0; }
.code {
  background: #15221f;
  color: #d7fff2;
  border-radius: 14px;
  padding: 14px;
  overflow: auto;
  font-size: 0.86rem;
  line-height: 1.55;
  white-space: pre-wrap;
}
.bullets { margin: 10px 0 0; padding-right: 18px; color: var(--muted); line-height: 1.8; }
.providers {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 12px;
}
.provider {
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 14px;
  background: #fff;
}
.provider h3 { margin: 0 0 6px; }
.tip { border-color: rgba(196, 92, 38, 0.28); }
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
  margin: 12px 0;
  overflow: auto;
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
  z-index: 40;
}
@media (max-width: 700px) {
  .flow .arrow { display: none; }
}
</style>
