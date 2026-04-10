<template>
  <div class="login-container">
    <div class="login-left">
      <div class="brand">
        <div class="logo"></div>
        <h1>Confluence Lite</h1>
      </div>
      <p class="tagline">Your modern team workspace.</p>
    </div>
    
    <div class="login-right">
      <div class="login-card glass-panel">
        <h2>Welcome Back</h2>
        <p class="subtitle">Please enter your credentials to access your workspace.</p>
        
        <form @submit.prevent="handleLogin" class="login-form">
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
          
          <button type="submit" class="premium-btn login-btn">
            Sign In
          </button>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useAuthStore } from '../store/auth'

const username = ref('')
const password = ref('')
const authStore = useAuthStore()

const handleLogin = () => {
  if (username.value && password.value) {
    authStore.login(username.value, password.value)
  }
}
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
  width: 48px;
  height: 48px;
  background-color: white;
  border-radius: var(--radius-md);
  box-shadow: var(--shadow-md);
}

.brand h1 {
  font-size: 2.5rem;
  font-weight: 700;
  letter-spacing: -0.025em;
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
  margin-top: 1rem;
  width: 100%;
  padding: 0.875rem;
  font-size: 1rem;
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
