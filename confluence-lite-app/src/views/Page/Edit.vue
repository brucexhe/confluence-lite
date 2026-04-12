<template>
    <div class="editor-container">
        <!-- Title Input -->
        <div id="teleport-title-dest">
            <input type="text" class="editor-title-input" v-model="pageTitle" placeholder="Page Title" />
        </div>

        <editor v-if="editorReady" v-model="pageContent" :init="editorConfig" api-key="no-api-key" @init="onEditorInit" />
        <div v-else class="editor-loading">Loading editor...</div>

        <div class="editor-actions">
            <a-button type="primary" @click="savePage" style="margin-right: 8px; background-color: #0052cc">
                {{ isCreating ? 'Create' : 'Update' }}
            </a-button>
            <a-button @click="cancelEdit">Cancel</a-button>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import Editor from "@tinymce/tinymce-vue";
import { pageApi } from "../../api";

const route = useRoute();
const router = useRouter();
const pageId = computed(() => route.params.id);
const isCreating = computed(() => !pageId.value);
const parentId = computed(() => route.query.parentId || null);

// 页面数据
const pageTitle = ref("");
const pageContent = ref("");

// 懒加载 TinyMCE
const loadTinyMCE = async () => {
    const [tm, css] = await Promise.all([
        import("tinymce/tinymce"),
        import("tinymce/skins/content/default/content.css?raw"),
    ]);
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
        import("tinymce/skins/ui/oxide/skin.css"),
        import("tinymce/themes/silver"),
        import("tinymce/icons/default"),
        import("tinymce/models/dom"),
        import("tinymce/icons/default/icons")
    ]);
    return { tinymce: tm.default, contentCss: css.default };
};

const editorReady = ref(false);
const tinymce = ref(null);
const contentCss = ref("");

// 编辑模式：加载已有页面数据
const loadPageData = async () => {
    if (isCreating.value) return;
    try {
        const data = await pageApi.getById(pageId.value);
        if (data) {
            pageTitle.value = data.title || "";
            pageContent.value = data.content || "";
        }
    } catch (e) {
        console.error("加载页面失败:", e);
    }
};

onMounted(async () => {
    loadPageData();
    loadTinyMCE().then(({ tinymce: tm, contentCss: css }) => {
        tinymce.value = tm;
        contentCss.value = css;
        editorReady.value = true;
    }).catch(err => {
        console.error("TinyMCE 加载失败:", err);
    });
});

const editorConfig = computed(() => ({
    min_height: 500,
    menubar: false,
    statusbar: false,
    plugins: [
        "autoresize", "advlist", "autolink", "lists", "link", "image",
        "charmap", "preview", "anchor", "searchreplace", "visualblocks",
        "code", "fullscreen", "insertdatetime", "media", "table", "help", "wordcount",
    ],
    toolbar:
        "undo redo | formatselect | " +
        "bold italic forecolor backcolor | alignleft aligncenter " +
        "alignright alignjustify | bullist numlist outdent indent | " +
        "table image | removeformat | help",
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

const onEditorInit = () => {
    setTimeout(() => {
        const titleEl = document.querySelector("#teleport-title-dest");
        const headerEl = document.querySelector(".tox-editor-header");
        if (titleEl && headerEl) {
            headerEl.after(titleEl);
        }
    }, 10);
};

const savePage = async () => {
    if (!pageTitle.value.trim()) {
        alert('请输入页面标题');
        return;
    }
    try {
        if (isCreating.value) {
            const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
            const space = spaces.find(s => s.key === route.params.spaceKey)
            const data = await pageApi.create({
                title: pageTitle.value,
                content: pageContent.value,
                workspaceId: space?.id,
                parentId: parentId.value,
                status: 1,
            });
            // 创建成功后全页刷新，确保 PageTree 和面包屑都更新
            window.location.href = `/${route.params.spaceKey}/page/${data.id}`;
        } else {
            await pageApi.update(pageId.value, {
                title: pageTitle.value,
                content: pageContent.value,
            });
            // 更新成功后全页刷新
            window.location.href = `/${route.params.spaceKey}/page/${pageId.value}`;
        }
    } catch (e) {
        console.error("保存页面失败:", e);
    }
};

const cancelEdit = () => {
    if (isCreating.value) {
        router.push(`/${route.params.spaceKey}`);
    } else {
        router.replace(`/${route.params.spaceKey}/page/${pageId.value}`);
    }
};
</script>

<style scoped>
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
    margin-top: auto;
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
