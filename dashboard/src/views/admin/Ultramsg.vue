<template>
  <div class="otp-page">
    <header class="otp-hero">
      <div class="otp-hero-text">
        <p class="otp-kicker">{{ $t('otp.kicker') }}</p>
        <h1>{{ $t('otp.title') }}</h1>
        <p class="muted">{{ $t('otp.subtitle') }}</p>
      </div>
      <div class="otp-hero-actions">
        <div class="hero-pills">
          <span class="pill" :class="settings?.enabled ? 'on' : 'off'">
            <i :class="settings?.enabled ? 'bi bi-check-circle-fill' : 'bi bi-x-circle-fill'" aria-hidden="true"></i>
            {{ settings?.enabled ? $t('otp.masterOn') : $t('otp.masterOff') }}
          </span>
          <span class="pill soft">{{ channelLabel }}</span>
        </div>
        <button class="btn save-btn" type="button" :disabled="saving || (tab === 'notifications' ? !notifSettings : !settings)" @click="save">
          <i class="bi bi-check2" aria-hidden="true"></i>
          {{ saving ? $t('common.loading') : $t('common.save') }}
        </button>
      </div>
    </header>

    <p v-if="message" class="flash ok">{{ message }}</p>
    <p v-if="error" class="flash err">{{ error }}</p>

    <div v-if="settings || notifSettings" class="otp-layout">
      <aside class="otp-side">
        <nav class="side-nav" aria-label="OTP sections">
          <button type="button" class="side-item" :class="{ active: tab === 'general' }" @click="tab = 'general'">
            <span class="side-ico"><i class="bi bi-sliders" aria-hidden="true"></i></span>
            <span class="side-copy">
              <strong>{{ $t('otp.tabGeneral') }}</strong>
              <small>{{ $t('otp.tabGeneralHint') }}</small>
            </span>
          </button>
          <button type="button" class="side-item" :class="{ active: tab === 'whatsapp' }" @click="tab = 'whatsapp'">
            <span class="side-ico wa"><i class="bi bi-whatsapp" aria-hidden="true"></i></span>
            <span class="side-copy">
              <strong>{{ $t('otp.tabWhatsapp') }}</strong>
              <small :class="isReady ? 'ok-text' : ''">{{ statusTitle }}</small>
            </span>
          </button>
          <button type="button" class="side-item" :class="{ active: tab === 'email' }" @click="tab = 'email'">
            <span class="side-ico mail"><i class="bi bi-envelope" aria-hidden="true"></i></span>
            <span class="side-copy">
              <strong>{{ $t('otp.tabEmail') }}</strong>
              <small>{{ settings.emailEnabled ? $t('otp.emailOn') : $t('otp.emailOff') }}</small>
            </span>
          </button>
          <button type="button" class="side-item" :class="{ active: tab === 'notifications' }" @click="tab = 'notifications'">
            <span class="side-ico bell"><i class="bi bi-bell" aria-hidden="true"></i></span>
            <span class="side-copy">
              <strong>{{ $t('notifications.title') }}</strong>
              <small>{{ $t('notifications.settingsTitle') }}</small>
            </span>
          </button>
        </nav>
      </aside>

      <section class="otp-main">
        <!-- General -->
        <div v-if="tab === 'general'" class="stack">
          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('otp.generalTitle') }}</h3>
                <p class="muted">{{ $t('otp.generalHint') }}</p>
              </div>
            </div>

            <label class="switch-row">
              <input type="checkbox" v-model="settings.enabled" />
              <div>
                <strong>{{ $t('otp.masterSwitch') }}</strong>
                <span>{{ $t('otp.masterSwitchHint') }}</span>
              </div>
            </label>

            <div class="section-label">{{ $t('otp.channelPick') }}</div>
            <div class="channel-grid">
              <button type="button" class="channel-card" :class="{ active: settings.channel === 'WhatsApp' }" @click="settings.channel = 'WhatsApp'">
                <i class="bi bi-whatsapp" aria-hidden="true"></i>
                <strong>{{ $t('otp.channelWa') }}</strong>
                <span>{{ $t('otp.channelWaHint') }}</span>
              </button>
              <button type="button" class="channel-card" :class="{ active: settings.channel === 'Email' }" @click="settings.channel = 'Email'">
                <i class="bi bi-envelope" aria-hidden="true"></i>
                <strong>{{ $t('otp.channelEmail') }}</strong>
                <span>{{ $t('otp.channelEmailHint') }}</span>
              </button>
              <button type="button" class="channel-card" :class="{ active: settings.channel === 'Both' }" @click="settings.channel = 'Both'">
                <i class="bi bi-layers" aria-hidden="true"></i>
                <strong>{{ $t('otp.channelBoth') }}</strong>
                <span>{{ $t('otp.channelBothHint') }}</span>
              </button>
            </div>
          </article>

          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('otp.policyTitle') }}</h3>
                <p class="muted">{{ $t('otp.policyHint') }}</p>
              </div>
            </div>
            <div class="policy-list">
              <label class="policy-item">
                <input type="checkbox" v-model="settings.requireMerchantRegisterOtp" />
                <div>
                  <strong>{{ $t('ultramsg.requireRegister') }}</strong>
                </div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="settings.requireCheckoutOtp" />
                <div>
                  <strong>{{ $t('ultramsg.requireCheckout') }}</strong>
                </div>
              </label>
            </div>
          </article>
        </div>

        <!-- WhatsApp -->
        <div v-else-if="tab === 'whatsapp'" class="wa-grid">
          <div class="stack">
            <article class="panel">
              <div class="status-hero" :class="statusTone">
                <div class="status-hero-main">
                  <span class="status-dot" aria-hidden="true"></span>
                  <div>
                    <strong>{{ statusTitle }}</strong>
                    <p>{{ statusDetail }}</p>
                  </div>
                </div>
                <button class="btn secondary" type="button" :disabled="loadingStatus" @click="refreshStatus()">
                  <i class="bi bi-arrow-clockwise" aria-hidden="true"></i>
                  {{ loadingStatus ? $t('common.loading') : $t('ultramsg.refreshStatus') }}
                </button>
              </div>

              <label class="switch-row">
                <input type="checkbox" v-model="settings.whatsAppEnabled" />
                <div>
                  <strong>{{ $t('otp.waEnable') }}</strong>
                  <span>{{ $t('otp.waEnableHint') }}</span>
                </div>
              </label>

              <div class="section-label">{{ $t('otp.waCreds') }}</div>
              <div class="creds-grid">
                <div class="field">
                  <label>Instance ID</label>
                  <input v-model="settings.instanceId" class="ltr" placeholder="instance12345" />
                </div>
                <div class="field">
                  <label>Token</label>
                  <input v-model="settings.token" type="password" class="ltr" placeholder="••••••••" />
                </div>
                <div class="field">
                  <label>{{ $t('ultramsg.countryCode') }}</label>
                  <input v-model="settings.defaultCountryCode" class="ltr" placeholder="964" />
                </div>
              </div>
            </article>

            <article class="panel">
              <div class="panel-head">
                <div>
                  <h3>{{ $t('otp.waTemplates') }}</h3>
                  <p class="muted">{{ $t('ultramsg.templateHint') }}</p>
                </div>
              </div>
              <div class="field">
                <label>{{ $t('ultramsg.registerMsg') }}</label>
                <textarea v-model="settings.merchantRegisterMessage" rows="3"></textarea>
              </div>
              <div class="field">
                <label>{{ $t('ultramsg.checkoutMsg') }}</label>
                <textarea v-model="settings.checkoutMessage" rows="3"></textarea>
              </div>
            </article>

            <article class="panel test-panel">
              <div class="panel-head">
                <div>
                  <h3>{{ $t('ultramsg.testTitle') }}</h3>
                </div>
              </div>
              <div class="test-row">
                <div class="field grow">
                  <label>{{ $t('auth.phone') }}</label>
                  <input v-model="testPhone" class="ltr" placeholder="07xxxxxxxxx" />
                </div>
                <button class="btn" type="button" :disabled="testing" @click="sendWaTest">
                  <i class="bi bi-send" aria-hidden="true"></i>
                  {{ testing ? $t('common.loading') : $t('ultramsg.sendTest') }}
                </button>
              </div>
            </article>
          </div>

          <article class="panel qr-panel">
            <template v-if="isReady">
              <div class="ready-panel">
                <div class="ready-icon"><i class="bi bi-check-lg" aria-hidden="true"></i></div>
                <h3>{{ $t('ultramsg.readyTitle') }}</h3>
                <p class="muted">{{ $t('ultramsg.readyBody') }}</p>
              </div>
            </template>
            <template v-else>
              <div class="panel-head">
                <div>
                  <h3>{{ $t('ultramsg.qrTitle') }}</h3>
                  <p class="muted">{{ $t('ultramsg.qrHint') }}</p>
                </div>
              </div>
              <div class="qr-frame">
                <img v-if="qrUrl" :src="qrUrl" alt="QR" />
                <div v-else class="qr-empty">
                  <i class="bi bi-qr-code" aria-hidden="true"></i>
                  <span>{{ $t('ultramsg.qrEmpty') }}</span>
                </div>
              </div>
              <button class="btn secondary qr-btn" type="button" :disabled="loadingQr" @click="loadQr">
                <i class="bi bi-qr-code-scan" aria-hidden="true"></i>
                {{ loadingQr ? $t('common.loading') : $t('ultramsg.loadQr') }}
              </button>
            </template>
          </article>
        </div>

        <!-- Email -->
        <div v-else-if="tab === 'email'" class="stack">
          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('otp.emailTitle') }}</h3>
                <p class="muted">{{ $t('otp.emailHint') }}</p>
              </div>
            </div>

            <label class="switch-row">
              <input type="checkbox" v-model="settings.emailEnabled" />
              <div>
                <strong>{{ $t('otp.emailEnable') }}</strong>
                <span>{{ $t('otp.emailEnableHint') }}</span>
              </div>
            </label>

            <div class="section-label">SMTP</div>
            <div class="creds-grid">
              <div class="field">
                <label>SMTP Host</label>
                <input v-model="settings.smtpHost" class="ltr" placeholder="smtp.gmail.com" />
              </div>
              <div class="field">
                <label>Port</label>
                <input v-model.number="settings.smtpPort" type="number" class="ltr" />
              </div>
              <div class="field">
                <label>Username</label>
                <input v-model="settings.smtpUsername" class="ltr" />
              </div>
              <div class="field">
                <label>Password</label>
                <input v-model="settings.smtpPassword" type="password" class="ltr" />
              </div>
              <div class="field">
                <label>From Email</label>
                <input v-model="settings.fromEmail" class="ltr" placeholder="noreply@fynexpay.iq" />
              </div>
              <div class="field">
                <label>From Name</label>
                <input v-model="settings.fromName" placeholder="Fynexpay" />
              </div>
            </div>

            <label class="policy-item smtp-ssl">
              <input type="checkbox" v-model="settings.smtpUseSsl" />
              <div><strong>SSL / TLS</strong></div>
            </label>
          </article>

          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('otp.emailTemplates') }}</h3>
                <p class="muted">{{ $t('ultramsg.templateHint') }}</p>
              </div>
            </div>

            <details class="tpl" open>
              <summary>
                <i class="bi bi-person-plus" aria-hidden="true"></i>
                {{ $t('otp.emailRegisterSubject') }}
              </summary>
              <div class="tpl-body">
                <div class="field">
                  <label>{{ $t('otp.emailSubject') }}</label>
                  <input v-model="settings.emailRegisterSubject" />
                </div>
                <div class="field">
                  <label>{{ $t('otp.emailHtmlBody') }}</label>
                  <textarea v-model="settings.emailRegisterBody" rows="5" class="code-area ltr"></textarea>
                </div>
              </div>
            </details>

            <details class="tpl">
              <summary>
                <i class="bi bi-cart-check" aria-hidden="true"></i>
                {{ $t('otp.emailCheckoutSubject') }}
              </summary>
              <div class="tpl-body">
                <div class="field">
                  <label>{{ $t('otp.emailSubject') }}</label>
                  <input v-model="settings.emailCheckoutSubject" />
                </div>
                <div class="field">
                  <label>{{ $t('otp.emailHtmlBody') }}</label>
                  <textarea v-model="settings.emailCheckoutBody" rows="5" class="code-area ltr"></textarea>
                </div>
              </div>
            </details>
          </article>

          <article class="panel test-panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('otp.emailTestTitle') }}</h3>
              </div>
            </div>
            <div class="test-row">
              <div class="field grow">
                <label>{{ $t('auth.email') }}</label>
                <input v-model="testEmail" class="ltr" placeholder="you@email.com" />
              </div>
              <button class="btn" type="button" :disabled="testingEmail" @click="sendEmailTest">
                <i class="bi bi-envelope-check" aria-hidden="true"></i>
                {{ testingEmail ? $t('common.loading') : $t('otp.sendEmailTest') }}
              </button>
            </div>
          </article>
        </div>

        <!-- Notifications -->
        <div v-else-if="tab === 'notifications' && notifSettings" class="stack">
          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('notifications.settingsTitle') }}</h3>
                <p class="muted">{{ $t('notifications.settingsHint') }}</p>
              </div>
            </div>

            <div class="policy-list" style="margin-top:16px">
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.inAppEnabled" />
                <div><strong>{{ $t('notifications.inApp') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.emailEnabled" />
                <div><strong>{{ $t('notifications.email') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.whatsAppEnabled" />
                <div><strong>{{ $t('notifications.whatsapp') }}</strong></div>
              </label>
            </div>
            <p class="muted" style="margin:14px 0 0;font-size:.85rem">{{ $t('notifications.channelNote') }}</p>
          </article>

          <article class="panel">
            <div class="panel-head">
              <div>
                <h3>{{ $t('notifications.eventsTitle') }}</h3>
              </div>
            </div>
            <div class="policy-list" style="margin-top:16px">
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyAdminMerchantRegistered" />
                <div><strong>{{ $t('notifications.eventMerchantReg') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyMerchantStatusChanged" />
                <div><strong>{{ $t('notifications.eventMerchantStatus') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyAdminPlatformSubmitted" />
                <div><strong>{{ $t('notifications.eventPlatformSubmit') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyMerchantPlatformReviewed" />
                <div><strong>{{ $t('notifications.eventPlatformReview') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyAdminPayoutRequested" />
                <div><strong>{{ $t('notifications.eventPayoutRequest') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyMerchantPayoutReviewed" />
                <div><strong>{{ $t('notifications.eventPayoutReview') }}</strong></div>
              </label>
              <label class="policy-item">
                <input type="checkbox" v-model="notifSettings.notifyMerchantPaymentPaid" />
                <div><strong>{{ $t('notifications.eventPaymentPaid') }}</strong></div>
              </label>
            </div>
          </article>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'

const { t } = useI18n()
const tab = ref('general')
const settings = ref(null)
const notifSettings = ref(null)
const status = ref(null)
const qrUrl = ref('')
const error = ref('')
const message = ref('')
const saving = ref(false)
const loadingStatus = ref(false)
const loadingQr = ref(false)
const testing = ref(false)
const testingEmail = ref(false)
const testPhone = ref('')
const testEmail = ref('')
let qrObjectUrl = ''
let pollTimer = null

const isReady = computed(() => !!status.value?.isReady)
const channelLabel = computed(() => {
  const c = settings.value?.channel
  if (c === 'Email') return t('otp.channelEmail')
  if (c === 'Both') return t('otp.channelBoth')
  return t('otp.channelWa')
})

const statusTone = computed(() => {
  if (!status.value) return 'tone-muted'
  if (status.value.isReady) return 'tone-ok'
  const s = (status.value.accountStatus || '').toLowerCase()
  if (s === 'error' || s.includes('disconnect') || s === 'unconfigured') return 'tone-bad'
  return 'tone-warn'
})

const statusTitle = computed(() => {
  if (!status.value) return t('ultramsg.statusUnknown')
  if (status.value.isReady) return t('ultramsg.statusConnected')
  return status.value.accountStatus || t('ultramsg.statusUnknown')
})

const statusDetail = computed(() => {
  if (!status.value) return t('ultramsg.statusHintUnknown')
  if (status.value.error) return status.value.error
  if (status.value.isReady) return t('ultramsg.statusHintConnected')
  return t('ultramsg.statusHintAction')
})

async function load() {
  const [{ data }, notif] = await Promise.all([
    api.get('/api/admin/ultramsg'),
    api.get('/api/admin/notification-settings')
  ])
  settings.value = data
  notifSettings.value = notif.data
  await refreshStatus()
  startPolling()
}

function startPolling() {
  stopPolling()
  pollTimer = setInterval(() => {
    if (!status.value?.isReady) refreshStatus(true)
  }, 8000)
}
function stopPolling() {
  if (pollTimer) clearInterval(pollTimer)
  pollTimer = null
}

async function save() {
  error.value = ''
  message.value = ''
  saving.value = true
  try {
    if (tab.value === 'notifications') {
      const { data } = await api.put('/api/admin/notification-settings', notifSettings.value)
      notifSettings.value = data
      message.value = t('notifications.saved')
    } else {
      const { data } = await api.put('/api/admin/ultramsg', settings.value)
      settings.value = data
      message.value = t('otp.saved')
      await refreshStatus()
    }
  } catch (e) {
    error.value = e.response?.data?.message || (tab.value === 'notifications' ? t('notifications.saveFail') : t('otp.saveFail'))
  } finally {
    saving.value = false
  }
}

async function refreshStatus(silent = false) {
  if (!silent) loadingStatus.value = true
  try {
    const { data } = await api.get('/api/admin/ultramsg/status')
    status.value = data
    if (data.isReady) stopPolling()
  } catch (e) {
    status.value = { accountStatus: 'error', isReady: false, error: e.response?.data?.message }
  } finally {
    loadingStatus.value = false
  }
}

async function loadQr() {
  error.value = ''
  loadingQr.value = true
  try {
    const { data } = await api.get('/api/admin/ultramsg/qr', { responseType: 'blob' })
    if (qrObjectUrl) URL.revokeObjectURL(qrObjectUrl)
    qrObjectUrl = URL.createObjectURL(data)
    qrUrl.value = qrObjectUrl
  } catch (e) {
    error.value = t('ultramsg.qrFail')
  } finally {
    loadingQr.value = false
  }
}

async function sendWaTest() {
  error.value = ''
  message.value = ''
  testing.value = true
  try {
    const { data } = await api.post('/api/admin/ultramsg/test', { phone: testPhone.value })
    message.value = data.message
  } catch (e) {
    error.value = e.response?.data?.message || t('ultramsg.testFail')
  } finally {
    testing.value = false
  }
}

async function sendEmailTest() {
  error.value = ''
  message.value = ''
  testingEmail.value = true
  try {
    const { data } = await api.post('/api/admin/ultramsg/test-email', { to: testEmail.value || undefined })
    message.value = data.message
  } catch (e) {
    error.value = e.response?.data?.message || t('otp.emailTestFail')
  } finally {
    testingEmail.value = false
  }
}

onMounted(load)
onUnmounted(() => {
  stopPolling()
  if (qrObjectUrl) URL.revokeObjectURL(qrObjectUrl)
})
</script>

<style scoped>
.otp-page { width: 100%; }

.otp-hero {
  display: flex;
  justify-content: space-between;
  gap: 24px;
  align-items: flex-end;
  margin-bottom: 20px;
  padding: 0 0 20px;
  border-bottom: 1px solid var(--line);
}
.otp-kicker {
  margin: 0 0 6px;
  font-size: 12px;
  font-weight: 800;
  color: var(--brand-secondary);
  letter-spacing: 0.06em;
}
.otp-hero h1 {
  margin: 0 0 8px;
  font-size: 28px;
  font-weight: 800;
  letter-spacing: -0.02em;
}
.otp-hero .muted { margin: 0; max-width: 54ch; line-height: 1.7; }
.otp-hero-actions {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 12px;
  flex-shrink: 0;
}
.hero-pills { display: flex; gap: 8px; flex-wrap: wrap; justify-content: flex-end; }
.pill {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  border-radius: 999px;
  padding: 7px 12px;
  font-weight: 700;
  font-size: 12px;
  background: var(--brand-soft);
  color: var(--brand-secondary);
}
.pill i { font-size: 13px; }
.pill.on { background: var(--ok-soft); color: #047857; }
.pill.off { background: #fee2e2; color: #b91c1c; }
.pill.soft { background: #fff; border: 1px solid var(--line); color: var(--muted); }
.save-btn { min-width: 120px; justify-content: center; gap: 8px; }

.flash {
  margin: 0 0 16px;
  padding: 12px 14px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.9rem;
}
.flash.ok { background: var(--ok-soft); color: #047857; }
.flash.err { background: var(--danger-soft); color: #b91c1c; }

.otp-layout {
  display: grid;
  grid-template-columns: 240px minmax(0, 1fr);
  gap: 20px;
  align-items: start;
}
.otp-side {
  position: sticky;
  top: 88px;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 18px;
  padding: 10px;
  box-shadow: var(--shadow-sm);
}
.side-nav { display: flex; flex-direction: column; gap: 4px; }
.side-item {
  display: flex;
  gap: 12px;
  align-items: center;
  text-align: start;
  width: 100%;
  border: 0;
  background: transparent;
  border-radius: 14px;
  padding: 12px;
  cursor: pointer;
  font: inherit;
  color: inherit;
  transition: background 0.15s ease;
}
.side-item:hover { background: var(--bg); }
.side-item.active {
  background: var(--brand-soft);
  box-shadow: inset 3px 0 0 var(--brand-secondary);
}
html[dir="ltr"] .side-item.active { box-shadow: inset -3px 0 0 var(--brand-secondary); }
.side-copy { min-width: 0; }
.side-item strong { display: block; font-size: 14px; font-weight: 800; }
.side-item small {
  display: block;
  color: var(--muted);
  font-size: 12px;
  font-weight: 600;
  margin-top: 2px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}
.side-item small.ok-text { color: #047857; }
.side-ico {
  width: 40px;
  height: 40px;
  border-radius: 12px;
  display: grid;
  place-items: center;
  background: #eef2f7;
  color: var(--brand);
  font-size: 1.15rem;
  flex-shrink: 0;
}
.side-ico.wa { background: rgba(37, 211, 102, 0.14); color: #128c7e; }
.side-ico.mail { background: rgba(3, 24, 56, 0.12); color: var(--brand-secondary); }
.side-ico.bell { background: rgba(14, 165, 233, 0.12); color: #0284c7; }

.stack { display: grid; gap: 16px; }
.panel {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 18px;
  padding: 22px 24px;
  box-shadow: var(--shadow-sm);
}
.panel-head { margin-bottom: 4px; }
.panel-head h3 {
  margin: 0 0 6px;
  font-size: 17px;
  font-weight: 800;
}
.panel-head .muted { margin: 0; font-size: 0.9rem; line-height: 1.6; }
.section-label {
  margin: 18px 0 10px;
  font-size: 12px;
  font-weight: 800;
  color: var(--muted);
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.switch-row {
  display: flex;
  gap: 12px;
  align-items: flex-start;
  padding: 14px 16px;
  border-radius: 14px;
  background: linear-gradient(180deg, #f8fafc, #f1f5f9);
  border: 1px solid var(--line);
  margin: 16px 0 0;
  cursor: pointer;
}
.switch-row input { margin-top: 3px; width: 18px; height: 18px; accent-color: var(--brand-secondary); }
.switch-row strong { display: block; font-size: 14px; }
.switch-row span { color: var(--muted); font-size: 13px; line-height: 1.5; }

.channel-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 12px;
}
.channel-card {
  border: 1px solid var(--line);
  background: #fff;
  border-radius: 16px;
  padding: 16px;
  text-align: start;
  cursor: pointer;
  font: inherit;
  color: inherit;
  display: grid;
  gap: 6px;
  transition: border-color 0.15s ease, box-shadow 0.15s ease, background 0.15s ease;
}
.channel-card i {
  font-size: 1.25rem;
  color: var(--brand-secondary);
  margin-bottom: 4px;
}
.channel-card strong { display: block; font-size: 14px; }
.channel-card span { color: var(--muted); font-size: 12px; line-height: 1.55; }
.channel-card.active {
  border-color: rgba(3, 24, 56, 0.45);
  box-shadow: 0 0 0 3px rgba(3, 24, 56, 0.1);
  background: rgba(3, 24, 56, 0.04);
}

.policy-list { display: grid; gap: 10px; margin-top: 16px; }
.policy-item {
  display: flex;
  gap: 12px;
  align-items: center;
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid var(--line);
  background: #fff;
  cursor: pointer;
}
.policy-item input { width: 18px; height: 18px; accent-color: var(--brand-secondary); }
.policy-item strong { font-size: 14px; }
.smtp-ssl { margin-top: 14px; }

.creds-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 12px 14px;
}
.field { margin-bottom: 0; }
.field + .field { margin-top: 12px; }
.creds-grid .field + .field { margin-top: 0; }
.panel .field { margin-bottom: 12px; }
.panel .field:last-child { margin-bottom: 0; }

textarea {
  width: 100%;
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  font: inherit;
  resize: vertical;
  background: #fbfcfe;
}
textarea:focus, .panel input:focus {
  outline: none;
  border-color: rgba(3, 24, 56, 0.45);
  box-shadow: 0 0 0 3px rgba(3, 24, 56, 0.1);
  background: #fff;
}
.code-area {
  font-family: ui-monospace, SFMono-Regular, Menlo, Consolas, monospace;
  font-size: 12px;
  line-height: 1.55;
  min-height: 120px;
}
.ltr { direction: ltr; text-align: left; }

.tpl {
  border: 1px solid var(--line);
  border-radius: 14px;
  background: #fbfcfe;
  margin-top: 12px;
  overflow: hidden;
}
.tpl summary {
  list-style: none;
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 14px 16px;
  font-weight: 800;
  font-size: 14px;
  cursor: pointer;
  user-select: none;
}
.tpl summary::-webkit-details-marker { display: none; }
.tpl summary i { color: var(--brand-secondary); font-size: 1.1rem; }
.tpl[open] summary { border-bottom: 1px solid var(--line); background: #fff; }
.tpl-body { padding: 16px; }

.wa-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.2fr) minmax(260px, 0.8fr);
  gap: 16px;
  align-items: start;
}
.status-hero {
  display: flex;
  justify-content: space-between;
  gap: 12px;
  align-items: center;
  padding: 14px 16px;
  border-radius: 14px;
  border: 1px solid var(--line);
  margin-bottom: 4px;
}
.status-hero .btn { gap: 6px; }
.status-hero-main { display: flex; gap: 12px; min-width: 0; }
.status-hero p {
  margin: 4px 0 0;
  color: var(--muted);
  font-size: 0.85rem;
  font-weight: 600;
  line-height: 1.45;
}
.status-dot {
  width: 12px;
  height: 12px;
  border-radius: 50%;
  margin-top: 5px;
  flex-shrink: 0;
  background: #94a3b8;
  box-shadow: 0 0 0 4px rgba(148, 163, 184, 0.2);
}
.tone-ok { background: rgba(16, 185, 129, 0.08); border-color: rgba(16, 185, 129, 0.25); }
.tone-ok .status-dot { background: #10b981; box-shadow: 0 0 0 4px rgba(16, 185, 129, 0.2); }
.tone-warn { background: rgba(245, 158, 11, 0.08); border-color: rgba(245, 158, 11, 0.25); }
.tone-warn .status-dot { background: #f59e0b; box-shadow: 0 0 0 4px rgba(245, 158, 11, 0.2); }
.tone-bad { background: rgba(239, 68, 68, 0.08); border-color: rgba(239, 68, 68, 0.25); }
.tone-bad .status-dot { background: #ef4444; box-shadow: 0 0 0 4px rgba(239, 68, 68, 0.2); }
.tone-muted { background: var(--bg); }

.qr-panel { text-align: center; }
.qr-frame {
  aspect-ratio: 1;
  max-width: 240px;
  margin: 16px auto 0;
  border-radius: 18px;
  border: 1px dashed var(--line);
  display: grid;
  place-items: center;
  background: #fff;
}
.qr-frame img { width: 100%; height: 100%; object-fit: contain; padding: 14px; }
.qr-empty {
  color: var(--muted);
  font-weight: 600;
  padding: 24px;
  display: grid;
  gap: 8px;
  place-items: center;
  font-size: 0.9rem;
}
.qr-empty i { font-size: 2rem; opacity: 0.5; }
.qr-btn { margin-top: 14px; width: 100%; justify-content: center; gap: 8px; }
.ready-panel { text-align: center; padding: 32px 12px; }
.ready-icon {
  width: 64px;
  height: 64px;
  margin: 0 auto 14px;
  border-radius: 50%;
  display: grid;
  place-items: center;
  background: rgba(16, 185, 129, 0.12);
  color: #059669;
  font-size: 1.6rem;
}
.ready-panel h3 { margin: 0 0 8px; }
.ready-panel .muted { margin: 0; }

.test-panel .panel-head { margin-bottom: 12px; }
.test-row {
  display: flex;
  gap: 12px;
  align-items: flex-end;
}
.test-row .grow { flex: 1; min-width: 0; }
.test-row .btn {
  flex-shrink: 0;
  min-height: 48px;
  gap: 8px;
  white-space: nowrap;
}

@media (max-width: 1024px) {
  .otp-layout, .wa-grid, .channel-grid { grid-template-columns: 1fr; }
  .otp-side { position: static; }
  .side-nav {
    display: grid;
    grid-template-columns: repeat(3, 1fr);
    gap: 6px;
  }
  .side-item { flex-direction: column; text-align: center; padding: 10px 8px; gap: 8px; }
  .side-item.active { box-shadow: inset 0 -3px 0 var(--brand-secondary); }
  html[dir="ltr"] .side-item.active { box-shadow: inset 0 -3px 0 var(--brand-secondary); }
  .side-item small { display: none; }
  .otp-hero { flex-direction: column; align-items: stretch; }
  .otp-hero-actions { align-items: stretch; }
  .hero-pills { justify-content: flex-start; }
  .creds-grid { grid-template-columns: 1fr; }
  .test-row { flex-direction: column; align-items: stretch; }
}
</style>
