<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Fynex Store</p>
        <h1>{{ t('platCreateTitle') }}</h1>
        <p class="m-lead">{{ t('platCreateSub') }}</p>
        <div class="m-cta">
          <RouterLink class="btn hero-start" to="/register">
            <i class="bi bi-shop"></i>
            {{ t('storeCta') }}
          </RouterLink>
          <RouterLink class="btn ghost" to="/demo">{{ t('navDemo') }}</RouterLink>
        </div>
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
useTitle('platCreateTitle')
</script>
