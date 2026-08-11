<template>
  <div class="docs">
    <div class="page-head">
      <div>
        <h1>{{ $t('docs.title') }}</h1>
        <p class="sub">{{ $t('docs.subtitle') }}</p>
      </div>
      <div class="row">
        <RouterLink class="btn secondary" to="/merchant/platforms">{{ $t('docs.ctaPlatforms') }}</RouterLink>
        <RouterLink class="btn" to="/merchant/test">{{ $t('docs.ctaTest') }}</RouterLink>
      </div>
    </div>

    <section class="card platform-banner">
      <div class="banner-copy">
        <span class="eyebrow">{{ $t('docs.platformBadge') }}</span>
        <h2>{{ $t('docs.platformTitle') }}</h2>
        <p>{{ $t('docs.platformBody') }}</p>
        <ul class="rules">
          <li>{{ $t('docs.ruleKey') }}</li>
          <li>{{ $t('docs.ruleCors') }}</li>
          <li>{{ $t('docs.ruleServer') }}</li>
        </ul>
      </div>
      <div class="banner-side">
        <div v-if="approvedPlatforms.length" class="platform-list">
          <div v-for="p in approvedPlatforms" :key="p.id" class="platform-pill">
            <strong>{{ p.name }}</strong>
            <span class="mono" dir="ltr">{{ p.domain }}</span>
            <span class="badge ok">{{ $t('status.Approved') }}</span>
          </div>
        </div>
        <div v-else class="empty-platform">
          <p>{{ $t('docs.noPlatform') }}</p>
          <RouterLink class="btn" to="/merchant/platforms">{{ $t('docs.addPlatform') }}</RouterLink>
        </div>
      </div>
    </section>

    <section class="steps">
      <article v-for="(s, i) in steps" :key="i" class="step card">
        <div class="num">{{ i + 1 }}</div>
        <div class="step-body">
          <h3>{{ s.title }}</h3>
          <p>{{ s.text }}</p>
          <RouterLink v-if="s.link" class="btn secondary sm" :to="s.link">{{ s.linkText }}</RouterLink>
        </div>
      </article>
    </section>

    <section class="card">
      <h2>{{ $t('docs.flowTitle') }}</h2>
      <div class="flow">
        <div class="flow-item" v-for="(f, i) in flow" :key="i">
          <span class="flow-n">{{ i + 1 }}</span>
          <span>{{ f }}</span>
        </div>
      </div>
    </section>

    <section class="card">
      <div class="section-head">
        <div>
          <h2>{{ $t('docs.createTitle') }}</h2>
          <p class="muted">{{ $t('docs.createHint') }}</p>
        </div>
        <button class="btn secondary" type="button" @click="copy(createExample, $t('docs.copied'))">{{ $t('docs.copy') }}</button>
      </div>
      <pre class="code mono" dir="ltr">{{ createExample }}</pre>

      <h3>{{ $t('docs.fieldsTitle') }}</h3>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{{ $t('docs.field') }}</th>
              <th>{{ $t('docs.required') }}</th>
              <th>{{ $t('docs.desc') }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in fields" :key="row.field">
              <td class="mono">{{ row.field }}</td>
              <td>{{ row.required }}</td>
              <td>{{ row.desc }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <h3>{{ $t('docs.responseTitle') }}</h3>
      <pre class="code mono" dir="ltr">{{ createResponse }}</pre>
      <p class="muted">{{ $t('docs.checkoutHint') }}</p>
    </section>

    <div class="dash-grid">
      <section class="card">
        <div class="section-head">
          <div>
            <h2>{{ $t('docs.statusTitle') }}</h2>
            <p class="muted">{{ $t('docs.statusHint') }}</p>
          </div>
          <button class="btn secondary" type="button" @click="copy(statusExample, $t('docs.copied'))">{{ $t('docs.copy') }}</button>
        </div>
        <pre class="code mono" dir="ltr">{{ statusExample }}</pre>
        <div class="chips">
          <span class="badge warn">Pending</span>
          <span class="badge ok">Paid</span>
          <span class="badge danger">Failed</span>
          <span class="badge">Cancelled</span>
        </div>
      </section>

      <section class="card">
        <div class="section-head">
          <div>
            <h2>{{ $t('docs.webhookTitle') }}</h2>
            <p class="muted">{{ $t('docs.webhookHint') }}</p>
          </div>
          <button class="btn secondary" type="button" :disabled="!secret" @click="copy(secret, $t('docs.copied'))">{{ $t('docs.copySecret') }}</button>
        </div>
        <ul class="bullets">
          <li><code>X-Fynexpay-Signature</code></li>
          <li>HMAC-SHA256</li>
          <li>{{ $t('docs.webhookPaidOnly') }}</li>
        </ul>
        <div v-if="secret" class="copy-box">
          <code class="mono" dir="ltr">{{ secret }}</code>
        </div>
        <pre class="code mono" dir="ltr">{{ webhookExample }}</pre>
      </section>
    </div>

    <section class="card">
      <h2>{{ $t('docs.providersTitle') }}</h2>
      <p class="muted">{{ $t('docs.providersHint') }} <RouterLink to="/merchant/payment-methods">{{ $t('nav.paymentMethods') }}</RouterLink></p>
      <div class="providers">
        <div class="provider" v-for="p in providers" :key="p.key">
          <img class="prov-logo" :src="p.logo" alt="" />
          <span>{{ p.desc }}</span>
        </div>
      </div>
    </section>

    <section class="card tip">
      <h2>{{ $t('docs.tipsTitle') }}</h2>
      <ul class="bullets">
        <li>{{ $t('docs.tip1') }}</li>
        <li>{{ $t('docs.tip2') }}</li>
        <li>{{ $t('docs.tip3') }}</li>
        <li>{{ $t('docs.tip4') }}</li>
        <li>{{ $t('docs.tipSwagger') }}</li>
      </ul>
      <div class="row actions">
        <a class="btn" :href="swaggerUrl" target="_blank" rel="noopener">{{ $t('docs.openApi') }}</a>
        <RouterLink class="btn secondary" to="/merchant/platforms">{{ $t('docs.ctaPlatforms') }}</RouterLink>
        <RouterLink class="btn secondary" to="/merchant/payments">{{ $t('nav.payments') }}</RouterLink>
      </div>
    </section>

    <div v-if="toast" class="toast">{{ toast }}</div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api, API_BASE } from '../../api'
import { mediaUrl, useProviders } from '../../composables/useProviders'

const { t } = useI18n()
const { catalog, ensureCatalog, logoOf } = useProviders()
const secret = ref('')
const platforms = ref([])
const toast = ref('')
let timer = null

const swaggerUrl = `${API_BASE}/swagger/index.html?urls.primaryName=${encodeURIComponent('Merchant API')}`
const approvedPlatforms = computed(() => (platforms.value || []).filter(p => p.status === 'Approved'))
const sampleDomain = computed(() => approvedPlatforms.value[0]?.domain || 'shop.example.com')
const sampleKey = 'YOUR_PLATFORM_API_KEY'

const steps = computed(() => [
  { title: t('docs.step1Title'), text: t('docs.step1Body'), link: '/merchant', linkText: t('nav.overview') },
  { title: t('docs.step2Title'), text: t('docs.step2Body'), link: '/merchant/platforms', linkText: t('nav.platforms') },
  { title: t('docs.step3Title'), text: t('docs.step3Body'), link: null },
  { title: t('docs.step4Title'), text: t('docs.step4Body'), link: '/merchant/payments', linkText: t('nav.payments') }
])

const flow = computed(() => [
  t('docs.flow1'),
  t('docs.flow2'),
  t('docs.flow3'),
  t('docs.flow4')
])

const fields = computed(() => [
  { field: 'amount', required: t('docs.yes'), desc: t('docs.fieldAmount') },
  { field: 'serviceType', required: t('docs.yes'), desc: t('docs.fieldService') },
  { field: 'orderId', required: t('docs.no'), desc: t('docs.fieldOrder') },
  { field: 'callbackUrl', required: t('docs.optional'), desc: t('docs.fieldCallback') },
  { field: 'successUrl', required: t('docs.optional'), desc: t('docs.fieldSuccess') },
  { field: 'failureUrl', required: t('docs.optional'), desc: t('docs.fieldFailure') }
])

const providers = computed(() => {
  const hintByKey = {
    fib: t('docs.provFib'),
    zaincash: t('docs.provZain'),
    qi: t('docs.provQi'),
    superqi: t('docs.provSuperQi'),
    alqaseh: t('docs.provAlqaseh')
  }
  const list = catalog.value || []
  if (!list.length) {
    return ['Fib', 'ZainCash', 'Qi', 'SuperQi', 'Alqaseh'].map((key) => ({
      key,
      logo: logoOf(key),
      desc: hintByKey[key.toLowerCase()] || t('docs.provFib')
    }))
  }
  return list
    .filter((p) => p.enabled !== false)
    .map((p) => ({
      key: p.key,
      logo: mediaUrl(p.logoUrl) || logoOf(p.key),
      desc: hintByKey[String(p.key).toLowerCase()] || t('methods.customerSees')
    }))
})

const createExample = computed(() => {
  const origin = sampleDomain.value.startsWith('localhost') || sampleDomain.value.startsWith('127.0.0.1')
    ? `http://${sampleDomain.value}`
    : `https://${sampleDomain.value}`
  return `curl -X POST ${API_BASE}/v1/payments \\
  -H "X-Api-Key: ${sampleKey}" \\
  -H "Content-Type: application/json" \\
  -H "X-Idempotency-Key: order-1001" \\
  -H "Origin: ${origin}" \\
  -d '{
    "amount": 5000,
    "serviceType": "Monthly subscription",
    "orderId": "ORD-1001",
    "callbackUrl": "${origin}/hooks/fynexpay",
    "successUrl": "${origin}/success",
    "failureUrl": "${origin}/failed"
  }'`
})

const createResponse = `{
  "id": "5bac8b83-....",
  "orderId": "ORD-....",
  "amount": 5000,
  "currency": "IQD",
  "status": "Pending",
  "provider": "PendingSelection",
  "description": "Monthly subscription",
  "checkoutUrl": "${API_BASE}/checkout/....",
  "createdAtUtc": "2026-08-11T19:40:36Z",
  "paidAtUtc": null,
  "expiredAtUtc": "2026-08-11T20:40:36Z",
  "failureReason": null
}`

const statusExample = computed(() => `curl ${API_BASE}/v1/payments/PAYMENT_ID \\
  -H "X-Api-Key: ${sampleKey}" \\
  -H "Origin: https://${sampleDomain.value}"`)

const webhookExample = `{
  "id": "5bac8b83-....",
  "orderId": "ORD-1001",
  "amount": 5000,
  "currency": "IQD",
  "status": "Paid",
  "provider": "Fib",
  "description": "Monthly subscription",
  "checkoutUrl": "${API_BASE}/checkout/....",
  "createdAtUtc": "2026-08-11T19:40:36Z",
  "paidAtUtc": "2026-08-11T19:41:10Z",
  "expiredAtUtc": "2026-08-11T20:40:36Z",
  "failureReason": null
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
  ensureCatalog()
  try {
    const [s, p] = await Promise.all([
      api.get('/api/merchant/webhook-secret'),
      api.get('/api/merchant/platforms')
    ])
    secret.value = s.data.secret || ''
    platforms.value = p.data || []
  } catch {
    secret.value = ''
    platforms.value = []
  }
})
</script>

<style scoped>
.platform-banner {
  display: grid;
  grid-template-columns: 1.4fr 1fr;
  gap: 20px;
  background:
    radial-gradient(700px 220px at 100% 0%, rgba(3, 24, 56, 0.12), transparent 55%),
    #fff;
  border-color: rgba(3, 24, 56, 0.18);
}
.eyebrow {
  display: inline-block;
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--brand-secondary);
  background: var(--brand-soft);
  border-radius: 999px;
  padding: 5px 10px;
  margin-bottom: 10px;
}
.banner-copy h2 {
  margin: 0 0 8px;
  color: var(--brand);
  font-size: 1.25rem;
}
.banner-copy p {
  margin: 0 0 12px;
  color: var(--muted);
  font-weight: 600;
  line-height: 1.6;
}
.rules {
  margin: 0;
  padding: 0;
  list-style: none;
  display: grid;
  gap: 8px;
}
.rules li {
  position: relative;
  padding-inline-start: 18px;
  color: var(--brand);
  font-weight: 700;
  font-size: 0.92rem;
}
.rules li::before {
  content: "";
  position: absolute;
  inset-inline-start: 0;
  top: 0.55em;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--brand-secondary);
}
.platform-list { display: grid; gap: 10px; }
.platform-pill {
  display: grid;
  gap: 4px;
  padding: 12px 14px;
  border: 1px solid var(--line);
  border-radius: 14px;
  background: #f8fafc;
}
.platform-pill strong { color: var(--brand); }
.platform-pill .mono { font-size: 0.82rem; color: var(--muted); }
.empty-platform {
  border: 1px dashed rgba(3, 24, 56, 0.35);
  border-radius: 16px;
  padding: 18px;
  background: rgba(3, 24, 56, 0.04);
  display: grid;
  gap: 12px;
  align-content: start;
}
.empty-platform p { margin: 0; color: var(--muted); font-weight: 600; }

.steps {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 12px;
  margin-bottom: 18px;
}
.step {
  margin: 0;
  display: grid;
  gap: 12px;
  align-content: start;
}
.num {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  font-weight: 800;
  color: #fff;
  background: linear-gradient(145deg, var(--brand), var(--brand-secondary));
  box-shadow: 0 10px 20px rgba(3, 24, 56, 0.25);
}
.step-body h3 { margin: 0 0 6px; color: var(--brand); font-size: 0.98rem; }
.step-body p { margin: 0 0 12px; color: var(--muted); font-size: 0.88rem; font-weight: 600; line-height: 1.5; }
.btn.sm { padding: 8px 12px; font-size: 0.82rem; box-shadow: none; }

.flow {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
}
.flow-item {
  display: grid;
  gap: 8px;
  padding: 14px;
  border-radius: 14px;
  border: 1px solid var(--line);
  background: #f8fafc;
  font-weight: 700;
  color: var(--brand);
  font-size: 0.9rem;
  line-height: 1.45;
}
.flow-n {
  width: 24px;
  height: 24px;
  border-radius: 8px;
  display: grid;
  place-items: center;
  font-size: 0.75rem;
  background: var(--brand-soft);
  color: var(--brand-secondary);
}

.section-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 12px;
}
.section-head h2, .card > h2 { margin: 0 0 6px; color: var(--brand); }
.section-head .muted, .card > .muted { margin: 0; }
.card h3 {
  margin: 18px 0 10px;
  font-size: 0.95rem;
  color: var(--brand);
}
.code {
  background: #031838;
  color: #e2e8f0;
  border-radius: 16px;
  padding: 14px 16px;
  overflow: auto;
  font-size: 0.82rem;
  line-height: 1.55;
  white-space: pre-wrap;
  margin: 0;
}
.table-wrap { overflow: auto; border: 1px solid var(--line); border-radius: 14px; }
table { width: 100%; border-collapse: collapse; }
th, td { padding: 11px 12px; text-align: start; border-bottom: 1px solid var(--line); font-size: 0.9rem; }
th { background: #f8fafc; color: var(--muted); font-size: 0.78rem; }
tr:last-child td { border-bottom: 0; }
.chips { display: flex; flex-wrap: wrap; gap: 8px; margin-top: 12px; }
.bullets {
  margin: 0 0 12px;
  padding-inline-start: 18px;
  color: var(--muted);
  font-weight: 600;
  line-height: 1.8;
}
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 10px 12px;
  margin-bottom: 12px;
  background: #f8fafc;
  overflow: auto;
}
.providers {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  margin-top: 12px;
}
.provider {
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 14px;
  background: #f8fafc;
  display: flex;
  align-items: center;
  gap: 12px;
}
.provider .prov-logo {
  width: 36px;
  height: 36px;
  object-fit: contain;
  border-radius: 10px;
  border: 1px solid var(--line);
  background: #fff;
  flex-shrink: 0;
}
.provider span { color: var(--muted); font-size: 0.85rem; font-weight: 600; }
.tip {
  background:
    radial-gradient(600px 180px at 0% 0%, rgba(3, 24, 56, 0.08), transparent 55%),
    #fff;
}
.actions { margin-top: 14px; gap: 10px; flex-wrap: wrap; }
.toast {
  position: fixed;
  bottom: 24px;
  left: 50%;
  transform: translateX(-50%);
  background: var(--brand);
  color: #fff;
  padding: 12px 18px;
  border-radius: 999px;
  z-index: 40;
  font-weight: 700;
  box-shadow: var(--shadow);
}
@media (max-width: 1100px) {
  .steps, .flow, .providers { grid-template-columns: 1fr 1fr; }
  .platform-banner { grid-template-columns: 1fr; }
}
@media (max-width: 700px) {
  .steps, .flow, .providers { grid-template-columns: 1fr; }
  .section-head { flex-direction: column; }
}
</style>
