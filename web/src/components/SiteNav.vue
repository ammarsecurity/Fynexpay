<template>
  <header class="site-nav">
    <div class="nav-inner">
      <RouterLink class="logo" to="/" aria-label="Fynexpay" @click="closeMenu">
        <img src="/full-logo.png" alt="Fynexpay" class="logo-img" />
      </RouterLink>

      <nav class="nav-links desktop-only" v-if="c" :aria-label="locale === 'en' ? 'Main' : 'القائمة الرئيسية'">
        <a href="/#features" :class="{ active: isSection('features') }" @click="closeMenu">{{ c.navFeatures }}</a>
        <a href="/#api" :class="{ active: isSection('api') }" @click="closeMenu">{{ c.navDevelopers }}</a>
        <RouterLink to="/contact" @click="closeMenu">{{ c.navContact }}</RouterLink>
      </nav>

      <div class="nav-actions">
        <div class="lang-switch" role="group" :aria-label="locale === 'en' ? 'Language' : 'اللغة'">
          <button type="button" :class="{ active: locale === 'ar' }" @click="setLocale('ar')">ع</button>
          <button type="button" :class="{ active: locale === 'en' }" @click="setLocale('en')">EN</button>
        </div>
        <RouterLink class="nav-chip nav-login" to="/login">{{ c?.login }}</RouterLink>
        <RouterLink class="nav-chip nav-start" to="/register">
          <span class="full">{{ c?.startNow }}</span>
          <span class="short">{{ locale === 'en' ? 'Start' : 'ابدأ' }}</span>
        </RouterLink>
        <button
          class="nav-chip menu-toggle"
          type="button"
          :aria-expanded="open"
          :aria-label="locale === 'en' ? 'Menu' : 'القائمة'"
          @click="open = !open"
        >
          <span /><span /><span />
        </button>
      </div>
    </div>

    <div class="mobile-sheet" :class="{ open }" v-if="c">
      <nav class="mobile-links">
        <a href="/#features" @click="closeMenu">{{ c.navFeatures }}</a>
        <a href="/#api" @click="closeMenu">{{ c.navDevelopers }}</a>
        <RouterLink to="/contact" @click="closeMenu">{{ c.navContact }}</RouterLink>
      </nav>
      <div class="mobile-cta">
        <RouterLink class="btn soft" to="/login" @click="closeMenu">{{ c.login }}</RouterLink>
        <RouterLink class="btn primary" to="/register" @click="closeMenu">{{ c.startNow }}</RouterLink>
      </div>
    </div>
    <button v-if="open" class="nav-backdrop" type="button" aria-label="Close" @click="closeMenu" />
  </header>
</template>

<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useLanding } from '../composables/useLanding'

const { c, locale, setLocale } = useLanding()
const route = useRoute()
const open = ref(false)
const hash = ref(typeof window !== 'undefined' ? window.location.hash : '')

function closeMenu() {
  open.value = false
}

function isSection(id) {
  return route.path === '/' && hash.value === `#${id}`
}

function syncHash() {
  hash.value = window.location.hash || ''
}

watch(() => route.fullPath, () => {
  closeMenu()
  syncHash()
})
watch(open, (v) => {
  document.body.style.overflow = v ? 'hidden' : ''
})
onMounted(() => {
  syncHash()
  window.addEventListener('hashchange', syncHash)
})
onUnmounted(() => {
  document.body.style.overflow = ''
  window.removeEventListener('hashchange', syncHash)
})
</script>
