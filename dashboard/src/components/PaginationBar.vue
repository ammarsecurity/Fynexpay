<template>
  <div class="pagination" v-if="total > 0">
    <div class="meta muted">
      {{ $t('common.showing') }}
      <strong>{{ from }}–{{ to }}</strong>
      {{ $t('common.of') }}
      <strong>{{ total }}</strong>
      {{ $t('common.results') }}
    </div>
    <div class="controls">
      <select class="page-size" :value="pageSize" @change="onSize($event.target.value)">
        <option v-for="n in sizes" :key="n" :value="n">{{ n }} / {{ $t('common.page') }}</option>
      </select>
      <button class="btn secondary icon-only" type="button" :disabled="page <= 1" :aria-label="$t('common.page')" @click="go(page - 1)">
        <i class="bi bi-chevron-right" aria-hidden="true"></i>
      </button>
      <span class="page-label">{{ $t('common.page') }} {{ page }} {{ $t('common.of') }} {{ pages }}</span>
      <button class="btn secondary icon-only" type="button" :disabled="page >= pages" :aria-label="$t('common.page')" @click="go(page + 1)">
        <i class="bi bi-chevron-left" aria-hidden="true"></i>
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  page: { type: Number, required: true },
  pageSize: { type: Number, required: true },
  total: { type: Number, required: true },
  sizes: { type: Array, default: () => [10, 20, 50] }
})

const emit = defineEmits(['update:page', 'update:pageSize', 'change'])

const pages = computed(() => Math.max(1, Math.ceil(props.total / props.pageSize)))
const from = computed(() => (props.total === 0 ? 0 : (props.page - 1) * props.pageSize + 1))
const to = computed(() => Math.min(props.total, props.page * props.pageSize))

function go(p) {
  const next = Math.min(pages.value, Math.max(1, p))
  emit('update:page', next)
  emit('change')
}

function onSize(v) {
  emit('update:pageSize', Number(v))
  emit('update:page', 1)
  emit('change')
}
</script>

<style scoped>
.pagination {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  align-items: center;
  justify-content: space-between;
  margin-top: 14px;
  padding-top: 12px;
  border-top: 1px solid var(--line);
}
.controls { display: flex; align-items: center; gap: 8px; }
.page-size {
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 8px 10px;
  background: #fff;
}
.page-label { font-weight: 600; font-size: 0.9rem; color: var(--muted); }
.controls .btn { padding: 0; }
:global(html[dir="ltr"]) .controls .bi {
  display: inline-block;
  transform: scaleX(-1);
}
</style>
