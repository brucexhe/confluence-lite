<template>
    <div class="page-wrapper" v-if="pageId">
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
        </div>

        <!-- Editing Mode -->
        <div v-else class="editor-container">
            <!-- Native Title that will be dynamically teleported INSIDE TinyMCE -->
            <div id="teleport-title-dest">
                <input type="text" class="editor-title-input" v-model="pageTitle" placeholder="Page Title" />
            </div>

            <editor v-model="pageContent" :init="editorConfig" api-key="no-api-key" @init="onEditorInit" />

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

import tinymce from "tinymce/tinymce";
import contentCss from "tinymce/skins/content/default/content.css?raw";
import "tinymce/skins/ui/oxide/skin.css";
import "tinymce/themes/silver";
import "tinymce/themes/silver/theme";
import "tinymce/icons/default";
import "tinymce/models/dom";

import "tinymce/icons/default/icons";
import "tinymce/plugins/image";
import "tinymce/plugins/table";
import "tinymce/plugins/lists";
import "tinymce/plugins/wordcount";
import "tinymce/plugins/code";
import "tinymce/plugins/fullscreen";
import "tinymce/plugins/autoresize";
import Editor from "@tinymce/tinymce-vue";

const route = useRoute();
const router = useRouter();
const pageId = computed(() => route.params.id);
const isEditing = computed(() => route.query.edit === "true");

const pageTitle = ref("");
const pageContent = ref("");

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

onMounted(() => {
    loadPageData();
});

watch(pageId, () => {
    loadPageData();
});

const editorConfig = {
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
        "code",
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
    file_picker_types: 'image',
    file_picker_callback: (cb, value, meta) => {
        const input = document.createElement('input');
        input.setAttribute('type', 'file');
        input.setAttribute('accept', 'image/*');
        input.onchange = function () {
            const file = this.files[0];
            const reader = new FileReader();
            reader.onload = function () {
                const id = 'blobid' + (new Date()).getTime();
                // 暂时使用本地 Blobs，不需要后端 API 就能展示完美图片效果
                const blobCache = tinymce.activeEditor.editorUpload.blobCache;
                const base64 = reader.result.split(',')[1];
                const blobInfo = blobCache.create(id, file, base64);
                blobCache.add(blobInfo);
                cb(blobInfo.blobUri(), { title: file.name });
            };
            reader.readAsDataURL(file);
        };
        input.click();
    },
    content_style:
        contentCss.toString() +
        '\nbody { margin: 0 !important; max-width: 900px !important; padding:5px 2rem 0 !important; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.714; color: #172b4d; }',
};

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

const savePage = () => {
    console.log("Saving Content: ", pageContent.value);
    cancelEdit();
};

const cancelEdit = () => {
    router.push({ path: route.path });
};
</script>

<style scoped>
.page-view {
    max-width: 900px;
    padding: 1rem 3rem 4rem 3rem;
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
</style>
