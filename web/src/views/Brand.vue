<template>
  <SiteNav />

  <main class="legal-page" v-if="page">
    <aside class="legal-toc">
      <p class="toc-title">{{ page.tocTitle }}</p>
      <a href="#assets">{{ locale === 'en' ? 'Assets' : 'الأصول' }}</a>
      <a v-for="(s, i) in page.sections" :key="i" :href="'#' + slug(s.heading, i)">{{ s.heading }}</a>
    </aside>

    <article class="legal-article">
      <p class="legal-kicker">{{ page.updated }}</p>
      <h1>{{ page.title }}</h1>
      <p class="lead" v-if="page.intro">{{ page.intro }}</p>

      <section id="assets" class="brand-assets">
        <h2>{{ locale === 'en' ? 'Official assets' : 'الأصول الرسمية' }}</h2>
        <div class="asset-grid">
          <figure class="asset light">
            <img src="/full-logo.png" alt="Fynexpay" />
            <figcaption>{{ locale === 'en' ? 'Wordmark — light backgrounds' : 'الشعار النصي — خلفيات فاتحة' }}</figcaption>
          </figure>
          <figure class="asset dark">
            <img class="invert-logo" src="/full-logo.png" alt="Fynexpay" />
            <figcaption>{{ locale === 'en' ? 'Wordmark — dark backgrounds' : 'الشعار النصي — خلفيات داكنة' }}</figcaption>
          </figure>
          <figure class="asset light">
            <img src="/icon-logo.png" alt="" />
            <figcaption>{{ locale === 'en' ? 'App icon' : 'أيقونة التطبيق' }}</figcaption>
          </figure>
          <figure class="asset dark">
            <img src="/icon-logo-white.png" alt="" />
            <figcaption>{{ locale === 'en' ? 'Icon on dark' : 'الأيقونة على داكن' }}</figcaption>
          </figure>
        </div>
        <div class="swatches">
          <div class="swatch" style="--sw:#031838"><span>#031838</span></div>
          <div class="swatch pale" style="--sw:#f5f7fb"><span>#F5F7FB</span></div>
        </div>
      </section>

      <section v-for="(s, i) in page.sections" :key="i" :id="slug(s.heading, i)">
        <h2>{{ s.heading }}</h2>
        <p v-for="(para, pi) in paras(s.body)" :key="pi">{{ para }}</p>
        <ul v-if="s.items?.length">
          <li v-for="(item, ii) in s.items" :key="ii">{{ item }}</li>
        </ul>
      </section>
    </article>
  </main>

  <SiteFooter />
</template>

<script setup>
import { computed, watch } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { useLanding } from '../composables/useLanding'

const { c, locale } = useLanding()
const page = computed(() => c.value?.legal?.brand || null)
watch(page, (p) => {
  if (p?.title) document.title = `${p.title} | Fynexpay`
}, { immediate: true })

function paras(body) {
  return String(body || '').split(/\n+/).map((p) => p.trim()).filter(Boolean)
}
function slug(heading, i) {
  const base = String(heading || 's')
    .toLowerCase()
    .replace(/[^\p{L}\p{N}]+/gu, '-')
    .replace(/^-|-$/g, '')
  return base || `s-${i}`
}
</script>
