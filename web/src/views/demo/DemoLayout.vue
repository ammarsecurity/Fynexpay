<template>
  <div class="nx">
    <p class="nx-announce">{{ t('demoAnnounce') }}</p>

    <header class="nx-top">
      <div class="nx-wrap nx-top-inner">
        <RouterLink class="nx-logo" to="/demo">
          <span>Nex</span>
          <small>{{ t('demoTag') }}</small>
        </RouterLink>

        <form class="nx-search" @submit.prevent="goShop">
          <i class="bi bi-search"></i>
          <input v-model="q" type="search" :placeholder="t('demoSearch')" @input="goShop" />
        </form>

        <nav class="nx-nav">
          <RouterLink to="/demo">{{ t('demoShop') }}</RouterLink>
          <RouterLink to="/demo/orders">{{ t('demoOrders') }}</RouterLink>
          <RouterLink class="nx-bag" to="/demo/cart">
            <i class="bi bi-bag"></i>
            <span>{{ t('demoCart') }}</span>
            <em v-if="count">{{ count }}</em>
          </RouterLink>
        </nav>
      </div>
    </header>

    <main class="nx-main">
      <router-view />
    </main>

    <footer class="nx-foot">
      <div class="nx-wrap nx-foot-grid">
        <div>
          <strong>Nex</strong>
          <p>{{ t('demoTag') }}</p>
          <RouterLink class="nx-powered" to="/">{{ t('demoPowered') }}</RouterLink>
        </div>
        <div>
          <h4>{{ t('demoHelp') }}</h4>
          <RouterLink to="/demo/orders">{{ t('demoOrders') }}</RouterLink>
          <RouterLink to="/contact">{{ t('navContact') }}</RouterLink>
        </div>
      </div>
    </footer>

    <nav class="nx-dock">
      <RouterLink to="/demo"><i class="bi bi-grid"></i>{{ t('demoShop') }}</RouterLink>
      <RouterLink to="/demo/orders"><i class="bi bi-receipt"></i>{{ t('demoOrders') }}</RouterLink>
      <RouterLink to="/demo/cart">
        <i class="bi bi-bag"></i>{{ t('demoCart') }}
        <em v-if="count">{{ count }}</em>
      </RouterLink>
    </nav>

    <div class="nx-toast" v-if="toast === 'added'">
      <i class="bi bi-check-circle-fill"></i>
      {{ t('demoAdded') }}
      <RouterLink to="/demo/cart">{{ t('demoCart') }}</RouterLink>
    </div>
  </div>
</template>

<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useSiteCopy } from '../../composables/useSiteCopy'
import { useDemoStore } from '../../composables/useDemoStore'

const { t, useTitle } = useSiteCopy()
const { count, toast } = useDemoStore()
const route = useRoute()
const router = useRouter()
const q = ref(typeof route.query.q === 'string' ? route.query.q : '')

useTitle('demoName')

watch(() => route.query.q, (v) => {
  q.value = typeof v === 'string' ? v : ''
})

function goShop() {
  const query = q.value.trim() ? { q: q.value.trim() } : {}
  if (route.path !== '/demo') router.push({ path: '/demo', query })
  else router.replace({ path: '/demo', query })
}
</script>
