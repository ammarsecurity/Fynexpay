<template>
  <footer class="site-footer" v-if="c">
    <div class="foot-inner">
      <div class="foot-brand">
        <img src="/full-logo.png" alt="Fynexpay" />
        <p>© {{ year }} Fynexpay — {{ c.footer }}</p>
      </div>
      <nav class="foot-links" :aria-label="locale === 'en' ? 'Legal' : 'قانوني'">
        <RouterLink to="/terms">{{ legal.terms?.nav }}</RouterLink>
        <RouterLink to="/privacy">{{ legal.privacy?.nav }}</RouterLink>
        <RouterLink to="/prohibited">{{ legal.prohibited?.nav }}</RouterLink>
        <RouterLink to="/brand">{{ legal.brand?.nav }}</RouterLink>
        <RouterLink to="/company">{{ legal.company?.nav }}</RouterLink>
        <RouterLink to="/contact">{{ c.navContact }}</RouterLink>
      </nav>
    </div>
    <div class="foot-notes" v-if="c.footerDisclaimer || c.footerLegalNote">
      <p v-if="c.footerDisclaimer">{{ c.footerDisclaimer }}</p>
      <p v-if="c.footerLegalNote">{{ c.footerLegalNote }}</p>
    </div>
  </footer>
</template>

<script setup>
import { computed } from 'vue'
import { useLanding } from '../composables/useLanding'

const { c, locale, year } = useLanding()
const legal = computed(() => c.value?.legal || {})
</script>
