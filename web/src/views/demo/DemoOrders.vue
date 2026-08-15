<template>
  <div class="nx-page">
    <header class="nx-page-head">
      <h1>{{ t('demoOrders') }}</h1>
    </header>

    <div v-if="!orders.length" class="nx-empty">
      <i class="bi bi-receipt"></i>
      <p>{{ t('demoNoOrders') }}</p>
      <RouterLink class="nx-btn" to="/demo">{{ t('demoShop') }}</RouterLink>
    </div>

    <div v-else class="nx-orders">
      <article v-for="o in orders" :key="o.id">
        <div>
          <strong>{{ t('demoOrder') }} {{ o.id }}</strong>
          <p>{{ o.customer?.name }} · {{ o.items?.length || 0 }} {{ t('demoItems') }}</p>
        </div>
        <span class="nx-st" :class="o.status">{{ statusLabel(o.status) }}</span>
        <b>{{ money(o.total) }} {{ t('demoIq') }}</b>
        <RouterLink class="nx-btn sm" :to="resultTo(o)">{{ t('demoView') }}</RouterLink>
      </article>
    </div>
  </div>
</template>

<script setup>
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const { t } = useSiteCopy()
const { orders, money } = useDemoStore()

function statusLabel(s) {
  if (s === 'paid') return t('demoStatusPaid')
  if (s === 'failed') return t('demoStatusFailed')
  return t('demoStatusPending')
}

function resultTo(o) {
  if (o.status === 'paid') return `/demo/success/${o.id}`
  if (o.status === 'failed') return `/demo/failed/${o.id}`
  return `/demo/pay/${o.id}`
}
</script>
