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
                    <span style="font-size: 14px">{{ $t("page.edit") }}</span>
                </a-button>
                <a-button @click="handleShare">
                    <span style="font-size: 14px">{{ $t("page.share") }}</span>
                </a-button>
                <a-dropdown>
                    <a-button>
                        <span style="font-size: 16px; font-weight: bold; line-height: 1">⋮</span>
                    </a-button>
                    <template #overlay>
                        <a-menu>
                            <a-menu-item @click="handleViewHistory">{{ $t("page.viewHistory") }}</a-menu-item>
                            <a-menu-item @click="handleViewAttachments">
                                {{ $t("page.attachments") }}({{ attachmentCount }})
                            </a-menu-item>
                            <a-menu-item @click="handleViewSource">{{ $t("page.viewSource") }}</a-menu-item>
                            <a-menu-item @click="handleExportPdf">{{ $t("page.exportPdf") }}</a-menu-item>
                            <a-menu-item @click="handleMove">{{ $t("page.moveTo") }}</a-menu-item>
                            <a-menu-divider />
                            <a-menu-item @click="handleDelete" danger>{{ $t("page.delete") }}</a-menu-item>
                        </a-menu>
                    </template>
                </a-dropdown>
            </div>
        </div>

        <!-- Viewing Mode -->
        <div class="page-view">
            <a-skeleton active :loading="loading" :title="{ width: '30%' }" :paragraph="false">
                <h1 class="page-title bold">{{ pageTitle }}</h1>
            </a-skeleton>

            <a-skeleton active :loading="loading" :title="{ width: '10%' }" :paragraph="false">
                <div class="page-meta">
                    <span>{{ $t("page.updatedBy", { name: pageCreatorName }) }}</span>
                    <span class="date">{{ pageUpdatedTime }}</span>
                </div>
            </a-skeleton>

            <a-skeleton active :loading="loading" :title="false" :paragraph="{ rows: 1 }">
                <div class="page-content" ref="contentRef" v-html="pageContent"></div>
            </a-skeleton>

            <!-- Comments Section -->
            <PageComments :pageId="pageId" />
        </div>

        <!-- Version History Drawer -->
        <PageVersionHistory v-model:open="historyVisible" :pageId="pageId" @restored="loadPageData" />

        <!-- Attachments Drawer -->
        <PageAttachments v-model:open="attachmentsVisible" :pageId="pageId" @changed="loadAttachmentCount" />

        <!-- View Source Modal -->
        <a-modal
            v-model:open="sourceVisible"
            :title="$t('page.viewSource')"
            :width="isMobile ? '95%' : 800"
            :footer="null"
        >
            <div class="source-viewer">
                <pre><code>{{ pageContent }}</code></pre>
            </div>
        </a-modal>

        <!-- 图片预览组件 -->
        <ImagePreview
            v-model:open="imagePreviewOpen"
            :src="currentImageSrc"
            :images="pageImages"
            v-model:currentIndex="currentImageIndex"
        />

        <!-- Office 文件预览组件 -->
        <OfficePreview v-model:open="officePreviewOpen" :filePath="officeFilePath" :fileName="officeFileName" />

        <!-- 视频预览组件 -->
        <VideoPreview v-model:open="videoPreviewOpen" :src="videoSrc" :fileName="videoFileName" />

        <!-- Share Modal -->
        <a-modal
            v-model:open="shareVisible"
            :title="$t('page.sharePage')"
            :width="isMobile ? '95%' : 520"
            :footer="null"
            @cancel="shareVisible = false"
        >
            <!-- Create form -->
            <div v-if="!createdShare">
                <a-form layout="vertical">
                    <a-form-item :label="$t('page.shareType')">
                        <a-radio-group v-model:value="shareForm.shareType">
                            <a-radio value="anonymous">{{ $t("page.anyoneWithLink") }}</a-radio>
                            <a-radio value="user">{{ $t("page.specificUser") }}</a-radio>
                        </a-radio-group>
                    </a-form-item>

                    <a-form-item v-if="shareForm.shareType === 'user'" :label="$t('page.selectUser')">
                        <a-select
                            v-model:value="shareForm.sharedWithId"
                            show-search
                            :filter-option="false"
                            :loading="userSearchLoading"
                            :placeholder="$t('page.searchUser')"
                            @search="handleUserSearch"
                        >
                            <a-select-option v-for="u in userList" :key="u.value" :value="u.value">
                                {{ u.label }}
                            </a-select-option>
                        </a-select>
                    </a-form-item>

                    <a-form-item :label="$t('page.accessPassword')">
                        <a-input-password
                            v-model:value="shareForm.visitPassword"
                            :placeholder="$t('page.leaveBlankNoPassword')"
                        />
                    </a-form-item>

                    <a-form-item :label="$t('page.expirationTime')">
                        <a-date-picker
                            v-model:value="shareForm.expireAt"
                            show-time
                            :placeholder="$t('page.leaveBlankNeverExpire')"
                            style="width: 100%"
                        />
                    </a-form-item>

                    <a-form-item :label="$t('page.allowEdit')">
                        <a-switch v-model:checked="shareForm.allowEdit" />
                    </a-form-item>

                    <a-button
                        type="primary"
                        block
                        :loading="shareLoading"
                        @click="handleCreateShare"
                        style="background-color: #0052cc"
                    >
                        {{ $t("page.createShareLink") }}
                    </a-button>
                </a-form>
            </div>

            <!-- Created result -->
            <div v-else class="share-result">
                <div class="share-result-icon">
                    <svg width="40" height="40" viewBox="0 0 24 24" fill="none" stroke="#36b37e" stroke-width="2">
                        <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14" />
                        <polyline points="22 4 12 14.01 9 11.01" />
                    </svg>
                </div>
                <h3 style="color: #172b4d; margin: 12px 0 8px">{{ $t("page.shareLinkCreated") }}</h3>
                <div class="share-link-box">
                    <input type="text" readonly :value="shareLink" class="share-link-input" />
                    <a-button type="primary" size="small" @click="copyShareLink" style="background-color: #0052cc">
                        {{ $t("page.copyLink") }}
                    </a-button>
                </div>
                <div class="share-link-info">
                    <span v-if="createdShare.hasPassword">{{ $t("page.passwordProtected") }}</span>
                    <span v-if="createdShare.expireAt">
                        {{ $t("page.validUntil", { time: new Date(createdShare.expireAt).toLocaleString(locale) }) }}
                    </span>
                    <span v-if="!createdShare.expireAt">{{ $t("page.neverExpires") }}</span>
                </div>
            </div>
        </a-modal>

        <!-- Scroll to Top Button -->
        <transition name="fade-up">
            <button v-if="showScrollTop" class="scroll-top-btn" @click="scrollToTop" :title="$t('page.backToTop')">
                <svg width="20" height="20" viewBox="0 0 20 20" fill="none">
                    <path d="M10 4L4 10h4v6h4v-6h4L10 4z" fill="currentColor" />
                </svg>
            </button>
        </transition>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted, nextTick, inject } from "vue";
import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { useI18n } from "vue-i18n";
import PageComments from "../../components/PageComments.vue";
import PageVersionHistory from "../../components/PageVersionHistory.vue";
import PageAttachments from "../../components/PageAttachments.vue";
import UserAvatar from "../../components/UserAvatar.vue";
import ImagePreview from "../../components/ImagePreview.vue";
import OfficePreview from "../../components/OfficePreview.vue";
import VideoPreview from "../../components/VideoPreview.vue";
import { pageApi, attachmentApi, recentApi, shareApi } from "../../api";
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

// Mobile detection
const isMobile = ref(false);
function checkMobile() {
    isMobile.value = window.innerWidth <= 768;
}

// 从 MainLayout 注入 setNotFound 方法
const setNotFound = inject("setNotFound");
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const pageTreeStore = usePageTreeStore();
const { t, locale } = useI18n();
const pageId = computed(() => route.params.id);
const contentRef = ref(null);
const loading = ref(true);

// 页面数据
const pageTitle = ref("");
const pageContent = ref("");
const pageCreator = ref(null);
const pageCreatorName = ref("");
const pageUpdatedTime = ref("");

// 面包屑空间名
const spaceName = computed(() => {
    const spaces = JSON.parse(localStorage.getItem("auth_spaces") || "[]");
    const key = route.params.spaceKey;
    const space = spaces.find((s) => s.key === key);
    return space?.name || key || "";
});

// 面包屑：从页面树中提取父级链
const pageTreeMap = computed(() => {
    const map = new Map();
    const treeData = pageTreeStore.currentTreeData;
    function walk(nodes, parentId = null) {
        for (const node of nodes) {
            map.set(node.id, { ...node, parentId });
            if (node.children?.length) walk(node.children, node.id);
        }
    }
    walk(treeData || []);
    return map;
});

const parentCrumbs = computed(() => {
    const id = Number(pageId.value);
    const crumbs = [];
    let currentId = id;
    const visited = new Set();
    while (currentId) {
        const node = pageTreeMap.value.get(currentId);
        if (!node || visited.has(currentId)) break;
        visited.add(currentId);
        if (node.id !== id) {
            crumbs.unshift({ id: node.id, title: node.title });
        }
        currentId = node.parentId;
    }
    return crumbs;
});

// 重置页面数据
const resetPageData = () => {
    pageTitle.value = "";
    pageContent.value = "";
    pageCreator.value = null;
    pageCreatorName.value = "";
    pageUpdatedTime.value = "";
    attachmentCount.value = 0;
};

// 加载页面数据
const loadPageData = async () => {
    loading.value = true;
    resetPageData();
    try {
        const data = await pageApi.getById(pageId.value);
        if (data) {
            pageTitle.value = data.title || "";
            pageContent.value = data.content || "";
            pageCreator.value = data.creator;
            pageCreatorName.value = data.creator?.displayName || data.creator?.username || "Unknown";
            pageUpdatedTime.value = formatTime(data.updatedAt);
            //nextTick(() => highlightCode());
            // 异步记录最近访问
            recentApi.add(pageId.value).catch((err) => console.error("记录最近访问失败:", err));
        }
    } catch (e) {
        console.error("加载页面失败:", e);

        setNotFound(true);
    } finally {
        loading.value = false;
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
    if (!dateStr) return "";
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return t("common.justNow");
    if (minutes < 60) return t("common.minutesAgo", { n: minutes });
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return t("common.hoursAgo", { n: hours });
    const days = Math.floor(hours / 24);
    if (days < 30) return t("common.daysAgo", { n: days });
    return date.toLocaleDateString(locale.value);
}

// 为代码块添加行号 class 并执行语法高亮
function highlightCode() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll('pre[class*="language-"]').forEach((pre) => {
            pre.classList.add("line-numbers");
            if (!pre.querySelector(".code-copy-btn")) {
                const btn = document.createElement("button");
                btn.className = "code-copy-btn";
                btn.textContent = "Copy";
                btn.onclick = () => {
                    const code = pre.querySelector("code");
                    navigator.clipboard.writeText(code?.textContent || pre.textContent || "").then(() => {
                        btn.textContent = "Copied!";
                        setTimeout(() => {
                            btn.textContent = "Copy";
                        }, 2000);
                    });
                };
                pre.style.position = "relative";
                pre.appendChild(btn);
            }
        });
        Prism.highlightAll();
    });
}

// 为 v-html 渲染内容中的图片添加点击事件
function initImagePreview() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;

        // 获取所有图片 URL 列表
        pageImages.value = Array.from(el.querySelectorAll("img")).map((img) => img.src);

        // 为每张图片添加点击事件
        el.querySelectorAll("img").forEach((img, index) => {
            img.style.cursor = "pointer";

            if (!img.dataset.hasPreviewListener) {
                img.addEventListener("click", (e) => {
                    e.preventDefault();
                    e.stopPropagation();
                    currentImageSrc.value = img.src;
                    currentImageIndex.value = index;
                    imagePreviewOpen.value = true;
                });
                img.dataset.hasPreviewListener = "true";
            }
        });
    });
}

// 为 v-html 渲染内容中的 Office 文件链接添加预览事件
function initOfficePreview() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        const officeExtensions = [".docx", ".xlsx", ".pptx", ".pdf"];
        el.querySelectorAll("a.file").forEach((link) => {
            const href = link.getAttribute("href") || "";
            const isOffice = officeExtensions.some((ext) => href.toLowerCase().endsWith(ext));
            if (!isOffice) return;
            if (link.dataset.hasOfficePreview) return;
            link.dataset.hasOfficePreview = "true";
            link.addEventListener("click", (e) => {
                e.preventDefault();
                e.stopPropagation();
                officeFilePath.value = href;
                officeFileName.value = link.textContent?.trim() || href.split("/").pop() || "Document";
                officePreviewOpen.value = true;
            });
        });
    });
}

// 为 v-html 渲染内容中的视频链接添加预览事件
function initVideoPreview() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll("a.video").forEach((link) => {
            const href = link.getAttribute("href") || "";
            if (link.dataset.hasVideoPreview) return;
            link.dataset.hasVideoPreview = "true";
            link.addEventListener("click", (e) => {
                e.preventDefault();
                e.stopPropagation();
                videoSrc.value = href;
                videoFileName.value = link.textContent?.trim() || href.split("/").pop() || "Video";
                videoPreviewOpen.value = true;
            });
        });
    });
}

// 为 v-html 渲染内容中的 a 标签设置新窗口打开
function initExternalLinks() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll("a").forEach((link) => {
            link.setAttribute("target", "_blank");
            link.setAttribute("rel", "noopener noreferrer");
        });
    });
}

// 表格排序
function initTableSort() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll("table").forEach((table) => {
            // 确保 thead/tbody 结构存在
            let thead = table.querySelector("thead");
            let tbody = table.querySelector("tbody");
            if (!thead && !tbody) {
                // TinyMCE 默认插入的表格没有 thead/tbody，首行全是 <td>
                const rows = Array.from(table.querySelectorAll("tr"));
                if (rows.length < 2) return;
                thead = document.createElement("thead");
                tbody = document.createElement("tbody");
                // 首行 td 转为 th
                rows[0].querySelectorAll("td").forEach((td) => {
                    const th = document.createElement("th");
                    th.innerHTML = td.innerHTML;
                    td.replaceWith(th);
                });
                thead.appendChild(rows[0]);
                rows.slice(1).forEach((r) => tbody.appendChild(r));
                table.appendChild(thead);
                table.appendChild(tbody);
            }
            if (!thead) thead = table.querySelector("thead");
            if (!tbody) tbody = table.querySelector("tbody");
            if (!thead || !tbody) return;

            const ths = thead.querySelectorAll("th");
            if (ths.length === 0) return;
            ths.forEach((th, colIndex) => {
                th.setAttribute("data-sortable", "");
                th.classList.remove("sort-asc", "sort-desc");
                th.addEventListener("click", () => {
                    const rows = Array.from(tbody.querySelectorAll("tr"));
                    const isAsc = th.classList.contains("sort-asc");
                    ths.forEach((h) => h.classList.remove("sort-asc", "sort-desc"));
                    rows.sort((a, b) => {
                        const aText = a.children[colIndex]?.textContent?.trim() || "";
                        const bText = b.children[colIndex]?.textContent?.trim() || "";
                        const aNum = Number(aText),
                            bNum = Number(bText);
                        if (!isNaN(aNum) && !isNaN(bNum)) {
                            return isAsc ? bNum - aNum : aNum - bNum;
                        }
                        return isAsc ? bText.localeCompare(aText) : aText.localeCompare(bText);
                    });
                    rows.forEach((r) => tbody.appendChild(r));
                    th.classList.add(isAsc ? "sort-desc" : "sort-asc");
                });
            });
        });
    });
}

watch(pageContent, () => {
    initTableSort();
    highlightCode();
    initImagePreview();
    initOfficePreview();
    initVideoPreview();
    initExternalLinks();
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
    if (!confirm(t("page.confirmDelete"))) return;
    try {
        await pageApi.remove(pageId.value);
        // Invalidate cache so the tree reflects the deletion
        const spaces = JSON.parse(localStorage.getItem("auth_spaces") || "[]");
        const space = spaces.find((s) => s.key === route.params.spaceKey);
        if (space?.id) {
            pageTreeStore.invalidateWorkspace(space.id);
        }
        router.push(`/${route.params.spaceKey}`);
    } catch (e) {
        console.error("删除页面失败:", e);
    }
};

const handleShare = () => {
    shareVisible.value = true;
    createdShare.value = null;
    shareForm.value = {
        shareType: "anonymous",
        sharedWithId: null,
        visitPassword: "",
        expireAt: null,
        allowEdit: false,
    };
};

// Share dialog state
const shareVisible = ref(false);
const shareLoading = ref(false);
const shareForm = ref({
    shareType: "anonymous",
    sharedWithId: null,
    visitPassword: "",
    expireAt: null,
    allowEdit: false,
});
const createdShare = ref(null);
const userList = ref([]);
const userSearchLoading = ref(false);

const shareLink = computed(() => {
    if (!createdShare.value) return "";
    return `${window.location.origin}/share/${createdShare.value.code}`;
});

const handleUserSearch = async (query) => {
    if (!query || query.length < 1) {
        userList.value = [];
        return;
    }
    userSearchLoading.value = true;
    try {
        const { userApi } = await import("../../api");
        const data = await userApi.getList(1, 20);
        userList.value = (data?.items || [])
            .filter((u) => u.username.includes(query) || (u.displayName && u.displayName.includes(query)))
            .map((u) => ({ value: u.id, label: u.displayName || u.username }));
    } catch {
        userList.value = [];
    }
    userSearchLoading.value = false;
};

const handleCreateShare = async () => {
    if (shareForm.value.shareType === "user" && !shareForm.value.sharedWithId) {
        message.warning(t("page.pleaseSelectUser"));
        return;
    }
    shareLoading.value = true;
    try {
        const payload = {
            pageId: Number(pageId.value),
            sharedWithId: shareForm.value.shareType === "user" ? shareForm.value.sharedWithId : null,
            visitPassword: shareForm.value.visitPassword || null,
            expireAt: shareForm.value.expireAt ? shareForm.value.expireAt.toISOString() : null,
            allowEdit: shareForm.value.allowEdit,
        };
        const data = await shareApi.create(payload);
        createdShare.value = data;
        message.success(t("page.shareCreated"));
    } catch (e) {
        message.error(e.message || t("page.createShareFailed"));
    }
    shareLoading.value = false;
};

const copyShareLink = () => {
    navigator.clipboard.writeText(shareLink.value).then(() => {
        message.success(t("page.linkCopied"));
    });
};

const handleViewHistory = () => {
    historyVisible.value = true;
};
const handleViewSource = () => {
    sourceVisible.value = true;
};
const handleViewAttachments = () => {
    attachmentsVisible.value = true;
};
const handleExportPdf = () => console.log("Export PDF clicked - TODO");
const handleWatch = () => console.log("Watch Page clicked - TODO");
const handleMove = () => console.log("Move Page clicked - TODO");

// 版本历史
const historyVisible = ref(false);
// 附件
const attachmentsVisible = ref(false);
// View Source
const sourceVisible = ref(false);
// 附件数量
const attachmentCount = ref(0);
// 图片预览
const imagePreviewOpen = ref(false);
const currentImageSrc = ref("");
const pageImages = ref([]);
const currentImageIndex = ref(0);
// Office 文件预览
const officePreviewOpen = ref(false);
const officeFilePath = ref("");
const officeFileName = ref("");
// 视频预览
const videoPreviewOpen = ref(false);
const videoSrc = ref("");
const videoFileName = ref("");

// Scroll to top
const showScrollTop = ref(false);
let scrollContainer = null;

function handleScroll() {
    if (scrollContainer) {
        showScrollTop.value = scrollContainer.scrollTop > 50;
    }
}

function scrollToTop() {
    if (scrollContainer) {
        scrollContainer.scrollTo({ top: 0, behavior: "smooth" });
    }
}

onMounted(() => {
    checkMobile();
    window.addEventListener("resize", checkMobile);
    scrollContainer = document.querySelector(".content-area");
    if (scrollContainer) {
        scrollContainer.addEventListener("scroll", handleScroll);
    }
});

onUnmounted(() => {
    window.removeEventListener("resize", checkMobile);
    if (scrollContainer) {
        scrollContainer.removeEventListener("scroll", handleScroll);
    }
});
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
:deep(.page-content img) {
    max-width: 100%;
    height: auto;
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
    margin: 0;
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
    background: rgba(255, 255, 255, 0.85);
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

:deep(.page-content table th) {
    user-select: auto;
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
    font-family: "SFMono-Regular", Consolas, "Liberation Mono", Menlo, monospace;
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

/* Scroll to Top Button */
.scroll-top-btn {
    position: fixed;
    bottom: 32px;
    right: 32px;
    width: 40px;
    height: 40px;
    border-radius: 50%;
    border: none;
    background-color: #dfe1e6;
    color: #172b4d;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    transition:
        background-color 0.2s,
        transform 0.2s;
    z-index: 1000;
}

.scroll-top-btn:hover {
    background-color: #c1c7d0;
    transform: scale(1.1);
}

.fade-up-enter-active,
.fade-up-leave-active {
    transition:
        opacity 0.3s,
        transform 0.3s;
}

.fade-up-enter-from,
.fade-up-leave-to {
    opacity: 0;
    transform: translateY(10px);
}

/* Share Modal */
.share-result {
    text-align: center;
    padding: 16px 0;
}

.share-result-icon {
    margin-bottom: 8px;
}

.share-link-box {
    display: flex;
    gap: 8px;
    margin: 16px 0;
}

.share-link-input {
    flex: 1;
    padding: 6px 12px;
    border: 1px solid #dfe1e6;
    border-radius: 4px;
    font-size: 13px;
    color: #172b4d;
    background: #f4f5f7;
}

.share-link-info {
    color: #6b778c;
    font-size: 12px;
    display: flex;
    gap: 12px;
    justify-content: center;
}

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
    .page-header {
        padding: 10px 16px 0;
        flex-wrap: wrap;
    }

    .page-header :deep(.ant-breadcrumb) {
        font-size: 12px;
        max-width: 200px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
    }

    .page-actions {
        gap: 0.25rem;
    }

    .page-actions :deep(.ant-btn) {
        height: 28px !important;
        padding: 4px 8px !important;
    }

    .page-actions :deep(.ant-btn span) {
        font-size: 12px !important;
    }

    .page-view {
        padding: 0 16px;
    }

    .page-title {
        font-size: 22px;
    }

    :deep(.page-content pre[class*="language-"]) {
        padding-left: 1em;
        font-size: 12px;
    }

    .scroll-top-btn {
        bottom: 16px;
        right: 16px;
        width: 36px;
        height: 36px;
    }

    .share-link-box {
        flex-direction: column;
    }

    .share-link-input {
        width: 100%;
    }
}
</style>
