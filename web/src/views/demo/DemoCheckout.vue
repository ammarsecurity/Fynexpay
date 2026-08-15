<template>
  <div class="nx-page">
    <header class="nx-page-head">
      <h1>{{ t('demoCheckout') }}</h1>
      <p>{{ t('demoPaySecure') }}</p>
    </header>

    <div v-if="!lines.length" class="nx-empty">
      <p>{{ t('demoEmpty') }}</p>
      <RouterLink class="nx-btn" to="/demo">{{ t('demoContinue') }}</RouterLink>
    </div>

    <form v-else class="nx-split" @submit.prevent="go">
      <div class="nx-card">
        <h2>{{ t('demoCust') }}</h2>
        <label>{{ t('demoNameF') }} <input v-model="form.name" required /></label>
        <label>{{ t('demoPhone') }} <input v-model="form.phone" class="ltr" required placeholder="07xx" /></label>
        <label>{{ t('demoEmail') }} <input v-model="form.email" type="email" class="ltr" required /></label>
        <label>{{ t('demoCity') }} <input v-model="form.city" required /></label>
      </div>

      <aside class="nx-sum">
        <h2>{{ t('demoSummary') }}</h2>
        <div v-for="line in lines" :key="line.id" class="mini">
          <img :src="line.image" alt="" />
          <span>{{ line.name[locale] }} × {{ line.qty }}</span>
          <b>{{ money(line.line) }}</b>
        </div>
        <div class="total"><span>{{ t('demoTotal') }}</span><b>{{ money(subtotal) }} {{ t('demoIq') }}</b></div>
        <button class="nx-btn full" type="submit">{{ t('demoPay') }}</button>
      </aside>
    </form>
  </div>
</template>

<script setup>
import { reactive } from 'vue'
import { useRouter } from 'vue-router'
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const { t, locale } = useSiteCopy()
const { lines, subtotal, createOrder, money } = useDemoStore()
const router = useRouter()
const form = reactive({ name: '', phone: '', email: '', city: 'Baghdad' })

function go() {
  if (!lines.value.length) return
  const order = createOrder({ ...form }, '')
  router.push(`/demo/pay/${order.id}`)
}
</script>
