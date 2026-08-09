<template>
  <div>
    <h1>{{ $t('providers.title') }}</h1>
    <p class="muted">{{ $t('providers.subtitle') }}</p>

    <div class="card" v-if="settings">
      <div class="row" style="justify-content:space-between;margin-bottom:8px">
        <div>
          <h3 style="margin:0">{{ $t('providers.activeEnv') }}</h3>
          <p class="muted" style="margin:6px 0 0">{{ $t('providers.activeEnvHint') }}</p>
        </div>
        <div class="env-toggle">
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Test' ? '' : 'secondary'"
            @click="switchEnv('Test')"
          >Test</button>
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Production' ? 'accent' : 'secondary'"
            @click="switchEnv('Production')"
          >Production</button>
        </div>
      </div>
      <p>
        {{ $t('providers.current') }}:
        <span class="badge" :class="settings.activeEnvironment === 'Production' ? 'warn' : 'ok'">
          {{ settings.activeEnvironment }}
        </span>
      </p>
      <label class="row" style="gap:8px;margin-top:12px">
        <input type="checkbox" v-model="settings.useMockWhenMissingCredentials" />
        {{ $t('providers.useMock') }}
      </label>
    </div>

    <p v-if="message" class="badge ok" style="margin-bottom:12px">{{ message }}</p>
    <p v-if="error" class="error">{{ error }}</p>

    <div v-if="settings" class="provider-list">
      <div class="card" v-for="item in providerCards" :key="item.key">
        <div class="row" style="justify-content:space-between">
          <div class="row" style="gap:12px">
            <img
              class="provider-logo-preview"
              :src="logoSrc(item.key)"
              :alt="item.title"
            />
            <div>
              <h3 style="margin:0">{{ item.title }}</h3>
              <p class="muted" style="margin:6px 0 0">{{ $t('providers.priority') }} {{ settings[item.key].priority }}</p>
            </div>
          </div>
          <label class="row" style="gap:8px">
            <input type="checkbox" v-model="settings[item.key].enabled" />
            {{ $t('common.enabled') }}
          </label>
        </div>

        <div class="provider-logo-box">
          <div style="flex:1">
            <strong>{{ $t('providers.logo') }}</strong>
            <p class="muted" style="margin:4px 0 0;font-size:.85rem">{{ $t('providers.logoHint') }}</p>
          </div>
          <label class="btn secondary" style="cursor:pointer;margin:0">
            {{ $t('providers.uploadLogo') }}
            <input type="file" accept="image/png,image/jpeg,image/webp,image/svg+xml" hidden @change="onLogo($event, item.key)" />
          </label>
          <button
            v-if="settings[item.key].logoUrl"
            class="btn ghost"
            type="button"
            :disabled="saving"
            @click="removeLogo(item.key)"
          >{{ $t('providers.removeLogo') }}</button>
        </div>

        <div class="field" style="margin-top:14px;max-width:160px">
          <label>{{ $t('providers.priority') }}</label>
          <input class="input-compact" type="number" v-model.number="settings[item.key].priority" />
        </div>

        <div class="tabs">
          <button
            class="tab"
            :class="{ active: editEnv[item.key] === 'Test' }"
            @click="editEnv[item.key] = 'Test'"
          >{{ $t('providers.testData') }}</button>
          <button
            class="tab"
            :class="{ active: editEnv[item.key] === 'Production' }"
            @click="editEnv[item.key] = 'Production'"
          >{{ $t('providers.prodData') }}</button>
        </div>

        <div class="creds-grid">
          <div class="field" v-for="f in item.fields" :key="f.key">
            <label>{{ f.label }}</label>
            <input
              :type="f.secret ? 'password' : 'text'"
              v-model="currentCreds(item.key)[f.key]"
              :placeholder="f.placeholder || ''"
              autocomplete="off"
            />
          </div>
        </div>
      </div>
    </div>

    <div class="row" style="margin-top:8px" v-if="settings">
      <button class="btn" :disabled="saving" @click="save">{{ $t('providers.saveSettings') }}</button>
      <button class="btn accent" :disabled="saving" @click="loadDemo">{{ $t('providers.loadDemo') }}</button>
      <button class="btn secondary" :disabled="saving" @click="load">{{ $t('providers.reload') }}</button>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { defaultLogoPath, mediaUrl, useProviders } from '../../composables/useProviders'

const { t } = useI18n()
const { refresh } = useProviders()
const settings = ref(null)
const saving = ref(false)
const message = ref('')
const error = ref('')
const editEnv = reactive({
  fib: 'Test',
  zainCash: 'Test',
  qi: 'Test',
  superQi: 'Test'
})

const providerCards = [
  {
    key: 'fib',
    title: 'FIB',
    fields: [
      { key: 'authUrl', label: 'Auth URL' },
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'clientId', label: 'Client ID' },
      { key: 'clientSecret', label: 'Client Secret', secret: true }
    ]
  },
  {
    key: 'zainCash',
    title: 'ZainCash',
    fields: [
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'authUrl', label: 'Auth URL' },
      { key: 'merchantId', label: 'Merchant ID' },
      { key: 'msisdn', label: 'MSISDN' },
      { key: 'secret', label: 'Secret', secret: true },
      { key: 'clientId', label: 'Client ID' },
      { key: 'clientSecret', label: 'Client Secret', secret: true }
    ]
  },
  {
    key: 'qi',
    title: 'QI Gate',
    fields: [
      { key: 'baseUrl', label: 'Base URL' },
      { key: 'username', label: 'Username' },
      { key: 'password', label: 'Password', secret: true },
      { key: 'terminalId', label: 'Terminal ID' }
    ]
  },
  {
    key: 'superQi',
    title: 'SuperQi',
    fields: [
      { key: 'baseUrl', label: 'Base URL (QI Gate)' },
      { key: 'username', label: 'Username' },
      { key: 'password', label: 'Password', secret: true },
      { key: 'terminalId', label: 'Terminal ID' }
    ]
  }
]

function currentCreds(key) {
  const bundle = settings.value[key]
  return editEnv[key] === 'Production' ? bundle.production : bundle.test
}

function logoSrc(key) {
  const custom = settings.value?.[key]?.logoUrl
  if (custom) return mediaUrl(custom)
  return defaultLogoPath(key === 'zainCash' ? 'zaincash' : key === 'superQi' ? 'superqi' : key)
}

async function load() {
  error.value = ''
  const { data } = await api.get('/api/admin/providers')
  settings.value = data
  editEnv.fib = data.activeEnvironment === 'Production' ? 'Production' : 'Test'
  editEnv.zainCash = editEnv.fib
  editEnv.qi = editEnv.fib
  editEnv.superQi = editEnv.fib
}

async function save() {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.put('/api/admin/providers', settings.value)
    settings.value = data
    message.value = t('providers.saved')
    await refresh()
  } catch (e) {
    error.value = e.response?.data?.message || t('providers.saveFail')
  } finally {
    saving.value = false
  }
}

async function switchEnv(environment) {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    await api.put('/api/admin/providers', settings.value)
    const { data } = await api.post('/api/admin/providers/environment', { environment })
    settings.value = data
    editEnv.fib = environment
    editEnv.zainCash = environment
    editEnv.qi = environment
    editEnv.superQi = environment
    message.value = t('providers.switched', { env: environment })
  } catch (e) {
    error.value = e.response?.data?.message || t('providers.switchFail')
  } finally {
    saving.value = false
  }
}

async function loadDemo() {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.post('/api/admin/providers/load-demo')
    settings.value = data
    editEnv.fib = 'Test'
    editEnv.zainCash = 'Test'
    editEnv.qi = 'Test'
    editEnv.superQi = 'Test'
    message.value = t('providers.demoLoaded')
    await refresh()
  } catch (e) {
    error.value = e.response?.data?.message || t('providers.demoFail')
  } finally {
    saving.value = false
  }
}

async function onLogo(event, key) {
  const file = event.target.files?.[0]
  event.target.value = ''
  if (!file) return
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const form = new FormData()
    form.append('file', file)
    const { data } = await api.post(`/api/admin/providers/${key}/logo`, form)
    settings.value = data
    message.value = t('providers.logoUploaded')
    await refresh()
  } catch (e) {
    error.value = e.response?.data?.message || t('providers.logoFail')
  } finally {
    saving.value = false
  }
}

async function removeLogo(key) {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.delete(`/api/admin/providers/${key}/logo`)
    settings.value = data
    await refresh()
  } catch (e) {
    error.value = e.response?.data?.message || t('providers.logoFail')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.env-toggle { display: flex; gap: 8px; }
.tabs {
  display: flex;
  gap: 8px;
  margin: 16px 0 12px;
}
.tab {
  border: 1px solid var(--line);
  background: transparent;
  border-radius: 999px;
  padding: 8px 14px;
  color: var(--muted);
}
.tab.active {
  background: var(--brand-soft);
  color: var(--brand-dark);
  border-color: transparent;
  font-weight: 600;
}
.creds-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
}
.provider-list { display: grid; gap: 16px; }
</style>
