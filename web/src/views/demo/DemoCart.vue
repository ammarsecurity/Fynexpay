<template>
  <div class="nx-page">
    <header class="nx-page-head">
      <h1>{{ t('demoCart') }}</h1>
      <p v-if="lines.length">{{ lines.length }} {{ t('demoItems') }}</p>
    </header>

    <div v-if="!lines.length" class="nx-empty">
      <i class="bi bi-bag"></i>
      <h2>{{ t('demoEmpty') }}</h2>
      <p>{{ t('demoEmptyB') }}</p>
      <RouterLink class="nx-btn" to="/demo">{{ t('demoContinue') }}</RouterLink>
    </div>

    <div v-else class="nx-split">
      <div class="nx-lines">
        <article v-for="line in lines" :key="line.id">
          <img :src="line.image" :alt="line.name[locale]" />
          <div>
            <h3>{{ line.name[locale] }}</h3>
            <p>{{ money(line.price) }} {{ t('demoIq') }}</p>
            <div class="nx-step">
              <button type="button" @click="setQty(line.id, line.qty - 1)">−</button>
              <span>{{ line.qty }}</span>
              <button type="button" @click="setQty(line.id, line.qty + 1)">+</button>
            </div>
          </div>
          <div class="nx-line-end">
            <strong>{{ money(line.line) }}</strong>
            <button class="nx-link" type="button" @click="remove(line.id)">{{ t('demoRemove') }}</button>
          </div>
        </article>
      </div>

      <aside class="nx-sum">
        <h2>{{ t('demoSummary') }}</h2>
        <div><span>{{ t('demoSubtotal') }}</span><b>{{ money(subtotal) }} {{ t('demoIq') }}</b></div>
        <div><span>{{ t('demoShip') }}</span><b>{{ t('demoShipFree') }}</b></div>
        <div class="total"><span>{{ t('demoTotal') }}</span><b>{{ money(subtotal) }} {{ t('demoIq') }}</b></div>
        <RouterLink class="nx-btn full" to="/demo/checkout">{{ t('demoCheckout') }}</RouterLink>
        <RouterLink class="nx-ghost" to="/demo">{{ t('demoContinue') }}</RouterLink>
      </aside>
    </div>
  </div>
</template>

<script setup>
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'
const { t, locale } = useSiteCopy()
const { lines, subtotal, setQty, remove, money } = useDemoStore()
</script>
