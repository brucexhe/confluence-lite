<template>
    <div class="page-wrapper">
        <!-- Page Header with Breadcrumb and Actions -->
        <div class="page-header">
            <a-breadcrumb>
                <a-breadcrumb-item>
                    <router-link :to="`/${$route.params.spaceKey}`">{{ spaceName }}</router-link>
                </a-breadcrumb-item>
                <a-breadcrumb-item v-for="crumb in parentCrumbs" :key="crumb.id">
                    <router-link :to="`/${$route.params.spaceKey}/page/${crumb.id}`">{{ crumb.title }}</router-link>
                </a-breadcrumb-item>
            </a-breadcrumb>
            <div class="page-actions">
                <a-button @click="enterEditMode">
                    <span style="font-size: 14px">Edit</span>
                </a-button>
                <a-button @click="handleShare">
                    <span style="font-size: 14px">Share</span>
                </a-button>
                <a-dropdown>
                    <a-button>
                        <span style="font-size: 16px; font-weight: bold; line-height: 1">⋮</span>
                    </a-button>
                    <template #overlay>
                        <a-menu>
                            <a-menu-item @click="handleViewHistory">View History</a-menu-item>
                            <a-menu-item @click="handleWatch">Watch Page</a-menu-item>
                            <a-menu-item @click="handleMove">Move Page</a-menu-item>
                            <a-menu-divider />
                            <a-menu-item @click="handleDelete" danger>Delete</a-menu-item>
                        </a-menu>
                    </template>
                </a-dropdown>
            </div>
        </div>

        <!-- Viewing Mode -->
        <div class="page-view">
            <h1 class="page-title">{{ pageTitle }}</h1>

            <div class="page-meta">
                <a-avatar
                    style="
                        background-color: #3b82f6;
                        margin-right: 12px;
                        width: 24px;
                        height: 24px;
                        line-height: 24px;
                        font-size: 12px;
                    "
                >
                    {{ pageCreatorInitial }}
                </a-avatar>
                <span class="author">{{ pageCreatorName }}</span>
                <span class="bullet">•</span>
                <span class="date">{{ pageUpdatedTime }}</span>
            </div>

            <div class="page-content" v-html="pageContent"></div>

            <!-- Comments Section -->
            <PageComments :userInitial="userInitial" />
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import PageComments from "../../components/PageComments.vue";
import { pageApi } from "../../api";
import { useAuthStore } from "../../store/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const pageId = computed(() => route.params.id);

const userInitial = computed(() => {
    return authStore.user?.name?.charAt(0)?.toUpperCase() || 'U';
});

// 页面数据
const pageTitle = ref("");
const pageContent = ref("");
const pageCreatorName = ref("");
const pageCreatorInitial = ref("U");
const pageUpdatedTime = ref("");

// 面包屑空间名
const spaceName = computed(() => {
    const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
    const key = route.params.spaceKey
    const space = spaces.find(s => s.key === key)
    return space?.name || key || ''
});

// 面包屑：从页面树中提取父级链
const pageTreeMap = ref(new Map())
const parentCrumbs = computed(() => {
    const id = Number(pageId.value)
    const crumbs = []
    let currentId = id
    const visited = new Set()
    while (currentId) {
        const node = pageTreeMap.value.get(currentId)
        if (!node || visited.has(currentId)) break
        visited.add(currentId)
        if (node.id !== id) {
            crumbs.unshift({ id: node.id, title: node.title })
        }
        currentId = node.parentId
    }
    return crumbs
})

async function loadPageTree() {
    const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
    const key = route.params.spaceKey
    const space = spaces.find(s => s.key === key)
    if (!space?.id) return
    try {
        const tree = await pageApi.getTree(space.id)
        const map = new Map()
        function walk(nodes) {
            for (const node of nodes) {
                map.set(node.id, node)
                if (node.children?.length) walk(node.children)
            }
        }
        walk(tree || [])
        pageTreeMap.value = map
    } catch { /* ignore */ }
}

// 加载页面数据
const loadPageData = async () => {
    try {
        const data = await pageApi.getById(pageId.value);
        if (data) {
            pageTitle.value = data.title || "";
            pageContent.value = data.content || "";
            pageCreatorName.value = data.creator?.displayName || data.creator?.username || "Unknown";
            pageCreatorInitial.value = pageCreatorName.value.charAt(0).toUpperCase();
            pageUpdatedTime.value = formatTime(data.updatedAt);
        }
    } catch (e) {
        console.error("加载页面失败:", e);
    }
};

function formatTime(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return '刚刚';
    if (minutes < 60) return `${minutes} 分钟前`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} 小时前`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days} 天前`;
    return date.toLocaleDateString('zh-CN');
}

onMounted(() => {
    loadPageData();
    loadPageTree();
});

watch(pageId, () => {
    loadPageData();
    loadPageTree();
});

const enterEditMode = () => {
    router.push({ path: `/${route.params.spaceKey}/page/${pageId.value}/edit` });
};

const handleDelete = async () => {
    if (!confirm('确定要删除此页面吗？')) return;
    try {
        await pageApi.remove(pageId.value);
        router.push(`/${route.params.spaceKey}`);
    } catch (e) {
        console.error("删除页面失败:", e);
    }
};

const handleShare = () => console.log('Share clicked - TODO');
const handleViewHistory = () => console.log('View History clicked - TODO');
const handleWatch = () => console.log('Watch Page clicked - TODO');
const handleMove = () => console.log('Move Page clicked - TODO');
</script>

<style scoped>
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 2rem 0;
    margin-bottom: 1rem;
}

.page-header :deep(.ant-breadcrumb) {
    font-size: 14px;
}

.page-header :deep(.ant-breadcrumb-link) {
    color: #42526e;
}

.page-header :deep(.ant-breadcrumb-link:hover) {
    color: #0052cc;
}

.page-actions {
    display: flex;
    gap: 0.5rem;
}

.page-actions :deep(.ant-btn) {
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

.page-actions :deep(.ant-btn:hover) {
    background-color: rgba(9, 30, 66, 0.08) !important;
    color: #172b4d !important;
}

.page-view {
    max-width: 900px;
    padding: 10px 2rem 0;
    animation: fadeIn 0.3s ease-in-out;
}

.page-title {
    font-size: 28px;
    font-weight: 500;
    color: #172b4d;
    margin-top: 0;
    margin-bottom: 8px;
    letter-spacing: -0.01em;
    line-height: 1.25;
}

.page-meta {
    display: flex;
    align-items: center;
    color: #6b778c;
    font-size: 14px;
    margin-bottom: 24px;
}

.author {
    font-weight: 500;
    color: #42526e;
    cursor: pointer;
}

.author:hover {
    text-decoration: underline;
}

.bullet {
    margin: 0 8px;
    color: #dfe1e6;
}

.date {
    color: #6b778c;
}

:deep(.page-content .lead-text) {
    font-size: 14px;
    color: #172b4d;
    line-height: 1.714;
    margin-bottom: 16px;
}
:deep(.page-content ul),
:deep(.page-content ol) {
    padding-left: 1.5em;
    margin-bottom: 12px;
}
:deep(.page-content li) {
    font-size: 14px;
    line-height: 1.714;
    color: #172b4d;
    margin-bottom: 4px;
}
:deep(.page-content h2) {
    font-size: 20px;
    font-weight: 500;
    color: #172b4d;
    margin-top: 24px;
    margin-bottom: 12px;
}
:deep(.page-content p) {
    font-size: 14px;
    margin-bottom: 12px;
    line-height: 1.714;
    color: #172b4d;
}
:deep(.page-content pre) {
    background-color: #f4f5f7;
    border-radius: 3px;
    padding: 16px;
    margin: 16px 0;
    font-family:
        SFMono-Regular,
        Consolas,
        Liberation Mono,
        Menlo,
        monospace;
    font-size: 14px;
    color: #172b4d;
    border: 1px solid #dfe1e6;
}

@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(5px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
</style>
