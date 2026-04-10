<template>
    <div class="page-wrapper" v-if="pageId">
        <!-- Page Header with Breadcrumb and Actions -->
        <div class="page-header" v-if="!isEditing">
            <a-breadcrumb>
                <a-breadcrumb-item>
                    <a href="/spaces">Engineering Space</a>
                </a-breadcrumb-item>
                <a-breadcrumb-item>Overview</a-breadcrumb-item>
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
        <div class="page-view" v-if="!isEditing">
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
                    U
                </a-avatar>
                <span class="author">Created by Admin</span>
                <span class="bullet">•</span>
                <span class="date">Last updated just now</span>
            </div>

            <div class="page-content-mock" v-html="pageContent"></div>

            <!-- Comments Section -->
            <PageComments :userInitial="userInitial" />
        </div>

        <!-- Editing Mode -->
        <div v-else class="editor-container">
            <!-- Native Title that will be dynamically teleported INSIDE TinyMCE -->
            <div id="teleport-title-dest">
                <input type="text" class="editor-title-input" v-model="pageTitle" placeholder="Page Title" />
            </div>

            <editor v-if="editorReady" v-model="pageContent" :init="editorConfig" api-key="no-api-key" @init="onEditorInit" />
            <div v-else class="editor-loading">Loading editor...</div>

            <div class="editor-actions">
                <a-button type="primary" @click="savePage" style="margin-right: 8px; background-color: #0052cc">
                    Update
                </a-button>
                <a-button @click="cancelEdit">Cancel</a-button>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import Editor from "@tinymce/tinymce-vue"; // Editor 组件很轻量，可以直接导入

// 注意：不在顶部直接 import TinyMCE 核心库，否则会导致页面初始加载变慢
// 我们使用动态 import + 预加载策略

// 懒加载 TinyMCE - 在页面加载时后台预加载
const loadTinyMCE = async () => {
    // 并行加载所有 TinyMCE 相关模块
    const [tm, css] = await Promise.all([
        import("tinymce/tinymce"),
        import("tinymce/skins/content/default/content.css?raw"),
    ]);

    // 同时加载插件（不阻塞主流程）
    Promise.all([
        import("tinymce/plugins/image"),
        import("tinymce/plugins/table"),
        import("tinymce/plugins/lists"),
        import("tinymce/plugins/wordcount"),
        import("tinymce/plugins/code"),
        import("tinymce/plugins/fullscreen"),
        import("tinymce/plugins/autoresize"),
        import("tinymce/plugins/advlist"),
        import("tinymce/plugins/autolink"),
        import("tinymce/plugins/link"),
        import("tinymce/plugins/charmap"),
        import("tinymce/plugins/preview"),
        import("tinymce/plugins/anchor"),
        import("tinymce/plugins/searchreplace"),
        import("tinymce/plugins/visualblocks"),
        import("tinymce/plugins/insertdatetime"),
        import("tinymce/plugins/media"),
        import("tinymce/plugins/help"),
        // 主题和图标
        import("tinymce/skins/ui/oxide/skin.css"),
        import("tinymce/themes/silver"),
        import("tinymce/icons/default"),
        import("tinymce/models/dom"),
        import("tinymce/icons/default/icons")
    ]);

    return { tinymce: tm.default, contentCss: css.default };
};

const route = useRoute();
const router = useRouter();
const pageId = computed(() => route.params.id);
const isEditing = computed(() => route.query.edit === "true");

const pageTitle = ref("");
const pageContent = ref("");
const editorReady = ref(false);
const tinymce = ref(null);
const contentCss = ref("");

// 预加载编辑器（在组件挂载时就开始加载，而不是等到点击编辑按钮）
onMounted(async () => {
    loadPageData();
    // 后台预加载编辑器，不阻塞页面渲染
    loadTinyMCE().then(({ tinymce: tm, contentCss: css }) => {
        tinymce.value = tm;
        contentCss.value = css;
        editorReady.value = true;
        console.log("TinyMCE 预加载完成");
    }).catch(err => {
        console.error("TinyMCE 加载失败:", err);
    });
});

const defaultHtml = (id) => `
<p class="lead-text">
  This is a mock page loaded natively via Vue Router using ID <strong>${id}</strong>. In a complete implementation, this component will dynamically fetch HTML from the backend.
</p>
<h2>Architecture Overview</h2>
<p>
  The system relies on a Vue 3 frontend making asynchronous requests to a .NET 10 Web API. PostgreSql handles the heavy lifting of recursive hierarchical queries.
</p>
<pre><code>SELECT * FROM pages WHERE parent_id = $1;
-- Using pg_trgm for full-text search</code></pre>
<p>You can edit this placeholder directly using the Rich Text Editor.</p>
`;

const loadPageData = () => {
    pageTitle.value = `Mock Page Title (ID: ${pageId.value})`;
    pageContent.value = defaultHtml(pageId.value);
};

watch(pageId, () => {
    loadPageData();
});

// 使用 computed 确保 editorConfig 在 contentCss 加载后正确更新
const editorConfig = computed(() => ({
    min_height: 500,
    menubar: false,
    statusbar: false,
    plugins: [
        "autoresize",
        "advlist",
        "autolink",
        "lists",
        "link",
        "image",
        "charmap",
        "preview",
        "anchor",
        "searchreplace",
        "visualblocks",
        "code",
        "fullscreen",
        "insertdatetime",
        "media",
        "table",
        "help",
        "wordcount",
    ],
    toolbar:
        "undo redo | formatselect | " +
        "bold italic forecolor backcolor | alignleft aligncenter " +
        "alignright alignjustify | bullist numlist outdent indent | " +
        "table image | removeformat | help",

    // 开启本地图片粘贴和上传的沉浸式支持
    paste_data_images: true,
    image_title: true,
    automatic_uploads: true,
    file_picker_types: "image",
    file_picker_callback: (cb, value, meta) => {
        const input = document.createElement("input");
        input.setAttribute("type", "file");
        input.setAttribute("accept", "image/*");
        input.onchange = function () {
            const file = this.files[0];
            const reader = new FileReader();
            reader.onload = function () {
                const id = "blobid" + new Date().getTime();
                // 暂时使用本地 Blobs，不需要后端 API 就能展示完美图片效果
                const blobCache = tinymce.value?.activeEditor?.editorUpload?.blobCache;
                if (!blobCache) return;
                const base64 = reader.result.split(",")[1];
                const blobInfo = blobCache.create(id, file, base64);
                blobCache.add(blobInfo);
                cb(blobInfo.blobUri(), { title: file.name });
            };
            reader.readAsDataURL(file);
        };
        input.click();
    },
    content_style:
        (contentCss.value || "") +
        '\nbody { margin: 0 !important; max-width: 900px !important; padding:5px 2rem 0 !important; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.714; color: #172b4d; }',
}));

// Shift the native Vue title input directly into TinyMCE DOM hierarchy logic
const onEditorInit = () => {
    // Safely wait a tick for full DOM tree integration
    setTimeout(() => {
        const titleEl = document.querySelector("#teleport-title-dest");
        const headerEl = document.querySelector(".tox-editor-header");
        if (titleEl && headerEl) {
            headerEl.after(titleEl); // Push the title block physically below the toolbar, inside wrapper
        }
    }, 10);
};

// Page action handlers
const enterEditMode = () => {
    router.push({ query: { edit: 'true' } });
};

const handleShare = () => {
    console.log('Share clicked - TODO: implement share dialog');
};

const handleViewHistory = () => {
    console.log('View History clicked - TODO: navigate to history page');
};

const handleWatch = () => {
    console.log('Watch Page clicked - TODO: implement watch functionality');
};

const handleMove = () => {
    console.log('Move Page clicked - TODO: implement move dialog');
};

const handleDelete = () => {
    console.log('Delete clicked - TODO: implement delete confirmation');
};

const savePage = () => {
    console.log("Saving Content: ", pageContent.value);
    cancelEdit();
};

const cancelEdit = () => {
    router.push({ path: route.path });
};
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

:deep(.page-content-mock .lead-text) {
    font-size: 14px;
    color: #172b4d;
    line-height: 1.714;
    margin-bottom: 16px;
}
:deep(.page-content-mock h2) {
    font-size: 20px;
    font-weight: 500;
    color: #172b4d;
    margin-top: 24px;
    margin-bottom: 12px;
}
:deep(.page-content-mock p) {
    font-size: 14px;
    margin-bottom: 12px;
    line-height: 1.714;
    color: #172b4d;
}
:deep(.page-content-mock pre) {
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

.editor-container {
    animation: fadeIn 0.3s ease-in-out;
    margin-top: 0;
    display: flex;
    flex-direction: column;
    min-height: calc(100vh - 40px);
}

#teleport-title-dest {
    max-width: 900px;
    padding: 15px 2rem 0;
}

.editor-title-input {
    width: 100%;
    font-size: 28px;
    font-weight: 500;
    color: #172b4d;
    border: none;
    border-bottom: 1px solid transparent;
    padding: 0 0 8px 0;
    margin-bottom: 0px;
    outline: none;
    background: transparent;
    transition: border-bottom-color 0.2s;
    letter-spacing: -0.01em;
    line-height: 1.25;
}

.editor-title-input:focus {
    border-bottom-color: #0052cc;
}

.editor-actions {
    position: sticky;
    bottom: 0;
    background-color: rgba(255, 255, 255, 0.95);
    backdrop-filter: blur(4px);
    z-index: 100;
    padding: 5px 15px;
    border-top: 1px solid #dfe1e6;
    margin-top: auto; /* 保证了哪怕是一篇空文章，也会强行把按钮顶到屏幕下边缘 */
    display: flex;
    justify-content: flex-end;
}

/* Base TinyMCE customisation for Classic mode natively sticky */
:deep(.tox-tinymce) {
    border: none !important;
    overflow: visible !important;
}
:deep(.tox-editor-container) {
    overflow: visible !important;
}

:deep(.tox:not(.tox-tinymce-inline) .tox-editor-header) {
    box-shadow: 0 1px 3px rgba(0, 0, 0, 0.05) !important;
    border-bottom: 1px solid #dfe1e6 !important;
    padding: 0 !important;
    position: sticky !important;
    top: 0 !important;
    z-index: 100 !important;
    background-color: white !important;
    margin: 0 !important;
}

/* Remove TinyMCE default Focus Outlines */
:deep(.tox-tinymce--focused),
:deep(.tox .tox-edit-area::before) {
    box-shadow: none !important;
    outline: none !important;
    border: none !important;
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

.editor-loading {
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 60px 20px;
    color: #6b778c;
    font-size: 16px;
    min-height: 400px;
}

.editor-loading::before {
    content: "";
    display: inline-block;
    width: 20px;
    height: 20px;
    margin-right: 12px;
    border: 2px solid #dfe1e6;
    border-top-color: #0052cc;
    border-radius: 50%;
    animation: spin 0.8s linear infinite;
}

@keyframes spin {
    to {
        transform: rotate(360deg);
    }
}
</style>
