<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Pricing</p>
        <h1>{{ t('priceTitle') }}</h1>
        <p class="m-lead">{{ t('priceSub') }}</p>
      </div>
    </section>
    <section class="m-section">
      <div class="price-grid">
        <article class="m-card">
          <h3>{{ t('price1t') }}</h3>
          <p>{{ t('price1b') }}</p>
        </article>
        <article class="m-card">
          <h3>{{ t('price2t') }}</h3>
          <p>{{ t('price2b') }}</p>
        </article>
        <article class="m-card">
          <h3>{{ t('price3t') }}</h3>
          <p>{{ t('price3b') }}</p>
        </article>
      </div>
      <div class="m-cta" style="margin-top:32px">
        <RouterLink class="btn hero-start" to="/contact">{{ t('priceCta') }}</RouterLink>
      </div>
    </section>
  </main>
  <SiteFooter />
</template>

<script setup>
import SiteNav from '../../components/SiteNav.vue'
import SiteFooter from '../../components/SiteFooter.vue'
import { useSiteCopy } from '../../composables/useSiteCopy'
const { t, useTitle } = useSiteCopy()
useTitle('priceTitle')
</script>
