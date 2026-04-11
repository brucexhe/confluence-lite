import { createRouter, createWebHistory } from 'vue-router'
import { h } from 'vue'
import MainLayout from '../layouts/MainLayout.vue'
import Login from '../views/Login.vue'
import Setup from '../views/Setup.vue'
import WorkspaceHome from '../views/Workspace/Index.vue'

// Simple error page component for API unreachable
const ErrorPage = {
  render() {
    return h('div', {
      style: 'min-height:100vh;display:flex;align-items:center;justify-content:center;background:#f4f5f7;'
    }, [
      h('div', {
        style: 'text-align:center;padding:48px;background:white;border-radius:12px;box-shadow:0 2px 8px rgba(0,0,0,0.1);max-width:480px;'
      }, [
        h('div', { style: 'font-size:48px;margin-bottom:16px;' }, '⚠'),
        h('h2', { style: 'color:#172b4d;margin:0 0 8px;' }, '服务不可用'),
        h('p', { style: 'color:#6b778c;font-size:14px;margin:0 0 24px;' }, '无法连接到后端服务，请检查服务是否已启动。'),
        h('button', {
          style: 'background:#0049b0;color:white;border:none;padding:10px 24px;border-radius:6px;cursor:pointer;font-size:14px;',
          onClick: () => { window.__resetInstallCheck(); window.location.href = '/' }
        }, '重试')
      ])
    ])
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/setup',
      name: 'setup',
      component: Setup,
      meta: { public: true }
    },
    {
      path: '/login',
      name: 'login',
      component: Login,
      meta: { public: true }
    },
    {
      path: '/error',
      name: 'error',
      component: ErrorPage,
      meta: { public: true }
    },
    {
      path: '/',
      component: MainLayout,
      meta: { requiresAuth: true },
      children: [
        { path: '', name: 'home', component: WorkspaceHome },
        { path: 'page/:id', name: 'page', component: () => import('../views/Workspace/Page.vue') },
        { path: 'spaces', name: 'space-list', component: () => import('../views/Workspace/List.vue') }
      ]
    }
  ]
})

// Install status cache
let _installChecked = false
let _isInstalled = false
let _apiAvailable = true

async function checkInstalled() {
  if (_installChecked) return { installed: _isInstalled, apiAvailable: _apiAvailable }
  try {
    const res = await fetch('/api/setup/status')
    if (!res.ok) throw new Error('API error')
    const data = await res.json()
    _isInstalled = data.data?.installed === true
    _apiAvailable = true
    _installChecked = true
    return { installed: _isInstalled, apiAvailable: true }
  } catch {
    _apiAvailable = false
    _installChecked = true
    return { installed: false, apiAvailable: false }
  }
}

// Allow re-check after setup completes
window.__resetInstallCheck = () => {
  _installChecked = false
  _isInstalled = false
  _apiAvailable = true
}

router.beforeEach(async (to, from, next) => {
  const { installed, apiAvailable } = await checkInstalled()

  // API unavailable → show error (block all pages)
  if (!apiAvailable && to.name !== 'error') {
    next({ name: 'error' })
    return
  }

  // Not installed → force to setup page
  if (apiAvailable && !installed && to.name !== 'setup') {
    next({ name: 'setup' })
    return
  }

  // Already installed → don't allow setup page
  if (apiAvailable && installed && to.name === 'setup') {
    next({ path: '/' })
    return
  }

  // Auth check
  const isAuthenticated = localStorage.getItem('auth_token')
  if (to.meta.requiresAuth && !isAuthenticated) {
    next({ name: 'login' })
  } else if (to.name === 'login' && isAuthenticated) {
    next({ path: '/' })
  } else {
    next()
  }
})

export default router
