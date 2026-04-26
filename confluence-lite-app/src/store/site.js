import { ref } from 'vue'
import { siteInfoApi } from '../api'

const siteName = ref('Confluence Lite')
const siteLogo = ref('')
const allowRegistration = ref(true)
const installed = ref(false)
const loaded = ref(false)

export async function loadSiteInfo() {
  try {
    const data = await siteInfoApi.get()
    if (data) {
      siteName.value = data.siteName || 'Confluence Lite'
      siteLogo.value = data.siteLogo || ''
      allowRegistration.value = data.allowRegistration !== false
      installed.value = data.installed === true
    }
    loaded.value = true
    return { installed: installed.value, apiAvailable: true }
  } catch {
    loaded.value = true
    return { installed: false, apiAvailable: false }
  }
}

export function useSiteInfo() {
  return { siteName, siteLogo, allowRegistration, installed, loaded }
}
