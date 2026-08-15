<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">For E-commerce Platforms</p>
        <h1>{{ t('ecomTitle') }}</h1>
        <p class="m-lead">{{ t('ecomLead') }}</p>
        <div class="m-cta">
          <RouterLink class="btn hero-start" to="/developers">{{ t('navDevelopers') }}</RouterLink>
          <RouterLink class="btn ghost" to="/contact">{{ t('navContact') }}</RouterLink>
        </div>
      </div>
    </section>
    <section class="m-section">
      <div class="trio">
        <article class="m-card">
          <i class="bi bi-braces"></i>
          <h3>{{ t('ecom1t') }}</h3>
          <p>{{ t('ecom1b') }}</p>
        </article>
        <article class="m-card">
          <i class="bi bi-broadcast"></i>
          <h3>{{ t('ecom2t') }}</h3>
          <p>{{ t('ecom2b') }}</p>
        </article>
        <article class="m-card">
          <i class="bi bi-beaker"></i>
          <h3>{{ t('ecom3t') }}</h3>
          <p>{{ t('ecom3b') }}</p>
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
useTitle('ecomTitle')
</script>
