import { createI18n } from 'vue-i18n'
import ar from './ar'
import en from './en'

const saved = localStorage.getItem('fx_locale')
const locale = saved === 'en' || saved === 'ar' ? saved : 'ar'

export const i18n = createI18n({
  legacy: false,
  locale,
  fallbackLocale: 'en',
  messages: { ar, en }
})

export function applyDocumentLocale(loc) {
  const html = document.documentElement
  html.lang = loc
  html.dir = loc === 'ar' ? 'rtl' : 'ltr'
  html.classList.toggle('locale-en', loc === 'en')
  html.classList.toggle('locale-ar', loc === 'ar')
}

export function setLocale(loc) {
  if (loc !== 'ar' && loc !== 'en') return
  i18n.global.locale.value = loc
  localStorage.setItem('fx_locale', loc)
  applyDocumentLocale(loc)
}

applyDocumentLocale(locale)
