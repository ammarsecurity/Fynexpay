<template>
  <div>
    <div class="page-head">
      <div>
        <h1>{{ $t('profile.title') }}</h1>
        <p class="sub">{{ $t('profile.adminSub') }}</p>
      </div>
    </div>

    <div class="card">
      <form class="profile-form" @submit.prevent="save">
        <label class="field">
          <span>{{ $t('auth.fullName') }}</span>
          <input v-model="form.fullName" required autocomplete="name" />
        </label>
        <label class="field">
          <span>{{ $t('auth.email') }}</span>
          <input v-model="form.email" type="email" required dir="ltr" autocomplete="email" />
        </label>
        <label class="field">
          <span>{{ $t('auth.phone') }}</span>
          <input v-model="form.phone" dir="ltr" autocomplete="tel" :placeholder="$t('profile.phonePh')" />
        </label>
        <p v-if="error" class="error">{{ error }}</p>
        <p v-if="ok" class="ok-msg">{{ ok }}</p>
        <button class="btn" type="submit" :disabled="loading || saving">
          {{ saving ? $t('common.loading') : $t('profile.save') }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { onMounted, reactive, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { api } from '../../api'
import { useAuthStore } from '../../stores/auth'

const { t } = useI18n()
const auth = useAuthStore()

const form = reactive({
  fullName: '',
  email: '',
  phone: ''
})
const loading = ref(true)
const saving = ref(false)
const error = ref('')
const ok = ref('')

async function load() {
  loading.value = true
  error.value = ''
  try {
    const { data } = await api.get('/api/admin/profile')
    form.fullName = data.fullName || ''
    form.email = data.email || ''
    form.phone = data.phone || ''
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.loadFail')
  } finally {
    loading.value = false
  }
}

async function save() {
  saving.value = true
  error.value = ''
  ok.value = ''
  try {
    const { data } = await api.put('/api/admin/profile', {
      fullName: form.fullName,
      email: form.email,
      phone: form.phone || null
    })
    auth.applyAuth(data)
    ok.value = t('profile.saved')
  } catch (e) {
    error.value = e.response?.data?.message || t('profile.saveFail')
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.profile-form {
  display: grid;
  gap: 14px;
  max-width: 420px;
}
.field {
  display: grid;
  gap: 6px;
}
.field span {
  font-size: 0.85rem;
  color: var(--muted, #64748b);
}
.field input {
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  padding: 10px 12px;
  font: inherit;
  background: #fff;
}
.ok-msg {
  color: #15803d;
  margin: 0;
  font-size: 0.9rem;
}
</style>
