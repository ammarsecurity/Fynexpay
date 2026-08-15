<template>
  <footer class="lp-foot">
    <div class="lp-wrap lp-foot-grid">
      <div class="lp-foot-brand">
        <img src="/full-logo.png" alt="FynexPay" />
        <p>{{ t('footTag') }}</p>
        <div class="lp-social">
          <a href="https://facebook.com" target="_blank" rel="noopener" aria-label="Facebook"><i class="bi bi-facebook"></i></a>
          <a href="https://instagram.com" target="_blank" rel="noopener" aria-label="Instagram"><i class="bi bi-instagram"></i></a>
          <a href="https://linkedin.com" target="_blank" rel="noopener" aria-label="LinkedIn"><i class="bi bi-linkedin"></i></a>
          <a href="https://youtube.com" target="_blank" rel="noopener" aria-label="YouTube"><i class="bi bi-youtube"></i></a>
        </div>
      </div>
      <nav>
        <h4>{{ t('footFeat') }}</h4>
        <RouterLink to="/demo">{{ t('navStores') }}</RouterLink>
        <RouterLink to="/payments">{{ t('checkoutName') }}</RouterLink>
        <RouterLink to="/payments#links">{{ t('lpLinksName') }}</RouterLink>
        <RouterLink to="/pricing">{{ t('navPricing') }}</RouterLink>
      </nav>
      <nav>
        <h4>{{ t('footDev') }}</h4>
        <RouterLink to="/developers">{{ t('navApiDocs') }}</RouterLink>
        <a :href="apiUrl + '/swagger/index.html'" target="_blank" rel="noopener">{{ t('navApiRef') }}</a>
        <RouterLink to="/developers#webhooks">{{ t('navWebhooks') }}</RouterLink>
        <RouterLink to="/developers#sandbox">{{ t('navSandbox') }}</RouterLink>
      </nav>
      <nav>
        <h4>{{ t('footCompany') }}</h4>
        <RouterLink to="/company">{{ t('navAbout') }}</RouterLink>
        <RouterLink to="/for-platforms">{{ t('navPartners') }}</RouterLink>
        <RouterLink to="/contact">{{ t('footBlog') }}</RouterLink>
        <RouterLink to="/contact">{{ t('navContact') }}</RouterLink>
      </nav>
      <nav>
        <h4>{{ t('footSupport') }}</h4>
        <RouterLink to="/contact">{{ t('footHelp') }}</RouterLink>
        <RouterLink to="/contact">{{ t('footFaq') }}</RouterLink>
        <RouterLink to="/company">{{ t('footCareer') }}</RouterLink>
      </nav>
    </div>
    <div class="lp-wrap lp-foot-bar">
      <p>© {{ year }} FynexPay. All rights reserved.</p>
      <div>
        <RouterLink to="/terms">{{ t('footTerms') }}</RouterLink>
        <RouterLink to="/privacy">{{ t('footPrivacy') }}</RouterLink>
      </div>
    </div>
  </footer>
</template>

<script setup>
import { useSiteCopy } from '../composables/useSiteCopy'
const { t, year, apiUrl } = useSiteCopy()
</script>
