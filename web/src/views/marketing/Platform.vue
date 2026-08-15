<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Fynex Store</p>
        <h1>{{ t('platTitle') }}</h1>
        <p class="m-lead">{{ t('platSub') }}</p>
        <div class="m-cta">
          <RouterLink class="btn hero-start" to="/platform/create">{{ t('navCreateStore') }}</RouterLink>
          <RouterLink class="btn ghost" to="/demo">{{ t('navDemo') }}</RouterLink>
        </div>
      </div>
    </section>
    <section class="m-section">
      <div class="trio">
        <article class="m-card">
          <i class="bi bi-plus-square"></i>
          <h3>{{ t('navCreateStore') }}</h3>
          <p>{{ t('platCreateSub') }}</p>
          <RouterLink to="/platform/create">{{ t('storeCta') }}</RouterLink>
        </article>
        <article class="m-card">
          <i class="bi bi-stars"></i>
          <h3>{{ t('navStoreFeatures') }}</h3>
          <p>{{ t('platFeatTitle') }}</p>
          <RouterLink to="/platform/features">{{ t('navStoreFeatures') }}</RouterLink>
        </article>
        <article class="m-card">
          <i class="bi bi-play-circle"></i>
          <h3>{{ t('demoName') }}</h3>
          <p>{{ t('demoTag') }}</p>
          <RouterLink to="/demo">{{ t('demoBannerCta') }}</RouterLink>
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
useTitle('platTitle')
</script>
