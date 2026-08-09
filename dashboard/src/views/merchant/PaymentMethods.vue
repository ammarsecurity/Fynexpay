<template>
  <div class="methods">
    <div class="hero">
      <h1>{{ $t('methods.title') }}</h1>
      <p class="muted">{{ $t('methods.subtitle') }}</p>
    </div>

    <div class="card">
      <p v-if="message" class="ok-msg">{{ message }}</p>
      <p v-if="error" class="error">{{ error }}</p>

      <div class="list" v-if="form">
        <label
          v-for="item in methodItems"
          :key="item.key"
          class="item"
          :class="{ off: !platformHas(item.platformKey) }"
        >
          <div class="row" style="gap:12px">
            <ProviderBadge :provider="item.platformKey" :show-name="false" size="lg" />
            <div>
              <strong>{{ item.title }}</strong>
              <p class="muted">{{ item.hint }}</p>
              <span v-if="!platformHas(item.platformKey)" class="badge warn">{{ $t('methods.platformOff') }}</span>
            </div>
          </div>
          <input v-model="form[item.model]" type="checkbox" :disabled="!platformHas(item.platformKey)" />
        </label>
      </div>

      <div class="effective" v-if="form">
        <h3>{{ $t('methods.customerSees') }}</h3>
        <div class="chips">
          <ProviderBadge v-for="p in form.effectiveProviders" :key="p" :provider="p" />
          <span v-if="!form.effectiveProviders?.length" class="badge danger">{{ $t('methods.none') }}</span>
        </div>
      </div>

      <button class="btn" :disabled="saving" @click="save">
        {{ saving ? $t('methods.saving') : $t('methods.save') }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import ProviderBadge from '../../components/ProviderBadge.vue'
import { useProviders } from '../../composables/useProviders'

const { t } = useI18n()
const { refresh } = useProviders()
const form = ref(null)
const saving = ref(false)
const message = ref('')
const error = ref('')

const methodItems = computed(() => [
  { key: 'fib', platformKey: 'Fib', model: 'fibEnabled', title: 'FIB', hint: t('methods.fibHint') },
  { key: 'zain', platformKey: 'ZainCash', model: 'zainCashEnabled', title: 'ZainCash', hint: t('methods.zainHint') },
  { key: 'qi', platformKey: 'Qi', model: 'qiEnabled', title: 'QI Card', hint: t('methods.qiHint') },
  { key: 'super', platformKey: 'SuperQi', model: 'superQiEnabled', title: 'SuperQi', hint: t('methods.superQiHint') }
])

function platformHas(name) {
  return form.value?.platformEnabled?.includes(name)
}

async function load() {
  const { data } = await api.get('/api/merchant/payment-methods')
  form.value = data
  await refresh()
}

async function save() {
  if (!form.value) return
  saving.value = true
  error.value = ''
  message.value = ''
  try {
    const { data } = await api.put('/api/merchant/payment-methods', {
      fibEnabled: form.value.fibEnabled,
      zainCashEnabled: form.value.zainCashEnabled,
      qiEnabled: form.value.qiEnabled,
      superQiEnabled: form.value.superQiEnabled
    })
    form.value = data
    message.value = t('methods.saved')
  } catch (e) {
    error.value = e.response?.data?.message || t('methods.saveFail')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.hero { margin-bottom: 16px; }
.list { display: grid; gap: 10px; margin: 12px 0 18px; }
.item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  padding: 14px 16px;
  border: 1px solid var(--line);
  border-radius: 16px;
  background: #fff;
  cursor: pointer;
}
.item.off { opacity: .65; }
.item p { margin: 4px 0 0; }
.item input { width: 20px; height: 20px; }
.effective { margin-bottom: 16px; }
.effective h3 { margin: 0 0 8px; font-size: 1rem; }
.chips { display: flex; flex-wrap: wrap; gap: 10px; }
.ok-msg { color: #0f7a45; font-weight: 600; }
</style>
