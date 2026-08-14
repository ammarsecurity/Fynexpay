import { createApp } from 'vue'
import { createPinia } from 'pinia'
import App from './App.vue'
import router from './router'
import { i18n } from './i18n'
import { setUnauthorizedHandler } from './api'
import { useAuthStore } from './stores/auth'
import { useNotificationsStore } from './stores/notifications'
import './assets/main.css'

const app = createApp(App)
const pinia = createPinia()
app.use(pinia).use(router).use(i18n)

const guestNames = new Set(['login', 'register', 'forgot', 'forgot-password', 'auth-handoff'])

setUnauthorizedHandler(() => {
  const auth = useAuthStore()
  const notifications = useNotificationsStore()
  notifications.stopPolling?.()
  auth.logout()
  const name = router.currentRoute.value.name
  if (guestNames.has(String(name || ''))) return
  router.replace({ path: '/login', query: { session: 'expired' } })
})

app.mount('#app')
