<template>
  <SiteNav />

  <section class="hero" v-if="c">
    <div class="hero-grid">
      <div>
        <div class="badge-pill">{{ c.badge }}</div>
        <h1>{{ c.heroTitle }}</h1>
        <p class="sub">{{ c.heroSubtitle }}</p>
        <div class="hero-cta">
          <a class="btn primary" :href="dashboardUrl + '/register'">{{ c.ctaMerchant }}</a>
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
          <div class="chip-row">
            <span class="chip" v-for="p in c.providerPills?.slice(0, 4)" :key="p">{{ p }}</span>
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

  <section class="section" id="providers" v-if="c">
    <div class="section-head">
      <span class="eyebrow">{{ c.providersEyebrow }}</span>
      <h2>{{ c.providersTitle }}</h2>
      <p>{{ c.providersSubtitle }}</p>
    </div>
    <div class="providers">
      <span class="provider-pill" v-for="p in c.providerPills" :key="p">{{ p }}</span>
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
      <div>
        <h2>{{ c.ctaTitle }}</h2>
        <p>{{ c.ctaSubtitle }}</p>
      </div>
      <div class="cta-actions">
        <a class="btn white" :href="dashboardUrl + '/register'">{{ c.ctaRegister }}</a>
        <RouterLink class="btn outline-white" to="/contact">{{ c.navContact }}</RouterLink>
      </div>
    </div>
  </section>

  <footer class="site-footer" v-if="c">© {{ year }} Fynexpay — {{ c.footer }}</footer>
</template>

<script setup>
import SiteNav from '../components/SiteNav.vue'
import { useLanding } from '../composables/useLanding'

const { c, apiUrl, dashboardUrl, year } = useLanding()
</script>
