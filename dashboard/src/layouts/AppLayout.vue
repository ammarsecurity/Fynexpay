<template>
  <div class="app-shell" :class="{ 'nav-open': navOpen }">
    <div class="sidebar-backdrop" v-if="navOpen" @click="navOpen = false" />

    <aside class="sidebar" :class="{ open: navOpen }">
      <div class="sidebar-top">
        <RouterLink to="/" class="brand-row" aria-label="Fynexpay" @click="closeNav">
          <img src="/full-logo.png" alt="Fynexpay" class="brand-logo" />
        </RouterLink>
        <button class="sidebar-close" type="button" @click="closeNav" :aria-label="$t('common.close')">
          <i class="bi bi-x-lg" aria-hidden="true"></i>
        </button>
      </div>

      <nav class="nav" v-if="auth.isMerchant" @click="onNavClick">
        <div class="nav-label">{{ $t('nav.groupWork') }}</div>
        <RouterLink to="/merchant" active-class="nav-prefix" exact-active-class="router-link-active">
          <i class="bi bi-house-door" aria-hidden="true"></i>
          {{ $t('nav.overview') }}
        </RouterLink>
        <RouterLink to="/merchant/payments">
          <i class="bi bi-credit-card" aria-hidden="true"></i>
          {{ $t('nav.payments') }}
        </RouterLink>
        <RouterLink to="/merchant/payouts">
          <i class="bi bi-box-arrow-up" aria-hidden="true"></i>
          {{ $t('nav.payouts') }}
        </RouterLink>

        <div class="nav-label">{{ $t('nav.groupConnect') }}</div>
        <RouterLink to="/merchant/payment-methods">
          <i class="bi bi-wallet2" aria-hidden="true"></i>
          {{ $t('nav.paymentMethods') }}
        </RouterLink>
        <RouterLink to="/merchant/platforms">
          <i class="bi bi-grid" aria-hidden="true"></i>
          {{ $t('nav.platforms') }}
        </RouterLink>
        <RouterLink to="/merchant/keys">
          <i class="bi bi-key" aria-hidden="true"></i>
          {{ $t('nav.keys') }}
        </RouterLink>
        <RouterLink to="/merchant/test">
          <i class="bi bi-play-circle" aria-hidden="true"></i>
          {{ $t('nav.test') }}
        </RouterLink>
        <RouterLink to="/merchant/docs">
          <i class="bi bi-book" aria-hidden="true"></i>
          {{ $t('nav.docs') }}
        </RouterLink>

        <div class="nav-label">{{ $t('nav.groupAccount') }}</div>
        <RouterLink to="/merchant/profile">
          <i class="bi bi-person" aria-hidden="true"></i>
          {{ $t('nav.profile') }}
        </RouterLink>
      </nav>

      <nav class="nav" v-if="auth.isAdmin" @click="onNavClick">
        <div class="nav-label">{{ $t('nav.groupWork') }}</div>
        <RouterLink to="/admin" active-class="nav-prefix" exact-active-class="router-link-active">
          <i class="bi bi-speedometer2" aria-hidden="true"></i>
          {{ $t('nav.overview') }}
        </RouterLink>
        <RouterLink to="/admin/merchants">
          <i class="bi bi-people" aria-hidden="true"></i>
          {{ $t('nav.merchants') }}
        </RouterLink>
        <RouterLink to="/admin/payments">
          <i class="bi bi-credit-card" aria-hidden="true"></i>
          {{ $t('nav.payments') }}
        </RouterLink>
        <RouterLink to="/admin/payouts">
          <i class="bi bi-box-arrow-up" aria-hidden="true"></i>
          {{ $t('nav.adminPayouts') }}
        </RouterLink>

        <div class="nav-label">{{ $t('nav.groupSettings') }}</div>
        <RouterLink to="/admin/platforms">
          <i class="bi bi-grid" aria-hidden="true"></i>
          {{ $t('nav.platforms') }}
        </RouterLink>
        <RouterLink to="/admin/providers">
          <i class="bi bi-sliders" aria-hidden="true"></i>
          {{ $t('nav.providers') }}
        </RouterLink>
        <RouterLink to="/admin/ultramsg">
          <i class="bi bi-whatsapp" aria-hidden="true"></i>
          {{ $t('nav.ultramsg') }}
        </RouterLink>
        <RouterLink to="/admin/landing">
          <i class="bi bi-globe" aria-hidden="true"></i>
          {{ $t('nav.landing') }}
        </RouterLink>

        <div class="nav-label">{{ $t('nav.groupAccount') }}</div>
        <RouterLink to="/admin/profile">
          <i class="bi bi-person" aria-hidden="true"></i>
          {{ $t('nav.profile') }}
        </RouterLink>
      </nav>

      <div class="sidebar-spacer"></div>

      <RouterLink class="user-chip" :to="auth.isAdmin ? '/admin/profile' : '/merchant/profile'" @click="closeNav">
        <div class="avatar">{{ initials }}</div>
        <div class="meta">
          <div class="name">{{ auth.user?.fullName || 'User' }}</div>
          <div class="email">{{ auth.user?.email }}</div>
        </div>
      </RouterLink>
      <button class="btn ghost logout-btn" type="button" @click="logout">
        <i class="bi bi-box-arrow-right" aria-hidden="true"></i>
        {{ $t('common.logout') }}
      </button>
    </aside>

    <div class="content-col">
      <header class="topbar">
        <button class="menu-btn" type="button" @click="navOpen = true" :aria-label="$t('common.menu')">
          <i class="bi bi-list" aria-hidden="true"></i>
        </button>
        <div class="search-box">
          <i class="bi bi-search" aria-hidden="true"></i>
          <input v-model="q" type="search" :placeholder="$t('nav.searchPlaceholder')" />
        </div>
        <div class="top-actions">
          <LangSwitch />
          <NotificationBell />
          <RouterLink v-if="auth.isMerchant" class="btn top-cta" to="/merchant/test">
            <i class="bi bi-plus-lg" aria-hidden="true"></i>
            <span class="full">{{ $t('nav.newPayment') }}</span>
          </RouterLink>
          <RouterLink v-else class="btn top-cta" to="/admin/merchants">
            <i class="bi bi-plus-lg" aria-hidden="true"></i>
            <span class="full">{{ $t('nav.manageMerchants') }}</span>
          </RouterLink>
        </div>
      </header>
      <main class="main">
        <router-view />
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, onUnmounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'
import LangSwitch from '../components/LangSwitch.vue'
import NotificationBell from '../components/NotificationBell.vue'

const auth = useAuthStore()
const router = useRouter()
const route = useRoute()
const q = ref('')
const navOpen = ref(false)

const initials = computed(() => {
  const n = auth.user?.fullName || 'F'
  return n.trim().split(/\s+/).slice(0, 2).map((p) => p[0]).join('').toUpperCase()
})

function closeNav() {
  navOpen.value = false
}

function onNavClick(e) {
  if (e.target?.closest('a')) closeNav()
}

function logout() {
  auth.logout()
  closeNav()
  router.push('/login')
}

watch(() => route.fullPath, closeNav)
watch(navOpen, (v) => {
  document.body.style.overflow = v ? 'hidden' : ''
})
onUnmounted(() => {
  document.body.style.overflow = ''
})
</script>
