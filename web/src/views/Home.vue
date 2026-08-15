<template>
  <SiteNav />

  <main class="lp">
    <section class="lp-hero lp-wrap">
      <div class="lp-hero-copy">
        <span class="lp-badge">
          <i class="bi bi-shield-check"></i>
          {{ t('lpBadge') }}
        </span>
        <h1>{{ t('lpHero') }}</h1>
        <p>{{ t('lpHeroSub') }}</p>
        <div class="lp-hero-cta">
          <RouterLink class="lp-btn lg" to="/register">{{ t('startFree') }}</RouterLink>
          <RouterLink class="lp-btn-out" to="/platform">
            <i class="bi bi-play-circle"></i>
            {{ t('explore') }}
          </RouterLink>
        </div>
        <ul class="lp-trust">
          <li><i class="bi bi-lightning-charge-fill"></i>{{ t('lpFast') }}</li>
          <li><i class="bi bi-shield-lock-fill"></i>{{ t('lpSecure') }}</li>
          <li><i class="bi bi-headset"></i>{{ t('lpSupport') }}</li>
        </ul>
      </div>

      <div class="lp-devices" aria-hidden="true">
        <div class="lp-tablet">
          <div class="lp-tablet-top">
            <span>{{ locale === 'en' ? 'Balance' : 'الرصيد' }}</span>
            <b>12,400,000 IQD</b>
          </div>
          <svg class="lp-chart" viewBox="0 0 320 120" fill="none" xmlns="http://www.w3.org/2000/svg">
            <defs>
              <linearGradient id="lpFill" x1="0" y1="0" x2="0" y2="1">
                <stop offset="0%" stop-color="#6366F1" stop-opacity="0.28" />
                <stop offset="100%" stop-color="#6366F1" stop-opacity="0" />
              </linearGradient>
            </defs>
            <path d="M0 88 C28 80 48 96 72 70 C96 44 112 52 140 38 C168 24 188 48 216 30 C244 12 268 22 320 8 V120 H0 Z" fill="url(#lpFill)" />
            <path d="M0 88 C28 80 48 96 72 70 C96 44 112 52 140 38 C168 24 188 48 216 30 C244 12 268 22 320 8" stroke="#6366F1" stroke-width="3" stroke-linecap="round" />
            <circle cx="216" cy="30" r="4.5" fill="#6366F1" />
          </svg>
        </div>
        <div class="lp-phone lp-phone-lottie">
          <LpLottie src="/OrderConfirmed.lottie" />
        </div>
        <div class="lp-toast">
          <i class="bi bi-check-circle-fill"></i>
          <div>
            <strong>{{ t('lpToast') }}</strong>
            <span>{{ t('lpToastAmt') }}</span>
          </div>
        </div>
      </div>
    </section>

    <section class="lp-logos">
      <div class="lp-wrap">
        <p>{{ t('lpTrusted') }}</p>
        <div class="lp-logo-row">
          <span>TechStore</span>
          <span>Fashion Hub</span>
          <span>Digital World</span>
          <span>Home &amp; More</span>
          <span>BookLand</span>
          <span>Beauty Care</span>
        </div>
      </div>
    </section>

    <section class="lp-block" id="features">
      <div class="lp-wrap">
        <h2>{{ t('lpFeatTitle') }}</h2>
        <div class="lp-feats">
          <article>
            <i class="bi bi-code-slash c-violet"></i>
            <h3>{{ t('lpF1t') }}</h3>
            <p>{{ t('lpF1b') }}</p>
          </article>
          <article>
            <i class="bi bi-bar-chart-line c-blue"></i>
            <h3>{{ t('lpF2t') }}</h3>
            <p>{{ t('lpF2b') }}</p>
          </article>
          <article>
            <i class="bi bi-link-45deg c-gold"></i>
            <h3>{{ t('lpF3t') }}</h3>
            <p>{{ t('lpF3b') }}</p>
          </article>
          <article>
            <i class="bi bi-wallet2 c-green"></i>
            <h3>{{ t('lpF4t') }}</h3>
            <p>{{ t('lpF4b') }}</p>
          </article>
          <article>
            <i class="bi bi-shield-lock c-pink"></i>
            <h3>{{ t('lpF5t') }}</h3>
            <p>{{ t('lpF5b') }}</p>
          </article>
        </div>
      </div>
    </section>

    <section class="lp-stats">
      <div class="lp-wrap lp-stats-row">
        <div>
          <i class="bi bi-activity"></i>
          <strong>99.9%</strong>
          <span>{{ t('lpStat1') }}</span>
        </div>
        <div>
          <i class="bi bi-cash-stack"></i>
          <strong>+45M IQD</strong>
          <span>{{ t('lpStat2') }}</span>
        </div>
        <div>
          <i class="bi bi-people"></i>
          <strong>+2.5M</strong>
          <span>{{ t('lpStat3') }}</span>
        </div>
        <div>
          <i class="bi bi-shop-window"></i>
          <strong>+25,000</strong>
          <span>{{ t('lpStat4') }}</span>
        </div>
      </div>
    </section>

    <section class="lp-block soft">
      <div class="lp-wrap">
        <h2>{{ t('lpSolTitle') }}</h2>
        <div class="lp-sols">
          <article>
            <div class="lp-sol-visual store">
              <LpLottie src="/online-shopping.lottie" />
            </div>
            <h3>Fynex Store</h3>
            <p class="role">{{ t('lpStoreRole') }}</p>
            <p>{{ t('storeLead') }}</p>
            <RouterLink to="/demo">{{ t('lpStoreCta') }} <i class="bi bi-arrow-left"></i></RouterLink>
          </article>
          <article>
            <div class="lp-sol-visual check">
              <LpLottie src="/credit-card.lottie" />
            </div>
            <h3>Fynex Checkout</h3>
            <p class="role">{{ t('lpCheckRole') }}</p>
            <p>{{ t('checkoutLead') }}</p>
            <a :href="docsUrl" target="_blank" rel="noopener">{{ t('lpCheckCta') }} <i class="bi bi-arrow-left"></i></a>
          </article>
          <article>
            <div class="lp-sol-visual links">
              <LpLottie src="/link.lottie" />
            </div>
            <h3>{{ t('lpLinksName') }}</h3>
            <p class="role">{{ t('lpLinksRole') }}</p>
            <p>{{ t('lpLinksLead') }}</p>
            <RouterLink to="/payments#links">{{ t('lpLinksCta') }} <i class="bi bi-arrow-left"></i></RouterLink>
          </article>
        </div>
      </div>
    </section>

    <section class="lp-dev-wrap">
      <div class="lp-wrap lp-dev">
        <div class="lp-dev-copy">
          <span class="lp-badge">{{ t('lpDevKicker') }}</span>
          <h2>{{ t('lpDevTitle') }}</h2>
          <ul>
            <li><i class="bi bi-check-circle-fill"></i>{{ t('lpDev1') }}</li>
            <li><i class="bi bi-check-circle-fill"></i>{{ t('lpDev2') }}</li>
            <li><i class="bi bi-check-circle-fill"></i>{{ t('lpDev3') }}</li>
            <li><i class="bi bi-check-circle-fill"></i>{{ t('lpDev4') }}</li>
          </ul>
          <a class="lp-btn lg" :href="docsUrl" target="_blank" rel="noopener">{{ t('lpDevBtn') }}</a>
        </div>
        <div class="lp-code">
          <div class="lp-code-bar"><span /><span /><span /><em>POST api.fynexpay.net/v1/payments</em></div>
          <pre><span class="k">{</span>
  <span class="s">"amount"</span>: <span class="n">250000</span>,
  <span class="s">"currency"</span>: <span class="s">"IQD"</span>,
  <span class="s">"order_id"</span>: <span class="s">"ORD-1042"</span>,
  <span class="s">"success_url"</span>: <span class="s">"https://store.iq/ok"</span>
<span class="k">}</span></pre>
        </div>
      </div>
    </section>

    <section class="lp-block">
      <div class="lp-wrap">
        <h2>{{ t('lpStepsTitle') }}</h2>
        <div class="lp-steps">
          <article>
            <div class="step-ico">
              <i class="bi bi-person-plus"></i>
              <em>1</em>
            </div>
            <h3>{{ t('lpStep1') }}</h3>
            <p>{{ t('lpStep1b') }}</p>
          </article>
          <article>
            <div class="step-ico">
              <i class="bi bi-link-45deg"></i>
              <em>2</em>
            </div>
            <h3>{{ t('lpStep2') }}</h3>
            <p>{{ t('lpStep2b') }}</p>
          </article>
          <article>
            <div class="step-ico">
              <i class="bi bi-wallet2"></i>
              <em>3</em>
            </div>
            <h3>{{ t('lpStep3') }}</h3>
            <p>{{ t('lpStep3b') }}</p>
          </article>
        </div>
      </div>
    </section>

    <section class="lp-ready">
      <div class="lp-wrap lp-ready-inner">
        <div>
          <h2>{{ t('lpReady') }}</h2>
          <p>{{ t('lpReadySub') }}</p>
          <div class="lp-hero-cta">
            <RouterLink class="lp-btn white" to="/register">{{ t('startFree') }}</RouterLink>
            <RouterLink class="lp-btn ghost-w" to="/contact">{{ t('lpSales') }}</RouterLink>
          </div>
        </div>
        <div class="lp-ready-phone" aria-hidden="true">
          <LpLottie src="/Search.lottie" />
        </div>
      </div>
    </section>
  </main>

  <SiteFooter />
</template>

<script setup>
import { computed } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import LpLottie from '../components/LpLottie.vue'
import { useSiteCopy } from '../composables/useSiteCopy'

const { t, locale, apiUrl, useTitle } = useSiteCopy()
useTitle('docTitle')
const docsUrl = computed(() => `${apiUrl}/swagger/index.html`)
</script>
