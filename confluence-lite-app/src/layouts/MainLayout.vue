<template>
  <div class="layout-wrapper" :class="{ 'is-resizing': isResizing }">
    <!-- Top Navigation -->
    <header class="top-navbar">
      <div class="nav-left">
        <div class="logo"></div>
        <span class="nav-title">Confluence Lite</span>
        <nav class="nav-links">
          <a-dropdown :trigger="['click']" placement="bottomLeft">
            <a class="nav-link active dropdown-link" @click.prevent>
              Spaces
              <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" style="margin-left: 2px; margin-top: 1px;"><polyline points="6 9 12 15 18 9"></polyline></svg>
            </a>
            <template #overlay>
              <a-menu style="width: 280px; padding: 8px 0; border-radius: 3px; box-shadow: 0 4px 8px -2px rgba(9,30,66,0.25), 0 0 1px rgba(9,30,66,0.31);">
                <div style="padding: 4px 16px 8px; font-size: 11px; font-weight: 700; color: #6b778c; text-transform: uppercase;">Recent Spaces</div>
                <a-menu-item key="1" style="padding: 8px 16px;">
                  <div style="display: flex; align-items: center; gap: 12px;">
                    <div style="width: 32px; height: 32px; background: linear-gradient(135deg, #10B981, #059669); border-radius: 3px;"></div>
                    <div>
                      <div style="font-weight: 500; color: #172b4d; font-size: 14px; line-height: 1.2;">Engineering Space</div>
                      <div style="font-size: 12px; color: #6b778c;">Knowledge Base</div>
                    </div>
                  </div>
                </a-menu-item>
                <a-menu-item key="2" style="padding: 8px 16px;">
                  <div style="display: flex; align-items: center; gap: 12px;">
                    <div style="width: 32px; height: 32px; background: linear-gradient(135deg, #3B82F6, #2563EB); border-radius: 3px;"></div>
                    <div>
                      <div style="font-weight: 500; color: #172b4d; font-size: 14px; line-height: 1.2;">Marketing & Design</div>
                      <div style="font-size: 12px; color: #6b778c;">Team Space</div>
                    </div>
                  </div>
                </a-menu-item>
                <a-menu-divider />
                <a-menu-item key="3" style="padding: 4px 16px;">
                  <span style="color: #0052cc; font-size: 14px; font-weight: 500;">View all spaces</span>
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
          <a-input-search
            placeholder="Search..."
            class="confluence-search"
            style="width: 200px;"
          />
        </div>
        <button class="create-btn">Create</button>
        <div class="user-profile" @click="handleLogout" title="Click to logout">
          <a-avatar style="background-color: #ffffff; color: #0049b0; cursor: pointer; width: 28px; height: 28px; line-height: 28px; font-size: 14px;">
            {{ userInitials }}
          </a-avatar>
        </div>
      </div>
    </header>

    <div class="main-container">
      <!-- Sidebar Navigation -->
      <aside class="sidebar" :style="{ width: sidebarWidth + 'px', minWidth: sidebarWidth + 'px' }">
        <div class="space-header">
          <div class="space-icon"></div>
          <div class="space-info">
            <h3>Engineering Space</h3>
            <p>Knowledge Base</p>
          </div>
        </div>
        
        <div class="sidebar-section">
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem; padding: 0 0.5rem;">
            <h4 class="section-title">Pages</h4>
            <a-button type="text" size="small" style="color: var(--color-text-secondary);">+</a-button>
          </div>
          <PageTree />
        </div>

        <!-- Drag Handle for resizing sidebar -->
        <div class="sidebar-resizer" @mousedown="startResize"></div>
      </aside>

      <!-- Main Content Area -->
      <main class="content-area">
        <div class="content-header" v-show="!route?.query?.edit">
          <a-breadcrumb>
            <a-breadcrumb-item>Engineering Space</a-breadcrumb-item>
            <a-breadcrumb-item>Overview</a-breadcrumb-item>
          </a-breadcrumb>
          <div class="page-actions" v-if="!route?.query?.edit">
            <a-button @click="router.push({ query: { edit: 'true' } })"><span style="font-size: 14px">Edit</span></a-button>
            <a-button><span style="font-size: 14px">Share</span></a-button>
            <a-button><span style="font-size: 16px; font-weight: bold; line-height: 1;">...</span></a-button>
          </div>
        </div>
        
        <div class="page-content">
          <router-view />
        </div>
      </main>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onUnmounted } from 'vue'
import { useAuthStore } from '../store/auth'
import PageTree from '../components/PageTree.vue'
import { useRoute, useRouter } from 'vue-router'

const authStore = useAuthStore()
const route = useRoute()
const router = useRouter()

const userInitials = computed(() => {
  if (authStore.user && authStore.user.name) {
    return authStore.user.name.charAt(0).toUpperCase()
  }
  return 'U'
})

const handleLogout = () => {
  authStore.logout()
}

// Sidebar Resize Logic
const sidebarWidth = ref(260)
const isResizing = ref(false)

const startResize = (e) => {
  isResizing.value = true
  document.addEventListener('mousemove', handleMouseMove)
  document.addEventListener('mouseup', stopResize)
  document.body.style.userSelect = 'none'
}

const handleMouseMove = (e) => {
  if (!isResizing.value) return
  const newWidth = e.clientX
  if (newWidth >= 200 && newWidth <= 800) {
    sidebarWidth.value = newWidth
  }
}

const stopResize = () => {
  isResizing.value = false
  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('mouseup', stopResize)
  document.body.style.userSelect = ''
}

onUnmounted(() => {
  document.removeEventListener('mousemove', handleMouseMove)
  document.removeEventListener('mouseup', stopResize)
})
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
  box-shadow: 0 1px 2px rgba(0,0,0,0.1);
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
  background-color: rgba(255, 255, 255, 0.15);
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
  background-color: rgba(255, 255, 255, 0.25);
}

.user-profile {
  cursor: pointer;
  display: flex;
  align-items: center;
  margin-left: 0.25rem;
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

.sidebar-resizer:hover, .layout-wrapper.is-resizing .sidebar-resizer {
  background-color: #0052CC; /* Confluence blue hover */
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
  background: linear-gradient(135deg, #10B981, #059669);
  border-radius: var(--radius-md);
}

.space-info h3 {
  font-size: 1rem;
  font-weight: 600;
  color: var(--color-text-primary);
  margin-bottom: 0;
}

.space-info p {
  font-size: 0.8rem;
  color: var(--color-text-secondary);
  margin-bottom: 0;
}

.sidebar-section {
  padding: 1rem;
  flex: 1;
  overflow-y: auto;
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

.content-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 1rem 3rem 0.75rem 3rem;
}

.page-actions {
  display: flex;
  gap: 0.5rem;
}

:deep(.page-actions .ant-btn) {
  border: none !important;
  background-color: rgba(9, 30, 66, 0.04) !important;
  color: #42526e !important;
  border-radius: 3px !important;
  font-weight: 500 !important;
  height: 32px !important;
  padding: 4px 12px !important;
  box-shadow: none !important;
  transition: background-color 0.1s !important;
}

:deep(.page-actions .ant-btn:hover) {
  background-color: rgba(9, 30, 66, 0.08) !important;
  color: #172b4d !important;
}
</style>
