<template>
    <div class="layout-wrapper">
        <!-- Top Navigation -->
        <TopNavbar />

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
                        <router-link to="/settings/office-preview" class="nav-item" :class="{ active: route.path === '/settings/office-preview' }">
                            Office 预览
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
import TopNavbar from '../components/TopNavbar.vue';

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
</script>

<style scoped>
.layout-wrapper {
    display: flex;
    flex-direction: column;
    height: 100vh;
    overflow: hidden;
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
