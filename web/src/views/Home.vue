<template>
  <SiteNav />

  <section class="hero" v-if="c">
    <div class="hero-grid">
      <div>
        <div class="badge-pill">{{ c.badge }}</div>
        <h1>{{ c.heroTitle }}</h1>
        <p class="sub">{{ c.heroSubtitle }}</p>
        <div class="hero-cta">
          <RouterLink class="btn primary" to="/register">{{ c.ctaMerchant }}</RouterLink>
          <a class="btn ghost" :href="apiUrl + '/swagger/index.html?urls.primaryName=' + encodeURIComponent('Merchant API')">{{ c.ctaDocs }}</a>
        </div>
      </div>

      <div class="mock" aria-hidden="true">
        <div class="mock-card mock-main">
          <div class="mock-top">
            <strong>{{ c.mockDashboard }}</strong>
            <div><span class="dot"></span><span class="dot"></span><span class="dot b"></span></div>
          </div>
          <div class="kpi-row">
            <div class="kpi"><span class="muted">{{ c.mockToday }}</span><strong>4.2M</strong></div>
            <div class="kpi"><span class="muted">{{ c.mockSuccess }}</span><strong>98%</strong></div>
          </div>
          <div class="bars">
            <i style="height:45%"></i><i style="height:70%"></i><i style="height:55%"></i>
            <i style="height:90%"></i><i style="height:62%"></i><i style="height:78%"></i>
          </div>
        </div>
        <div class="mock-card mock-phone">
          <strong style="display:block;margin-bottom:10px">Checkout</strong>
          <div class="kpi"><span class="muted">{{ c.mockAmount }}</span><strong>5,000 IQD</strong></div>
          <div class="chip-row" style="margin-top:14px">
            <span class="chip">{{ c.mockChooseProvider }}</span>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="section" id="features" v-if="c">
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

  <section class="section" id="api" v-if="c">
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
            <path d="M15 6 9 12l6 6" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round"/>
          </svg>
        </RouterLink>
        <RouterLink class="btn cta-secondary" to="/login">{{ c.ctaContact }}</RouterLink>
      </div>
    </div>
  </section>

  <footer class="site-footer" v-if="c">© {{ year }} Fynexpay — {{ c.footer }}</footer>
</template>

<script setup>
import SiteNav from '../components/SiteNav.vue'
import { useLanding } from '../composables/useLanding'

const { c, apiUrl, year } = useLanding()
</script>
