<template>
  <div class="nx-page" v-if="order">
    <div class="nx-receipt" :class="{ fail: !ok }">
      <i class="bi" :class="ok ? 'bi-check-circle-fill' : 'bi-x-circle-fill'"></i>
      <h1>{{ ok ? t('demoSuccess') : t('demoFailed') }}</h1>
      <p>{{ ok ? t('demoSuccessB') : t('demoFailedB') }}</p>
      <div class="nx-receipt-box">
        <div><span>{{ t('demoOrder') }}</span><b>{{ order.id }}</b></div>
        <div><span>{{ t('demoTotal') }}</span><b>{{ money(order.total) }} {{ t('demoIq') }}</b></div>
      </div>
      <div class="nx-actions">
        <RouterLink class="nx-btn" to="/demo/orders">{{ t('demoBackOrders') }}</RouterLink>
        <RouterLink v-if="!ok" class="nx-ghost" :to="`/demo/pay/${order.id}`">{{ t('demoPayAgain') }}</RouterLink>
        <RouterLink class="nx-ghost" to="/demo">{{ t('demoContinue') }}</RouterLink>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute } from 'vue-router'
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const props = defineProps({ ok: { type: Boolean, default: true } })
const { t } = useSiteCopy()
const { getOrder, money } = useDemoStore()
const route = useRoute()
const order = computed(() => getOrder(route.params.id))
const ok = computed(() => props.ok && order.value?.status === 'paid')
</script>
