<template>
  <SiteNav />

  <section class="hero" v-if="c">
    <div class="hero-glow" aria-hidden="true" />
    <div class="hero-inner">
      <h1>
        <template v-if="c.heroAccent">
          <span>{{ c.heroBefore }}</span>
          <em>{{ c.heroAccent }}</em>
          <span>{{ c.heroAfter }}</span>
        </template>
        <template v-else>{{ c.heroTitle }}</template>
      </h1>
      <p class="sub">{{ c.heroSubtitle }}</p>
      <div class="hero-cta">
        <RouterLink class="btn hero-start" to="/register">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M5 12h14M13 6l6 6-6 6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
          {{ c.ctaMerchant }}
        </RouterLink>
        <a class="btn hero-docs" :href="apiUrl + '/swagger/index.html'">{{ c.ctaDocs }}</a>
      </div>
    </div>
  </section>

  <div class="section-wrap soft" id="features" v-if="c">
    <section class="section">
      <div class="section-head">
        <span class="eyebrow">{{ c.featuresEyebrow }}</span>
        <h2>{{ c.featuresTitle }}</h2>
        <p>{{ c.featuresSubtitle }}</p>
      </div>
      <div class="features">
        <article class="feature" v-for="(f, i) in c.features" :key="i">
          <div class="icon" :class="'c' + ((i % 6) + 1)">{{ f.icon }}</div>
          <h3>{{ f.title }}</h3>
          <p>{{ f.body }}</p>
        </article>
      </div>
    </section>
  </div>

  <div class="section-wrap" id="api" v-if="c">
    <section class="section narrow">
      <div class="section-head">
        <span class="eyebrow">{{ c.apiEyebrow }}</span>
        <h2>{{ c.apiTitle }}</h2>
        <p>{{ c.apiSubtitle }}</p>
      </div>
      <pre class="code-box">curl -X POST {{ apiUrl }}/v1/payments \
  -H "X-Api-Key: fx_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 5000,
    "serviceType": "monthly_subscription",
    "callbackUrl": "https://your.app/hooks/fynexpay",
    "successUrl": "https://your.app/success"
  }'</pre>
    </section>
  </div>

  <section class="cta-band" v-if="c">
    <div class="cta-inner">
      <div class="cta-copy">
        <img class="cta-brand" src="/full-logo.png" alt="Fynexpay" />
        <h2>{{ c.ctaTitle }}</h2>
        <p>{{ c.ctaSubtitle }}</p>
      </div>
      <div class="cta-actions">
        <RouterLink class="btn cta-primary" to="/register">
          {{ c.ctaRegister }}
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" aria-hidden="true">
            <path d="M5 12h14M13 6l6 6-6 6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </RouterLink>
        <RouterLink class="btn cta-secondary" to="/login">{{ c.ctaContact }}</RouterLink>
      </div>
    </div>
  </section>

  <SiteFooter />
</template>

<script setup>
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { useLanding } from '../composables/useLanding'

const { c, apiUrl } = useLanding()
</script>
