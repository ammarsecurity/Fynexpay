<template>
  <span class="provider-badge" :class="size">
    <img v-if="logo" :src="logo" :alt="label" class="logo" />
    <span v-else class="fallback">{{ initial }}</span>
    <span v-if="showName" class="name">{{ label }}</span>
  </span>
</template>

<script setup>
import { computed } from 'vue'
import { useProviders } from '../composables/useProviders'

const props = defineProps({
  provider: { type: String, default: '' },
  showName: { type: Boolean, default: true },
  size: { type: String, default: 'md' }
})

const { logoOf, nameOf } = useProviders()
const logo = computed(() => logoOf(props.provider))
const label = computed(() => nameOf(props.provider))
const initial = computed(() => (label.value || 'P').charAt(0).toUpperCase())
</script>

<style scoped>
.provider-badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  max-width: 100%;
}
.logo, .fallback {
  width: 28px;
  height: 28px;
  border-radius: 8px;
  border: 1px solid var(--line);
  background: #fff;
  object-fit: contain;
  flex-shrink: 0;
}
.fallback {
  display: grid;
  place-items: center;
  font-weight: 800;
  font-size: 0.75rem;
  color: var(--brand-secondary);
  background: var(--brand-soft);
}
.name {
  font-weight: 600;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.sm .logo, .sm .fallback { width: 22px; height: 22px; border-radius: 6px; }
.lg .logo, .lg .fallback { width: 40px; height: 40px; border-radius: 12px; }
</style>
