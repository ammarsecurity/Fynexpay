<template>
  <div class="payouts">
    <div class="page-head">
      <div>
        <h1>{{ $t('payouts.title') }}</h1>
        <p class="sub">{{ $t('payouts.merchantSub') }}</p>
      </div>
    </div>

    <div class="kpi-grid">
      <div class="kpi">
        <span class="kpi-label">{{ $t('payouts.available') }}</span>
        <strong class="mono">{{ money(wallet.availableBalance) }}</strong>
      </div>
      <div class="kpi">
        <span class="kpi-label">{{ $t('payouts.pending') }}</span>
        <strong class="mono">{{ money(wallet.pendingBalance) }}</strong>
      </div>
      <div class="kpi">
        <span class="kpi-label">{{ $t('payouts.lifetime') }}</span>
        <strong class="mono">{{ money(wallet.lifetimePayouts) }}</strong>
      </div>
    </div>

    <div class="layout">
      <section class="card request-card">
        <div class="card-head">
          <div>
            <h2>{{ $t('payouts.create') }}</h2>
            <p class="muted">{{ $t('payouts.confirmHint') }}</p>
          </div>
        </div>

        <div v-if="!accountComplete" class="need-account">
          <i class="bi bi-bank" aria-hidden="true"></i>
          <div>
            <strong>{{ $t('payouts.needAccount') }}</strong>
            <p class="muted">{{ $t('payouts.bankIncomplete') }}</p>
          </div>
          <RouterLink class="btn" to="/merchant/profile">
            <i class="bi bi-pencil" aria-hidden="true"></i>
            {{ $t('payouts.addAccount') }}
          </RouterLink>
        </div>

        <form v-else class="request-form" @submit.prevent="create">
          <label class="field">
            <span>{{ $t('payouts.amountLabel') }}</span>
            <div class="amount-wrap">
              <input
                v-model.number="amount"
                type="number"
                min="1"
                step="1"
                required
                dir="ltr"
                class="amount-input"
                :placeholder="$t('payouts.amountPh')"
              />
              <span class="ccy">{{ $t('common.currency') }}</span>
            </div>
          </label>
          <p class="hint">{{ $t('payouts.useSaved') }}</p>
          <p v-if="error" class="error">{{ error }}</p>
          <p v-if="ok" class="ok-msg">{{ ok }}</p>
          <button class="btn" type="submit" :disabled="saving || !amount">
            {{ saving ? $t('common.loading') : $t('payouts.create') }}
          </button>
        </form>
      </section>

      <section class="card account-card">
        <div class="card-head">
          <div>
            <h2>{{ $t('profile.bankTitle') }}</h2>
            <p class="muted">{{ $t('profile.bankSub') }}</p>
          </div>
          <span class="badge" :class="accountComplete ? 'ok' : 'warn'">
            {{ accountComplete ? $t('profile.bankReady') : $t('profile.bankMissing') }}
          </span>
        </div>

        <dl v-if="accountComplete" class="bank-kv">
          <div>
            <dt>{{ $t('profile.bankName') }}</dt>
            <dd>{{ account.bankName }}</dd>
          </div>
          <div>
            <dt>{{ $t('profile.bankHolder') }}</dt>
            <dd>{{ account.bankAccountHolder }}</dd>
          </div>
          <div>
            <dt>{{ $t('profile.bankNumber') }}</dt>
            <dd class="mono ltr">{{ account.bankAccountNumber }}</dd>
          </div>
        </dl>
        <p v-else class="muted empty-account">{{ $t('payouts.needAccount') }}</p>
        <RouterLink class="btn secondary" to="/merchant/profile">
          {{ accountComplete ? $t('payouts.editAccount') : $t('payouts.addAccount') }}
        </RouterLink>
      </section>
    </div>

    <DataToolbar
      v-model="filters"
      :statuses="['Pending', 'Approved', 'Completed', 'Rejected']"
      :search-placeholder="$t('payouts.searchPlaceholder')"
      :status-all-label="$t('payouts.allStatuses')"
      @apply="applyFilters"
      @reset="resetFilters"
    />

    <div class="card table-card">
      <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>{{ $t('common.amount') }}</th>
            <th>{{ $t('payouts.destination') }}</th>
            <th>{{ $t('common.status') }}</th>
            <th>{{ $t('common.date') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in payouts" :key="p.id">
            <td class="mono">{{ money(p.amount) }}</td>
            <td class="dest">{{ p.destinationDetails }}</td>
            <td><span class="badge" :class="badge(p.status)">{{ $t(`status.${p.status}`, p.status) }}</span></td>
            <td>{{ formatDate(p.createdAtUtc) }}</td>
          </tr>
        </tbody>
      </table>
      </div>
      <div v-if="!payouts.length" class="empty">
        <strong>{{ $t('payouts.emptyTitle') }}</strong>
        <p class="muted">{{ $t('payouts.emptyHint') }}</p>
      </div>
      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total="total"
        @change="load"
      />
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import DataToolbar from '../../components/DataToolbar.vue'
import PaginationBar from '../../components/PaginationBar.vue'

const { t, locale } = useI18n()
const payouts = ref([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const amount = ref(null)
const error = ref('')
const ok = ref('')
const saving = ref(false)
const wallet = reactive({
  availableBalance: 0,
  pendingBalance: 0,
  lifetimePayouts: 0
})
const account = reactive({
  bankName: '',
  bankAccountHolder: '',
  bankAccountNumber: '',
  bankIban: '',
  isComplete: false
})
const filters = reactive({ q: '', status: '', from: '', to: '' })
const applied = reactive({ q: '', status: '', from: '', to: '' })
const accountComplete = computed(() => !!account.isComplete)

function money(v) {
  const loc = locale.value === 'ar' ? 'en-IQ' : 'en-IQ'
  return new Intl.NumberFormat(loc).format(Math.round(Number(v ?? 0))) + (locale.value === 'ar' ? ' د.ع' : ' IQD')
}
function formatDate(v) {
  return new Date(v).toLocaleString(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
    dateStyle: 'medium',
    timeStyle: 'short'
  })
}
function badge(s) {
  if (s === 'Completed' || s === 'Approved') return 'ok'
  if (s === 'Pending') return 'warn'
  return 'danger'
}

async function loadWallet() {
  const [{ data: w }, { data: a }] = await Promise.all([
    api.get('/api/merchant/wallet'),
    api.get('/api/merchant/payout-account')
  ])
  wallet.availableBalance = w.availableBalance || 0
  wallet.pendingBalance = w.pendingBalance || 0
  wallet.lifetimePayouts = w.lifetimePayouts || 0
  account.bankName = a.bankName || ''
  account.bankAccountHolder = a.bankAccountHolder || ''
  account.bankAccountNumber = a.bankAccountNumber || ''
  account.bankIban = a.bankIban || ''
  account.isComplete = !!a.isComplete
}

async function load() {
  const { data } = await api.get('/api/merchant/payouts', {
    params: {
      page: page.value,
      pageSize: pageSize.value,
      q: applied.q || undefined,
      status: applied.status || undefined,
      from: applied.from || undefined,
      to: applied.to || undefined
    }
  })
  payouts.value = data.items || []
  total.value = data.total || 0
}

function applyFilters() {
  Object.assign(applied, filters)
  page.value = 1
  load()
}

function resetFilters() {
  filters.q = ''
  filters.status = ''
  filters.from = ''
  filters.to = ''
  applyFilters()
}

async function create() {
  error.value = ''
  ok.value = ''
  saving.value = true
  try {
    await api.post('/api/merchant/payouts', { amount: amount.value })
    ok.value = t('payouts.ok')
    amount.value = null
    await Promise.all([load(), loadWallet()])
  } catch (e) {
    error.value = e.response?.data?.message || t('payouts.fail')
  } finally {
    saving.value = false
  }
}

onMounted(async () => {
  await Promise.all([load(), loadWallet()])
})
</script>

<style scoped>
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 16px;
}
.kpi {
  background: var(--card);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: 16px 18px;
  box-shadow: var(--shadow-sm);
  display: grid;
  gap: 8px;
}
.kpi-label { color: var(--muted); font-size: 0.85rem; font-weight: 600; }
.kpi strong { font-size: 1.35rem; font-weight: 800; }
.layout {
  display: grid;
  grid-template-columns: minmax(0, 1.1fr) minmax(0, 0.9fr);
  gap: 16px;
  margin-bottom: 16px;
}
.card-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  margin-bottom: 16px;
}
.card-head h2 { margin: 0 0 4px; font-size: 1.1rem; }
.request-form, .need-account { display: grid; gap: 14px; }
.need-account {
  grid-template-columns: auto 1fr auto;
  align-items: center;
  padding: 16px;
  border-radius: 14px;
  background: #fffbeb;
  border: 1px solid #fcd34d;
}
.need-account i { font-size: 1.4rem; color: #b45309; }
.field { display: grid; gap: 6px; }
.field span { font-size: 0.85rem; color: var(--muted); }
.amount-wrap {
  display: flex;
  align-items: center;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  background: #fff;
  overflow: hidden;
}
.amount-input {
  flex: 1;
  border: 0;
  padding: 12px 14px;
  font: inherit;
  font-size: 1.15rem;
  font-weight: 700;
  outline: none;
}
.ccy {
  padding: 0 14px;
  font-weight: 700;
  color: var(--muted);
  border-inline-start: 1px solid #e2e8f0;
}
.hint { margin: 0; color: var(--muted); font-size: 0.85rem; }
.bank-kv { display: grid; gap: 10px; margin: 0 0 16px; }
.bank-kv div { display: grid; gap: 2px; }
.bank-kv dt { font-size: 0.78rem; color: var(--muted); }
.bank-kv dd { margin: 0; font-weight: 700; }
.ltr { direction: ltr; text-align: start; }
.dest { max-width: 420px; white-space: pre-wrap; font-size: 0.88rem; }
.empty { text-align: center; padding: 28px 12px; }
.empty strong { display: block; margin-bottom: 6px; }
.empty-account { margin: 0 0 16px; }
@media (max-width: 900px) {
  .kpi-grid, .layout { grid-template-columns: 1fr; }
  .need-account { grid-template-columns: 1fr; }
}
</style>
