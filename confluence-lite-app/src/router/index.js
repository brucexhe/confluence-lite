import {createRouter, createWebHistory} from 'vue-router'
import {h} from 'vue'
import MainLayout from '../layouts/MainLayout.vue'
import SettingsLayout from '../layouts/SettingsLayout.vue'
import Login from '../views/Login.vue'
import Setup from '../views/Setup.vue'
import WorkspaceHome from '../views/Workspace/Index.vue'
import {workspaceApi} from '../api'
import { loadSiteInfo, useSiteInfo } from '../store/site'

const ErrorPage = {
    render() {
        return h('div', {
            style: 'min-height:100vh;display:flex;align-items:center;justify-content:center;background:#f4f5f7;'
        }, [
            h('div', {
                style: 'text-align:center;padding:48px;background:white;border-radius:12px;box-shadow:0 2px 8px rgba(0,0,0,0.1);max-width:480px;'
            }, [
                h('div', {style: 'font-size:48px;margin-bottom:16px;'}, '⚠'),
                h('h2', {style: 'color:#172b4d;margin:0 0 8px;'}, '服务不可用'),
                h('p', {style: 'color:#6b778c;font-size:14px;margin:0 0 24px;'}, '无法连接到后端服务，请检查服务是否已启动。'),
                h('button', {
                    style: 'background:#0049b0;color:white;border:none;padding:10px 24px;border-radius:6px;cursor:pointer;font-size:14px;',
                    onClick: () => {
                        window.__resetInstallCheck();
                        window.location.href = '/'
                    }
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
            meta: {public: true}
        },
        {
            path: '/login',
            name: 'login',
            component: Login,
            meta: {public: true}
        },
        {
            path: '/error',
            name: 'error',
            component: ErrorPage,
            meta: {public: true}
        },
        {
            path: '/spaces',
            name: 'spaces',
            component: MainLayout, //() => import('../views/Workspace/List.vue'),
            meta: {requiresAuth: true},
            redirect: 'space-index',
            children: [
                {
                    path: '',
                    name: 'space-index',
                    component: () => import('../views/Workspace/List.vue')
                },
            ]
        },
        {
            path: '/',
            name: 'home',
            meta: {requiresAuth: true},
            component: MainLayout
        },
        {
            path: '/:spaceKey',
            component: MainLayout,
            meta: {requiresAuth: true},
            children: [
                {
                    path: '',
                    name: 'space-home',
                    component: WorkspaceHome
                },
                {
                    path: 'page/new',
                    name: 'page-create',
                    component: () => import('../views/Page/Edit.vue')
                },
                {
                    path: 'page/:id/edit',
                    name: 'page-edit',
                    component: () => import('../views/Page/Edit.vue')
                },
                {
                    path: 'page/:id',
                    name: 'page',
                    component: () => import('../views/Page/View.vue')
                }
            ]
        },
        {
            path: '/settings',
            component: SettingsLayout,
            meta: {requiresAuth: true},
            children: [
                {
                    path: '',
                    name: 'settings',
                    component: () => import('../views/SystermSetting/Index.vue')
                },
                {
                    path: 'security',
                    name: 'settings-security',
                    component: () => import('../views/SystermSetting/Security.vue')
                },
                {
                    path: 'display',
                    name: 'settings-display',
                    component: () => import('../views/SystermSetting/Display.vue')
                },
                {
                    path: 'mail',
                    name: 'settings-mail',
                    component: () => import('../views/SystermSetting/Mail.vue')
                },
                {
                    path: 'authentication',
                    name: 'settings-authentication',
                    component: () => import('../views/SystermSetting/Authentication.vue')
                },
                {
                    path: 'users',
                    name: 'settings-users',
                    component: () => import('../views/SystermSetting/Users.vue')
                },
                {
                    path: 'groups',
                    name: 'settings-groups',
                    component: () => import('../views/SystermSetting/Groups.vue')
                },
                {
                    path: 'workspaces',
                    name: 'settings-workspaces',
                    component: () => import('../views/SystermSetting/Workspaces.vue')
                },
                {
                    path: 'pages',
                    name: 'settings-pages',
                    component: () => import('../views/SystermSetting/Pages.vue')
                },
                {
                    path: 'office-preview',
                    name: 'settings-office-preview',
                    component: () => import('../views/SystermSetting/OfficePreview.vue')
                },
                {
                    path: 'system-info',
                    name: 'settings-system-info',
                    component: () => import('../views/SystermSetting/SystemInfo.vue')
                },
                {
                    path: 'logs',
                    name: 'settings-logs',
                    component: () => import('../views/SystermSetting/Logs.vue')
                },
                {
                    path: 'backup',
                    name: 'settings-backup',
                    component: () => import('../views/SystermSetting/Backup.vue')
                },
                {
                    path: 'jobs',
                    name: 'settings-jobs',
                    component: () => import('../views/SystermSetting/Jobs.vue')
                },
                {
                    path: 'cache',
                    name: 'settings-cache',
                    component: () => import('../views/SystermSetting/Cache.vue')
                }
            ]
        }, 
        {
            path: '/:pathMatch(.*)*',
            redirect: '/404'
        }
    ]
})

// Install status cache
let _installChecked = false

async function checkInstalled() {
    if (_installChecked) {
        const site = useSiteInfo()
        return {installed: site.installed.value, apiAvailable: true}
    }
    _installChecked = true
    const result = await loadSiteInfo()
    return result
}

window.__resetInstallCheck = () => {
    _installChecked = false
}

router.beforeEach(async (to, from, next) => {
    const {installed, apiAvailable} = await checkInstalled()

    if (!apiAvailable && to.name !== 'error') {
        next({name: 'error'})
        return
    }

    if (apiAvailable && !installed && to.name !== 'setup') {
        next({name: 'setup'})
        return
    }

    if (apiAvailable && installed && to.name === 'setup') {
        next({path: '/'})
        return
    }

    // 处理空间 KEY 大小写 - 如果是空间路由且 key 是小写，重定向到大写
    if (to.params.spaceKey && to.params.spaceKey !== to.params.spaceKey.toUpperCase()) {
        next({
            path: to.path.replace(to.params.spaceKey, to.params.spaceKey.toUpperCase()),
            query: to.query,
            hash: to.hash
        })
        return
    }

    // Auth check
    const isAuthenticated = localStorage.getItem('auth_token')

    // Not logged in → login page (except public routes)
    // 检查 meta 是否存在以及 requiresAuth 属性
    if (to.meta && to.meta.requiresAuth && !isAuthenticated) {
        next({name: 'login'})
        return
    }

    // Logged in + on home/login → redirect to first space
    if ((to.name === 'login' || to.name === 'home') && isAuthenticated) {
        const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
        if (spaces.length > 0) {
            next({path: `/${spaces[0].key.toUpperCase()}`})
        } else {
            // No spaces → try fetch from API
            try {
                const data = await workspaceApi.getMy()
                if (data && data.length > 0) {
                    next({path: `/${data[0].key.toUpperCase()}`})
                } else {
                    next({name: 'login'})
                }
            } catch {
                next({name: 'login'})
            }
        }
        return
    }

    next()
})

// 路由切换时滚动到顶部
router.afterEach((to, from) => {
    // 只有在实际切换页面时才滚动（不是同一路由的参数变化）
    if (to.path !== from.path) {
        window.scrollTo({ top: 0, behavior: 'smooth' })
    }
})

export default router
