<template>
  <div class="redirecting">{{ $t('common.loading') }}</div>
</template>

<script setup>
import { onMounted } from 'vue'

const WEB_URL = (import.meta.env.VITE_WEB_URL || 'http://localhost:5174').replace(/\/$/, '')
const path = window.location.pathname.includes('register')
  ? '/register'
  : window.location.pathname.includes('forgot')
    ? '/forgot'
    : '/login'

onMounted(() => {
  window.location.replace(`${WEB_URL}${path}${window.location.search || ''}`)
})
</script>

<style scoped>
.redirecting {
  min-height: 100vh;
  display: grid;
  place-items: center;
  color: var(--muted);
  font-weight: 700;
}
</style>
