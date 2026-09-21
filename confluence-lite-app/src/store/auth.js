import { defineStore } from 'pinia'
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { userApi, workspaceApi } from '../api'

export const useAuthStore = defineStore('auth', () => {
  const token = ref(null)
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

  /** 从服务端刷新空间列表并同步到 localStorage（加入/退出/创建/被邀请后调用） */
  async function refreshSpaces() {
    try {
      const data = await workspaceApi.getMy()
      const normalized = (data || []).map(ws => ({
        ...ws,
        key: ws.key?.toUpperCase() || ''
      }))
      localStorage.setItem('auth_spaces', JSON.stringify(normalized))
      return normalized
    } catch {
      return null
    }
  }

  async function login(username, password) {
    try {
      const data = await userApi.login(username, password)
      if (data) {
        user.value = {
          id: data.user.id,
          name: data.user.displayName || data.user.username,
          username: data.user.username,
          avatarUrl: data.user.avatarUrl,
          isAdmin: !!data.user.isAdmin
        }
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

  async function logout() {
    try {
      await userApi.logout()
    } catch {
      // 忽略登出 API 错误
    }
    token.value = null
    user.value = null
    localStorage.removeItem('auth_user')
    localStorage.removeItem('auth_spaces')
    router.push('/login')
  }

  function setFromSetup(setupData, displayName, spaceKey) {
    user.value = {
      id: setupData.userId,
      name: displayName,
      isAdmin: true
    }
    localStorage.setItem('auth_user', JSON.stringify(user.value))
    localStorage.setItem('auth_spaces', JSON.stringify([
      { id: setupData.workspaceId, name: '', key: spaceKey?.toUpperCase() || '' }
    ]))
  }

  return { token, user, login, logout, setFromSetup, refreshSpaces }
})
