<template>
  <div>
    <div class="page-head">
      <div>
        <h1>مرحباً بعودتك، {{ firstName }} 👋</h1>
        <p class="sub">ملخص محفظتك وحالة حساب التاجر في Fynexpay.</p>
      </div>
      <div class="row">
        <span class="badge" :class="statusClass(merchant?.status)" v-if="merchant">{{ merchant.status }}</span>
        <RouterLink class="btn" to="/merchant/test">+ دفعة جديدة</RouterLink>
      </div>
    </div>

    <div class="grid" v-if="merchant">
      <div class="stat">
        <div class="ico purple">$</div>
        <div class="label">الرصيد المتاح</div>
        <div class="value">{{ format(wallet?.availableBalance) }}</div>
        <div class="trend">جاهز للسحب</div>
      </div>
      <div class="stat">
        <div class="ico amber">…</div>
        <div class="label">الرصيد المعلّق</div>
        <div class="value">{{ format(wallet?.pendingBalance) }}</div>
        <div class="trend">قيد التسوية</div>
      </div>
      <div class="stat">
        <div class="ico green">↑</div>
        <div class="label">إجمالي المقبوضات</div>
        <div class="value">{{ format(wallet?.lifetimeGross) }}</div>
        <div class="trend">+ نمو المحفظة</div>
      </div>
      <div class="stat">
        <div class="ico sky">%</div>
        <div class="label">عمولة المنصة</div>
        <div class="value">{{ merchant.commissionPercent }}%</div>
        <div class="trend">على كل دفعة ناجحة</div>
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-head">
          <div>
            <h3>{{ merchant?.businessName || 'متجرك' }}</h3>
            <p class="muted" style="margin:0">{{ merchant?.businessNameAr || merchant?.contactEmail }}</p>
          </div>
          <RouterLink class="btn secondary" to="/merchant/docs">دليل الربط</RouterLink>
        </div>
        <p class="muted" v-if="merchant?.status === 'Pending'">
          حسابك بانتظار موافقة الإدارة. بعد التفعيل يمكنك إنشاء مدفوعات حقيقية.
        </p>
        <div class="chart-line" style="height:180px">
          <svg viewBox="0 0 400 160" preserveAspectRatio="none">
            <defs>
              <linearGradient id="mg" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#6c3cec" stop-opacity="0.3" />
                <stop offset="100%" stop-color="#6c3cec" stop-opacity="0" />
              </linearGradient>
            </defs>
            <path d="M0 120 C50 100, 90 130, 130 80 S210 30, 250 60 S320 110, 400 40 L400 160 L0 160 Z" fill="url(#mg)" />
            <path d="M0 120 C50 100, 90 130, 130 80 S210 30, 250 60 S320 110, 400 40" fill="none" stroke="#6c3cec" stroke-width="3" />
          </svg>
        </div>
        <div class="row" style="margin-top:8px">
          <RouterLink class="btn" to="/merchant/keys">مفاتيح API</RouterLink>
          <RouterLink class="btn secondary" to="/merchant/payment-methods">طرق الدفع</RouterLink>
        </div>
      </div>

      <div class="card ledger-card">
        <div class="card-head">
          <div>
            <h3>{{ $t('merchantOverview.ledgerTitle') }}</h3>
            <p class="muted ledger-sub">{{ $t('merchantOverview.ledgerSub') }}</p>
          </div>
          <RouterLink class="btn ghost" to="/merchant/payments">{{ $t('nav.payments') }}</RouterLink>
        </div>
        <div v-if="wallet?.recentEntries?.length" class="ledger-list">
          <article class="tx-item" v-for="e in wallet.recentEntries.slice(0, 6)" :key="e.id">
            <div class="tx-ico" :class="entryTone(e)">{{ entryIcon(e) }}</div>
            <div class="tx-main">
              <div class="tx-title-row">
                <strong>{{ entryTitle(e) }}</strong>
                <span class="tx-chip" :class="entryTone(e)">{{ entryTypeLabel(e.type) }}</span>
              </div>
              <div class="tx-meta">
                <span v-if="entryRef(e)" class="mono">{{ entryRef(e) }}</span>
                <span>{{ when(e.createdAtUtc) }}</span>
              </div>
            </div>
            <div class="tx-money">
              <div class="amt mono" :class="entryTone(e)">{{ signedMoney(e.amount) }}</div>
              <div class="tx-balance muted">{{ $t('merchantOverview.balanceAfter') }} {{ format(e.balanceAfter) }}</div>
            </div>
          </article>
        </div>
        <p v-else class="muted">{{ $t('merchantOverview.ledgerEmpty') }}</p>
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-head"><h3>جاهزية الربط</h3></div>
        <div class="progress-list">
          <div class="progress-row">
            <div class="top"><span>تفعيل الحساب</span><span>{{ merchant?.status === 'Active' ? 100 : 40 }}%</span></div>
            <div class="bar"><span :style="{ width: (merchant?.status === 'Active' ? 100 : 40) + '%' }"></span></div>
          </div>
          <div class="progress-row">
            <div class="top"><span>مفاتيح API</span><span>80%</span></div>
            <div class="bar"><span style="width:80%"></span></div>
          </div>
          <div class="progress-row">
            <div class="top"><span>طرق الدفع</span><span>90%</span></div>
            <div class="bar"><span style="width:90%"></span></div>
          </div>
        </div>
      </div>
      <div class="card">
        <div class="card-head"><h3>اختصارات سريعة</h3></div>
        <div class="row">
          <RouterLink class="btn secondary" to="/merchant/test">تجربة الدفع</RouterLink>
          <RouterLink class="btn secondary" to="/merchant/payouts">طلب سحب</RouterLink>
          <RouterLink class="btn secondary" to="/merchant/docs">التوثيق</RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useAuthStore } from '../../stores/auth'

const { t, locale } = useI18n()
const auth = useAuthStore()
const merchant = ref(null)
const wallet = ref(null)
const firstName = computed(() => (auth.user?.fullName || merchant.value?.businessName || 'تاجر').split(/\s+/)[0])

function format(v) {
  const loc = locale.value === 'ar' ? 'ar-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(v ?? 0) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function signedMoney(v) {
  const n = Number(v ?? 0)
  const sign = n > 0 ? '+' : n < 0 ? '−' : ''
  return sign + format(Math.abs(n))
}
function when(v) {
  if (!v) return '—'
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}
function statusClass(s) {
  if (s === 'Active') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}
function entryTypeLabel(type) {
  return t(`merchantOverview.ledgerTypes.${type}`, type || '—')
}
function entryTone(e) {
  const n = Number(e?.amount ?? 0)
  if (n > 0) return 'credit'
  if (n < 0) return 'debit'
  return 'neutral'
}
function entryIcon(e) {
  const tone = entryTone(e)
  if (tone === 'credit') return '↑'
  if (tone === 'debit') return '↓'
  return '•'
}
function shortId(id) {
  if (!id) return ''
  return String(id).replace(/-/g, '').slice(0, 8).toUpperCase()
}
function entryRef(e) {
  if (e.paymentId) return `#${shortId(e.paymentId)}`
  if (e.payoutRequestId) return `#${shortId(e.payoutRequestId)}`
  const m = String(e.description || '').match(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i)
  return m ? `#${shortId(m[0])}` : ''
}
function entryTitle(e) {
  const label = entryTypeLabel(e.type)
  if (label && label !== e.type) return label
  const cleaned = String(e.description || '')
    .replace(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi, '')
    .replace(/\s{2,}/g, ' ')
    .trim()
  return cleaned || label || '—'
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
