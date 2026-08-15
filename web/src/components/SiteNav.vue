<template>
  <header class="lp-nav" :class="{ 'is-open': open, 'is-scrolled': scrolled }">
    <div class="lp-wrap lp-nav-bar">
      <RouterLink class="lp-logo" to="/" aria-label="FynexPay" @click="closeMenu">
        <img src="/full-logo.png" alt="FynexPay" />
      </RouterLink>

      <nav class="lp-links" :aria-label="locale === 'en' ? 'Main' : 'القائمة الرئيسية'">
        <RouterLink to="/" :class="{ 'is-on': route.path === '/' }">{{ t('navHome') }}</RouterLink>
        <a href="/#features">{{ t('navFeatures') }}</a>
        <RouterLink to="/demo">{{ t('navStores') }}</RouterLink>
        <RouterLink to="/pricing">{{ t('navPricing') }}</RouterLink>
        <RouterLink to="/developers">{{ t('navDevelopers') }}</RouterLink>
        <RouterLink to="/for-platforms">{{ t('navPartners') }}</RouterLink>
        <RouterLink to="/contact">{{ t('navContact') }}</RouterLink>
      </nav>

      <div class="lp-actions">
        <div class="lang-switch" role="group" :aria-label="locale === 'en' ? 'Language' : 'اللغة'">
          <button type="button" :class="{ active: locale === 'ar' }" @click="setLocale('ar')">ع</button>
          <button type="button" :class="{ active: locale === 'en' }" @click="setLocale('en')">EN</button>
        </div>
        <RouterLink class="lp-login" to="/login">{{ t('login') }}</RouterLink>
        <RouterLink class="lp-btn lp-nav-cta" to="/register">
          <span class="cta-full">{{ t('startFree') }}</span>
          <span class="cta-short">{{ t('start') }}</span>
        </RouterLink>
        <button
          class="lp-menu"
          type="button"
          :aria-expanded="open"
          :aria-label="open ? (locale === 'en' ? 'Close menu' : 'إغلاق القائمة') : (locale === 'en' ? 'Open menu' : 'فتح القائمة')"
          @click="open = !open"
        >
          <i class="bi" :class="open ? 'bi-x-lg' : 'bi-list'"></i>
        </button>
      </div>
    </div>

    <div class="lp-drawer" v-if="open">
      <button class="lp-drawer-bg" type="button" :aria-label="locale === 'en' ? 'Close' : 'إغلاق'" @click="closeMenu"></button>
      <aside class="lp-drawer-panel" role="dialog" aria-modal="true">
        <div class="lp-drawer-head">
          <RouterLink class="lp-logo" to="/" @click="closeMenu">
            <img src="/full-logo.png" alt="FynexPay" />
          </RouterLink>
          <button class="lp-menu" type="button" @click="closeMenu">
            <i class="bi bi-x-lg"></i>
          </button>
        </div>
        <nav class="lp-drawer-links">
          <RouterLink to="/" :class="{ 'is-on': route.path === '/' }" @click="closeMenu">
            <i class="bi bi-house"></i>{{ t('navHome') }}
          </RouterLink>
          <a href="/#features" @click="closeMenu"><i class="bi bi-stars"></i>{{ t('navFeatures') }}</a>
          <RouterLink to="/demo" @click="closeMenu"><i class="bi bi-shop"></i>{{ t('navStores') }}</RouterLink>
          <RouterLink to="/pricing" @click="closeMenu"><i class="bi bi-tag"></i>{{ t('navPricing') }}</RouterLink>
          <RouterLink to="/developers" @click="closeMenu"><i class="bi bi-code-slash"></i>{{ t('navDevelopers') }}</RouterLink>
          <RouterLink to="/for-platforms" @click="closeMenu"><i class="bi bi-people"></i>{{ t('navPartners') }}</RouterLink>
          <RouterLink to="/contact" @click="closeMenu"><i class="bi bi-chat-dots"></i>{{ t('navContact') }}</RouterLink>
        </nav>
        <div class="lp-drawer-foot">
          <div class="lang-switch" role="group">
            <button type="button" :class="{ active: locale === 'ar' }" @click="setLocale('ar')">ع</button>
            <button type="button" :class="{ active: locale === 'en' }" @click="setLocale('en')">EN</button>
          </div>
          <RouterLink class="lp-login" to="/login" @click="closeMenu">{{ t('login') }}</RouterLink>
          <RouterLink class="lp-btn" to="/register" @click="closeMenu">{{ t('startFree') }}</RouterLink>
        </div>
      </aside>
    </div>
  </header>
</template>

<script setup>
import { onMounted, onUnmounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { useSiteCopy } from '../composables/useSiteCopy'

const { t, locale, setLocale } = useSiteCopy()
const route = useRoute()
const open = ref(false)
const scrolled = ref(false)

function closeMenu() { open.value = false }
function onScroll() { scrolled.value = window.scrollY > 8 }
function onKey(e) { if (e.key === 'Escape') closeMenu() }

watch(() => route.fullPath, closeMenu)
watch(open, (v) => { document.body.style.overflow = v ? 'hidden' : '' })

onMounted(() => {
  onScroll()
  window.addEventListener('scroll', onScroll, { passive: true })
  window.addEventListener('keydown', onKey)
})
onUnmounted(() => {
  document.body.style.overflow = ''
  window.removeEventListener('scroll', onScroll)
  window.removeEventListener('keydown', onKey)
})
</script>
