<template>
  <header class="site-nav">
    <div class="nav-inner">
      <RouterLink class="logo" to="/">Fynex<span>pay</span></RouterLink>
      <nav class="nav-links" v-if="c">
        <RouterLink to="/#features">{{ c.navFeatures }}</RouterLink>
        <RouterLink to="/#providers">{{ c.navProviders }}</RouterLink>
        <RouterLink to="/#api">{{ c.navDevelopers }}</RouterLink>
        <RouterLink to="/contact">{{ c.navContact }}</RouterLink>
      </nav>
      <div class="nav-actions">
        <div class="lang-switch" role="group">
          <button type="button" :class="{ active: locale === 'ar' }" @click="setLocale('ar')">ع</button>
          <button type="button" :class="{ active: locale === 'en' }" @click="setLocale('en')">EN</button>
        </div>
        <a class="btn soft" :href="dashboardUrl + '/login'">{{ c?.login }}</a>
        <a class="btn primary" :href="dashboardUrl + '/register'">{{ c?.startNow }}</a>
      </div>
    </div>
  </header>
</template>

<script setup>
import { useLanding } from '../composables/useLanding'

const { c, locale, setLocale, dashboardUrl } = useLanding()
</script>
