<template>
  <div class="app-shell" :class="{ 'nav-open': navOpen }">
    <div class="sidebar-backdrop" v-if="navOpen" @click="navOpen = false" />

    <aside class="sidebar" :class="{ open: navOpen }">
      <div class="sidebar-top">
        <RouterLink to="/" class="brand-row" aria-label="Fynexpay" @click="closeNav">
          <img src="/full-logo.png" alt="Fynexpay" class="brand-logo" />
        </RouterLink>
        <button class="sidebar-close" type="button" @click="closeNav" :aria-label="$t('common.close')">
          ✕
        </button>
      </div>

      <div class="nav-label">{{ $t('common.menu') }}</div>
      <nav class="nav" v-if="auth.isMerchant" @click="onNavClick">
        <RouterLink to="/merchant">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5Z"/></svg>
          {{ $t('nav.overview') }}
        </RouterLink>
        <RouterLink to="/merchant/docs">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M7 4h8l4 4v12a1 1 0 0 1-1 1H7a1 1 0 0 1-1-1V5a1 1 0 0 1 1-1Z"/><path d="M14 4v5h5M9 13h6M9 17h6"/></svg>
          {{ $t('nav.docs') }}
        </RouterLink>
        <RouterLink to="/merchant/test">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="8"/><path d="M12 8v4l3 2"/></svg>
          {{ $t('nav.test') }}
        </RouterLink>
        <RouterLink to="/merchant/payment-methods">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="6" width="18" height="12" rx="2"/><path d="M3 10h18M7 15h4"/></svg>
          {{ $t('nav.paymentMethods') }}
        </RouterLink>
        <RouterLink to="/merchant/payments">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 7h16v10H4z"/><path d="M4 11h16M8 15h3"/></svg>
          {{ $t('nav.payments') }}
        </RouterLink>
        <RouterLink to="/merchant/platforms">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="4" width="18" height="6" rx="2"/><rect x="3" y="14" width="18" height="6" rx="2"/></svg>
          {{ $t('nav.platforms') }}
        </RouterLink>
        <RouterLink to="/merchant/keys">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="8" cy="12" r="3"/><path d="M11 12h10M17 12v3M20 12v2"/></svg>
          {{ $t('nav.keys') }}
        </RouterLink>
        <RouterLink to="/merchant/payouts">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 3v12"/><path d="m8 11 4 4 4-4"/><path d="M5 19h14"/></svg>
          {{ $t('nav.payouts') }}
        </RouterLink>
      </nav>

      <nav class="nav" v-if="auth.isAdmin" @click="onNavClick">
        <RouterLink to="/admin">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 10.5 12 4l8 6.5V20a1 1 0 0 1-1 1h-5v-6H10v6H5a1 1 0 0 1-1-1v-9.5Z"/></svg>
          {{ $t('nav.admin') }}
        </RouterLink>
        <RouterLink to="/admin/merchants">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M16 21v-2a4 4 0 0 0-4-4H7a4 4 0 0 0-4 4v2"/><circle cx="9.5" cy="7" r="3.5"/><path d="M20 8v6M17 11h6"/></svg>
          {{ $t('nav.merchants') }}
        </RouterLink>
        <RouterLink to="/admin/platforms">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="3" y="4" width="18" height="6" rx="2"/><rect x="3" y="14" width="18" height="6" rx="2"/></svg>
          {{ $t('nav.platforms') }}
        </RouterLink>
        <RouterLink to="/admin/payments">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 7h16v10H4z"/><path d="M4 11h16"/></svg>
          {{ $t('nav.payments') }}
        </RouterLink>
        <RouterLink to="/admin/payouts">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 3v12"/><path d="m8 11 4 4 4-4"/><path d="M5 19h14"/></svg>
          {{ $t('nav.adminPayouts') }}
        </RouterLink>
        <RouterLink to="/admin/providers">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="12" cy="12" r="3"/><path d="M12 3v3M12 18v3M3 12h3M18 12h3M5.6 5.6l2.1 2.1M16.3 16.3l2.1 2.1M18.4 5.6l-2.1 2.1M7.7 16.3l-2.1 2.1"/></svg>
          {{ $t('nav.providers') }}
        </RouterLink>
        <RouterLink to="/admin/ultramsg">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M12 21a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z"/><path d="M8.2 15.4c1.6 1.5 3.6 2.1 5.6 1.7l1.5.8-.4-1.6A6.4 6.4 0 0 0 17 11.2c0-3-2.5-5.4-5.5-5.4S6 8.2 6 11.2c0 1.5.6 2.9 1.7 3.9l.5.3Z"/></svg>
          {{ $t('nav.ultramsg') }}
        </RouterLink>
        <RouterLink to="/admin/landing">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M4 5h16v14H4z"/><path d="M8 9h8M8 13h5"/></svg>
          {{ $t('nav.landing') }}
        </RouterLink>
      </nav>

      <div class="sidebar-spacer"></div>

      <div class="upgrade-card" v-if="auth.isMerchant">
        <h4>{{ $t('nav.upgradeTitle') }}</h4>
        <p>{{ $t('nav.upgradeBody') }}</p>
        <ul>
          <li>Hosted Checkout</li>
          <li>{{ $t('nav.upgradeMethods') }}</li>
          <li>Signed Webhooks</li>
        </ul>
        <RouterLink class="btn" to="/merchant/test" @click="closeNav">{{ $t('nav.tryPayment') }}</RouterLink>
      </div>

      <div class="upgrade-card" v-else>
        <h4>{{ $t('nav.controlTitle') }}</h4>
        <p>{{ $t('nav.controlBody') }}</p>
        <RouterLink class="btn" to="/admin/providers" @click="closeNav">{{ $t('nav.setupProviders') }}</RouterLink>
      </div>

      <div class="user-chip">
        <div class="avatar">{{ initials }}</div>
        <div class="meta">
          <div class="name">{{ auth.user?.fullName || 'User' }}</div>
          <div class="email">{{ auth.user?.email }}</div>
        </div>
      </div>
      <button class="btn secondary logout-btn" type="button" @click="logout">{{ $t('common.logout') }}</button>
    </aside>

    <div class="content-col">
      <header class="topbar">
        <button class="menu-btn" type="button" @click="navOpen = true" :aria-label="$t('common.menu')">
          <span /><span /><span />
        </button>
        <div class="search-box">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><circle cx="11" cy="11" r="7"/><path d="m20 20-3.5-3.5"/></svg>
          <input v-model="q" type="search" :placeholder="$t('nav.searchPlaceholder')" />
          <kbd>⌘ K</kbd>
        </div>
        <div class="top-actions">
          <LangSwitch />
          <NotificationBell />
          <RouterLink v-if="auth.isMerchant" class="btn top-cta" to="/merchant/test">
            <span class="full">{{ $t('nav.newPayment') }}</span>
            <span class="short">+</span>
          </RouterLink>
          <RouterLink v-else class="btn top-cta" to="/admin/merchants">
            <span class="full">{{ $t('nav.manageMerchants') }}</span>
            <span class="short">+</span>
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
