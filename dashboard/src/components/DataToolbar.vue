<template>
  <div class="data-toolbar card">
    <div class="filters">
      <div class="field search" v-if="showSearch">
        <label>{{ $t('common.search') }}</label>
        <input
          :value="modelValue.q"
          type="search"
          :placeholder="searchPlaceholder || $t('common.search')"
          @input="patch({ q: $event.target.value })"
          @keydown.enter.prevent="emit('apply')"
        />
      </div>
      <div class="field" v-if="statuses?.length">
        <label>{{ $t('common.status') }}</label>
        <select :value="modelValue.status || ''" @change="patch({ status: $event.target.value })">
          <option value="">{{ statusAllLabel || $t('common.all') }}</option>
          <option v-for="s in statuses" :key="s" :value="s">{{ $t(`status.${s}`, s) }}</option>
        </select>
      </div>
      <div class="field" v-if="providers?.length">
        <label>{{ $t('common.provider') }}</label>
        <select :value="modelValue.provider || ''" @change="patch({ provider: $event.target.value })">
          <option value="">{{ providerAllLabel || $t('common.all') }}</option>
          <option v-for="p in providers" :key="p" :value="p">{{ p }}</option>
        </select>
      </div>
      <div class="field date" v-if="showDates">
        <label>{{ $t('common.from') }}</label>
        <input type="date" :value="modelValue.from || ''" @change="patch({ from: $event.target.value })" />
      </div>
      <div class="field date" v-if="showDates">
        <label>{{ $t('common.to') }}</label>
        <input type="date" :value="modelValue.to || ''" @change="patch({ to: $event.target.value })" />
      </div>
    </div>
    <div class="actions">
      <button class="btn secondary" type="button" @click="emit('reset')">
        <i class="bi bi-arrow-counterclockwise" aria-hidden="true"></i>
        {{ $t('common.reset') }}
      </button>
      <button class="btn" type="button" @click="emit('apply')">
        <i class="bi bi-funnel" aria-hidden="true"></i>
        {{ $t('common.apply') }}
      </button>
    </div>
  </div>
</template>

<script setup>
const props = defineProps({
  modelValue: { type: Object, required: true },
  statuses: { type: Array, default: null },
  providers: { type: Array, default: null },
  showSearch: { type: Boolean, default: true },
  showDates: { type: Boolean, default: true },
  searchPlaceholder: { type: String, default: '' },
  statusAllLabel: { type: String, default: '' },
  providerAllLabel: { type: String, default: '' }
})

const emit = defineEmits(['update:modelValue', 'apply', 'reset'])

function patch(partial) {
  emit('update:modelValue', { ...props.modelValue, ...partial })
}
</script>

<style scoped>
.data-toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 12px 16px;
  align-items: flex-end;
  justify-content: space-between;
  margin-bottom: 14px;
}
.filters {
  display: flex;
  flex-wrap: wrap;
  gap: 12px;
  flex: 1;
}
.field { margin-bottom: 0; min-width: 140px; }
.field.search { min-width: 220px; flex: 1; }
.actions { display: flex; gap: 8px; }
@media (max-width: 600px) {
  .data-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
    padding: 16px;
  }
  .filters {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
  }
  .field,
  .field.search {
    min-width: 0;
    width: 100%;
  }
  .field.search { grid-column: 1 / -1; }
  .field:not(.search):not(.date) { grid-column: 1 / -1; }
  .actions {
    display: grid;
    grid-template-columns: 1fr 1fr;
    width: 100%;
  }
  .actions .btn {
    width: 100%;
    min-height: 44px;
  }
}
</style>
