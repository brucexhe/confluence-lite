import { createRouter, createWebHistory } from 'vue-router'
import MainLayout from '../layouts/MainLayout.vue'
import Login from '../views/Login.vue'
import WorkspaceHome from '../views/Workspace/Index.vue'

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: Login
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

// Navigation guard for dummy auth
router.beforeEach((to, from, next) => {
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
