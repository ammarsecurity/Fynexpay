<template>
  <div class="handoff">
    <p>{{ message }}</p>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()
const message = ref('…')

onMounted(() => {
  try {
    const raw = (window.location.hash || '').replace(/^#/, '')
    if (!raw) throw new Error('missing')
    const data = JSON.parse(decodeURIComponent(escape(atob(raw))))
    const status = String(data.user?.merchantStatus || '')
    if (!data?.token || status.toLowerCase() === 'pending') {
      const web = (import.meta.env.VITE_WEB_URL || 'https://fynexpay.net').replace(/\/$/, '')
      window.location.replace(`${web}/login?pending=1`)
      return
    }
    auth.applyAuth({
      token: data.token,
      userId: data.user?.userId,
      email: data.user?.email,
      fullName: data.user?.fullName,
      role: data.user?.role,
      merchantId: data.user?.merchantId,
      merchantStatus: data.user?.merchantStatus
    })
    // Clear hash from URL
    history.replaceState(null, '', window.location.pathname)
    if (auth.isAdmin) router.replace('/admin')
    else router.replace('/merchant')
  } catch {
    message.value = 'Auth handoff failed'
    router.replace('/login')
  }
})
</script>

<style scoped>
.handoff {
  min-height: 100vh;
  display: grid;
  place-items: center;
  color: var(--muted);
  font-weight: 700;
}
</style>
