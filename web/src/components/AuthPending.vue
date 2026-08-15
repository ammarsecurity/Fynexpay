<template>
  <div class="auth-pending" role="status">
    <span class="auth-pending-ico" aria-hidden="true">
      <i class="bi bi-hourglass-split"></i>
    </span>
    <h2>{{ title }}</h2>
    <p class="lead">{{ lead }}</p>
    <p class="body">{{ body }}</p>
    <div class="auth-pending-actions">
      <RouterLink class="btn primary submit" to="/">{{ t('pendingHome') }}</RouterLink>
      <RouterLink class="btn soft submit" to="/contact">{{ t('pendingContact') }}</RouterLink>
    </div>
  </div>
</template>

<script setup>
defineProps({
  title: { type: String, required: true },
  lead: { type: String, required: true },
  body: { type: String, required: true },
  t: { type: Function, required: true }
})
</script>
