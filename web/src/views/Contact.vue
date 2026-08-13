<template>
  <SiteNav />

  <main class="page-shell contact-page" v-if="c">
    <div class="page-hero">
      <span class="eyebrow">{{ c.contactEyebrow }}</span>
      <h1>{{ c.contactTitle }}</h1>
      <p>{{ c.contactSubtitle }}</p>
    </div>

    <div class="contact-grid">
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
        <button class="btn hero-start submit" type="submit">{{ c.contactFormSubmit }}</button>
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
  padding-bottom: 72px;
}
.contact-grid {
  max-width: 980px;
  margin: 0 auto;
  display: grid;
  grid-template-columns: 0.9fr 1.1fr;
  gap: 28px 40px;
  align-items: start;
}
.contact-list {
  list-style: none;
  margin: 0;
  padding: 8px 0 0;
  display: grid;
  gap: 18px;
}
.contact-list .label {
  display: block;
  margin-bottom: 6px;
  color: var(--muted);
  font-size: 0.8rem;
  font-weight: 800;
}
.contact-list .value {
  color: var(--ink);
  font-weight: 800;
  font-size: 1.05rem;
  line-height: 1.45;
  word-break: break-word;
}
.contact-list a.value:hover { color: var(--accent); }
.ltr {
  direction: ltr;
  text-align: start;
  unicode-bidi: isolate;
  display: inline-block;
}
.contact-form {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 20px;
  padding: 28px;
}
.contact-form h2 {
  margin: 0 0 6px;
  font-size: 1.25rem;
}
.form-note {
  margin: 0 0 20px;
  color: var(--muted);
  font-size: 0.92rem;
  line-height: 1.65;
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
  border-color: rgba(29, 78, 216, 0.45);
  box-shadow: 0 0 0 4px rgba(29, 78, 216, 0.1);
}
.ok {
  color: #047857;
  font-weight: 700;
  margin: 0 0 12px;
}
.submit {
  width: 100%;
  justify-content: center;
  margin-top: 4px;
}
@media (max-width: 860px) {
  .contact-grid { grid-template-columns: 1fr; gap: 28px; }
  .contact-form { padding: 22px 18px; }
}
</style>
