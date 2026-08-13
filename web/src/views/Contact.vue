<template>
  <SiteNav />

  <main class="contact-page" v-if="c">
    <div class="contact-shell">
      <aside class="contact-panel">
        <span class="eyebrow">{{ c.contactEyebrow }}</span>
        <h1>{{ c.contactTitle }}</h1>
        <p>{{ c.contactSubtitle }}</p>

        <ul class="contact-list">
          <li>
            <span class="label">{{ c.contactEmailLabel }}</span>
            <a class="value ltr" :href="'mailto:' + c.contactEmail">{{ c.contactEmail }}</a>
          </li>
          <li>
            <span class="label">{{ c.contactPhoneLabel }}</span>
            <a class="value ltr" :href="'tel:' + c.contactPhone">{{ c.contactPhone }}</a>
          </li>
          <li>
            <span class="label">{{ c.contactAddressLabel }}</span>
            <strong class="value">{{ c.contactAddress }}</strong>
          </li>
          <li>
            <span class="label">{{ c.contactHoursLabel }}</span>
            <strong class="value">{{ c.contactHours }}</strong>
          </li>
        </ul>
      </aside>

      <form class="contact-form" @submit.prevent="submit">
        <h2>{{ c.contactFormSubmit }}</h2>
        <p class="form-note">{{ c.contactFormNote }}</p>

        <div class="field">
          <label for="contact-name">{{ c.contactFormName }}</label>
          <input id="contact-name" v-model="form.name" autocomplete="name" required />
        </div>
        <div class="field">
          <label for="contact-email">{{ c.contactFormEmail }}</label>
          <input id="contact-email" v-model="form.email" type="email" autocomplete="email" dir="ltr" required />
        </div>
        <div class="field">
          <label for="contact-message">{{ c.contactFormMessage }}</label>
          <textarea id="contact-message" v-model="form.message" rows="5" required />
        </div>

        <p v-if="sent" class="ok" role="status">{{ c.contactFormSuccess }}</p>
        <button class="btn primary submit" type="submit">{{ c.contactFormSubmit }}</button>
      </form>
    </div>
  </main>

  <SiteFooter />
</template>

<script setup>
import { reactive, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import SiteFooter from '../components/SiteFooter.vue'
import { useLanding } from '../composables/useLanding'

const { c } = useLanding()
const sent = ref(false)
const form = reactive({ name: '', email: '', message: '' })

function submit() {
  const to = c.value?.contactEmail || 'hello@fynexpay.iq'
  const subject = encodeURIComponent(`Fynexpay contact — ${form.name}`)
  const body = encodeURIComponent(`${form.message}\n\n— ${form.name}\n${form.email}`)
  window.location.href = `mailto:${to}?subject=${subject}&body=${body}`
  sent.value = true
}
</script>

<style scoped>
.contact-page {
  min-height: calc(100vh - 64px);
  padding: 40px 24px 64px;
  background:
    radial-gradient(800px 380px at 100% 0%, rgba(3, 24, 56, 0.07), transparent 55%),
    var(--bg-soft);
}

.contact-shell {
  max-width: 960px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 0.95fr 1.05fr;
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 24px;
  overflow: hidden;
  box-shadow: var(--shadow-sm);
}

.contact-panel {
  padding: 36px 32px;
  background:
    radial-gradient(500px 280px at 0% 100%, rgba(255, 255, 255, 0.08), transparent 55%),
    linear-gradient(160deg, #021225 0%, #031838 55%, #0a2450 100%);
  color: #fff;
}
.contact-panel .eyebrow {
  display: inline-flex;
  padding: 6px 12px;
  border-radius: 999px;
  background: rgba(255, 255, 255, 0.12);
  font-size: 0.78rem;
  font-weight: 800;
}
.contact-panel h1 {
  margin: 16px 0 10px;
  font-size: clamp(1.7rem, 3vw, 2.2rem);
  line-height: 1.25;
  letter-spacing: -0.02em;
}
.contact-panel > p {
  margin: 0 0 28px;
  color: rgba(255, 255, 255, 0.78);
  line-height: 1.7;
  font-size: 0.98rem;
}

.contact-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: grid;
  gap: 14px;
}
.contact-list li {
  padding: 14px 0 0;
  border-top: 1px solid rgba(255, 255, 255, 0.14);
}
.contact-list .label {
  display: block;
  margin-bottom: 6px;
  color: rgba(255, 255, 255, 0.62);
  font-size: 0.78rem;
  font-weight: 800;
}
.contact-list .value {
  color: #fff;
  font-weight: 800;
  font-size: 1.02rem;
  line-height: 1.45;
  word-break: break-word;
}
.contact-list a.value:hover {
  text-decoration: underline;
}
.ltr {
  direction: ltr;
  text-align: start;
  unicode-bidi: isolate;
  display: inline-block;
}

.contact-form {
  padding: 36px 32px;
  display: flex;
  flex-direction: column;
}
.contact-form h2 {
  margin: 0 0 6px;
  font-size: 1.35rem;
  color: var(--brand);
}
.form-note {
  margin: 0 0 22px;
  color: var(--muted);
  font-size: 0.92rem;
  line-height: 1.6;
}
.field {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 14px;
}
.field label {
  color: var(--muted);
  font-weight: 700;
  font-size: 0.9rem;
}
.field input,
.field textarea {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  outline: none;
  font: inherit;
  color: var(--ink);
  background: #fff;
  resize: vertical;
}
.field textarea { min-height: 120px; }
.field input:focus,
.field textarea:focus {
  border-color: rgba(3, 24, 56, 0.5);
  box-shadow: 0 0 0 4px rgba(3, 24, 56, 0.1);
}
.ok {
  color: #047857;
  font-weight: 700;
  margin: 0 0 12px;
}
.submit {
  width: 100%;
  justify-content: center;
  height: 48px;
  border-radius: 12px;
  margin-top: auto;
}

@media (max-width: 860px) {
  .contact-page { padding: 20px 16px 48px; }
  .contact-shell {
    grid-template-columns: 1fr;
    border-radius: 20px;
  }
  .contact-panel,
  .contact-form { padding: 24px 20px; }
  .contact-panel > p { margin-bottom: 20px; }
}
</style>
