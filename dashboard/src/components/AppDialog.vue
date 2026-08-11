<template>
  <Teleport to="body">
    <div v-if="state.open" class="dlg-root" @keydown.esc.prevent="onCancel">
      <div class="dlg-backdrop" @click="onCancel"></div>
      <div class="dlg-panel" role="alertdialog" aria-modal="true" :aria-labelledby="titleId">
        <div class="dlg-icon" :class="state.variant" aria-hidden="true">
          <span v-if="state.variant === 'danger'">!</span>
          <span v-else-if="state.variant === 'success'">✓</span>
          <span v-else>?</span>
        </div>
        <h2 :id="titleId" class="dlg-title">{{ displayTitle }}</h2>
        <p class="dlg-message">{{ state.message }}</p>
        <div class="dlg-actions">
          <button
            v-if="state.type === 'confirm'"
            class="btn secondary"
            type="button"
            @click="onCancel"
          >{{ displayCancel }}</button>
          <button
            class="btn"
            :class="{ danger: state.variant === 'danger' }"
            type="button"
            autofocus
            @click="onConfirm"
          >{{ displayConfirm }}</button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useDialog } from '../composables/useDialog'

const { t } = useI18n()
const { state, close } = useDialog()
const titleId = 'app-dialog-title'

const displayTitle = computed(() => {
  if (state.title) return state.title
  if (state.type === 'alert') return t('dialog.alertTitle')
  if (state.variant === 'danger') return t('dialog.dangerTitle')
  return t('dialog.confirmTitle')
})

const displayConfirm = computed(() => {
  if (state.confirmText) return state.confirmText
  if (state.type === 'alert') return t('dialog.ok')
  if (state.variant === 'danger') return t('dialog.confirmDanger')
  return t('dialog.confirm')
})

const displayCancel = computed(() => state.cancelText || t('common.cancel'))

function onConfirm() { close(true) }
function onCancel() { close(state.type === 'alert' ? true : false) }
</script>

<style scoped>
.dlg-root {
  position: fixed;
  inset: 0;
  z-index: 120;
  display: grid;
  place-items: center;
  padding: 20px;
}
.dlg-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(3, 24, 56, 0.55);
  backdrop-filter: blur(6px);
}
.dlg-panel {
  position: relative;
  width: min(420px, 100%);
  background:
    radial-gradient(500px 160px at 100% 0%, rgba(3, 24, 56, 0.1), transparent 60%),
    #fff;
  border: 1px solid var(--line);
  border-radius: 22px;
  box-shadow: 0 28px 70px rgba(3, 24, 56, 0.28);
  padding: 26px 24px 22px;
  text-align: center;
  animation: dlg-in 0.18s ease;
}
@keyframes dlg-in {
  from { opacity: 0; transform: translateY(8px) scale(0.98); }
  to { opacity: 1; transform: none; }
}
.dlg-icon {
  width: 52px;
  height: 52px;
  margin: 0 auto 14px;
  border-radius: 16px;
  display: grid;
  place-items: center;
  font-size: 1.35rem;
  font-weight: 800;
  background: var(--brand-soft);
  color: var(--brand-secondary);
}
.dlg-icon.danger {
  background: var(--danger-soft);
  color: var(--danger);
}
.dlg-icon.success {
  background: var(--ok-soft);
  color: var(--ok);
}
.dlg-title {
  margin: 0 0 8px;
  font-size: 1.15rem;
  color: var(--brand);
  letter-spacing: -0.02em;
}
.dlg-message {
  margin: 0 0 20px;
  color: var(--muted);
  font-weight: 600;
  line-height: 1.55;
  font-size: 0.95rem;
}
.dlg-actions {
  display: flex;
  justify-content: center;
  gap: 10px;
  flex-wrap: wrap;
}
.dlg-actions .btn { min-width: 110px; justify-content: center; }
</style>
