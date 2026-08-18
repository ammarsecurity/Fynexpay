<template>
  <div class="copy-box">
    <div v-if="label" class="key-label">
      {{ label }}
      <span v-if="hint" class="mono muted">{{ hint }}</span>
    </div>
    <code class="mono value" dir="ltr">{{ value || '—' }}</code>
    <button class="btn" type="button" :disabled="!value" @click="copy">
      {{ copied ? $t('platforms.copied') : (copyLabel || $t('platforms.copy')) }}
    </button>
    <slot />
  </div>
</template>

<script setup>
import { ref } from 'vue'

const props = defineProps({
  value: { type: String, default: '' },
  label: { type: String, default: '' },
  hint: { type: String, default: '' },
  copyLabel: { type: String, default: '' }
})

const copied = ref(false)
let timer = 0

async function copy() {
  if (!props.value) return
  try { await navigator.clipboard.writeText(props.value) } catch { /* ignore */ }
  copied.value = true
  clearTimeout(timer)
  timer = window.setTimeout(() => { copied.value = false }, 1600)
}
</script>

<style scoped>
.copy-box {
  display: flex;
  gap: 10px;
  align-items: center;
  border: 1px solid var(--line);
  border-radius: 14px;
  padding: 10px 12px;
  flex-wrap: wrap;
  background: #fff;
}
.key-label {
  font-weight: 800;
  font-size: 0.82rem;
  min-width: 120px;
}
.value {
  flex: 1;
  min-width: 0;
  overflow-x: auto;
  white-space: nowrap;
}
@media (max-width: 600px) {
  .copy-box { align-items: stretch; }
  .value { width: 100%; }
  .copy-box .btn { min-height: 44px; flex: 1; }
}
</style>
