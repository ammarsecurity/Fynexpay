<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Fynex Checkout</p>
        <h1>{{ t('payTitle') }}</h1>
        <p class="m-lead">{{ t('paySub') }}</p>
      </div>
    </section>
    <section class="m-section">
      <div class="trio">
        <article id="checkout" class="m-card">
          <i class="bi bi-credit-card-2-front"></i>
          <h3>{{ t('payCheckoutT') }}</h3>
          <p>{{ t('payCheckoutB') }}</p>
        </article>
        <article id="links" class="m-card">
          <i class="bi bi-link-45deg"></i>
          <h3>{{ t('payLinksT') }}</h3>
          <p>{{ t('payLinksB') }}</p>
        </article>
        <article id="methods" class="m-card">
          <i class="bi bi-wallet2"></i>
          <h3>{{ t('payMethodsT') }}</h3>
          <p>{{ t('payMethodsB') }}</p>
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
useTitle('payTitle')
</script>
