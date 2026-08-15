<template>
  <div>
    <section class="nx-hero">
      <div class="nx-hero-copy">
        <span class="nx-kicker">Nex Store</span>
        <h1>{{ t('demoHeroTitle') }}</h1>
        <p>{{ t('demoHeroSub') }}</p>
        <a class="nx-btn" href="#catalog">{{ t('demoShopNow') }}</a>
      </div>
      <div class="nx-hero-visual">
        <img src="/demo/hero.jpg" alt="" />
      </div>
    </section>

    <ul class="nx-trust">
      <li><i class="bi bi-truck"></i>{{ t('demoFreeShip') }}</li>
      <li><i class="bi bi-shield-lock"></i>{{ t('demoSecure') }}</li>
      <li><i class="bi bi-award"></i>{{ t('demoWarranty') }}</li>
    </ul>

    <section class="nx-catalog" id="catalog">
      <div class="nx-cat-row">
        <h2>{{ t('demoFeat') }}</h2>
        <div class="nx-cats">
          <button v-for="c in cats" :key="c.id" type="button" :class="{ on: cat === c.id }" @click="cat = c.id">
            {{ c.label }}
          </button>
        </div>
      </div>

      <div v-if="!list.length" class="nx-empty">
        <p>{{ t('demoSearchEmpty') }}</p>
      </div>

      <div v-else class="nx-grid">
        <article v-for="p in list" :key="p.id">
          <div class="nx-shot">
            <img :src="p.image" :alt="p.name[locale]" />
            <span v-if="p.badge === 'sale'" class="nx-tag sale">{{ t('demoSale') }}</span>
            <span v-else-if="p.badge === 'new'" class="nx-tag new">{{ t('demoNew') }}</span>
          </div>
          <div class="nx-meta">
            <h3>{{ p.name[locale] }}</h3>
            <p>{{ p.blurb[locale] }}</p>
            <div class="nx-rate">
              <i class="bi bi-star-fill"></i>
              <b>{{ p.rating }}</b>
              <span>({{ p.reviews }})</span>
            </div>
            <div class="nx-price">
              <strong>{{ money(p.price) }} <small>{{ t('demoIq') }}</small></strong>
              <s v-if="p.oldPrice">{{ money(p.oldPrice) }}</s>
            </div>
            <button class="nx-btn full" type="button" @click="add(p.id)">
              <i class="bi bi-bag-plus"></i>
              {{ t('demoAdd') }}
            </button>
          </div>
        </article>
      </div>
    </section>
  </div>
</template>

<script setup>
import { computed, ref } from 'vue'
import { useRoute } from 'vue-router'
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const { t, locale } = useSiteCopy()
const { products, add, money } = useDemoStore()
const route = useRoute()
const cat = ref('all')

const cats = computed(() => [
  { id: 'all', label: t('demoCatAll') },
  { id: 'audio', label: t('demoCatAudio') },
  { id: 'wear', label: t('demoCatWear') },
  { id: 'acc', label: t('demoCatAcc') },
  { id: 'digital', label: t('demoCatDigital') }
])

const list = computed(() => {
  const q = String(route.query.q || '').trim().toLowerCase()
  return products.filter((p) => {
    if (cat.value !== 'all' && p.cat !== cat.value) return false
    if (!q) return true
    return `${p.name.ar} ${p.name.en} ${p.blurb.ar} ${p.blurb.en}`.toLowerCase().includes(q)
  })
})
</script>
