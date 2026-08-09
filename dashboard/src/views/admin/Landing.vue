<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('landingCms.title') }}</h1>
        <p class="sub">{{ $t('landingCms.subtitle') }}</p>
      </div>
      <div class="row">
        <a class="btn secondary" :href="webUrl" target="_blank" rel="noopener">{{ $t('landingCms.openSite') }}</a>
        <a class="btn secondary" :href="webUrl + '/contact'" target="_blank" rel="noopener">{{ $t('landingCms.openContact') }}</a>
        <button class="btn secondary" type="button" :disabled="saving" @click="reset">{{ $t('landingCms.reset') }}</button>
        <button class="btn" type="button" :disabled="saving || !content" @click="save">{{ saving ? $t('common.loading') : $t('common.save') }}</button>
      </div>
    </div>

    <p v-if="message" class="badge ok" style="margin-bottom:12px">{{ message }}</p>
    <p v-if="error" class="error">{{ error }}</p>

    <div class="card" v-if="content">
      <div class="locale-tabs">
        <button type="button" class="tab" :class="{ active: localeTab === 'ar' }" @click="localeTab = 'ar'">العربية</button>
        <button type="button" class="tab" :class="{ active: localeTab === 'en' }" @click="localeTab = 'en'">English</button>
      </div>

      <div class="grid-2" v-if="active">
        <div class="field"><label>{{ $t('landingCms.navFeatures') }}</label><input v-model="active.navFeatures" /></div>
        <div class="field"><label>{{ $t('landingCms.navProviders') }}</label><input v-model="active.navProviders" /></div>
        <div class="field"><label>{{ $t('landingCms.navDevelopers') }}</label><input v-model="active.navDevelopers" /></div>
        <div class="field"><label>{{ $t('landingCms.navContact') }}</label><input v-model="active.navContact" /></div>
        <div class="field"><label>{{ $t('landingCms.login') }}</label><input v-model="active.login" /></div>
        <div class="field"><label>{{ $t('landingCms.startNow') }}</label><input v-model="active.startNow" /></div>
        <div class="field"><label>{{ $t('landingCms.badge') }}</label><input v-model="active.badge" /></div>
        <div class="field full"><label>{{ $t('landingCms.heroTitle') }}</label><input v-model="active.heroTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.heroSubtitle') }}</label><textarea v-model="active.heroSubtitle" rows="3" /></div>
        <div class="field"><label>{{ $t('landingCms.ctaMerchant') }}</label><input v-model="active.ctaMerchant" /></div>
        <div class="field"><label>{{ $t('landingCms.ctaDocs') }}</label><input v-model="active.ctaDocs" /></div>
      </div>
    </div>

    <div class="card" v-if="active">
      <h3>{{ $t('landingCms.featuresSection') }}</h3>
      <div class="grid-2">
        <div class="field"><label>{{ $t('landingCms.eyebrow') }}</label><input v-model="active.featuresEyebrow" /></div>
        <div class="field"><label>{{ $t('landingCms.sectionTitle') }}</label><input v-model="active.featuresTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.sectionSubtitle') }}</label><textarea v-model="active.featuresSubtitle" rows="2" /></div>
      </div>
      <div class="feature-editor" v-for="(f, idx) in active.features" :key="idx">
        <div class="field"><label>Icon</label><input v-model="f.icon" /></div>
        <div class="field"><label>{{ $t('landingCms.featureTitle') }}</label><input v-model="f.title" /></div>
        <div class="field full"><label>{{ $t('landingCms.featureBody') }}</label><textarea v-model="f.body" rows="2" /></div>
      </div>
    </div>

    <div class="card" v-if="active">
      <h3>{{ $t('landingCms.providersSection') }}</h3>
      <div class="grid-2">
        <div class="field"><label>{{ $t('landingCms.eyebrow') }}</label><input v-model="active.providersEyebrow" /></div>
        <div class="field"><label>{{ $t('landingCms.sectionTitle') }}</label><input v-model="active.providersTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.sectionSubtitle') }}</label><textarea v-model="active.providersSubtitle" rows="2" /></div>
        <div class="field full">
          <label>{{ $t('landingCms.providerPills') }}</label>
          <input v-model="pillsText" @change="syncPills" />
          <p class="muted" style="margin:6px 0 0;font-size:.82rem">{{ $t('landingCms.pillsHint') }}</p>
        </div>
      </div>
    </div>

    <div class="card" v-if="active">
      <h3>{{ $t('landingCms.apiSection') }}</h3>
      <div class="grid-2">
        <div class="field"><label>{{ $t('landingCms.eyebrow') }}</label><input v-model="active.apiEyebrow" /></div>
        <div class="field"><label>{{ $t('landingCms.sectionTitle') }}</label><input v-model="active.apiTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.sectionSubtitle') }}</label><textarea v-model="active.apiSubtitle" rows="2" /></div>
      </div>
    </div>

    <div class="card" v-if="active">
      <h3>{{ $t('landingCms.ctaSection') }}</h3>
      <div class="grid-2">
        <div class="field full"><label>{{ $t('landingCms.sectionTitle') }}</label><input v-model="active.ctaTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.sectionSubtitle') }}</label><textarea v-model="active.ctaSubtitle" rows="2" /></div>
        <div class="field"><label>{{ $t('landingCms.ctaRegister') }}</label><input v-model="active.ctaRegister" /></div>
        <div class="field"><label>{{ $t('landingCms.ctaContact') }}</label><input v-model="active.ctaContact" /></div>
        <div class="field full"><label>{{ $t('landingCms.footer') }}</label><input v-model="active.footer" /></div>
      </div>
    </div>

    <div class="card" v-if="active">
      <h3>{{ $t('landingCms.contactSection') }}</h3>
      <p class="muted" style="margin-top:0">{{ $t('landingCms.contactHint') }}</p>
      <div class="grid-2">
        <div class="field"><label>{{ $t('landingCms.eyebrow') }}</label><input v-model="active.contactEyebrow" /></div>
        <div class="field"><label>{{ $t('landingCms.sectionTitle') }}</label><input v-model="active.contactTitle" /></div>
        <div class="field full"><label>{{ $t('landingCms.sectionSubtitle') }}</label><textarea v-model="active.contactSubtitle" rows="2" /></div>
        <div class="field"><label>{{ $t('landingCms.contactEmailLabel') }}</label><input v-model="active.contactEmailLabel" /></div>
        <div class="field"><label>{{ $t('landingCms.contactEmail') }}</label><input v-model="active.contactEmail" /></div>
        <div class="field"><label>{{ $t('landingCms.contactPhoneLabel') }}</label><input v-model="active.contactPhoneLabel" /></div>
        <div class="field"><label>{{ $t('landingCms.contactPhone') }}</label><input v-model="active.contactPhone" /></div>
        <div class="field"><label>{{ $t('landingCms.contactAddressLabel') }}</label><input v-model="active.contactAddressLabel" /></div>
        <div class="field"><label>{{ $t('landingCms.contactAddress') }}</label><input v-model="active.contactAddress" /></div>
        <div class="field"><label>{{ $t('landingCms.contactHoursLabel') }}</label><input v-model="active.contactHoursLabel" /></div>
        <div class="field"><label>{{ $t('landingCms.contactHours') }}</label><input v-model="active.contactHours" /></div>
        <div class="field"><label>{{ $t('landingCms.contactFormName') }}</label><input v-model="active.contactFormName" /></div>
        <div class="field"><label>{{ $t('landingCms.contactFormEmail') }}</label><input v-model="active.contactFormEmail" /></div>
        <div class="field full"><label>{{ $t('landingCms.contactFormMessage') }}</label><input v-model="active.contactFormMessage" /></div>
        <div class="field"><label>{{ $t('landingCms.contactFormSubmit') }}</label><input v-model="active.contactFormSubmit" /></div>
        <div class="field"><label>{{ $t('landingCms.contactFormSuccess') }}</label><input v-model="active.contactFormSuccess" /></div>
        <div class="field full"><label>{{ $t('landingCms.contactFormNote') }}</label><textarea v-model="active.contactFormNote" rows="2" /></div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useDialog } from '../../composables/useDialog'

const { t } = useI18n()
const { confirm } = useDialog()
const webUrl = import.meta.env.VITE_WEB_URL || 'http://localhost:5174'
const content = ref(null)
const localeTab = ref('ar')
const pillsText = ref('')
const saving = ref(false)
const message = ref('')
const error = ref('')

const active = computed(() => {
  if (!content.value) return null
  return localeTab.value === 'en' ? content.value.en : content.value.ar
})

watch(active, (loc) => {
  if (!loc) return
  pillsText.value = (loc.providerPills || []).join(', ')
}, { immediate: true })

function syncPills() {
  if (!active.value) return
  active.value.providerPills = pillsText.value
    .split(',')
    .map((s) => s.trim())
    .filter(Boolean)
}

async function load() {
  error.value = ''
  const { data } = await api.get('/api/admin/landing')
  content.value = data
}

async function save() {
  if (!content.value) return
  syncPills()
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.put('/api/admin/landing', content.value)
    content.value = data
    message.value = t('landingCms.saved')
  } catch (e) {
    error.value = e.response?.data?.message || t('landingCms.saveFail')
  } finally {
    saving.value = false
  }
}

async function reset() {
  const ok = await confirm({
    variant: 'danger',
    title: t('dialog.dangerTitle'),
    message: t('landingCms.resetConfirm'),
    confirmText: t('landingCms.reset')
  })
  if (!ok) return
  saving.value = true
  message.value = ''
  error.value = ''
  try {
    const { data } = await api.post('/api/admin/landing/reset')
    content.value = data
    message.value = t('landingCms.resetDone')
  } catch (e) {
    error.value = e.response?.data?.message || t('landingCms.saveFail')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.locale-tabs { display: flex; gap: 8px; margin-bottom: 16px; }
.tab {
  border: 1px solid var(--line);
  background: #fff;
  border-radius: 999px;
  padding: 8px 16px;
  color: var(--muted);
  font-weight: 700;
}
.tab.active {
  background: var(--brand-soft);
  color: var(--brand-secondary);
  border-color: transparent;
}
.grid-2 {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px;
}
.field.full { grid-column: 1 / -1; }
.feature-editor {
  display: grid;
  grid-template-columns: 100px 1fr;
  gap: 12px;
  padding: 12px 0;
  border-top: 1px solid var(--line);
}
.feature-editor .full { grid-column: 1 / -1; }
@media (max-width: 800px) {
  .grid-2, .feature-editor { grid-template-columns: 1fr; }
}
</style>
