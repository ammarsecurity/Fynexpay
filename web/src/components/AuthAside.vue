<template>
  <aside class="auth-aside">
    <img src="/full-logo.png" alt="Fynexpay" class="aside-logo" />
    <h1>{{ title }}</h1>
    <p>{{ body }}</p>
    <ul class="aside-points">
      <li>{{ t('pointSecure') }}</li>
      <li>{{ t('pointWallet') }}</li>
      <li>{{ t('pointFast') }}</li>
    </ul>
  </aside>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  body: { type: String, required: true },
  t: { type: Function, required: true }
})
</script>
