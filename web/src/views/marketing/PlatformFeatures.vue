<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Fynex Store</p>
        <h1>{{ t('platFeatTitle') }}</h1>
        <p class="m-lead">{{ t('platSub') }}</p>
      </div>
    </section>
    <section class="m-section">
      <div class="feat-grid">
        <article class="m-card" v-for="item in feats" :key="item.i">
          <i class="bi" :class="item.icon"></i>
          <h3>{{ t(item.t) }}</h3>
          <p>{{ t(item.b) }}</p>
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
useTitle('platFeatTitle')
const feats = [
  { i: 1, icon: 'bi-box-seam', t: 'feat1t', b: 'feat1b' },
  { i: 2, icon: 'bi-cart3', t: 'feat2t', b: 'feat2b' },
  { i: 3, icon: 'bi-credit-card', t: 'feat3t', b: 'feat3b' },
  { i: 4, icon: 'bi-check2-circle', t: 'feat4t', b: 'feat4b' },
  { i: 5, icon: 'bi-clock-history', t: 'feat5t', b: 'feat5b' },
  { i: 6, icon: 'bi-building', t: 'feat6t', b: 'feat6b' }
]
</script>
