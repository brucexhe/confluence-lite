import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const API_BASE = ''

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('auth_token') || null)
  const user = ref(JSON.parse(localStorage.getItem('auth_user') || 'null'))
  const router = useRouter()

  async function login(username, password) {
    try {
      const res = await fetch(`${API_BASE}/api/user/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password })
      })
      const data = await res.json()
      if (data.success && data.data) {
        token.value = data.data.token
        user.value = {
          id: data.data.user.id,
          name: data.data.user.displayName || data.data.user.username,
          username: data.data.user.username,
          role: data.data.user.isAdmin ? 'admin' : 'user'
        }
        localStorage.setItem('auth_token', token.value)
        localStorage.setItem('auth_user', JSON.stringify(user.value))
        router.push('/')
        return true
      }
      return false
    } catch {
      return false
    }
  }

  function logout() {
    token.value = null
    user.value = null
    localStorage.removeItem('auth_token')
    localStorage.removeItem('auth_user')
    router.push('/login')
  }

  function setFromSetup(setupData, displayName) {
    token.value = setupData.token
    user.value = {
      id: setupData.userId,
      name: displayName,
      role: 'admin'
    }
    localStorage.setItem('auth_token', token.value)
    localStorage.setItem('auth_user', JSON.stringify(user.value))
  }

  return { token, user, login, logout, setFromSetup }
})
