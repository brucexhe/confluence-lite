<template>
    <div class="layout-wrapper" :class="{ 'is-resizing': isResizing }">
        <!-- Top Navigation -->
        <header class="top-navbar">
            <div class="nav-left">
                <div class="logo" @click="navigateTo('/')" style="cursor: pointer"></div>
                <a class="nav-title" href="/" style="cursor: pointer;color: #fff">Confluence Lite</a>
                <nav class="nav-links">
                    <a-dropdown :trigger="['click']" placement="bottomLeft">
                        <a class="nav-link active dropdown-link" @click.prevent>
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
                                <a-menu-item key="view-all" style="padding: 4px 16px" @click="$router.push('/spaces')">
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
            <aside class="sidebar" :style="{ width: sidebarWidth + 'px', minWidth: sidebarWidth + 'px' }">
                <div class="space-header">
                    <div class="space-icon" 
                    :style="{ 
                        background: currentSpaceColor, 
                        color: '#fff', 
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: '#fff',
                        fontWeight: 600,
                        fontSize: '14px'
                        }">{{ currentSpaceInitial }}</div>
                    <div class="space-info">
                        <h3 @click="navigateToSpace(currentSpaceKey)" style="cursor: pointer">{{ currentSpaceName }}</h3>
                        <p>{{ currentSpaceKey }}</p>
                    </div>
                </div>

                <div class="sidebar-section">
                    <div
                        style="
                            display: flex;
                            justify-content: space-between;
                            align-items: center;
                            margin-bottom: 0.5rem;
                            padding: 0 0.5rem;
                        "
                    >
                        <h4 class="section-title">Pages</h4>
                        <a-button type="text" size="small" style="color: var(--color-text-secondary)">+</a-button>
                    </div>
                    <PageTree :workspace-id="currentSpace?.id" :space-key="currentSpaceKey" />
                </div>

                <!-- Drag Handle for resizing sidebar -->
                <div class="sidebar-resizer" @mousedown="startResize"></div>
            </aside>

            <!-- Main Content Area -->
            <main class="content-area">
                <div class="page-content">
                    <router-view />
                </div>
            </main>
        </div>
    </div>
</template>

<script setup>
import { computed, ref, onUnmounted } from "vue";
import { useAuthStore } from "../store/auth";
import PageTree from "../components/PageTree.vue";
import { useRoute, useRouter } from "vue-router";

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();

// 空间列表（从 localStorage 读取）
const spaces = computed(() => {
    return JSON.parse(localStorage.getItem('auth_spaces') || '[]')
})

// 当前空间（根据路由 :spaceKey 匹配）
const currentSpace = computed(() => {
    const key = route.params.spaceKey
    if (!key) return spaces.value[0] || null
    return spaces.value.find(s => s.key === key) || { key, name: key }
})

const currentSpaceName = computed(() => currentSpace.value?.name || currentSpace.value?.key || '')
const currentSpaceKey = computed(() => currentSpace.value?.key || '')

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

const currentSpaceColor = computed(() => spaceColor(currentSpace.value?.id))

function spaceKeyInitial(space) {
    return (space.key || '?').charAt(0).toUpperCase()
}

const currentSpaceInitial = computed(() => spaceKeyInitial(currentSpace.value || { key: '?' }))

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
    const key = route.params.spaceKey
    if (key) {
        const currentId = route.params.id
        const query = currentId ? { parentId: currentId } : {}
        router.push({ path: `/${key}/page/new`, query })
    }
};

// Sidebar Resize Logic
const sidebarWidth = ref(260);
const isResizing = ref(false);

const startResize = (e) => {
    isResizing.value = true;
    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", stopResize);
    document.body.style.userSelect = "none";
};

const handleMouseMove = (e) => {
    if (!isResizing.value) return;
    const newWidth = e.clientX;
    if (newWidth >= 200 && newWidth <= 800) {
        sidebarWidth.value = newWidth;
    }
};

const stopResize = () => {
    isResizing.value = false;
    document.removeEventListener("mousemove", handleMouseMove);
    document.removeEventListener("mouseup", stopResize);
    document.body.style.userSelect = "";
};

onUnmounted(() => {
    document.removeEventListener("mousemove", handleMouseMove);
    document.removeEventListener("mouseup", stopResize);
});
</script>

<style scoped>
.layout-wrapper {
    display: flex;
    flex-direction: column;
    height: 100vh;
    overflow: hidden;
}

.layout-wrapper.is-resizing {
    cursor: col-resize;
}

.layout-wrapper.is-resizing * {
    pointer-events: none;
}

/* Navbar */
.top-navbar {
    height: 40px; /* Confluence 7 classic compact height */
    background-color: #0049b0; /* Confluence blue theme */
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

.nav-title {
    font-weight: 500;
    font-size: 14px;
}

.nav-title:hover {
    text-decoration: underline;
}

.logo:hover {
    opacity: 0.8;
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
}

.nav-link.dropdown-link {
    display: inline-flex;
    align-items: center;
}

.nav-link.active {
    color: #ffffff;
    background-color: rgba(0, 0, 0, 0.15);
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

/* Confluence 7 Search Input Customization */
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

/* Confluence 7 Create Button */
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
.sidebar {
    width: 260px; /* fallback */
    background-color: var(--color-bg-tertiary);
    border-right: 1px solid var(--color-border);
    display: flex;
    flex-direction: column;
    position: relative;
    overflow-y: auto;
    overflow-x: hidden;
}

.sidebar-resizer {
    position: absolute;
    top: 0;
    right: -3px;
    width: 6px;
    height: 100%;
    cursor: col-resize;
    z-index: 100;
    transition: background-color 0.2s;
}

.sidebar-resizer:hover,
.layout-wrapper.is-resizing .sidebar-resizer {
    background-color: #0052cc; /* Confluence blue hover */
}

/* Custom scrollbar for sidebar */
.sidebar::-webkit-scrollbar {
    width: 8px;
}

.sidebar::-webkit-scrollbar-track {
    background: transparent;
}

.sidebar::-webkit-scrollbar-thumb {
    background: rgba(9, 30, 66, 0.13);
    border-radius: 4px;
}

.sidebar::-webkit-scrollbar-thumb:hover {
    background: rgba(9, 30, 66, 0.25);
}

.space-header {
    padding: 1.5rem;
    display: flex;
    align-items: center;
    gap: 1rem;
    border-bottom: 1px solid var(--color-border);
}

.space-icon {
    width: 40px;
    height: 40px;
    background: linear-gradient(135deg, #10b981, #059669);
    border-radius: var(--radius-md);
}

.space-info h3 {
    font-size: 1rem;
    font-weight: 600;
    color: var(--color-text-primary);
    margin-bottom: 0;
}

.space-info h3:hover {
    text-decoration: underline;
}

.space-info p {
    font-size: 0.8rem;
    color: var(--color-text-secondary);
    margin-bottom: 0;
}

.sidebar-section {
    padding: 1rem;
}

.section-title {
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: var(--color-text-muted);
    font-weight: 600;
    margin-bottom: 0;
}

/* Content Area */
.content-area {
    flex: 1;
    background-color: var(--color-bg-secondary);
    overflow-y: auto;
}
</style>
