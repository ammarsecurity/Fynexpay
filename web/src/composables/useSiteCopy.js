import { computed, watch } from 'vue'
import { useLanding } from './useLanding'
import { siteCopy } from '../i18n/site'

export function useSiteCopy() {
  const { locale, setLocale, apiUrl, dashboardUrl, c, year } = useLanding()

  const pack = computed(() => siteCopy[locale.value === 'en' ? 'en' : 'ar'])

  function t(key) {
    return pack.value[key] || key
  }

  function useTitle(key) {
    watch(
      [locale, () => t(key)],
      () => { document.title = t(key) },
      { immediate: true }
    )
  }

  return { locale, setLocale, t, pack, apiUrl, dashboardUrl, c, year, useTitle }
}
