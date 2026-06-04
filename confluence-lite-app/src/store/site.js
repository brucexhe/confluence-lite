import { ref } from 'vue'
import { siteInfoApi } from '../api'

const siteName = ref('Confluence Lite')
const siteLogo = ref('')
const allowRegistration = ref(true)
const installed = ref(false)
const loaded = ref(false)
const lang = ref('en')

const authConfig = ref({
  passwordEnabled: true,
  emailLoginEnabled: false,
  oidcEnabled: false,
  oidcProviderName: '',
  ldapEnabled: false
})

export async function loadSiteInfo() {
  try {
    const data = await siteInfoApi.get()
    if (data) {
      siteName.value = data.siteName || 'Confluence Lite'
      siteLogo.value = data.siteLogo || ''
      lang.value = data.lang || 'en'
      allowRegistration.value = data.allowRegistration !== false
      installed.value = data.installed === true
      if (data.passwordEnabled !== undefined) {
        authConfig.value = {
          passwordEnabled: data.passwordEnabled !== false,
          emailLoginEnabled: data.emailLoginEnabled === true,
          oidcEnabled: data.oidcEnabled === true,
          oidcProviderName: data.oidcProviderName || '',
          ldapEnabled: data.ldapEnabled === true
        }
      }
    }
    loaded.value = true
    return { installed: installed.value, apiAvailable: true }
  } catch {
    loaded.value = true
    return { installed: false, apiAvailable: false }
  }
}

export function useSiteInfo() {
  return { siteName, siteLogo, lang, allowRegistration, installed, loaded, authConfig }
}
