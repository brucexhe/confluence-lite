import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { userApi } from '../api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(localStorage.getItem('auth_token') || null)
  const getUserFromStorage = () => {
    try {
      return JSON.parse(localStorage.getItem('auth_user') || 'null')
    } catch {
      localStorage.removeItem('auth_user')
      return null
    }
  }
  const user = ref(getUserFromStorage())
  const router = useRouter()

  async function login(username, password) {
    try {
      const data = await userApi.login(username, password)
      if (data) {
        token.value = data.token
        user.value = {
          id: data.user.id,
          name: data.user.displayName || data.user.username,
          username: data.user.username,
          role: data.user.isAdmin ? 'admin' : 'user'
        }
        localStorage.setItem('auth_token', token.value)
        localStorage.setItem('auth_user', JSON.stringify(user.value))
        if (data.workspaces) {
          // 将所有空间 key 转换为大写
          const normalizedWorkspaces = data.workspaces.map(ws => ({
            ...ws,
            key: ws.key?.toUpperCase() || ''
          }))
          localStorage.setItem('auth_spaces', JSON.stringify(normalizedWorkspaces))
        }
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
    localStorage.removeItem('auth_spaces')
    router.push('/login')
  }

  function setFromSetup(setupData, displayName, spaceKey) {
    token.value = setupData.token
    user.value = {
      id: setupData.userId,
      name: displayName,
      role: 'admin'
    }
    localStorage.setItem('auth_token', token.value)
    localStorage.setItem('auth_user', JSON.stringify(user.value))
    localStorage.setItem('auth_spaces', JSON.stringify([
      { id: setupData.workspaceId, name: '', key: spaceKey?.toUpperCase() || '' }
    ]))
  }

  return { token, user, login, logout, setFromSetup }
})
