<template>
  <SiteNav />

  <section class="section contact-hero" v-if="c">
    <div class="section-head">
      <span class="eyebrow">{{ c.contactEyebrow }}</span>
      <h1>{{ c.contactTitle }}</h1>
      <p>{{ c.contactSubtitle }}</p>
    </div>

    <div class="contact-grid">
      <div class="contact-info">
        <div class="info-card">
          <span class="label">{{ c.contactEmailLabel }}</span>
          <a :href="'mailto:' + c.contactEmail">{{ c.contactEmail }}</a>
        </div>
        <div class="info-card">
          <span class="label">{{ c.contactPhoneLabel }}</span>
          <a :href="'tel:' + c.contactPhone">{{ c.contactPhone }}</a>
        </div>
        <div class="info-card">
          <span class="label">{{ c.contactAddressLabel }}</span>
          <strong>{{ c.contactAddress }}</strong>
        </div>
        <div class="info-card">
          <span class="label">{{ c.contactHoursLabel }}</span>
          <strong>{{ c.contactHours }}</strong>
        </div>
      </div>

      <form class="contact-form" @submit.prevent="submit">
        <div class="field">
          <label>{{ c.contactFormName }}</label>
          <input v-model="form.name" required />
        </div>
        <div class="field">
          <label>{{ c.contactFormEmail }}</label>
          <input v-model="form.email" type="email" required />
        </div>
        <div class="field">
          <label>{{ c.contactFormMessage }}</label>
          <textarea v-model="form.message" rows="5" required />
        </div>
        <p class="muted note">{{ c.contactFormNote }}</p>
        <p v-if="sent" class="ok">{{ c.contactFormSuccess }}</p>
        <button class="btn primary" type="submit">{{ c.contactFormSubmit }}</button>
      </form>
    </div>
  </section>

  <footer class="site-footer" v-if="c">© {{ year }} Fynexpay — {{ c.footer }}</footer>
</template>

<script setup>
import { reactive, ref } from 'vue'
import SiteNav from '../components/SiteNav.vue'
import { useLanding } from '../composables/useLanding'

const { c, year } = useLanding()
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
.contact-hero { padding-top: 48px; }
.contact-hero h1 { font-size: clamp(2rem, 4vw, 2.8rem); margin: 8px 0 12px; }
.contact-grid {
  max-width: 1100px;
  margin: 32px auto 0;
  padding: 0 24px;
  display: grid;
  grid-template-columns: 0.9fr 1.1fr;
  gap: 24px;
}
.contact-info { display: grid; gap: 12px; }
.info-card {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 18px;
  padding: 18px 20px;
  box-shadow: var(--shadow-sm);
}
.info-card .label {
  display: block;
  color: var(--muted);
  font-size: 0.82rem;
  font-weight: 700;
  margin-bottom: 6px;
}
.info-card a, .info-card strong {
  color: var(--brand);
  font-weight: 700;
  font-size: 1.05rem;
}
.contact-form {
  background: #fff;
  border: 1px solid var(--line);
  border-radius: 22px;
  padding: 24px;
  box-shadow: var(--shadow-sm);
}
.field { display: flex; flex-direction: column; gap: 6px; margin-bottom: 14px; }
.field label { color: var(--muted); font-weight: 700; font-size: 0.9rem; }
.field input, .field textarea {
  border: 1px solid var(--line);
  border-radius: 12px;
  padding: 12px 14px;
  outline: none;
  font: inherit;
  color: var(--ink);
  background: #fff;
}
.field input:focus, .field textarea:focus {
  border-color: rgba(108, 60, 236, 0.55);
  box-shadow: 0 0 0 4px rgba(108, 60, 236, 0.12);
}
.note { margin: 0 0 14px; font-size: 0.88rem; }
.ok { color: #047857; font-weight: 700; margin: 0 0 12px; }
.contact-form .btn { width: 100%; justify-content: center; }
@media (max-width: 860px) {
  .contact-grid { grid-template-columns: 1fr; }
}
</style>
