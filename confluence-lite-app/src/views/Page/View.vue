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
                            <a-menu-item @click="handleViewAttachments">Attachmens({{ attachmentCount }})</a-menu-item>
                            <a-menu-item @click="handleViewSource">View Source</a-menu-item>
                            <a-menu-item @click="handleExportPdf">Export PDF</a-menu-item>
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
            <h1 class="page-title bold">{{ pageTitle }}</h1>

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
        <PageVersionHistory
            v-model:open="historyVisible"
            :pageId="pageId"
            @restored="loadPageData"
        />

        <!-- Attachments Drawer -->
        <PageAttachments
            v-model:open="attachmentsVisible"
            :pageId="pageId"
            @changed="loadAttachmentCount"
        />

        <!-- View Source Modal -->
        <a-modal
            v-model:open="sourceVisible"
            title="View Source"
            :width="800"
            :footer="null"
        >
            <div class="source-viewer">
                <pre><code>{{ pageContent }}</code></pre>
            </div>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import PageComments from "../../components/PageComments.vue";
import PageVersionHistory from "../../components/PageVersionHistory.vue";
import PageAttachments from "../../components/PageAttachments.vue";
import { pageApi, attachmentApi } from "../../api";
import { useAuthStore } from "../../store/auth";
import { usePageTreeStore } from "../../store/pageTree";
import Prism from "prismjs";
import "prismjs/themes/prism.css";
import "prismjs/plugins/line-numbers/prism-line-numbers.css";
import "prismjs/plugins/line-numbers/prism-line-numbers.js";
import "prismjs/components/prism-csharp";
import "prismjs/components/prism-go"; 
import "prismjs/components/prism-java";
import "prismjs/components/prism-python";
import "prismjs/components/prism-typescript";
import "prismjs/components/prism-sql";
import "prismjs/components/prism-bash";
import "prismjs/components/prism-yaml";
import "prismjs/components/prism-json"; 

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const pageTreeStore = usePageTreeStore();
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
const pageTreeMap = computed(() => {
    const map = new Map()
    const treeData = pageTreeStore.currentTreeData
    function walk(nodes, parentId = null) {
        for (const node of nodes) {
            map.set(node.id, { ...node, parentId })
            if (node.children?.length) walk(node.children, node.id)
        }
    }
    walk(treeData || [])
    return map
})

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
            nextTick(() => highlightCode());
        }
    } catch (e) {
        console.error("加载页面失败:", e);
    }
};

// 加载附件数量
const loadAttachmentCount = async () => {
    try {
        const attachments = await attachmentApi.getListByPage(pageId.value);
        attachmentCount.value = attachments?.length || 0;
    } catch (e) {
        console.error("加载附件数量失败:", e);
        attachmentCount.value = 0;
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

// 为代码块添加行号 class 并执行语法高亮
function highlightCode() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll('pre[class*="language-"]').forEach(pre => {
            pre.classList.add('line-numbers');
            if (!pre.querySelector('.code-copy-btn')) {
                const btn = document.createElement('button');
                btn.className = 'code-copy-btn';
                btn.textContent = 'Copy';
                btn.onclick = () => {
                    const code = pre.querySelector('code');
                    navigator.clipboard.writeText(code?.textContent || pre.textContent || '').then(() => {
                        btn.textContent = 'Copied!';
                        setTimeout(() => { btn.textContent = 'Copy'; }, 2000);
                    });
                };
                pre.style.position = 'relative';
                pre.appendChild(btn);
            }
        });
        Prism.highlightAll();
    });
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
    highlightCode();
});

onMounted(() => {
    loadPageData();
    loadAttachmentCount();
});

watch(pageId, () => {
    loadPageData();
    loadAttachmentCount();
});

const enterEditMode = () => {
    router.push({ path: `/${route.params.spaceKey}/page/${pageId.value}/edit` });
};

const handleDelete = async () => {
    if (!confirm('确定要删除此页面吗？')) return;
    try {
        await pageApi.remove(pageId.value);
        // Invalidate cache so the tree reflects the deletion
        const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
        const space = spaces.find(s => s.key === route.params.spaceKey)
        if (space?.id) {
            pageTreeStore.invalidateWorkspace(space.id)
        }
        router.push(`/${route.params.spaceKey}`);
    } catch (e) {
        console.error("删除页面失败:", e);
    }
};

const handleShare = () => console.log('Share clicked - TODO');
const handleViewHistory = () => { historyVisible.value = true };
const handleViewSource = () => { sourceVisible.value = true };
const handleViewAttachments = () => { attachmentsVisible.value = true };
const handleExportPdf = () => console.log('Export PDF clicked - TODO');
const handleWatch = () => console.log('Watch Page clicked - TODO');
const handleMove = () => console.log('Move Page clicked - TODO');

// 版本历史
const historyVisible = ref(false);
// 附件
const attachmentsVisible = ref(false);
// View Source
const sourceVisible = ref(false);
// 附件数量
const attachmentCount = ref(0);
</script>

<style scoped>
.page-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 10px 40px 0;
    margin-bottom: 5px;
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
    font-weight: 600;
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
    font-size: 12px;
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
    word-break: break-word;
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
    overflow-x: auto;
}
:deep(.page-content pre code) {
    background: none;
    padding: 0;
    border-radius: 0;
    font-size: inherit;
    color: inherit;
}
:deep(.page-content pre[class*="language-"]) { 
    padding-left: 3.8em;
}
:deep(.page-content pre[class*="language-"] .line-numbers-rows) {
    border-right: 1px solid #dfe1e6;
}
:deep(.page-content pre .code-copy-btn) {
    position: absolute;
    top: 4px;
    right: 4px;
    background: rgba(255,255,255,0.85);
    border: 1px solid #dfe1e6;
    border-radius: 3px;
    padding: 2px 8px;
    font-size: 12px;
    color: #42526e;
    cursor: pointer;
    opacity: 0;
    transition: opacity 0.2s;
    z-index: 1;
}
:deep(.page-content pre:hover .code-copy-btn) {
    opacity: 1;
}
:deep(.page-content :not(pre) > code) {
    background: #f4f5f7;
    padding: 2px 6px;
    border-radius: 3px;
    font-family:
        SFMono-Regular,
        Consolas,
        Liberation Mono,
        Menlo,
        monospace;
    font-size: 13px;
    color: #c7254e;
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

/* Source Viewer */
.source-viewer {
    max-height: 500px;
    overflow: auto;
    background-color: #f4f5f7;
    border-radius: 4px;
    padding: 16px;
}

.source-viewer pre {
    margin: 0;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
    font-size: 13px;
    line-height: 1.5;
    color: #172b4d;
    white-space: pre-wrap;
    word-wrap: break-word;
}

.source-viewer code {
    background: none;
    padding: 0;
    border: none;
}
</style>
