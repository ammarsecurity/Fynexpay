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
          v-for="item in visibleMethods"
          :key="item.key"
          class="item"
        >
          <div class="row" style="gap:12px">
            <ProviderBadge :provider="item.platformKey" :show-name="false" size="lg" />
            <div>
              <p class="muted">{{ item.hint }}</p>
            </div>
          </div>
          <input v-model="form[item.model]" type="checkbox" />
        </label>
        <p v-if="!visibleMethods.length" class="muted empty">{{ $t('methods.platformNone') }}</p>
      </div>

      <div class="effective" v-if="form">
        <h3>{{ $t('methods.customerSees') }}</h3>
        <div class="chips">
          <ProviderBadge v-for="p in form.effectiveProviders" :key="p" :provider="p" :show-name="false" />
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

const allMethods = computed(() => [
  { key: 'fib', platformKey: 'Fib', model: 'fibEnabled', title: 'FIB', hint: t('methods.fibHint') },
  { key: 'zain', platformKey: 'ZainCash', model: 'zainCashEnabled', title: 'ZainCash', hint: t('methods.zainHint') },
  { key: 'qi', platformKey: 'Qi', model: 'qiEnabled', title: 'QI Card', hint: t('methods.qiHint') },
  { key: 'super', platformKey: 'SuperQi', model: 'superQiEnabled', title: 'SuperQi', hint: t('methods.superQiHint') },
  { key: 'alqaseh', platformKey: 'Alqaseh', model: 'alqasehEnabled', title: 'Alqaseh', hint: t('methods.alqasehHint') }
])

/** Only providers enabled by platform admin — disabled ones are hidden, not grayed out. */
const visibleMethods = computed(() => {
  const enabled = form.value?.platformEnabled || []
  return allMethods.value.filter((item) => enabled.includes(item.platformKey))
})

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
      superQiEnabled: form.value.superQiEnabled,
      alqasehEnabled: form.value.alqasehEnabled
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
.item p { margin: 4px 0 0; }
.item input { width: 20px; height: 20px; }
.empty { margin: 8px 0 0; }
.effective { margin-bottom: 16px; }
.effective h3 { margin: 0 0 8px; font-size: 1rem; }
.chips { display: flex; flex-wrap: wrap; gap: 10px; }
.ok-msg { color: #0f7a45; font-weight: 600; }
</style>
