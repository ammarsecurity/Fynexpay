<template>
  <SiteNav />
  <main class="m-page">
    <section class="m-hero">
      <div class="m-hero-inner">
        <p class="m-kicker">Developers</p>
        <h1>{{ t('devTitle') }}</h1>
        <p class="m-lead">{{ t('devSub') }}</p>
      </div>
    </section>
    <section class="m-section">
      <div class="feat-grid">
        <article class="m-card">
          <i class="bi bi-book"></i>
          <h3>{{ t('devDocsT') }}</h3>
          <p>{{ t('devDocsB') }}</p>
          <a :href="dashboardUrl + '/merchant/docs'" target="_blank" rel="noopener">{{ t('devOpen') }}</a>
        </article>
        <article class="m-card">
          <i class="bi bi-code-slash"></i>
          <h3>{{ t('devRefT') }}</h3>
          <p>{{ t('devRefB') }}</p>
          <a :href="apiUrl + '/swagger/index.html'" target="_blank" rel="noopener">{{ t('devOpen') }}</a>
        </article>
        <article id="webhooks" class="m-card">
          <i class="bi bi-broadcast"></i>
          <h3>{{ t('devHookT') }}</h3>
          <p>{{ t('devHookB') }}</p>
        </article>
        <article id="sandbox" class="m-card">
          <i class="bi bi-beaker"></i>
          <h3>{{ t('devSandT') }}</h3>
          <p>{{ t('devSandB') }}</p>
          <RouterLink to="/demo">{{ t('navDemo') }}</RouterLink>
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
const { t, apiUrl, dashboardUrl, useTitle } = useSiteCopy()
useTitle('devTitle')
</script>
