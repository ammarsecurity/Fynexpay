<template>
  <SiteNav />

  <main class="legal-page company-page" v-if="co">
    <article class="legal-article wide">
      <p class="legal-kicker">{{ co.updated }}</p>
      <h1>{{ co.title }}</h1>
      <p class="lead">{{ co.intro }}</p>

      <h2>{{ co.registrationTitle }}</h2>
      <div class="info-card">
        <h3>{{ co.iraqTitle }}</h3>
        <dl>
          <div>
            <dt>{{ co.iraqLegalNameLabel }}</dt>
            <dd>{{ co.iraqLegalName }}</dd>
          </div>
          <div>
            <dt>{{ co.iraqRegistryLabel }}</dt>
            <dd>{{ co.iraqRegistry }}</dd>
          </div>
          <div>
            <dt>{{ co.iraqHqLabel }}</dt>
            <dd>{{ co.iraqHq }}</dd>
          </div>
        </dl>
      </div>

      <h2>{{ co.certsTitle }}</h2>
      <p v-if="co.certsBody">{{ co.certsBody }}</p>
      <div class="cert-grid">
        <article v-for="(item, i) in co.certs || []" :key="i" class="info-card">
          <h3>{{ item.heading }}</h3>
          <p>{{ item.body }}</p>
        </article>
      </div>

      <h2>{{ co.contactTitle }}</h2>
      <ul class="contact-plain">
        <li><a class="ltr" :href="'mailto:' + co.contactEmail">{{ co.contactEmail }}</a></li>
        <li><a class="ltr" :href="'tel:' + co.contactPhone">{{ co.contactPhone }}</a></li>
        <li><a class="ltr" :href="co.contactWebsite" target="_blank" rel="noopener">{{ co.contactWebsite }}</a></li>
      </ul>
      <p class="disclaimer">{{ co.disclaimer }}</p>
    </article>
  </main>

  <SiteFooter />
</template>

<script setup>
import { computed, watch } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { useLanding } from '../composables/useLanding'

const { c } = useLanding()
const co = computed(() => c.value?.legal?.company || null)
watch(co, (p) => {
  if (p?.title) document.title = `${p.title} | Fynexpay`
}, { immediate: true })
</script>
