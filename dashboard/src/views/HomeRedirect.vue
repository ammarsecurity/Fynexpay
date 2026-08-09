<script setup>
import { useAuthStore } from '../stores/auth'
import { useRouter } from 'vue-router'
const auth = useAuthStore()
const router = useRouter()
router.replace(auth.isAdmin ? '/admin' : '/merchant')
</script>
<template><p class="muted">جاري التوجيه...</p></template>
