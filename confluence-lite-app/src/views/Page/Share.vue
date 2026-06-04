<template>
    <div class="share-page-wrapper">
        <!-- Header (matches TopNavbar style) -->
        <header class="share-header">
            <div class="share-header-left">
                <a href="/" class="share-site-link">
                    <img v-if="siteLogo" :src="siteLogo" class="share-logo" />
                    <div v-else class="share-logo"></div>
                    <span class="share-site-name">{{ siteName }}</span>
                </a>
            </div>
            <div class="share-header-right">
                <a v-if="!isLoggedIn" href="/login" class="share-header-link">{{ $t('sharePage.login') }}</a>
                <a v-else href="/" class="share-header-link">{{ $t('sharePage.enterMainPage') }}</a>
            </div>
        </header>

        <!-- Main content area -->
        <div class="share-content-area">
            <!-- Loading -->
            <div v-if="loading" class="share-loading">
                <a-spin size="large" />
                <p>{{ $t('sharePage.loading') }}</p>
            </div>

            <!-- Expired -->
            <div v-else-if="errorState === 'expired'" class="share-error-state">
                <div class="share-error-icon">
                    <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="#6b778c" stroke-width="1.5">
                        <circle cx="12" cy="12" r="10"/>
                        <polyline points="12 6 12 12 16 14"/>
                    </svg>
                </div>
                <h2>{{ $t('sharePage.expired') }}</h2>
                <p>{{ $t('sharePage.expiredDesc') }}</p>
            </div>

            <!-- No access -->
            <div v-else-if="errorState === 'no_access'" class="share-error-state">
                <div class="share-error-icon">
                    <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="#6b778c" stroke-width="1.5">
                        <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                        <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                    </svg>
                </div>
                <h2>{{ $t('sharePage.noAccess') }}</h2>
                <p>{{ $t('sharePage.noAccessDesc') }}</p>
                <a href="/login" class="share-error-btn">{{ $t('sharePage.loginAccount') }}</a>
            </div>

            <!-- Not found -->
            <div v-else-if="errorState === 'not_found'" class="share-error-state">
                <div class="share-error-icon">
                    <svg width="64" height="64" viewBox="0 0 24 24" fill="none" stroke="#6b778c" stroke-width="1.5">
                        <circle cx="11" cy="11" r="8"/>
                        <line x1="21" y1="21" x2="16.65" y2="16.65"/>
                        <line x1="8" y1="11" x2="14" y2="11"/>
                    </svg>
                </div>
                <h2>{{ $t('sharePage.notFound') }}</h2>
                <p>{{ $t('sharePage.notFoundDesc') }}</p>
            </div>

            <!-- Password form -->
            <div v-else-if="passwordRequired" class="share-password-form">
                <div class="share-password-card">
                    <div class="share-password-icon">
                        <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="#0052cc" stroke-width="1.5">
                            <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                            <path d="M7 11V7a5 5 0 0 1 10 0v4"/>
                        </svg>
                    </div>
                    <h2>{{ $t('sharePage.passwordRequired') }}</h2>
                    <p>{{ $t('sharePage.passwordRequiredDesc') }}</p>
                    <div class="share-password-input">
                        <a-input-password
                            v-model:value="passwordInput"
                            :placeholder="$t('sharePage.enterPassword')"
                            size="large"
                            @pressEnter="handlePasswordSubmit"
                        />
                    </div>
                    <div v-if="passwordError" class="share-password-error">{{ passwordError }}</div>
                    <a-button
                        type="primary"
                        size="large"
                        block
                        :loading="passwordVerifying"
                        @click="handlePasswordSubmit"
                        style="background-color: #0052cc"
                    >
                        {{ $t('sharePage.verify') }}
                    </a-button>
                </div>
            </div>

            <!-- Edit mode -->
            <div v-else-if="isEditing" class="share-edit-mode">
                <div class="share-edit-header">
                    <input type="text" class="share-edit-title" v-model="editTitle" placeholder="Page Title" />
                    <div class="share-edit-actions">
                        <a-button type="primary" @click="handleSaveEdit" :loading="saveLoading" style="background-color: #0052cc">
                            {{ $t('sharePage.saveBtn') }}
                        </a-button>
                        <a-button @click="cancelEdit">{{ $t('sharePage.cancelBtn') }}</a-button>
                    </div>
                </div>
                <editor v-if="editorReady" v-model="editContent" :init="editorConfig" api-key="no-api-key" />
                <div v-else class="editor-loading">{{ $t('editor.loadingEditor') }}</div>
            </div>

            <!-- Page content -->
            <div v-else-if="pageData" class="share-page-content">
                <div class="share-page-header">
                    <h1 class="share-page-title">{{ pageData.title }}</h1>
                    <a-button v-if="shareInfo?.allowEdit" @click="enterEditMode" class="share-edit-btn">
                        <span style="font-size: 14px">{{ $t('sharePage.edit') }}</span>
                    </a-button>
                </div>
                <div class="share-page-meta">
                    <span>{{ $t('sharePage.createdBy', { name: pageData.creator?.displayName || 'Unknown' }) }}</span>
                    <span class="date">{{ formatTime(pageData.createdAt) }}</span>
                </div>
                <div class="share-page-body" ref="contentRef" v-html="pageData.content"></div>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from "vue";
import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { useI18n } from "vue-i18n";
import Editor from "@tinymce/tinymce-vue";
import { shareApi } from "../../api";
import { useSiteInfo } from "../../store/site";
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
const { t, locale } = useI18n();
const { siteName, siteLogo } = useSiteInfo();
const shareCode = computed(() => route.params.code);

const isLoggedIn = computed(() => !!JSON.parse(localStorage.getItem("auth_user")));

// State
const loading = ref(true);
const shareInfo = ref(null);
const pageData = ref(null);
const errorState = ref("");

const passwordRequired = ref(false);
const passwordInput = ref("");
const passwordVerifying = ref(false);
const passwordError = ref("");
const password = ref("");

// Edit mode
const isEditing = ref(false);
const editTitle = ref("");
const editContent = ref("");
const editorReady = ref(true);
const saveLoading = ref(false);
const contentRef = ref(null);

const editorConfig = {
    height: 600,
    menubar: true,
    plugins: [
        "advlist", "autolink", "lists", "link", "image", "charmap",
        "searchreplace", "visualblocks", "code", "fullscreen",
        "insertdatetime", "media", "table", "wordcount", "help",
        "codesample", "preview"
    ],
    toolbar: "undo redo | blocks | bold italic forecolor | alignleft aligncenter alignright | bullist numlist outdent indent | link image table codesample | removeformat | help",
    content_style: "body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; font-size: 14px; color: #172b4d; }",
    skin_url: "/tinymce/skins/ui/oxide",
    content_css: "/tinymce/skins/content/default/content.min.css",
    relative_urls: false,
    convert_urls: false,
    promotion: false,
    branding: false
};

// Load share info and page
onMounted(async () => {
    try {
        const info = await shareApi.getInfo(shareCode.value);
        shareInfo.value = info;

        if (info.isExpired) {
            errorState.value = "expired";
            loading.value = false;
            return;
        }

        if (info.isUserSpecific && !isLoggedIn.value) {
            errorState.value = "no_access";
            loading.value = false;
            return;
        }

        if (info.hasPassword) {
            passwordRequired.value = true;
            loading.value = false;
            return;
        }

        await loadPageContent();
    } catch {
        errorState.value = "not_found";
        loading.value = false;
    }
});

async function loadPageContent() {
    try {
        const page = await shareApi.getPageContent(shareCode.value, password.value || undefined);
        pageData.value = page;
        loading.value = false;
        nextTick(() => {
            highlightCode();
            initImagePreview();
        });
    } catch (e) {
        if (e.message?.includes("没有访问权限") || e.message?.includes("No access")) {
            errorState.value = "no_access";
        } else if (e.message?.includes("已过期") || e.message?.includes("expired")) {
            errorState.value = "expired";
        } else {
            errorState.value = "not_found";
        }
        loading.value = false;
    }
}

const handlePasswordSubmit = async () => {
    if (!passwordInput.value) return;
    passwordVerifying.value = true;
    passwordError.value = "";
    try {
        const page = await shareApi.getPageContent(shareCode.value, passwordInput.value);
        password.value = passwordInput.value;
        pageData.value = page;
        passwordRequired.value = false;
        loading.value = false;
        nextTick(() => {
            highlightCode();
            initImagePreview();
        });
    } catch (e) {
        passwordError.value = e.message || t('sharePage.passwordError');
    }
    passwordVerifying.value = false;
};

// Edit mode
function enterEditMode() {
    if (!pageData.value) return;
    editTitle.value = pageData.value.title;
    editContent.value = pageData.value.content;
    isEditing.value = true;
}

function cancelEdit() {
    isEditing.value = false;
}

async function handleSaveEdit() {
    saveLoading.value = true;
    try {
        const result = await shareApi.updatePageContent(
            shareCode.value,
            { title: editTitle.value, content: editContent.value },
            password.value || undefined
        );
        pageData.value = result;
        isEditing.value = false;
        message.success(t('sharePage.saveSuccess'));
        nextTick(() => {
            highlightCode();
            initImagePreview();
        });
    } catch (e) {
        message.error(e.message || t('sharePage.saveFailed'));
    }
    saveLoading.value = false;
}

function highlightCode() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll('pre[class*="language-"]').forEach(pre => {
            pre.classList.add("line-numbers");
            if (!pre.querySelector(".code-copy-btn")) {
                const btn = document.createElement("button");
                btn.className = "code-copy-btn";
                btn.textContent = "Copy";
                btn.onclick = () => {
                    const code = pre.querySelector("code");
                    navigator.clipboard.writeText(code?.textContent || pre.textContent || "").then(() => {
                        btn.textContent = "Copied!";
                        setTimeout(() => { btn.textContent = "Copy"; }, 2000);
                    });
                };
                pre.style.position = "relative";
                pre.appendChild(btn);
            }
        });
        Prism.highlightAll();
    });
}

function initImagePreview() {
    nextTick(() => {
        const el = contentRef.value;
        if (!el) return;
        el.querySelectorAll("img").forEach(img => {
            img.style.cursor = "pointer";
            if (!img.dataset.hasPreviewListener) {
                img.addEventListener("click", () => {
                    window.open(img.src, "_blank");
                });
                img.dataset.hasPreviewListener = "true";
            }
        });
    });
}

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
    if (days < 30) return t('common.daysAgo', { n: days });
    return date.toLocaleDateString(locale.value);
}
</script>

<style scoped>
.share-page-wrapper {
    min-height: 100vh;
    background: #fff;
}

/* Header - matches TopNavbar style */
.share-header {
    height: 40px;
    background-color: #0049b0;
    color: #ffffff;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 1rem;
    z-index: 10;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.share-site-link {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    text-decoration: none;
    color: #fff;
}

.share-logo {
    width: 24px;
    height: 24px;
    background-color: transparent;
    border-radius: 3px;
    object-fit: contain;
}

.share-site-name {
    font-weight: 500;
    font-size: 14px;
    color: #fff;
}

.share-site-name:hover {
    text-decoration: underline;
}

.share-header-link {
    color: rgba(255, 255, 255, 0.9);
    font-size: 14px;
    font-weight: 500;
    padding: 0.25rem 0.5rem;
    border-radius: 3px;
    text-decoration: none;
    cursor: pointer;
}

.share-header-link:hover {
    background-color: rgba(0, 0, 0, 0.1);
    color: #ffffff;
}

/* Content area */
.share-content-area {
    max-width: 1200px;
    margin: 0 auto;
    padding: 32px 40px;
}

/* Loading */
.share-loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 120px 0;
    gap: 16px;
    color: #6b778c;
}

/* Error states */
.share-error-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    padding: 80px 0;
    text-align: center;
    background: #fff;
    border-radius: 8px;
    margin-top: 40px;
}

.share-error-icon {
    margin-bottom: 24px;
}

.share-error-state h2 {
    font-size: 24px;
    color: #172b4d;
    margin: 0 0 8px;
}

.share-error-state p {
    color: #6b778c;
    font-size: 14px;
    margin: 0 0 24px;
}

.share-error-btn {
    display: inline-block;
    background: #0052cc;
    color: #fff;
    padding: 8px 20px;
    border-radius: 4px;
    text-decoration: none;
    font-size: 14px;
    font-weight: 500;
}

.share-error-btn:hover {
    background: #0049b0;
}

/* Password form */
.share-password-form {
    display: flex;
    justify-content: center;
    padding: 80px 0;
}

.share-password-card {
    width: 100%;
    max-width: 400px;
    text-align: center;
    background: #fff;
    border-radius: 8px;
    padding: 40px;
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.1);
}

.share-password-icon {
    margin-bottom: 16px;
}

.share-password-card h2 {
    font-size: 20px;
    color: #172b4d;
    margin: 0 0 8px;
}

.share-password-card p {
    color: #6b778c;
    font-size: 14px;
    margin: 0 0 24px;
}

.share-password-input {
    margin-bottom: 16px;
}

.share-password-error {
    color: #de350b;
    font-size: 13px;
    margin-bottom: 12px;
}

/* Page content */
.share-page-header {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    margin-bottom: 8px;
}

.share-page-title {
    font-size: 28px;
    font-weight: 600;
    color: #172b4d;
    margin: 0;
    letter-spacing: -0.01em;
    line-height: 1.25;
}

.share-edit-btn {
    border: none !important;
    background-color: rgba(9, 30, 66, 0.04) !important;
    color: #42526e !important;
    border-radius: 3px !important;
    font-weight: 500 !important;
    height: 32px !important;
    flex-shrink: 0;
}

.share-edit-btn:hover {
    background-color: rgba(9, 30, 66, 0.08) !important;
}

.share-page-meta {
    display: flex;
    align-items: center;
    color: #6b778c;
    font-size: 12px;
    margin-bottom: 24px;
}

.share-page-body {
    background: #fff;
    border-radius: 4px; 
    min-height: 200px;
}

/* Content styles (same as View.vue) */
.share-page-body :deep(img) {
    max-width: 100%;
    height: auto;
}

.share-page-body :deep(p) {
    font-size: 14px;
    margin-bottom: 12px;
    line-height: 1.714;
    color: #172b4d;
    word-break: break-word;
}

.share-page-body :deep(h2) {
    font-size: 20px;
    font-weight: 500;
    color: #172b4d;
    margin-top: 24px;
    margin-bottom: 12px;
}

.share-page-body :deep(ul),
.share-page-body :deep(ol) {
    padding-left: 1.5em;
    margin-bottom: 12px;
}

.share-page-body :deep(li) {
    font-size: 14px;
    line-height: 1.714;
    color: #172b4d;
    margin-bottom: 4px;
}

.share-page-body :deep(pre) {
    background-color: #f4f5f7;
    border-radius: 3px;
    padding: 16px;
    margin: 16px 0;
    font-family: SFMono-Regular, Consolas, Liberation Mono, Menlo, monospace;
    font-size: 14px;
    color: #172b4d;
    border: 1px solid #dfe1e6;
    overflow-x: auto;
}

.share-page-body :deep(pre code) {
    background: none;
    padding: 0;
    border-radius: 0;
    font-size: inherit;
    color: inherit;
}

.share-page-body :deep(pre[class*="language-"]) {
    padding-left: 3.8em;
}

.share-page-body :deep(pre .code-copy-btn) {
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

.share-page-body :deep(pre:hover .code-copy-btn) {
    opacity: 1;
}

.share-page-body :deep(:not(pre) > code) {
    background: #f4f5f7;
    padding: 2px 6px;
    border-radius: 3px;
    font-family: SFMono-Regular, Consolas, Liberation Mono, Menlo, monospace;
    font-size: 13px;
    color: #c7254e;
}

.share-page-body :deep(table) {
    border-collapse: collapse !important;
    margin: 16px 0;
    border: 1px solid #dfe1e6 !important;
    font-size: 14px;
}

.share-page-body :deep(table th),
.share-page-body :deep(table td) {
    border: 1px solid #dfe1e6 !important;
    padding: 8px 12px;
    text-align: left;
    vertical-align: top;
    line-height: 1.5;
}

.share-page-body :deep(table th) {
    background: #f4f5f7;
    color: #172b4d;
    font-weight: 600;
}

/* Edit mode */
.share-edit-header {
    display: flex;
    align-items: center;
    gap: 16px;
    margin-bottom: 16px;
}

.share-edit-title {
    flex: 1;
    font-size: 28px;
    font-weight: 600;
    color: #172b4d;
    border: none;
    outline: none;
    background: transparent;
    padding: 0;
    letter-spacing: -0.01em;
}

.share-edit-actions {
    display: flex;
    gap: 8px;
    flex-shrink: 0;
}

.editor-loading {
    padding: 40px;
    text-align: center;
    color: #6b778c;
}

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
    .share-content-area {
        padding: 20px 16px;
    }

    .share-password-card {
        padding: 24px 16px;
    }

    .share-page-title {
        font-size: 22px;
    }

    .share-page-header {
        flex-wrap: wrap;
    }

    .share-edit-title {
        font-size: 22px;
    }

    .share-edit-header {
        flex-wrap: wrap;
        gap: 8px;
    }

    .share-page-body :deep(pre[class*="language-"]) {
        padding-left: 1em;
        font-size: 12px;
    }

    .share-page-body :deep(table) {
        display: block;
        overflow-x: auto;
    }

    .share-error-state {
        padding: 40px 16px;
    }

    .share-loading {
        padding: 80px 0;
    }
}
</style>
