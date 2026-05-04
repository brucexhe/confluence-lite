<template>
  <div class="login-container">
    <div class="login-left">
      <div class="left-decor-circle decor-1"></div>
      <div class="left-decor-circle decor-2"></div>
      <div class="left-decor-circle decor-3"></div>
      <div class="left-content">
        <div class="brand">
          <img v-if="siteLogo" class="logo" :src="siteLogo" alt="" />
          <div v-else class="logo-placeholder">
            <svg width="28" height="28" viewBox="0 0 24 24" fill="none"><path d="M4 4h7v7H4V4zm9 0h7v7h-7V4zm-9 9h7v7H4v-7zm9.5 1.5a2.5 2.5 0 1 1 5 0 2.5 2.5 0 0 1-5 0z" fill="rgba(255,255,255,0.9)"/></svg>
          </div>
          <h1>{{ siteName }}</h1>
        </div>
        <p class="tagline">Your modern team workspace.</p>
      </div>
    </div>

    <div class="login-right">
      <div class="login-card">
        <!-- Login methods selection -->
        <div v-if="!showPasswordForm" class="login-options-view">
          <div class="card-header">
            <h2>Welcome Back</h2>
            <p class="subtitle">选择一种方式登录你的工作空间</p>
          </div>

          <div class="login-options">
            <!-- OpenID Connect -->
            <button
              v-if="authConfig.oidcEnabled && authConfig.oidcProviderName"
              @click="handleOidcLogin"
              class="option-btn oidc-btn"
              :disabled="loading"
            >
              <div class="option-btn-icon oidc-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2z" stroke="currentColor" stroke-width="1.5"/><path d="M12 6v12M6 12h12" stroke="currentColor" stroke-width="1.5" stroke-linecap="round"/></svg>
              </div>
              <div class="option-btn-text">
                <span class="option-title">使用 {{ authConfig.oidcProviderName }} 登录</span>
                <span class="option-desc">通过企业身份认证</span>
              </div>
              <svg class="option-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M9 5l7 7-7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </button>

            <!-- Password login -->
            <button
              v-if="authConfig.passwordEnabled"
              @click="showPasswordForm = true"
              class="option-btn password-btn"
            >
              <div class="option-btn-icon password-icon">
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.74 5.87L11 18H9v2H7v2H4a1 1 0 01-1-1v-2.59a1 1 0 01.29-.7l7.42-7.43A6 6 0 1121 9z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
              </div>
              <div class="option-btn-text">
                <span class="option-title">使用账号密码登录</span>
                <span class="option-desc">输入用户名和密码</span>
              </div>
              <svg class="option-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M9 5l7 7-7 7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </button>
          </div>
        </div>

        <!-- Password login form -->
        <form v-else @submit.prevent="handleLogin" class="login-form-view">
          <button type="button" @click="showPasswordForm = false" class="back-link">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M15 19l-7-7 7-7" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
            <span>返回</span>
          </button>

          <div class="card-header">
            <h2>账号密码登录</h2>
            <p class="subtitle">请输入你的用户名和密码</p>
          </div>

          <div class="alert-error" v-if="errorMsg">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M12 9v2m0 4h.01M12 2a10 10 0 100 20 10 10 0 000-20z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
            <span>{{ errorMsg }}</span>
          </div>

          <div class="form-field">
            <label for="username">用户名</label>
            <div class="input-wrapper">
              <svg class="input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M20 21v-2a4 4 0 00-4-4H8a4 4 0 00-4 4v2M12 3a4 4 0 110 8 4 4 0 010-8z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
              <input
                id="username"
                v-model="username"
                type="text"
                placeholder="请输入用户名"
                required
                autocomplete="username"
              />
            </div>
          </div>

          <div class="form-field">
            <label for="password">密码</label>
            <div class="input-wrapper">
              <svg class="input-icon" width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
              <input
                id="password"
                v-model="password"
                type="password"
                placeholder="请输入密码"
                required
                autocomplete="current-password"
              />
            </div>
          </div>

          <button type="submit" class="submit-btn" :disabled="loading">
            <span v-if="!loading">登 录</span>
            <span v-else class="loading-spinner"></span>
          </button>
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
/* ── Container ── */
.login-container {
  display: flex;
  height: 100vh;
  width: 100%;
}

/* ── Left panel ── */
.login-left {
  flex: 1;
  background: linear-gradient(160deg, #1e3a8a 0%, var(--color-primary-accent) 50%, #6366f1 100%);
  color: white;
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding: 4rem;
  position: relative;
  overflow: hidden;
}

.left-decor-circle {
  position: absolute;
  border-radius: 50%;
  background: rgba(255, 255, 255, 0.06);
}
.decor-1 { width: 500px; height: 500px; top: -15%; left: -10%; }
.decor-2 { width: 300px; height: 300px; bottom: -8%; right: -5%; background: rgba(255,255,255,0.04); }
.decor-3 { width: 150px; height: 150px; top: 40%; left: 55%; background: rgba(255,255,255,0.03); }

.left-content {
  position: relative;
  z-index: 2;
}

.brand {
  display: flex;
  align-items: center;
  gap: 0.875rem;
  margin-bottom: 1.25rem;
}

.logo {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  object-fit: contain;
}

.logo-placeholder {
  width: 42px;
  height: 42px;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.15);
  display: flex;
  align-items: center;
  justify-content: center;
  backdrop-filter: blur(4px);
}

.brand h1 {
  font-size: 2rem;
  font-weight: 700;
  letter-spacing: -0.02em;
  margin: 0;
}

.tagline {
  font-size: 1.125rem;
  opacity: 0.85;
  font-weight: 300;
  margin: 0;
}

/* ── Right panel ── */
.login-right {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: var(--color-bg-primary);
  padding: 2rem;
}

.login-card {
  width: 100%;
  max-width: 420px;
  padding: 2.5rem;
  border-radius: var(--radius-lg);
  background-color: var(--color-bg-secondary);
  box-shadow: var(--shadow-lg);
  border: 1px solid var(--color-border);
}

/* ── Card header ── */
.card-header {
  margin-bottom: 1.75rem;
}

.card-header h2 {
  font-size: 1.5rem;
  font-weight: 700;
  margin: 0 0 0.375rem;
  color: var(--color-text-primary);
  letter-spacing: -0.01em;
}

.subtitle {
  color: var(--color-text-secondary);
  font-size: 0.875rem;
  margin: 0;
}

/* ── Login option buttons ── */
.login-options {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.option-btn {
  width: 100%;
  display: flex;
  align-items: center;
  gap: 0.875rem;
  padding: 1rem;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-secondary);
  cursor: pointer;
  transition: all 0.2s ease;
  text-align: left;
}

.option-btn:hover:not(:disabled) {
  border-color: var(--color-primary-accent);
  background: #eff6ff;
}

.option-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.option-btn-icon {
  width: 40px;
  height: 40px;
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: var(--color-primary-accent);
}

.oidc-icon {
  background: #eff6ff;
}

.password-icon {
  background: #f0fdf4;
  color: #16a34a;
}

.option-btn-text {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 0.125rem;
}

.option-title {
  font-size: 0.9375rem;
  font-weight: 600;
  color: var(--color-text-primary);
}

.option-desc {
  font-size: 0.8125rem;
  color: var(--color-text-muted);
}

.option-arrow {
  color: var(--color-text-muted);
  flex-shrink: 0;
}

/* ── Login form ── */
.back-link {
  display: inline-flex;
  align-items: center;
  gap: 0.375rem;
  background: none;
  border: none;
  color: var(--color-text-secondary);
  font-size: 0.8125rem;
  cursor: pointer;
  padding: 0;
  margin-bottom: 1.25rem;
  transition: color 0.15s;
}

.back-link:hover {
  color: var(--color-primary-accent);
}

.alert-error {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: var(--radius-md);
  background: #fef2f2;
  border: 1px solid #fecaca;
  color: #991b1b;
  font-size: 0.8125rem;
  margin-bottom: 1.25rem;
}

.form-field {
  margin-bottom: 1.25rem;
}

.form-field label {
  display: block;
  font-size: 0.8125rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 0.375rem;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.input-icon {
  position: absolute;
  left: 0.875rem;
  color: var(--color-text-muted);
  pointer-events: none;
  transition: color 0.15s;
}

.input-wrapper input {
  width: 100%;
  padding: 0.75rem 0.875rem 0.75rem 2.75rem;
  border-radius: var(--radius-md);
  border: 1px solid var(--color-border);
  background: var(--color-bg-primary);
  color: var(--color-text-primary);
  font-size: 0.9375rem;
  transition: all 0.15s ease;
  outline: none;
}

.input-wrapper input::placeholder {
  color: var(--color-text-muted);
}

.input-wrapper input:focus {
  border-color: var(--color-primary-accent);
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.12);
}

.input-wrapper input:focus + .input-icon,
.input-wrapper:focus-within .input-icon {
  color: var(--color-primary-accent);
}

.submit-btn {
  width: 100%;
  padding: 0.8125rem;
  border: none;
  border-radius: var(--radius-md);
  background: var(--color-primary-accent);
  color: white;
  font-size: 0.9375rem;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  margin-top: 0.5rem;
}

.submit-btn:hover:not(:disabled) {
  background: var(--color-primary-hover);
  box-shadow: 0 4px 12px rgba(59, 130, 246, 0.3);
}

.submit-btn:disabled {
  opacity: 0.7;
  cursor: not-allowed;
}

.loading-spinner {
  display: inline-block;
  width: 18px;
  height: 18px;
  border: 2px solid rgba(255,255,255,0.3);
  border-top-color: white;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin {
  to { transform: rotate(360deg); }
}

/* ── Responsive ── */
@media (max-width: 768px) {
  .login-container {
    flex-direction: column;
  }
  .login-left {
    flex: none;
    padding: 2rem;
    min-height: auto;
  }
  .brand h1 {
    font-size: 1.5rem;
  }
  .login-right {
    padding: 1.5rem;
  }
  .login-card {
    padding: 1.75rem;
    box-shadow: none;
    border: none;
    background: transparent;
    max-width: 100%;
  }
}
</style>
