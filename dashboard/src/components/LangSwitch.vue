<template>
  <div class="lang-switch" role="group" :aria-label="$t('common.language')">
    <button type="button" :class="{ active: locale === 'ar' }" @click="set('ar')">ع</button>
    <button type="button" :class="{ active: locale === 'en' }" @click="set('en')">EN</button>
  </div>
</template>

<script setup>
import { useI18n } from 'vue-i18n'
import { setLocale } from '../i18n'

const { locale } = useI18n()

function set(loc) {
  setLocale(loc)
}
</script>

<style scoped>
.lang-switch {
  display: inline-flex;
  border: 1px solid var(--line);
  border-radius: 999px;
  background: #fff;
  overflow: hidden;
}
.lang-switch button {
  border: 0;
  background: transparent;
  padding: 6px 10px;
  font-weight: 700;
  font-size: 0.78rem;
  color: var(--muted);
  min-width: 36px;
}
.lang-switch button.active {
  background: var(--brand-soft);
  color: var(--brand-secondary);
}
</style>
