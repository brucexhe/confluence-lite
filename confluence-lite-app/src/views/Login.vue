<template>
  <div class="login-container">
    <div class="login-left">
      <div class="brand">
        <img v-if="siteLogo" class="logo" :src="siteLogo" alt="" />
        <div v-else class="logo"></div>
        <h1>{{ siteName }}</h1>
      </div>
      <p class="tagline">Your modern team workspace.</p>
    </div>

    <div class="login-right">
      <div class="login-card glass-panel">
        <h2>Welcome Back</h2>
        <p class="subtitle">Please enter your credentials to access your workspace.</p>

        <!-- 默认显示的登录方式选择 -->
        <div v-if="!showPasswordForm" class="login-options">
          <!-- OpenID Connect 登录按钮 -->
          <button
            v-if="authConfig.oidcEnabled && authConfig.oidcProviderName"
            @click="handleOidcLogin"
            class="option-btn oidc-btn"
            :disabled="loading"
          >
            <svg class="btn-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM12 20C7.59 20 4 16.41 4 12C4 7.59 7.59 4 12 4C16.41 4 20 7.59 20 12C20 16.41 16.41 20 12 20Z" fill="currentColor"/>
              <path d="M12 6C8.69 6 6 8.69 6 12C6 15.31 8.69 18 12 18C15.31 18 18 15.31 18 12C18 8.69 15.31 6 12 6ZM12 16C9.79 16 8 14.21 8 12C8 9.79 9.79 8 12 8C14.21 8 16 9.79 16 12C16 14.21 14.21 16 12 16Z" fill="currentColor"/>
            </svg>
            <span>使用 {{ authConfig.oidcProviderName }} 登录</span>
          </button>

          <!-- 账号密码登录按钮 -->
          <button
            v-if="authConfig.passwordEnabled"
            @click="showPasswordForm = true"
            class="option-btn password-btn"
          >
            <svg class="btn-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 15C13.6569 15 15 13.6569 15 12C15 10.3431 13.6569 9 12 9C10.3431 9 9 10.3431 9 12C9 13.6569 10.3431 15 12 15Z" fill="currentColor"/>
              <path fill-rule="evenodd" clip-rule="evenodd" d="M19.5 9.5C19.5 6.46243 17.0376 4 14 4H10C6.96243 4 4.5 6.46243 4.5 9.5V11.5C4.5 11.7761 4.72386 12 5 12H6C6.27614 12 6.5 11.7761 6.5 11.5V9.5C6.5 7.567 8.067 6 10 6H14C15.933 6 17.5 7.567 17.5 9.5V11.5C17.5 11.7761 17.7239 12 18 12H19C19.2761 12 19.5 11.7761 19.5 11.5V9.5Z" fill="currentColor"/>
              <path fill-rule="evenodd" clip-rule="evenodd" d="M5 12C3.34315 12 2 13.3431 2 15V19C2 20.6569 3.34315 22 5 22H19C20.6569 22 22 20.6569 22 19V15C22 13.3431 20.6569 12 19 12H5ZM12 17C10.3431 17 9 15.6569 9 14C9 12.3431 10.3431 11 12 11C13.6569 11 15 12.3431 15 14C15 15.6569 13.6569 17 12 17Z" fill="currentColor"/>
            </svg>
            <span>使用账号密码登录</span>
          </button>
        </div>

        <!-- 密码登录表单 -->
        <form v-else @submit.prevent="handleLogin" class="login-form">
          <div class="form-group">
            <label for="username">Username</label>
            <input
              id="username"
              v-model="username"
              type="text"
              class="premium-input"
              placeholder="e.g. admin"
              required
            />
          </div>

          <div class="form-group">
            <label for="password">Password</label>
            <input
              id="password"
              v-model="password"
              type="password"
              class="premium-input"
              placeholder="••••••••"
              required
            />
          </div>

          <button type="submit" class="premium-btn login-btn" :disabled="loading">
            {{ loading ? '登录中...' : 'Sign In' }}
          </button>

          <button type="button" @click="showPasswordForm = false" class="back-btn">
            返回
          </button>

          <p v-if="errorMsg" class="error-msg">{{ errorMsg }}</p>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useAuthStore } from '../store/auth'
import { useSiteInfo } from '../store/site'
import { authConfigApi } from '../api'

const { siteName, siteLogo } = useSiteInfo()

const username = ref('')
const password = ref('')
const loading = ref(false)
const errorMsg = ref('')
const showPasswordForm = ref(false)
const authStore = useAuthStore()

// 认证配置
const authConfig = ref({
  passwordEnabled: true,
  emailLoginEnabled: false,
  oidcEnabled: false,
  oidcProviderName: '',
  ldapEnabled: false
})

// 加载认证配置
const loadAuthConfig = async () => {
  try {
    const data = await authConfigApi.getPublicConfig()
    if (data) {
      authConfig.value = data
    }
  } catch (error) {
    console.error('加载认证配置失败:', error)
  }
}

const handleLogin = async () => {
  if (!username.value || !password.value) return
  loading.value = true
  errorMsg.value = ''
  try {
    const ok = await authStore.login(username.value, password.value)
    if (!ok) {
      errorMsg.value = '用户名或密码错误'
    }
  } catch {
    errorMsg.value = '登录失败，请检查网络连接'
  } finally {
    loading.value = false
  }
}

const handleOidcLogin = () => {
  // 保存当前 URL 作为登录后的回调地址
  const redirectUrl = window.location.origin + '/'
  localStorage.setItem('oidc_redirect', redirectUrl)

  // 跳转到后端的 OpenID Connect 授权端点
  // 后端会处理授权流程并回调
  window.location.href = '/api/auth/oidc/login'
}

onMounted(() => {
  loadAuthConfig()

  // 检查是否有登录错误信息
  const error = new URLSearchParams(window.location.search).get('error')
  if (error) {
    errorMsg.value = decodeURIComponent(error)
    showPasswordForm.value = true
    // 清除 URL 中的错误参数
    window.history.replaceState({}, '', window.location.pathname)
  }
})
</script>

<style scoped>
.login-container {
  display: flex;
  height: 100vh;
  width: 100%;
}

.login-left {
  flex: 1;
  background: linear-gradient(135deg, var(--color-primary-accent) 0%, #1E3A8A 100%);
  color: white;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 4rem;
  position: relative;
  overflow: hidden;
}

.login-left::before {
  content: '';
  position: absolute;
  top: -50%;
  left: -50%;
  width: 200%;
  height: 200%;
  background: radial-gradient(circle, rgba(255,255,255,0.1) 0%, transparent 60%);
  z-index: 1;
}

.brand {
  position: relative;
  z-index: 2;
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}

.logo {
  width: 40px;
  height: 40px;
  background-color: transparent;
  border-radius: var(--radius-md);
  object-fit: contain;
}

.brand h1 {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: -0.025em;
  margin-bottom:0;
}

.tagline {
  position: relative;
  z-index: 2;
  font-size: 1.25rem;
  opacity: 0.9;
  font-weight: 300;
}

.login-right {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: var(--color-bg-primary);
  position: relative;
}

.login-card {
  width: 100%;
  max-width: 440px;
  padding: 3rem;
  border-radius: var(--radius-lg);
  background-color: var(--color-bg-secondary);
}

.login-card h2 {
  font-size: 1.75rem;
  font-weight: 600;
  margin-bottom: 0.5rem;
  color: var(--color-text-primary);
}

.subtitle {
  color: var(--color-text-secondary);
  font-size: 0.95rem;
  margin-bottom: 2rem;
}

/* 登录方式选择按钮 */
.login-options {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.option-btn {
  width: 100%;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.75rem;
  padding: 0.875rem 1rem;
  border-radius: var(--radius-md);
  font-size: 0.95rem;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.oidc-btn {
  background-color: white;
  border: 1px solid #d1d5db;
  color: #374151;
}

.oidc-btn:hover:not(:disabled) {
  background-color: #f9fafb;
  border-color: #9ca3af;
}

.password-btn {
  background-color: var(--color-primary-accent);
  border: 1px solid var(--color-primary-accent);
  color: white;
}

.password-btn:hover {
  opacity: 0.9;
}

.option-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.btn-icon {
  color: currentColor;
}

/* 登录表单 */
.login-form {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.form-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.form-group label {
  font-size: 0.875rem;
  font-weight: 500;
  color: var(--color-text-secondary);
}

.login-btn {
  margin-top: 0.5rem;
  width: 100%;
  padding: 0.875rem;
  font-size: 1rem;
}

.back-btn {
  width: 100%;
  padding: 0.75rem;
  background: transparent;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-md);
  color: var(--color-text-secondary);
  font-size: 0.9rem;
  cursor: pointer;
  transition: all 0.2s ease;
}

.back-btn:hover {
  background-color: var(--color-bg-hover);
  color: var(--color-text-primary);
}

.error-msg {
  color: #bf2600;
  font-size: 0.875rem;
  text-align: center;
  margin: 0;
}

@media (max-width: 768px) {
  .login-container {
    flex-direction: column;
  }
  .login-left {
    flex: none;
    height: 30vh;
    padding: 2rem;
  }
}
</style>
