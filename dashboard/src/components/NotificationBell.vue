<template>
  <div class="notif-wrap" ref="root">
    <button class="icon-btn" type="button" :title="$t('common.notifications')" @click="store.toggle()">
      <i class="bi bi-bell" aria-hidden="true"></i>
      <span v-if="store.hasUnread" class="ping">{{ store.badge }}</span>
    </button>

    <div v-if="store.open" class="notif-panel" role="dialog" :aria-label="$t('notifications.title')">
      <div class="notif-head">
        <div>
          <strong>{{ $t('notifications.title') }}</strong>
          <p class="muted">{{ store.unreadCount ? $t('notifications.unread', { n: store.unreadCount }) : $t('notifications.allRead') }}</p>
        </div>
        <button
          v-if="store.hasUnread"
          class="linkish"
          type="button"
          :disabled="busy"
          @click="markAll"
        >
          {{ $t('notifications.markAll') }}
        </button>
      </div>

      <div v-if="store.loading && !store.items.length" class="notif-empty">{{ $t('common.loading') }}</div>
      <div v-else-if="!store.items.length" class="notif-empty">
        <i class="bi bi-bell-slash" aria-hidden="true"></i>
        <span>{{ $t('notifications.empty') }}</span>
      </div>
      <ul v-else class="notif-list">
        <li
          v-for="n in store.items"
          :key="n.id"
          :class="{ unread: !n.isRead }"
          @click="openItem(n)"
        >
          <span class="dot" aria-hidden="true"></span>
          <div class="body">
            <strong>{{ n.title }}</strong>
            <p>{{ n.body }}</p>
            <time>{{ formatTime(n.createdAtUtc) }}</time>
          </div>
        </li>
      </ul>
    </div>
  </div>
</template>

<script setup>
import { onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { useNotificationsStore } from '../stores/notifications'

const store = useNotificationsStore()
const router = useRouter()
const { locale } = useI18n()
const root = ref(null)
const busy = ref(false)

function onDocClick(e) {
  if (store.open && root.value && !root.value.contains(e.target)) store.close()
}

function formatTime(iso) {
  try {
    return new Intl.DateTimeFormat(locale.value === 'ar' ? 'ar-IQ' : 'en-GB', {
      dateStyle: 'medium',
      timeStyle: 'short'
    }).format(new Date(iso))
  } catch {
    return iso
  }
}

async function markAll() {
  busy.value = true
  try { await store.markAllRead() } finally { busy.value = false }
}

async function openItem(n) {
  if (!n.isRead) {
    try { await store.markRead(n.id) } catch { /* ignore */ }
  }
  store.close()
  if (n.linkUrl) router.push(n.linkUrl)
}

onMounted(() => {
  store.startPolling()
  document.addEventListener('click', onDocClick)
})
onUnmounted(() => {
  store.stopPolling()
  document.removeEventListener('click', onDocClick)
})
</script>

<style scoped>
.notif-wrap { position: relative; }
.icon-btn i { font-size: 1.05rem; }
.notif-panel {
  position: absolute;
  top: calc(100% + 10px);
  inset-inline-end: 0;
  width: min(380px, calc(100vw - 32px));
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 16px;
  box-shadow: 0 18px 48px rgba(3, 24, 56, 0.14);
  z-index: 80;
  overflow: hidden;
}
.notif-head {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: flex-start;
  padding: 14px 16px;
  border-bottom: 1px solid var(--line);
  background: #fbfcfe;
}
.notif-head strong { display: block; font-size: 0.95rem; }
.notif-head .muted { margin: 4px 0 0; font-size: 0.8rem; }
.linkish {
  border: 0;
  background: transparent;
  color: var(--brand-secondary);
  font: inherit;
  font-weight: 700;
  font-size: 0.82rem;
  cursor: pointer;
  white-space: nowrap;
}
.notif-list {
  list-style: none;
  margin: 0;
  padding: 0;
  max-height: 420px;
  overflow: auto;
}
.notif-list li {
  display: flex;
  gap: 10px;
  padding: 12px 16px;
  border-bottom: 1px solid #f1f5f9;
  cursor: pointer;
  transition: background 0.12s ease;
}
.notif-list li:hover { background: #f8fafc; }
.notif-list li.unread { background: rgba(3, 24, 56, 0.04); }
.dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-top: 7px;
  flex-shrink: 0;
  background: transparent;
}
.notif-list li.unread .dot { background: var(--brand-secondary); }
.body { min-width: 0; }
.body strong {
  display: block;
  font-size: 0.88rem;
  margin-bottom: 4px;
  color: var(--brand);
}
.body p {
  margin: 0;
  color: var(--muted);
  font-size: 0.82rem;
  line-height: 1.55;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  overflow: hidden;
}
.body time {
  display: block;
  margin-top: 6px;
  font-size: 0.72rem;
  color: #94a3b8;
  font-weight: 600;
}
.notif-empty {
  padding: 36px 20px;
  text-align: center;
  color: var(--muted);
  font-weight: 600;
  display: grid;
  gap: 8px;
  place-items: center;
}
.notif-empty i { font-size: 1.6rem; opacity: 0.45; }
</style>
