import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { api } from '../api'
import { useAuthStore } from './auth'

export const useNotificationsStore = defineStore('notifications', () => {
  const items = ref([])
  const unreadCount = ref(0)
  const loading = ref(false)
  const open = ref(false)
  let pollTimer = null

  const hasUnread = computed(() => unreadCount.value > 0)
  const badge = computed(() => (unreadCount.value > 99 ? '99+' : String(unreadCount.value)))

  function endpointBase() {
    const auth = useAuthStore()
    return auth.isAdmin ? '/api/admin/notifications' : '/api/merchant/notifications'
  }

  async function fetchSummary() {
    const auth = useAuthStore()
    if (!auth.token) return
    loading.value = true
    try {
      const { data } = await api.get(endpointBase())
      items.value = data.items || []
      unreadCount.value = data.unreadCount || 0
    } catch {
      /* ignore poll errors */
    } finally {
      loading.value = false
    }
  }

  async function markRead(id) {
    await api.post(`${endpointBase()}/${id}/read`)
    const row = items.value.find((n) => n.id === id)
    if (row && !row.isRead) {
      row.isRead = true
      unreadCount.value = Math.max(0, unreadCount.value - 1)
    }
  }

  async function markAllRead() {
    await api.post(`${endpointBase()}/read-all`)
    items.value = items.value.map((n) => ({ ...n, isRead: true }))
    unreadCount.value = 0
  }

  function startPolling() {
    stopPolling()
    fetchSummary()
    pollTimer = setInterval(fetchSummary, 30000)
  }

  function stopPolling() {
    if (pollTimer) clearInterval(pollTimer)
    pollTimer = null
  }

  function toggle() {
    open.value = !open.value
    if (open.value) fetchSummary()
  }

  function close() {
    open.value = false
  }

  return {
    items,
    unreadCount,
    loading,
    open,
    hasUnread,
    badge,
    fetchSummary,
    markRead,
    markAllRead,
    startPolling,
    stopPolling,
    toggle,
    close
  }
})
