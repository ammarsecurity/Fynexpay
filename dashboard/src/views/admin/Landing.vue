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
        <a class="btn secondary" :href="webUrl + '/terms'" target="_blank" rel="noopener">{{ $t('landingCms.legalTerms') }}</a>
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
        <div class="field"><label>{{ $t('landingCms.heroBefore') }}</label><input v-model="active.heroBefore" /></div>
        <div class="field"><label>{{ $t('landingCms.heroAccent') }}</label><input v-model="active.heroAccent" /></div>
        <div class="field full"><label>{{ $t('landingCms.heroAfter') }}</label><input v-model="active.heroAfter" /></div>
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
        <div class="field full"><label>{{ $t('landingCms.footerDisclaimer') }}</label><textarea v-model="active.footerDisclaimer" rows="3" /></div>
        <div class="field full"><label>{{ $t('landingCms.footerLegalNote') }}</label><textarea v-model="active.footerLegalNote" rows="3" /></div>
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

    <div class="card" v-if="active?.legal">
      <h3>{{ $t('landingCms.legalSection') }}</h3>
      <p class="muted" style="margin-top:0">{{ $t('landingCms.legalHint') }}</p>
      <div class="locale-tabs">
        <button type="button" class="tab" :class="{ active: legalTab === 'terms' }" @click="legalTab = 'terms'">{{ $t('landingCms.legalTerms') }}</button>
        <button type="button" class="tab" :class="{ active: legalTab === 'privacy' }" @click="legalTab = 'privacy'">{{ $t('landingCms.legalPrivacy') }}</button>
        <button type="button" class="tab" :class="{ active: legalTab === 'prohibited' }" @click="legalTab = 'prohibited'">{{ $t('landingCms.legalProhibited') }}</button>
        <button type="button" class="tab" :class="{ active: legalTab === 'brand' }" @click="legalTab = 'brand'">{{ $t('landingCms.legalBrand') }}</button>
        <button type="button" class="tab" :class="{ active: legalTab === 'company' }" @click="legalTab = 'company'">{{ $t('landingCms.legalCompany') }}</button>
      </div>

      <template v-if="legalTab !== 'company' && legalPage">
        <div class="grid-2">
          <div class="field"><label>{{ $t('landingCms.legalNav') }}</label><input v-model="legalPage.nav" /></div>
          <div class="field"><label>{{ $t('landingCms.legalUpdated') }}</label><input v-model="legalPage.updated" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalTitle') }}</label><input v-model="legalPage.title" /></div>
          <div class="field"><label>{{ $t('landingCms.legalToc') }}</label><input v-model="legalPage.tocTitle" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalIntro') }}</label><textarea v-model="legalPage.intro" rows="3" /></div>
        </div>
        <div class="feature-editor" v-for="(s, idx) in legalPage.sections" :key="idx">
          <div class="field full"><label>{{ $t('landingCms.legalHeading') }}</label><input v-model="s.heading" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalBody') }}</label><textarea v-model="s.body" rows="3" /></div>
          <div class="field full">
            <label>{{ $t('landingCms.legalItems') }}</label>
            <textarea :value="itemsText(s)" rows="4" @input="setItems(s, $event)" />
          </div>
          <div class="field full">
            <button class="btn secondary" type="button" @click="removeSection(legalPage.sections, idx)">{{ $t('landingCms.legalRemoveSection') }}</button>
          </div>
        </div>
        <button class="btn secondary" type="button" @click="addSection(legalPage.sections)">{{ $t('landingCms.legalAddSection') }}</button>
      </template>

      <template v-else-if="companyPage">
        <div class="grid-2">
          <div class="field"><label>{{ $t('landingCms.legalNav') }}</label><input v-model="companyPage.nav" /></div>
          <div class="field"><label>{{ $t('landingCms.legalUpdated') }}</label><input v-model="companyPage.updated" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalTitle') }}</label><input v-model="companyPage.title" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalIntro') }}</label><textarea v-model="companyPage.intro" rows="3" /></div>
          <div class="field"><label>{{ $t('landingCms.legalRegistration') }}</label><input v-model="companyPage.registrationTitle" /></div>
          <div class="field"><label>{{ $t('landingCms.legalIraqTitle') }}</label><input v-model="companyPage.iraqTitle" /></div>
          <div class="field"><label>{{ $t('landingCms.legalIraqNameLabel') }}</label><input v-model="companyPage.iraqLegalNameLabel" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalIraqName') }}</label><input v-model="companyPage.iraqLegalName" /></div>
          <div class="field"><label>{{ $t('landingCms.legalIraqRegLabel') }}</label><input v-model="companyPage.iraqRegistryLabel" /></div>
          <div class="field"><label>{{ $t('landingCms.legalIraqReg') }}</label><input v-model="companyPage.iraqRegistry" /></div>
          <div class="field"><label>{{ $t('landingCms.legalIraqHqLabel') }}</label><input v-model="companyPage.iraqHqLabel" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalIraqHq') }}</label><input v-model="companyPage.iraqHq" /></div>
          <div class="field"><label>{{ $t('landingCms.legalCertsTitle') }}</label><input v-model="companyPage.certsTitle" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalCertsBody') }}</label><textarea v-model="companyPage.certsBody" rows="2" /></div>
          <div class="field"><label>{{ $t('landingCms.legalContactTitle') }}</label><input v-model="companyPage.contactTitle" /></div>
          <div class="field"><label>{{ $t('landingCms.contactEmail') }}</label><input v-model="companyPage.contactEmail" /></div>
          <div class="field"><label>{{ $t('landingCms.contactPhone') }}</label><input v-model="companyPage.contactPhone" /></div>
          <div class="field"><label>{{ $t('landingCms.legalWebsite') }}</label><input v-model="companyPage.contactWebsite" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalDisclaimer') }}</label><textarea v-model="companyPage.disclaimer" rows="3" /></div>
        </div>
        <h4 style="margin:18px 0 8px">{{ $t('landingCms.legalCerts') }}</h4>
        <div class="feature-editor" v-for="(s, idx) in companyPage.certs" :key="idx">
          <div class="field full"><label>{{ $t('landingCms.legalHeading') }}</label><input v-model="s.heading" /></div>
          <div class="field full"><label>{{ $t('landingCms.legalBody') }}</label><textarea v-model="s.body" rows="3" /></div>
          <div class="field full">
            <button class="btn secondary" type="button" @click="removeSection(companyPage.certs, idx)">{{ $t('landingCms.legalRemoveSection') }}</button>
          </div>
        </div>
        <button class="btn secondary" type="button" @click="addSection(companyPage.certs)">{{ $t('landingCms.legalAddSection') }}</button>
      </template>
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
const legalTab = ref('terms')
const pillsText = ref('')
const saving = ref(false)
const message = ref('')
const error = ref('')

const active = computed(() => {
  if (!content.value) return null
  return localeTab.value === 'en' ? content.value.en : content.value.ar
})

const legalPage = computed(() => {
  const legal = active.value?.legal
  if (!legal || legalTab.value === 'company') return null
  return legal[legalTab.value] || null
})

const companyPage = computed(() => active.value?.legal?.company || null)

function itemsText(section) {
  return (section.items || []).join('\n')
}
function setItems(section, e) {
  section.items = String(e.target.value || '').split('\n')
}
function addSection(list) {
  if (!list) return
  list.push({ heading: '', body: '', items: [] })
}
function removeSection(list, idx) {
  if (!list) return
  list.splice(idx, 1)
}
function normalizeLegal(bundle) {
  if (!bundle) return
  for (const key of ['terms', 'privacy', 'prohibited', 'brand']) {
    const page = bundle[key]
    if (!page) continue
    page.sections = page.sections || []
    for (const s of page.sections) {
      s.items = (s.items || []).map((x) => String(x).trim()).filter(Boolean)
    }
  }
  if (bundle.company) bundle.company.certs = bundle.company.certs || []
}

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
  normalizeLegal(content.value.ar?.legal)
  normalizeLegal(content.value.en?.legal)
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
.locale-tabs { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 16px; }
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
