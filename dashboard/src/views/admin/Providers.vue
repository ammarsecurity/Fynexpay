<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('providers.title') }}</h1>
        <p class="sub">{{ $t('providers.subtitle') }}</p>
      </div>
      <div class="head-actions" v-if="settings">
        <button class="btn" :disabled="saving" @click="save">{{ $t('providers.saveSettings') }}</button>
        <button class="btn accent" :disabled="saving" @click="loadDemo">{{ $t('providers.loadDemo') }}</button>
        <button class="btn secondary" :disabled="saving" @click="load">{{ $t('providers.reload') }}</button>
      </div>
    </div>

    <div class="card env-card" v-if="settings">
      <div class="env-main">
        <div>
          <h3>{{ $t('providers.activeEnv') }}</h3>
          <p class="muted">{{ $t('providers.activeEnvHint') }}</p>
        </div>
        <div class="env-toggle">
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Test' ? '' : 'secondary'"
            type="button"
            :disabled="saving"
            @click="switchEnv('Test')"
          >Test</button>
          <button
            class="btn"
            :class="settings.activeEnvironment === 'Production' ? 'accent' : 'secondary'"
            type="button"
            :disabled="saving"
            @click="switchEnv('Production')"
          >Production</button>
        </div>
      </div>
      <div class="env-meta">
        <span class="badge" :class="settings.activeEnvironment === 'Production' ? 'warn' : 'ok'">
          {{ settings.activeEnvironment }}
        </span>
        <label class="mock-check">
          <input type="checkbox" v-model="settings.useMockWhenMissingCredentials" />
          {{ $t('providers.useMock') }}
        </label>
      </div>
    </div>

    <p v-if="message" class="flash ok">{{ message }}</p>
    <p v-if="error" class="flash err">{{ error }}</p>

    <div class="card" v-if="settings">
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{{ $t('providers.colProvider') }}</th>
              <th>{{ $t('common.status') }}</th>
              <th>{{ $t('providers.priority') }}</th>
              <th>{{ $t('providers.testData') }}</th>
              <th>{{ $t('providers.prodData') }}</th>
              <th>{{ $t('providers.logo') }}</th>
              <th>{{ $t('common.actions') }}</th>
            </tr>
          </thead>
          <tbody>
            <template v-for="item in providerCards" :key="item.key">
              <tr :class="{ expanded: editingKey === item.key }">
                <td>
                  <div class="prov-cell">
                    <img class="prov-logo" :src="logoSrc(item.key)" :alt="displayTitle(item)" width="36" height="36" />
                    <div class="prov-meta">
                      <input
                        class="name-input"
                        type="text"
                        maxlength="64"
                        v-model="settings[item.key].displayName"
                        :placeholder="item.title"
                        :aria-label="$t('providers.displayName')"
                      />
                      <span class="muted mono">{{ item.key }}</span>
                    </div>
                  </div>
                </td>
                <td>
                  <label class="switch">
                    <input type="checkbox" v-model="settings[item.key].enabled" />
                    <span>{{ settings[item.key].enabled ? $t('common.enabled') : $t('common.disabled') }}</span>
                  </label>
                </td>
                <td>
                  <input
                    class="prio-input"
                    type="number"
                    min="1"
                    max="99"
                    v-model.number="settings[item.key].priority"
                  />
                </td>
                <td>
                  <span class="badge" :class="credsReady(item.key, 'Test') ? 'ok' : 'warn'">
                    {{ credsReady(item.key, 'Test') ? $t('providers.ready') : $t('providers.incomplete') }}
                  </span>
                </td>
                <td>
                  <span class="badge" :class="credsReady(item.key, 'Production') ? 'ok' : 'warn'">
                    {{ credsReady(item.key, 'Production') ? $t('providers.ready') : $t('providers.incomplete') }}
                  </span>
                </td>
                <td>
                  <div class="logo-actions">
                    <label class="btn secondary sm" :class="{ disabled: saving }">
                      {{ $t('providers.uploadLogo') }}
                      <input
                        type="file"
                        accept="image/png,image/jpeg,image/webp,image/svg+xml"
                        hidden
                        :disabled="saving"
                        @change="onLogo($event, item.key)"
                      />
                    </label>
                    <button
                      v-if="settings[item.key].logoUrl"
                      class="btn ghost sm"
                      type="button"
                      :disabled="saving"
                      @click="removeLogo(item.key)"
                    >{{ $t('providers.removeLogo') }}</button>
                  </div>
                </td>
                <td>
                  <button
                    class="btn"
                    :class="editingKey === item.key ? 'secondary' : ''"
                    type="button"
                    @click="toggleEdit(item.key)"
                  >
                    {{ editingKey === item.key ? $t('providers.closeEdit') : $t('providers.editCreds') }}
                  </button>
                </td>
              </tr>
              <tr v-if="editingKey === item.key" class="edit-row">
                <td colspan="7">
                  <div class="edit-panel">
                    <div class="tabs">
                      <button
                        class="tab"
                        type="button"
                        :class="{ active: editEnv[item.key] === 'Test' }"
                        @click="editEnv[item.key] = 'Test'"
                      >{{ $t('providers.testData') }}</button>
                      <button
                        class="tab"
                        type="button"
                        :class="{ active: editEnv[item.key] === 'Production' }"
                        @click="editEnv[item.key] = 'Production'"
                      >{{ $t('providers.prodData') }}</button>
                    </div>
                    <div class="creds-grid">
                      <div class="field" v-for="f in item.fields" :key="f.key">
                        <label>{{ f.label }}</label>
                        <input
                          :type="f.secret ? 'password' : 'text'"
                          class="ltr"
                          v-model="currentCreds(item.key)[f.key]"
                          :placeholder="f.placeholder || ''"
                          autocomplete="off"
                        />
                      </div>
                    </div>
                  </div>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
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
const editingKey = ref('')
const editEnv = reactive({
  fib: 'Test',
  zainCash: 'Test',
  qi: 'Test',
  superQi: 'Test',
  alqaseh: 'Test'
})

const providerCards = [
  {
    key: 'fib',
    title: 'FIB',
    required: ['authUrl', 'baseUrl', 'clientId', 'clientSecret'],
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
    required: ['baseUrl', 'merchantId', 'secret'],
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
    required: ['baseUrl', 'username', 'password', 'terminalId'],
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
    required: ['baseUrl', 'username', 'password', 'terminalId'],
    fields: [
      { key: 'baseUrl', label: 'Base URL (QI Gate)' },
      { key: 'username', label: 'Username' },
      { key: 'password', label: 'Password', secret: true },
      { key: 'terminalId', label: 'Terminal ID' }
    ]
  },
  {
    key: 'alqaseh',
    title: 'Alqaseh',
    required: ['baseUrl', 'authUrl', 'clientId', 'clientSecret'],
    fields: [
      { key: 'baseUrl', label: 'API Base URL' },
      { key: 'authUrl', label: 'Pay Page Base URL' },
      { key: 'clientId', label: 'Client ID' },
      { key: 'clientSecret', label: 'Client Secret', secret: true },
      { key: 'webhookSecret', label: 'Webhook Secret (optional)', secret: true }
    ]
  }
]

function currentCreds(key) {
  const bundle = settings.value[key]
  return editEnv[key] === 'Production' ? bundle.production : bundle.test
}

function credsReady(key, env) {
  const item = providerCards.find((p) => p.key === key)
  if (!item || !settings.value) return false
  const bundle = settings.value[key]
  const creds = env === 'Production' ? bundle.production : bundle.test
  return item.required.every((f) => !!String(creds?.[f] || '').trim())
}

function logoSrc(key) {
  const custom = settings.value?.[key]?.logoUrl
  if (custom) return mediaUrl(custom)
  return defaultLogoPath(key === 'zainCash' ? 'zaincash' : key === 'superQi' ? 'superqi' : key)
}

function displayTitle(item) {
  return settings.value?.[item.key]?.displayName?.trim() || item.title
}

function ensureDisplayNames(data) {
  for (const item of providerCards) {
    if (!data[item.key]) continue
    if (!String(data[item.key].displayName || '').trim()) {
      data[item.key].displayName = item.title
    }
  }
  return data
}

function toggleEdit(key) {
  editingKey.value = editingKey.value === key ? '' : key
}

async function load() {
  error.value = ''
  const { data } = await api.get('/api/admin/providers')
  settings.value = ensureDisplayNames(data)
  const env = data.activeEnvironment === 'Production' ? 'Production' : 'Test'
  editEnv.fib = env
  editEnv.zainCash = env
  editEnv.qi = env
  editEnv.superQi = env
  editEnv.alqaseh = env
}

async function save() {
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.put('/api/admin/providers', settings.value)
    settings.value = ensureDisplayNames(data)
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
    settings.value = ensureDisplayNames(data)
    editEnv.fib = environment
    editEnv.zainCash = environment
    editEnv.qi = environment
    editEnv.superQi = environment
    editEnv.alqaseh = environment
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
    settings.value = ensureDisplayNames(data)
    editEnv.fib = 'Test'
    editEnv.zainCash = 'Test'
    editEnv.qi = 'Test'
    editEnv.superQi = 'Test'
    editEnv.alqaseh = 'Test'
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
    settings.value = ensureDisplayNames(data)
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
    settings.value = ensureDisplayNames(data)
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
.page-head {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-end;
  margin-bottom: 16px;
  flex-wrap: wrap;
}
.page-head h1 { margin: 0 0 6px; }
.page-head .sub { margin: 0; color: var(--muted); }
.head-actions { display: flex; flex-wrap: wrap; gap: 8px; }

.env-card { margin-bottom: 16px; }
.env-main {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: center;
  flex-wrap: wrap;
}
.env-main h3 { margin: 0 0 4px; font-size: 1rem; }
.env-main .muted { margin: 0; font-size: 0.88rem; }
.env-toggle { display: flex; gap: 8px; }
.env-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 14px;
  align-items: center;
  margin-top: 14px;
  padding-top: 14px;
  border-top: 1px solid var(--line);
}
.mock-check {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  color: var(--muted);
  cursor: pointer;
}

.flash {
  margin: 0 0 12px;
  padding: 10px 12px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.9rem;
}
.flash.ok { background: var(--ok-soft); color: #047857; }
.flash.err { background: var(--danger-soft); color: #b91c1c; }

.table-wrap { overflow-x: auto; }
.prov-cell {
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 140px;
}
.prov-logo {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  border: 1px solid var(--line);
  object-fit: contain;
  background: #fff;
  flex-shrink: 0;
}
.prov-cell strong {
  display: block;
  color: var(--brand);
  font-size: 0.92rem;
}
.prov-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  min-width: 140px;
}
.name-input {
  width: 100%;
  min-width: 140px;
  max-width: 220px;
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 8px 10px;
  font-weight: 700;
  font-size: 0.92rem;
  color: var(--brand);
  background: #fff;
}
.name-input:focus {
  outline: none;
  border-color: var(--brand-secondary);
  box-shadow: 0 0 0 3px rgba(3, 24, 56, 0.12);
}
.prov-cell .muted {
  display: block;
  font-size: 0.75rem;
}

.switch {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  font-weight: 700;
  font-size: 0.85rem;
  white-space: nowrap;
}
.prio-input {
  width: 72px;
  border: 1px solid var(--line);
  border-radius: 10px;
  padding: 8px 10px;
  text-align: center;
  font-weight: 700;
}

.logo-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}
.btn.sm {
  padding: 7px 12px;
  font-size: 0.78rem;
  border-radius: 10px;
  box-shadow: none;
  margin: 0;
  cursor: pointer;
}
.btn.sm.disabled { opacity: 0.6; pointer-events: none; }

tr.expanded td { background: rgba(3, 24, 56, 0.03); }
.edit-row td {
  background: #f8fafc;
  padding: 0 !important;
  border-bottom: 1px solid var(--line);
}
.edit-panel {
  padding: 16px 18px 18px;
  border-top: 1px dashed var(--line);
}
.tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 14px;
}
.tab {
  border: 1px solid var(--line);
  background: #fff;
  border-radius: 999px;
  padding: 8px 14px;
  color: var(--muted);
  font-weight: 700;
  cursor: pointer;
}
.tab.active {
  background: var(--brand-soft);
  color: var(--brand);
  border-color: transparent;
}
.creds-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 12px;
}
.creds-grid .field { margin: 0; }
.creds-grid label {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--muted);
}
.creds-grid input {
  width: 100%;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 11px 12px;
  background: #fff;
}
.ltr { direction: ltr; text-align: left; }

@media (max-width: 900px) {
  .page-head { align-items: stretch; }
  .head-actions { width: 100%; }
}
</style>
