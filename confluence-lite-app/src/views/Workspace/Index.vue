<template>
    <div v-if="notFound" class="not-found-container">
        <div class="not-found-content">
            <div class="error-icon">404</div>
            <h2>{{ $t('notFound.workspaceNotExist') }}</h2>
            <p>{{ $t('notFound.workspaceNotExistMsg', { key: route.params.spaceKey }) }}</p>
            <button class="back-btn" @click="goBack">{{ $t('common.backToHome') }}</button>
        </div>
    </div>
    <div v-else class="workspace-home">
        <!-- 欢迎头部 -->
        <div class="welcome-header">
            <h1 class="workspace-title">{{ workspaceName }}</h1>
            <p class="workspace-desc">{{ workspaceDescription }}</p>
        </div>

        <!-- 内容区域 -->
        <div class="content-section">
            <div class="activity-section">
                <a-card class="activity-card" :bordered="false">
                    <template #title>
                        <span class="card-title">{{ $t('workspace.recentActivity') }}</span>
                    </template>
                    <template #extra>
                        <a-button type="link" size="small" @click="loadActivities">{{ $t('common.refresh') }}</a-button>
                    </template>

                    <a-spin :spinning="loading">
                        <a-list
                            v-if="activities.length > 0"
                            class="activity-list"
                            :data-source="activities"
                            size="small"
                        >
                            <template #renderItem="{ item }">
                                <a-list-item class="activity-item">
                                    <template #avatar>
                                        <UserAvatar
                                            :user="item.user"
                                            :size="24"
                                        />
                                    </template>
                                    <template #default>
                                        <div class="activity-content">
                                            <div class="activity-page">
                                                <a
                                                    v-if="item.workspaceKey && item.pageId"
                                                    :href="`/${item.workspaceKey}/page/${item.pageId}`"
                                                    class="page-link"
                                                >
                                                    <FileText
                                                        :size="15"
                                                        color="#0066ff"
                                                        :stroke-width="1.5"
                                                        class="page-icon"
                                                    />
                                                    {{ item.pageTitle }}
                                                </a>
                                            </div>
                                            <div class="activity-meta">
                                                <span class="activity-user">
                                                    {{ item.user?.displayName || item.user?.username || "Unknown" }}
                                                </span>
                                                <span class="activity-separator">•</span>
                                                <span class="activity-time">{{ formatTime(item.createdAt) }}</span>
                                            </div>
                                        </div>
                                    </template>
                                </a-list-item>
                            </template>
                        </a-list>
                        <a-empty v-else-if="!loading" :description="$t('workspace.noActivity')" class="empty-state">
                            <template #description>
                                <span style="color: #6b778c; font-size: 13px">{{ $t('workspace.noActivityRecord') }}</span>
                            </template>
                        </a-empty>
                    </a-spin>
                </a-card>
            </div>

            <!-- 快速操作 -->
            <div class="quick-actions">
                <a-card :bordered="false" class="quick-card">
                    <template #title>
                        <span class="card-title">{{ $t('workspace.quickActions') }}</span>
                    </template>
                    <div class="action-buttons">
                        <a-button block @click="createPage">
                            <span style="font-size: 14px">{{ $t('workspace.createPage') }}</span>
                        </a-button>
                        <a-button block @click="goToSettings">
                            <span style="font-size: 14px">{{ $t('workspace.spaceSettings') }}</span>
                        </a-button>
                    </div>
                </a-card>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, inject, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { FileText } from "lucide-vue-next";
import { activityApi } from "@/api";
import UserAvatar from "@/components/UserAvatar.vue";

const { t, locale } = useI18n();
const route = useRoute();
const router = useRouter();

const loading = ref(false);
const activities = ref([]);
const notFound = ref(false);

// 从 MainLayout 注入 setNotFound 方法
const setNotFound = inject("setNotFound");

// 路由变化时重置 notFound 状态
watch(
    () => route.path,
    () => {
        notFound.value = false;
    },
);

// 监听空间变化，重新加载活动
watch(
    () => route.params.spaceKey,
    () => {
        notFound.value = false;
        activities.value = [];
        loadActivities();
    },
);

// 从 localStorage 读取空间列表
const spaces = computed(() => {
    return JSON.parse(localStorage.getItem("auth_spaces") || "[]");
});

// 当前空间（根据路由 spaceKey 匹配）
const currentSpace = computed(() => {
    const key = route.params.spaceKey?.toUpperCase();
    return spaces.value.find((s) => s.key.toUpperCase() === key);
});

const workspaceName = computed(() => currentSpace.value?.name || route.params.spaceKey || "");
const workspaceDescription = computed(() => currentSpace.value?.description || t('workspace.home'));

// 如果找不到空间，显示 404
watch(
    currentSpace,
    (newSpace) => {
        if (!newSpace && route.params.spaceKey) {
            setNotFound(true);
        }
    },
    { immediate: true },
);


function formatTime(dateStr) {
    if (!dateStr) return "";
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;

    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return t('common.justNow');
    if (minutes < 60) return t('common.minutesAgo', { n: minutes });

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return t('common.hoursAgo', { n: hours });

    const days = Math.floor(hours / 24);
    if (days < 7) return t('common.daysAgo', { n: days });

    return date.toLocaleDateString(locale.value);
}

async function loadWorkspaceInfo() {
    try {
        const key = route.params.spaceKey;
        if (!key) return;

        const data = await workspaceApi.getByKey(key);
        if (data) {
            workspaceName.value = data.name || key;
            workspaceDescription.value = data.description || t('workspace.home');
        } else {
            setNotFound(true);
        }
    } catch (error) {
        // API请求失败或返回404，显示404内容
        console.error("Failed to load workspace info:", error);
        setNotFound(true);
    }
}

function goBack() {
    router.push("/");
}

async function loadActivities() {
    loading.value = true;
    try {
        const data = await activityApi.getRecent({
            workspaceId: currentSpace.value?.id || null,
            count: 20,
        });

        if (Array.isArray(data)) {
            activities.value = data;
        } else {
            console.warn("Activity API returned unexpected format:", data);
            activities.value = [];
        }
    } catch (error) {
        console.error("Failed to load activities:", error);
        activities.value = [];
    } finally {
        loading.value = false;
    }
}

function createPage() {
    const key = route.params.spaceKey;
    router.push(`/${key}/page/new`);
}

function goToSettings() {
    router.push("/spaces");
}

onMounted(() => {
    loadActivities();
});
</script>

<style scoped>
/* 404 页面样式 */
.not-found-container {
    min-height: calc(100vh - 64px);
    display: flex;
    align-items: center;
    justify-content: center;
    background: #f4f5f7;
}

.not-found-content {
    text-align: center;
    padding: 48px;
    background: white;
    border-radius: 12px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    width: 500px;
}

.error-icon {
    font-size: 72px;
    font-weight: bold;
    color: #0049b0;
    margin-bottom: 16px;
}

.not-found-content h2 {
    color: #172b4d;
    margin: 0 0 8px;
}

.not-found-content p {
    color: #6b778c;
    font-size: 14px;
    margin: 0 0 24px;
}

.back-btn {
    background: #0049b0;
    color: white;
    border: none;
    padding: 10px 24px;
    border-radius: 6px;
    cursor: pointer;
    font-size: 14px;
    transition: background 0.2s;
}

.back-btn:hover {
    background: #0747a6;
}

.workspace-home {
    max-width: 1000px;
    padding: 20px 40px 0;
}

/* 欢迎头部 */
.welcome-header {
    margin-bottom: 16px;
    padding-bottom: 16px;
    border-bottom: 1px solid #dfe1e6;
}

.workspace-title {
    font-size: 24px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 4px 0;
}

.workspace-desc {
    font-size: 13px;
    color: #6b778c;
    margin: 0;
}

/* 内容区域 */
.content-section {
    display: flex;
    gap: 16px;
}

.activity-section {
    flex: 1;
    min-width: 0;
}

.quick-actions {
    width: 200px;
    flex-shrink: 0;
}

/* 卡片样式 */
.activity-card,
.quick-card {
    border-radius: 4px;
    box-shadow: none !important;
}

.activity-card :deep(.ant-card-head),
.quick-card :deep(.ant-card-head) {
    padding: 12px 16px;
    min-height: auto;
    border-bottom: 1px solid #dfe1e6;
}

.activity-card :deep(.ant-card-body),
.quick-card :deep(.ant-card-body) {
    padding: 12px 6px;
}

.card-title {
    font-size: 14px;
    font-weight: 600;
    color: #172b4d;
}

/* 活动列表 */
.activity-list {
    margin: 0;
}

.activity-list :deep(.ant-list-item) {
    border-bottom: none;
    padding: 4px 0;
}

.activity-list :deep(.ant-list-item:last-child) {
    border-bottom: none;
}

.activity-item {
    display: flex;
    align-items: flex-start;
}

.activity-content {
    flex: 1;
    min-width: 0;
    padding-left: 8px;
}

.activity-page {
    font-size: 14px;
    line-height: 1.4;
}

.page-link {
    color: #0052cc;
    text-decoration: none;
    font-weight: 400;
    display: flex;
    align-items: center;
    gap: 4px;
}

.page-icon {
    opacity: 0.6;
    flex-shrink: 0;
}

.page-link:hover {
    text-decoration: underline;
    color: #0052cc;
}

.activity-meta {
    font-size: 12px;
    color: #6b778c;
    display: flex;
    align-items: center;
    padding-left: 20px;
}

.activity-user {
    color: #42526e;
    font-weight: 400;
}

.activity-separator {
    margin: 0 4px;
    color: #dfe1e6;
}

.activity-time {
    color: #6b778c;
}

/* 空状态 */
.empty-state {
    padding: 32px 0;
}

/* 快速操作 */
.action-buttons {
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.action-buttons :deep(.ant-btn) {
    height: 36px;
    font-size: 13px;
    text-align: left;
    padding: 0 12px;
    border-radius: 3px;
}

/* 响应式 */
@media (max-width: 768px) {
    .content-section {
        flex-direction: column;
    }

    .quick-actions {
        width: 100%;
    }

    .workspace-home {
        padding: 12px 16px 0;
    }
}
</style>
