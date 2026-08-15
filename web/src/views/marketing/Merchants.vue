<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">For Merchants</p>
        <h1>{{ t('merchTitle') }}</h1>
        <p class="m-lead">{{ t('merchLead') }}</p>
        <div class="m-cta">
          <RouterLink class="btn hero-start" to="/register">{{ t('start') }}</RouterLink>
          <RouterLink class="btn ghost" to="/payments">{{ t('navPayments') }}</RouterLink>
        </div>
      </div>
    </section>
    <section class="m-section">
      <div class="trio">
        <article class="m-card">
          <i class="bi bi-window"></i>
          <h3>{{ t('merch1t') }}</h3>
          <p>{{ t('merch1b') }}</p>
        </article>
        <article class="m-card">
          <i class="bi bi-wallet2"></i>
          <h3>{{ t('merch2t') }}</h3>
          <p>{{ t('merch2b') }}</p>
        </article>
        <article class="m-card">
          <i class="bi bi-speedometer2"></i>
          <h3>{{ t('merch3t') }}</h3>
          <p>{{ t('merch3b') }}</p>
        </article>
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
useTitle('merchTitle')
</script>
