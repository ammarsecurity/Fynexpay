<template>
  <SiteNav />

  <main class="page-shell legal-shell" v-if="page">
    <div class="page-hero legal-hero">
      <p class="legal-kicker">{{ page.updated }}</p>
      <h1>{{ page.title }}</h1>
      <p v-if="page.intro">{{ page.intro }}</p>
    </div>

    <div class="legal-page">
      <aside class="legal-toc" v-if="page.sections?.length">
        <p class="toc-title">{{ page.tocTitle }}</p>
        <a v-for="(s, i) in page.sections" :key="i" :href="'#' + slug(s.heading, i)">{{ s.heading }}</a>
      </aside>

      <article class="legal-article">
        <section v-for="(s, i) in page.sections" :key="i" :id="slug(s.heading, i)">
          <h2>{{ s.heading }}</h2>
          <p v-for="(para, pi) in paras(s.body)" :key="pi">{{ para }}</p>
          <ul v-if="s.items?.length">
            <li v-for="(item, ii) in s.items" :key="ii">{{ item }}</li>
          </ul>
        </section>
      </article>
    </div>
  </main>

  <SiteFooter />
</template>

<script setup>
import { computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { useLanding } from '../composables/useLanding'

const route = useRoute()
const { c } = useLanding()
const key = computed(() => route.meta.legal || 'terms')
const page = computed(() => c.value?.legal?.[key.value] || null)
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
