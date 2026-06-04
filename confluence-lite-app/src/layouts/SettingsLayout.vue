<template>
    <div class="layout-wrapper">
        <!-- Top Navigation -->
        <TopNavbar />

        <div class="main-container">
            <!-- Sidebar Navigation -->
            <aside class="settings-sidebar">
                <div class="sidebar-header">
                    <h2>{{ $t('settings.title') }}</h2>
                    <p class="breadcrumb">{{ $t('settings.description') }}</p>
                </div>
                <nav class="sidebar-nav">
                    <div class="nav-group">
                        <div class="nav-group-title">{{ $t('settings.basicSettings') }}</div>
                        <router-link to="/settings" class="nav-item" :class="{ active: route.path === '/settings' }">
                            {{ $t('settings.siteSettings') }}
                        </router-link>
                        <router-link to="/settings/security" class="nav-item" :class="{ active: route.path === '/settings/security' }">
                            {{ $t('settings.securitySettings') }}
                        </router-link>
                        <router-link to="/settings/display" class="nav-item" :class="{ active: route.path === '/settings/display' }">
                            {{ $t('settings.displaySettings') }}
                        </router-link>
                        <router-link to="/settings/mail" class="nav-item" :class="{ active: route.path === '/settings/mail' }">
                            {{ $t('settings.mailSettings') }}
                        </router-link>
                        <router-link to="/settings/authentication" class="nav-item" :class="{ active: route.path === '/settings/authentication' }">
                            {{ $t('settings.authentication') }}
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">{{ $t('settings.usersPermissions') }}</div>
                        <router-link to="/settings/users" class="nav-item" :class="{ active: route.path === '/settings/users' }">
                            {{ $t('settings.userManagement') }}
                        </router-link>
                        <router-link to="/settings/groups" class="nav-item" :class="{ active: route.path === '/settings/groups' }">
                            {{ $t('settings.groupManagement') }}
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">{{ $t('settings.contentManagement') }}</div>
                        <router-link to="/settings/workspaces" class="nav-item" :class="{ active: route.path === '/settings/workspaces' }">
                            {{ $t('settings.workspaceManagement') }}
                        </router-link>
                        <router-link to="/settings/pages" class="nav-item" :class="{ active: route.path === '/settings/pages' }">
                            {{ $t('settings.pageManagement') }}
                        </router-link>
                        <router-link to="/settings/office-preview" class="nav-item" :class="{ active: route.path === '/settings/office-preview' }">
                            {{ $t('settings.officePreviewSetting') }}
                        </router-link>
                    </div>

                    <div class="nav-group">
                        <div class="nav-group-title">{{ $t('settings.systemManagement') }}</div>
                        <router-link to="/settings/system-info" class="nav-item" :class="{ active: route.path === '/settings/system-info' }">
                            {{ $t('settings.systemInfo') }}
                        </router-link>
                        <router-link to="/settings/logs" class="nav-item" :class="{ active: route.path === '/settings/logs' }">
                            {{ $t('settings.logs') }}
                        </router-link>
                        <router-link to="/settings/backup" class="nav-item" :class="{ active: route.path === '/settings/backup' }">
                            {{ $t('settings.backupRestore') }}
                        </router-link>
                        <router-link to="/settings/confluence-import" class="nav-item" :class="{ active: route.path === '/settings/confluence-import' }">
                            {{ $t('settings.confluenceImport') }}
                        </router-link>
                        <router-link to="/settings/jobs" class="nav-item" :class="{ active: route.path === '/settings/jobs' }">
                            {{ $t('settings.jobManagement') }}
                        </router-link>
                        <router-link to="/settings/cache" class="nav-item" :class="{ active: route.path === '/settings/cache' }">
                            {{ $t('settings.cacheManagement') }}
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

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
    .main-container {
        flex-direction: column;
    }

    .settings-sidebar {
        width: 100%;
        height: auto;
        max-height: none;
        border-right: none;
        border-bottom: 1px solid #dfe1e6;
        overflow-x: auto;
        overflow-y: hidden;
        flex-shrink: 0;
    }

    .sidebar-header {
        padding: 12px 16px 8px;
    }

    .sidebar-nav {
        display: flex;
        overflow-x: auto;
        padding: 0 8px 8px;
        gap: 0;
        white-space: nowrap;
    }

    .nav-group {
        margin-bottom: 0;
        display: flex;
        gap: 0;
    }

    .nav-group-title {
        display: none;
    }

    .nav-item {
        padding: 6px 12px;
        border-left: none;
        border-bottom: 2px solid transparent;
        font-size: 13px;
        white-space: nowrap;
    }

    .nav-item.active {
        border-left-color: transparent;
        border-bottom-color: #0052cc;
    }
}
</style>
