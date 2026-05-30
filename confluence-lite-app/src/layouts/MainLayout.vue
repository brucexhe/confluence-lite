<template>
  <div class="layout-wrapper" :class="{ 'is-resizing': isResizing }">
    <!-- Top Navigation -->
    <TopNavbar />

    <div class="main-container" v-if="!notFound">
      <!-- Sidebar Navigation -->
      <aside
        class="sidebar"
        :style="{ width: sidebarWidth + 'px', minWidth: sidebarWidth + 'px' }"
      >
        <div class="space-header">
          <img
            v-if="currentSpaceIcon && isImageUrl(currentSpaceIcon)"
            class="space-icon-img"
            :src="currentSpaceIcon"
            alt=""
          />
          <div
            v-else
            class="space-icon"
            :style="{
              background: currentSpaceIcon || currentSpaceColor,
              color: '#fff',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              fontWeight: 600,
              fontSize: '14px',
            }"
          >
            {{ currentSpaceInitial }}
          </div>
          <div class="space-info">
            <h3
              @click="navigateToSpace(currentSpaceKey)"
              style="cursor: pointer"
            >
              {{ currentSpaceName }}
            </h3>
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
              padding: 0 0 0 0.2rem;
            "
          >
            <h4 class="section-title">Pages</h4>
           <Settings :size="16"  @click="settingsVisible = true" /> 
          </div>
          <PageTree
            :workspace-id="currentSpace?.id"
            :space-key="currentSpaceKey"
          />
        </div>

        <!-- Drag Handle for resizing sidebar -->
        <div class="sidebar-resizer" @mousedown="startResize">
          <img src="/images/confluence-icon-grab-handle.svg" alt="grab handle" />
        </div>
      </aside>

      <!-- Main Content Area -->
      <main class="content-area">
        <div class="page-content">
          <router-view />
        </div>
      </main>
    </div>
    <div v-else>
      <NotFound />
    </div>
    <PageTreeSettings
      v-model:open="settingsVisible"
      :workspace-id="currentSpace?.id"
      :space-key="currentSpaceKey"
    />
  </div>
</template>

<script setup>
import { computed, ref, onUnmounted, provide, watch } from "vue";
import { useAuthStore } from "../store/auth";
import PageTree from "../components/PageTree.vue";
import PageTreeSettings from "../components/PageTreeSettings.vue";
import TopNavbar from "../components/TopNavbar.vue";
import { useRoute, useRouter } from "vue-router";
import NotFound from "../components/NotFound.vue";
import { Settings } from "lucide-vue-next";
import { getSpaceColorById, getSpaceInitial } from "../utils/workspace";

function isImageUrl(icon) {
  if (!icon) return false;
  return /^(https?:\/\/|data:image\/|\/)/.test(icon);
}

const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();

// 404 状态 - 子组件可以通过 setNotFound 通知父组件显示404
const notFound = ref(false);

// 页面排序设置弹窗
const settingsVisible = ref(false);

function setNotFound(value) {
  notFound.value = value;
  console.log("notFound:" + value);
}

// 路由变化时重置 notFound 状态
watch(
  () => route.path,
  () => {
    notFound.value = false;
  }
);

// 提供给子组件使用
provide("setNotFound", setNotFound);

// 空间列表（从 localStorage 读取）
const spaces = computed(() => {
  return JSON.parse(localStorage.getItem("auth_spaces") || "[]");
});

// 当前空间（根据路由 :spaceKey 匹配）
const currentSpace = computed(() => {
  const key = route.params.spaceKey;
  if (!key) return spaces.value[0] || null;
  return spaces.value.find((s) => s.key === key) || { key, name: key, icon: '' };
});

const currentSpaceName = computed(
  () => currentSpace.value?.name || currentSpace.value?.key || ""
);
const currentSpaceKey = computed(() => currentSpace.value?.key || "");

const currentSpaceColor = computed(() => getSpaceColorById(currentSpace.value?.id));

const currentSpaceIcon = computed(() => currentSpace.value?.icon || "");

const currentSpaceInitial = computed(() =>
  getSpaceInitial(currentSpace.value || { key: "?" })
);

function navigateToSpace(key) {
  router.push(`/${key}`);
}

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
  right: 1px;
  width: 6px;
  height: 100%;
  cursor: col-resize;
  z-index: 100;
  transition: background-color 0.2s;
  display: flex;
  align-items: center;
  justify-content: center;
  user-select: none;
}

.sidebar-resizer img {
  width: 6px;
  height: 14px;
  opacity: 1;
}

.sidebar-resizer:hover,
.layout-wrapper.is-resizing .sidebar-resizer {
  background-color: #eee; /* Confluence blue hover */
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

.space-icon-img {
  width: 40px;
  height: 40px;
  border-radius: var(--radius-md);
  object-fit: cover;
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
