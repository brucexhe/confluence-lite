<template>
    <div class="layout-wrapper">
        <!-- Top Navigation -->
        <header class="top-navbar">
            <div class="nav-left">
                <div class="logo" @click="navigateTo('/')" style="cursor: pointer"></div>
                <a class="nav-title" href="/" style="cursor: pointer;color: #fff">Confluence Lite</a>
                <nav class="nav-links">
                    <a-dropdown :trigger="['click']" placement="bottomLeft">
                        <a class="nav-link dropdown-link" @click.prevent>
                            Spaces
                            <svg
                                width="14"
                                height="14"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                stroke-width="2"
                                stroke-linecap="round"
                                stroke-linejoin="round"
                                style="margin-left: 2px; margin-top: 1px"
                            >
                                <polyline points="6 9 12 15 18 9"></polyline>
                            </svg>
                        </a>
                        <template #overlay>
                            <a-menu
                                style="
                                    width: 280px;
                                    padding: 8px 0;
                                    border-radius: 3px;
                                    box-shadow:
                                        0 4px 8px -2px rgba(9, 30, 66, 0.25),
                                        0 0 1px rgba(9, 30, 66, 0.31);
                                "
                            >
                                <div
                                    style="
                                        padding: 4px 16px 8px;
                                        font-size: 11px;
                                        font-weight: 700;
                                        color: #6b778c;
                                        text-transform: uppercase;
                                    "
                                >
                                    Recent Spaces
                                </div>
                                <a-menu-item
                                    v-for="space in spaces"
                                    :key="space.id"
                                    style="padding: 8px 16px"
                                    @click="navigateToSpace(space.key)"
                                >
                                    <div style="display: flex; align-items: center; gap: 12px">
                                        <div
                                            :style="{
                                                width: '32px',
                                                height: '32px',
                                                background: spaceColor(space.id),
                                                borderRadius: '3px',
                                                display: 'flex',
                                                alignItems: 'center',
                                                justifyContent: 'center',
                                                color: '#fff',
                                                fontWeight: 600,
                                                fontSize: '14px'
                                            }"
                                        >{{ spaceKeyInitial(space) }}</div>
                                        <div>
                                            <div
                                                style="
                                                    font-weight: 500;
                                                    color: #172b4d;
                                                    font-size: 14px;
                                                    line-height: 1.2;
                                                "
                                            >
                                                {{ space.name || space.key }}
                                            </div>
                                            <div style="font-size: 12px; color: #6b778c">{{ space.key }}</div>
                                        </div>
                                    </div>
                                </a-menu-item>
                                <a-menu-item v-if="spaces.length === 0" disabled style="padding: 8px 16px">
                                    <span style="color: #6b778c; font-size: 13px">暂无空间</span>
                                </a-menu-item>
                                <a-menu-divider v-if="spaces.length > 0" />
                                <a-menu-item key="view-all" style="padding: 4px 16px" @click="navigateTo('/spaces')">
                                    <span style="color: #0052cc; font-size: 14px; font-weight: 500">
                                        View all spaces
                                    </span>
                                </a-menu-item>
                            </a-menu>
                        </template>
                    </a-dropdown>
                    <a href="#" class="nav-link">Recent</a>
                    <a href="#" class="nav-link">People</a>
                </nav>
            </div>
            <div class="nav-right">
                <div class="search-box">
                    <a-input-search placeholder="Search..." class="confluence-search" style="width: 200px" />
                </div>
                <button class="create-btn" @click="createPage">Create</button>
                <a-dropdown :trigger="['click']">
                    <a-avatar
                        style="
                            background-color: #ffffff;
                            color: #0049b0;
                            cursor: pointer;
                            width: 28px;
                            height: 28px;
                            line-height: 28px;
                            font-size: 14px;
                        "
                        @click.prevent
                    >
                        {{ userInitials }}
                    </a-avatar>
                    <template #overlay>
                        <a-menu style="min-width: 120px; padding: 4px 0;">
                            <a-menu-item @click="navigateTo('/spaces')">
                                <span style="font-size: 14px; color: #172b4d;">空间列表</span>
                            </a-menu-item>
                            <a-menu-item @click="navigateTo('/recent')">
                                <span style="font-size: 14px; color: #172b4d;">最近浏览</span>
                            </a-menu-item>
                            <a-menu-item @click="navigateTo('/profile')">
                                <span style="font-size: 14px; color: #172b4d;">用户信息</span>
                            </a-menu-item>
                            <a-menu-item @click="navigateTo('/settings')">
                                <span style="font-size: 14px; color: #172b4d;">系统设置</span>
                            </a-menu-item>
                            <a-menu-divider />
                            <a-menu-item @click="handleLogout">
                                <span style="font-size: 14px; color: #ef4444;">退出登录</span>
                            </a-menu-item>
                        </a-menu>
                    </template>
                </a-dropdown>
            </div>
        </header>

        <div class="main-container">
            <!-- Sidebar Navigation -->
            <aside class="settings-sidebar">
                <div class="sidebar-header">
                    <h2>系统设置</h2>
                    <p class="breadcrumb">配置和管理您的系统</p>
                </div>
                <nav class="sidebar-nav">
                    <div class="nav-group">
                        <div class="nav-group-title">基本设置</div>
                        <router-link to="/settings" class="nav-item" :class="{ active: route.path === '/settings' }">
                            常规设置
                        </router-link>
                        <router-link to="/settings/security" class="nav-item" :class="{ active: route.path === '/settings/security' }">
                            安全设置
                        </router-link>
                        <router-link to="/settings/display" class="nav-item" :class="{ active: route.path === '/settings/display' }">
                            显示设置
                        </router-link>
                        <router-link to="/settings/mail" class="nav-item" :class="{ active: route.path === '/settings/mail' }">
                            邮件设置
                        </router-link>
                        <router-link to="/settings/authentication" class="nav-item" :class="{ active: route.path === '/settings/authentication' }">
                            身份验证
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">用户与权限</div>
                        <router-link to="/settings/users" class="nav-item" :class="{ active: route.path === '/settings/users' }">
                            用户管理
                        </router-link>
                        <router-link to="/settings/groups" class="nav-item" :class="{ active: route.path === '/settings/groups' }">
                            用户组管理
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">内容管理</div>
                        <router-link to="/settings/workspaces" class="nav-item" :class="{ active: route.path === '/settings/workspaces' }">
                            空间管理
                        </router-link>
                        <router-link to="/settings/pages" class="nav-item" :class="{ active: route.path === '/settings/pages' }">
                            页面管理
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">系统管理</div>
                        <router-link to="/settings/system-info" class="nav-item" :class="{ active: route.path === '/settings/system-info' }">
                            系统信息
                        </router-link>
                        <router-link to="/settings/logs" class="nav-item" :class="{ active: route.path === '/settings/logs' }">
                            日志
                        </router-link>
                        <router-link to="/settings/backup" class="nav-item" :class="{ active: route.path === '/settings/backup' }">
                            备份与还原
                        </router-link>
                        <router-link to="/settings/jobs" class="nav-item" :class="{ active: route.path === '/settings/jobs' }">
                            作业管理
                        </router-link>
                        <router-link to="/settings/cache" class="nav-item" :class="{ active: route.path === '/settings/cache' }">
                            缓存管理
                        </router-link>
                    </div>
                </nav>
            </aside>

            <!-- Main Content Area -->
            <main class="settings-content" ref="contentRef">
                <router-view />
            </main>
        </div>
    </div>
</template>

<script setup>
import { computed, watch, nextTick, ref } from 'vue';
import { useAuthStore } from '../store/auth';
import { useRouter, useRoute } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();
const route = useRoute();
const contentRef = ref(null);

// 监听路由变化，滚动内容区域到顶部
watch(() => route.path, (newPath, oldPath) => {
    if (newPath !== oldPath && contentRef.value) {
        nextTick(() => {
            contentRef.value.scrollTop = 0;
        });
    }
});

// 空间列表（从 localStorage 读取）
const spaces = computed(() => {
    return JSON.parse(localStorage.getItem('auth_spaces') || '[]')
})

const spaceColors = [
    'linear-gradient(135deg, #10b981, #059669)',
    'linear-gradient(135deg, #3b82f6, #2563eb)',
    'linear-gradient(135deg, #8b5cf6, #7c3aed)',
    'linear-gradient(135deg, #f59e0b, #d97706)',
    'linear-gradient(135deg, #ef4444, #dc2626)',
    'linear-gradient(135deg, #06b6d4, #0891b2)',
]

function spaceColor(id) {
    return spaceColors[(id || 0) % spaceColors.length]
}

function spaceKeyInitial(space) {
    return (space.key || '?').charAt(0).toUpperCase()
}

function navigateToSpace(key) {
    router.push(`/${key}`)
}

const userInitials = computed(() => {
    if (authStore.user && authStore.user.name) {
        return authStore.user.name.charAt(0).toUpperCase();
    }
    return "U";
});

const handleLogout = () => {
    authStore.logout();
};

const navigateTo = (path) => {
    router.push(path);
};

const createPage = () => {
    // 从 localStorage 获取第一个空间
    const spacesList = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
    if (spacesList.length > 0) {
        router.push({ path: `/${spacesList[0].key}/page/new` })
    }
};
</script>

<style scoped>
.layout-wrapper {
    display: flex;
    flex-direction: column;
    height: 100vh;
    overflow: hidden;
}

/* Navbar */
.top-navbar {
    height: 40px;
    background-color: #0049b0;
    color: #ffffff;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 1rem;
    z-index: 10;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.nav-left {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.logo {
    width: 24px;
    height: 24px;
    background-color: #ffffff;
    border-radius: 3px;
}

.logo:hover {
    opacity: 0.8;
}

.nav-title {
    font-weight: 500;
    font-size: 14px;
}

.nav-title:hover {
    text-decoration: underline;
}

.nav-links {
    display: flex;
    gap: 0.2rem;
    margin-left: 0.5rem;
}

.nav-link {
    color: rgba(255, 255, 255, 0.9);
    font-size: 14px;
    font-weight: 500;
    padding: 0.25rem 0.5rem;
    border-radius: 3px;
    text-decoration: none;
}

.nav-link.dropdown-link {
    display: inline-flex;
    align-items: center;
}

.nav-link:hover {
    background-color: rgba(0, 0, 0, 0.1);
    color: #ffffff;
}

.nav-right {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

:deep(.confluence-search .ant-input) {
    border-radius: 3px 0 0 3px !important;
    height: 30px !important;
    font-size: 13px !important;
    border: none !important;
    background-color: rgba(255, 255, 255, 0.2) !important;
    color: #ffffff !important;
}

:deep(.confluence-search .ant-input::placeholder) {
    color: rgba(255, 255, 255, 0.7) !important;
}

:deep(.confluence-search .ant-btn) {
    border-radius: 0 3px 3px 0 !important;
    height: 30px !important;
    border: none !important;
    background-color: rgba(255, 255, 255, 0.2) !important;
    color: #ffffff !important;
}

.create-btn {
    background-color: #0052cc;
    color: #ffffff;
    border: none;
    border-radius: 3px;
    font-weight: 500;
    font-size: 14px;
    height: 30px;
    padding: 0 12px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    transition: all 0.2s;
}

.create-btn:hover {
    background-color: #0065ff;
}

/* Main Container */
.main-container {
    display: flex;
    flex: 1;
    overflow: hidden;
}

/* Sidebar */
.settings-sidebar {
    width: 240px;
    background-color: #ffffff;
    border-right: 1px solid #dfe1e6;
    flex-shrink: 0;
    overflow-y: auto;
}

.sidebar-header {
    padding: 16px 16px 12px;
    border-bottom: 1px solid #dfe1e6;
}

.sidebar-header h2 {
    font-size: 16px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 2px 0;
}

.breadcrumb {
    font-size: 12px;
    color: #6b778c;
    margin: 0;
}

.sidebar-nav {
    padding: 8px 0;
}

.nav-group {
    margin-bottom: 16px;
}

.nav-group:last-child {
    margin-bottom: 0;
}

.nav-group-title {
    padding: 4px 16px;
    font-size: 11px;
    font-weight: 700;
    color: #6b778c;
    text-transform: uppercase;
    letter-spacing: 0.5px;
}

.nav-item {
    display: flex;
    align-items: center;
    padding: 6px 16px;
    color: #42526e;
    text-decoration: none;
    font-size: 14px;
    transition: all 0.15s ease;
    border-left: 3px solid transparent;
    line-height: 1.5;
}

.nav-item:hover {
    background-color: #ebecf0;
    color: #172b4d;
}

.nav-item.active {
    background-color: #e6effc;
    color: #0052cc;
    border-left-color: #0052cc;
    font-weight: 500;
}

.nav-item svg {
    flex-shrink: 0;
}

/* Content Area */
.settings-content {
    flex: 1;
    background-color: #f4f5f7;
    overflow-y: auto;
}
</style>
