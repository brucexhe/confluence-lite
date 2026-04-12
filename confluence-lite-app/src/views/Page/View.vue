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
                            <a-menu-item @click="">Attachmens(0)</a-menu-item>
                            <a-menu-item @click="handleMove">View Source</a-menu-item>
                            <a-menu-item @click="handleMove">Export PDF</a-menu-item>
                            <a-menu-item @click="handleMove">Move to</a-menu-item>
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
                        width: 20px;
                        height: 20px;
                        line-height: 20px;
                        font-size: 12px;
                    "
                >
                    {{ pageCreatorInitial }}
                </a-avatar>
                <span class="author">{{ pageCreatorName }}</span>
                <span class="bullet">•</span>
                <span class="date">{{ pageUpdatedTime }}</span>
            </div>

            <div class="page-content" ref="contentRef" v-html="pageContent"></div>

            <!-- Comments Section -->
            <PageComments :userInitial="userInitial" :pageId="pageId" />
        </div>

        <!-- Version History Drawer -->
        <a-drawer
            v-model:open="historyVisible"
            title="版本历史"
            width="520"
            :body-style="{ padding: '0' }"
        >
            <a-spin v-if="versionLoading" style="display:block;padding:2rem;text-align:center;" />
            <template v-else-if="viewingVersion">
                <div class="version-detail-header">
                    <a-button type="link" size="small" @click="viewingVersion = null">← 返回列表</a-button>
                    <span style="font-weight:500;">v{{ viewingVersion.versionNumber }} {{ viewingVersion.title }}</span>
                </div>
                <div style="padding: 0 16px 16px; font-size: 12px; color: #6b778c;">
                    {{ viewingVersion.editor?.displayName || viewingVersion.editor?.username || 'Unknown' }}
                    · {{ formatTime(viewingVersion.createdAt) }}
                </div>
                <div class="version-content" v-html="viewingVersion.content"></div>
            </template>
            <template v-else>
                <div v-if="versions.length === 0" style="padding:2rem;text-align:center;color:#6b778c;">暂无历史版本</div>
                <div v-for="v in versions" :key="v.id" class="version-item">
                    <div class="version-item-main" @click="viewVersion(v.id)">
                        <span class="version-number">v{{ v.versionNumber }}</span>
                        <div class="version-item-info">
                            <span class="version-title">{{ v.title }}</span>
                            <span class="version-meta">
                                {{ v.editor?.displayName || v.editor?.username || 'Unknown' }}
                                · {{ formatTime(v.createdAt) }}
                            </span>
                        </div>
                    </div>
                    <a-button type="text" size="small" danger @click="deleteVersion(v.id)">删除</a-button>
                </div>
            </template>
        </a-drawer>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import PageComments from "../../components/PageComments.vue";
import { pageApi } from "../../api";
import { useAuthStore } from "../../store/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const pageId = computed(() => route.params.id);
const contentRef = ref(null);

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

// 表格排序
function initTableSort() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll('table').forEach(table => {
            // 确保 thead/tbody 结构存在
            let thead = table.querySelector('thead');
            let tbody = table.querySelector('tbody');
            if (!thead && !tbody) {
                // TinyMCE 默认插入的表格没有 thead/tbody，首行全是 <td>
                const rows = Array.from(table.querySelectorAll('tr'));
                if (rows.length < 2) return;
                thead = document.createElement('thead');
                tbody = document.createElement('tbody');
                // 首行 td 转为 th
                rows[0].querySelectorAll('td').forEach(td => {
                    const th = document.createElement('th');
                    th.innerHTML = td.innerHTML;
                    td.replaceWith(th);
                });
                thead.appendChild(rows[0]);
                rows.slice(1).forEach(r => tbody.appendChild(r));
                table.appendChild(thead);
                table.appendChild(tbody);
            }
            if (!thead) thead = table.querySelector('thead');
            if (!tbody) tbody = table.querySelector('tbody');
            if (!thead || !tbody) return;

            const ths = thead.querySelectorAll('th');
            if (ths.length === 0) return;
            ths.forEach((th, colIndex) => {
                th.setAttribute('data-sortable', '');
                th.classList.remove('sort-asc', 'sort-desc');
                th.addEventListener('click', () => {
                    const rows = Array.from(tbody.querySelectorAll('tr'));
                    const isAsc = th.classList.contains('sort-asc');
                    ths.forEach(h => h.classList.remove('sort-asc', 'sort-desc'));
                    rows.sort((a, b) => {
                        const aText = a.children[colIndex]?.textContent?.trim() || '';
                        const bText = b.children[colIndex]?.textContent?.trim() || '';
                        const aNum = Number(aText), bNum = Number(bText);
                        if (!isNaN(aNum) && !isNaN(bNum)) {
                            return isAsc ? bNum - aNum : aNum - bNum;
                        }
                        return isAsc ? bText.localeCompare(aText) : aText.localeCompare(bText);
                    });
                    rows.forEach(r => tbody.appendChild(r));
                    th.classList.add(isAsc ? 'sort-desc' : 'sort-asc');
                });
            });
        });
    });
}

watch(pageContent, () => {
    initTableSort();
});

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
const handleViewHistory = () => { historyVisible.value = true; loadVersions(); };
const handleWatch = () => console.log('Watch Page clicked - TODO');
const handleMove = () => console.log('Move Page clicked - TODO');

// 版本历史
const historyVisible = ref(false);
const versions = ref([]);
const versionLoading = ref(false);
const viewingVersion = ref(null); // 正在查看的版本内容

async function loadVersions() {
    versionLoading.value = true;
    viewingVersion.value = null;
    try {
        const data = await pageApi.getVersions(pageId.value);
        versions.value = data || [];
    } catch (e) {
        console.error('加载版本历史失败:', e);
    } finally {
        versionLoading.value = false;
    }
}

async function viewVersion(versionId) {
    try {
        const data = await pageApi.getVersion(versionId);
        viewingVersion.value = data;
    } catch (e) {
        console.error('加载版本详情失败:', e);
    }
}

async function deleteVersion(versionId) {
    if (!confirm('确定要删除此版本吗？')) return;
    try {
        await pageApi.deleteVersion(versionId);
        await loadVersions();
    } catch (e) {
        console.error('删除版本失败:', e);
    }
}
</script>

<style scoped>
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 40px 0;
    margin-bottom: 1rem;
}

.page-header :deep(.ant-breadcrumb) {
    font-size: 14px;
}

.page-header :deep(.ant-breadcrumb-link),
.page-header :deep(.ant-breadcrumb-separator) {
    color: #0052cc;
}

.page-header :deep(.ant-breadcrumb-link:hover) {
    text-decoration: underline;
    background: none;
}

.page-header :deep(.ant-breadcrumb > span:last-child .ant-breadcrumb-link) {
    color: #172b4d;
    font-weight: 500;
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
    padding: 0px 40px 0;
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

/* Confluence-style Table */
:deep(.page-content table) {
    border-collapse: collapse !important;
    margin: 16px 0;
    border: 1px solid #dfe1e6 !important;
    font-size: 14px;
}

:deep(.page-content table th),
:deep(.page-content table td) {
    border: 1px solid #dfe1e6 !important;
    padding: 8px 12px;
    text-align: left;
    vertical-align: top;
    line-height: 1.5;
}

:deep(.page-content table th) {
    background: #f4f5f7 center right no-repeat;
    color: #172b4d;
    font-weight: 600;
    
    cursor: pointer;
    padding-right: 24px;
}

:deep(.page-content table th[data-sortable]) {
    user-select: none;
    white-space: nowrap;
    position: relative;
}

:deep(.page-content table th[data-sortable]::after) {
    content: "";
    position: absolute;
    right: 8px;
    top: 50%;
    transform: translateY(-50%);
    border-left: 4px solid transparent;
    border-right: 4px solid transparent;
    border-bottom: 5px solid #97a0af;
    opacity: 0;
}

:deep(.page-content table th[data-sortable]:hover::after) {
    opacity: 1;
}

:deep(.page-content table th.sort-asc::after) {
    border-bottom: none;
    border-top: 5px solid #0052cc;
    opacity: 1;
}

:deep(.page-content table th.sort-desc::after) {
    border-bottom-color: #0052cc;
    opacity: 1;
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

/* Version History Drawer */
.version-item {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 12px 16px;
    border-bottom: 1px solid #f0f0f0;
}

.version-item:last-child {
    border-bottom: none;
}

.version-item-main {
    display: flex;
    align-items: center;
    gap: 12px;
    cursor: pointer;
    flex: 1;
}

.version-item-main:hover .version-title {
    color: #0052cc;
}

.version-number {
    font-size: 12px;
    font-weight: 600;
    color: #0052cc;
    background: #deebff;
    padding: 2px 8px;
    border-radius: 3px;
    flex-shrink: 0;
}

.version-item-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
}

.version-title {
    font-size: 14px;
    color: #172b4d;
    font-weight: 500;
}

.version-meta {
    font-size: 12px;
    color: #6b778c;
}

.version-detail-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 16px;
    border-bottom: 1px solid #f0f0f0;
}

.version-content {
    padding: 16px;
    font-size: 14px;
    line-height: 1.714;
    color: #172b4d;
}
</style>
