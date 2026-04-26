<template>
    <div class="editor-container">
        <!-- 上传进度提示 -->
        <div v-if="uploadProgress.show" class="upload-progress">
            <a-spin />
            <span>上传中... {{ uploadProgress.percent }}%</span>
        </div>

        <editor v-if="editorReady" v-model="pageContent" :init="editorConfig" api-key="no-api-key" @init="onEditorInit" />
        <div v-else class="editor-loading">Loading editor...</div>

        <!-- Breadcrumb + Title: will be teleported inside TinyMCE after toolbar -->
        <div id="teleport-below-toolbar">
            <div class="editor-breadcrumb">
                <a-breadcrumb>
                    <a-breadcrumb-item>
                        <router-link :to="`/${$route.params.spaceKey}`">{{ spaceName }}</router-link>
                    </a-breadcrumb-item>
                    <a-breadcrumb-item v-for="crumb in parentCrumbs" :key="crumb.id">
                        <router-link :to="`/${$route.params.spaceKey}/page/${crumb.id}`">{{ crumb.title }}</router-link>
                    </a-breadcrumb-item>
                </a-breadcrumb>
            </div>
            <div id="teleport-title-dest">
                <input type="text" class="editor-title-input" v-model="pageTitle" placeholder="Page Title" />
            </div>
        </div>

        <div class="editor-actions">
            <span v-if="autoSaveStatus === 'saving'" class="auto-save-hint">保存中...</span>
            <span v-else-if="autoSaveStatus === 'saved'" class="auto-save-hint">已自动保存</span>
            <div class="spacer"></div>
            <a-button type="primary" @click="savePage" style="margin-right: 8px; background-color: #0052cc">
                {{ isCreating ? 'Create' : 'Update' }}
            </a-button>
            <a-button @click="cancelEdit">Cancel</a-button>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { message } from "ant-design-vue";
import Editor from "@tinymce/tinymce-vue";
import { pageApi, attachmentApi } from "../../api";
import { usePageTreeStore } from "../../store/pageTree";

// TinyMCE 已在 index.html 中从 /tinymce/ 全局加载
// 这里不需要任何 import 语句

const route = useRoute();
const router = useRouter();
const pageTreeStore = usePageTreeStore();
const pageId = computed(() => route.params.id);
const isCreating = computed(() => !pageId.value);
const parentId = computed(() => route.query.parentId || null);

// 获取当前页面 ID（用于上传附件）
const currentPageId = computed(() => {
    if (isCreating.value) return null;
    // 尝试从已有页面数据获取
    return Number(pageId.value);
});

// 页面数据
const pageTitle = ref("");
const pageContent = ref("");

// 上传进度
const uploadProgress = ref({ show: false, percent: 0 });

// 自动保存
const autoSaveStatus = ref(''); // '', 'saving', 'saved'
let autoSaveTimer = null;
let lastSavedTitle = '';
let lastSavedContent = '';

function startAutoSave() {
    stopAutoSave();
    if (isCreating.value) return;
    autoSaveTimer = setInterval(async () => {
        if (isCreating.value) return;
        if (pageTitle.value === lastSavedTitle && pageContent.value === lastSavedContent) return;
        try {
            autoSaveStatus.value = 'saving';
            await pageApi.update(pageId.value, {
                title: pageTitle.value,
                content: pageContent.value,
            });
            // Invalidate cache so the tree shows updated title
            const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
            const space = spaces.find(s => s.key === route.params.spaceKey)
            if (space?.id) {
                pageTreeStore.invalidateWorkspace(space.id)
            }
            lastSavedTitle = pageTitle.value;
            lastSavedContent = pageContent.value;
            autoSaveStatus.value = 'saved';
        } catch {
            autoSaveStatus.value = '';
        }
    }, 30000);
}

function stopAutoSave() {
    if (autoSaveTimer) {
        clearInterval(autoSaveTimer);
        autoSaveTimer = null;
    }
}

// 面包屑
const spaceName = computed(() => {
    const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
    const key = route.params.spaceKey
    const space = spaces.find(s => s.key === key)
    return space?.name || key || ''
});

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
    // 编辑已有页面：从当前页面往上找
    // 创建子页面：从父页面往上找
    const targetId = isCreating.value
        ? (parentId.value ? Number(parentId.value) : null)
        : Number(pageId.value)
    if (!targetId) return []
    const crumbs = []
    let currentId = targetId
    const visited = new Set()
    while (currentId) {
        const node = pageTreeMap.value.get(currentId)
        if (!node || visited.has(currentId)) break
        visited.add(currentId)
        crumbs.unshift({ id: node.id, title: node.title })
        currentId = node.parentId
    }
    return crumbs
})

const editorReady = ref(true);

// 编辑模式：加载已有页面数据
const loadPageData = async () => {
    if (isCreating.value) return;
    try {
        const data = await pageApi.getById(pageId.value);
        if (data) {
            pageTitle.value = data.title || "";
            pageContent.value = data.content || "";
            lastSavedTitle = pageTitle.value;
            lastSavedContent = pageContent.value;
        }
    } catch (e) {
        console.error("加载页面失败:", e);
    }
};

/**
 * 上传文件到服务器
 * @param {File} file - 要上传的文件
 * @param {string} [comment] - 可选的文件说明
 * @returns {Promise<{url: string, fileName: string}>}
 */
async function uploadFile(file, comment) {
    // 如果是新页面，先保存页面以获取 ID
    if (isCreating.value) {
        const spaces = JSON.parse(localStorage.getItem('auth_spaces') || '[]')
        const space = spaces.find(s => s.key === route.params.spaceKey)
        const data = await pageApi.create({
            title: pageTitle.value || 'Untitled',
            content: pageContent.value || '',
            workspaceId: space?.id,
            parentId: parentId.value,
            status: 0, // 草稿状态
        });
        // 更新路由到编辑模式
        router.replace({ path: `/${route.params.spaceKey}/page/${data.id}/edit` });
        return uploadFile(file, comment); // 递归调用，此时已有 pageId
    }

    uploadProgress.value = { show: true, percent: 0 };

    try {
        const result = await attachmentApi.upload(currentPageId.value, file, comment);
        uploadProgress.value = { show: false, percent: 0 };

        // 构建访问 URL
        const fileUrl = `/uploads/${result.storagePath}`;
        return { url: fileUrl, fileName: result.fileName };
    } catch (error) {
        uploadProgress.value = { show: false, percent: 0 };
        message.error(`上传失败: ${error.message || '未知错误'}`);
        throw error;
    }
}

/**
 * 处理拖拽上传
 */
function handleDropUpload(e) {
    // 阻止默认行为
    e.preventDefault();
    e.stopPropagation();

    const files = e.dataTransfer?.files;
    if (!files || files.length === 0) return;

    const file = files[0];
    handleFileInsert(file);
}

/**
 * 处理文件插入
 */
async function handleFileInsert(file) {
    // 检查文件类型
    const imageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
    const isImage = imageTypes.includes(file.type);

    // 支持的文件类型
    const supportedExtensions = /\.(md|txt|json|xml|zip|pptx|pdf|docx|xlsx|mp4|mp3)$/i;

    if (!isImage && !file.name.toLowerCase().match(supportedExtensions)) {
        message.warning('暂不支持此文件类型');
        return;
    }

    try {
        const { url } = await uploadFile(file, '');

        const editor = window.tinymce?.activeEditor;
        if (!editor) return;

        if (isImage) {
            // 插入图片
            editor.insertContent(`<img class="image" src="${url}" alt="${file.name}" style="max-width: 100%;">`);
        } else {
            // 插入文件链接
            editor.insertContent(`<p><a class="file" href="${url}" target="_blank">${file.name}</a></p>`);
        }
    } catch (error) {
        message.warning('文件插入失败');
        console.error('文件插入失败:', error);
    }
}

onMounted(async () => {
    await loadPageData();
    startAutoSave();

    // 添加拖拽上传支持
    document.addEventListener('drop', handleDropUpload, true);
    document.addEventListener('dragover', (e) => {
        e.preventDefault();
        e.stopPropagation();
    }, true);
});

onUnmounted(() => {
    stopAutoSave();
    document.removeEventListener('drop', handleDropUpload, true);
    document.removeEventListener('dragover', null, true);
});

const editorConfig = computed(() => ({
    base_url: '/tinymce/',
    min_height: 500,
    menubar: false,
    statusbar: false,
    // 禁用 URL 转换，保持绝对路径
    relative_urls: false,
    remove_script_host: false,
    convert_urls: false,
    plugins: [
        "autoresize", "advlist", "autolink", "lists", "link", "image",
        "charmap", "preview", "anchor", "searchreplace", "visualblocks",
        "code", "fullscreen", "media", "table", "codesample",
        // paste is built-in, no need to declare
    ],
    toolbar:
        "undo redo | formatselect | " +
        "bold italic forecolor backcolor | alignleft aligncenter " +
        "alignright alignjustify | bullist numlist | " +
        "table image codesample | removeformat",
    table_header_type: "section",
    table_use_colgroups: false,
    table_default_styles: {},
    table_default_attributes: {},
    setup(editor) {
        editor.on('ExecCommand', (e) => {
            if (e.command === 'mceInsertContent' || e.command === 'mceTableInsert') {
                setTimeout(() => {
                    editor.dom.select('table').forEach(table => {
                        const firstRow = table.querySelector('tr');
                        if (!firstRow) return;
                        const hasTh = firstRow.querySelector('th');
                        if (hasTh) return;
                        // 首行 td 转 th，并用 thead 包裹
                        const thead = document.createElement('thead');
                        firstRow.querySelectorAll('td').forEach(td => {
                            const th = document.createElement('th');
                            th.innerHTML = td.innerHTML;
                            td.replaceWith(th);
                        });
                        thead.appendChild(firstRow);
                        table.insertBefore(thead, table.firstChild);
                    });
                }, 0);
            }
        });

        // 支持拖拽文件到编辑器
        editor.on('drop', (e) => {
            const files = e.dataTransfer?.files;
            if (files && files.length > 0) {
                e.preventDefault();
                const file = files[0];
                // 支持的文件类型
                const imageTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/bmp', 'image/webp'];
                const supportedExtensions = /\.(md|txt|json|xml|zip|pptx|pdf|docx|xlsx|mp4|mp3)$/i;
                if (imageTypes.includes(file.type) || file.name.match(supportedExtensions)) {
                    handleFileInsert(file);
                } else {
                    message.warning('暂不支持此文件类型');
                }
            }
        });
    },
    paste_data_images: true,
    image_title: true,
    automatic_uploads: false, // 禁用自动上传，使用自定义上传
    file_picker_types: "image",
    file_picker_callback: (cb, value, meta) => {
        // 创建文件选择器
        const input = document.createElement("input");
        input.setAttribute("type", "file");
        input.setAttribute("accept", "image/*,.md,.txt,.json,.xml,.zip,.pptx,.pdf,.docx,.xlsx,.mp4,.mp3");
        input.onchange = async function () {
            const file = this.files[0];
            if (!file) return;

            try {
                const { url } = await uploadFile(file, '');
                cb(url, { title: file.name });
            } catch (error) {
                console.error('文件上传失败:', error);
            }
        };
        input.click();
    },
    // 粘贴图片时上传
    images_upload_handler: async (blobInfo, progress) => {
        // 将 blob 转换为 File
        const file = new File([blobInfo.blob()], blobInfo.filename(), { type: blobInfo.blob().type });

        try {
            const { url } = await uploadFile(file, '');
            return url;
        } catch (error) {
            throw error;
        }
    },
    content_style: `
body { margin: 0 !important; padding:5px 40px 0 !important; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.714; color: #172b4d; }
table { border-collapse: collapse !important; margin: 16px 0; border: 1px solid #dfe1e6 !important; font-size: 14px; }
table th, table td {min-width:30px; border: 1px solid #dfe1e6 !important; padding: 8px 12px; text-align: left; vertical-align: top; line-height: 1.5; }
table th { background: #f4f5f7 center right no-repeat; color: #172b4d; font-weight: 600; padding-right: 24px; }
pre[class*="language-"] { background: #f5f2f0; border-radius: 3px; padding: 16px; margin: 16px 0; border: 1px solid #dfe1e6; overflow-x: auto; }
code { font-family: SFMono-Regular, Consolas, Liberation Mono, Menlo, monospace; }
`,
}));

const onEditorInit = () => {
    setTimeout(() => {
        const block = document.querySelector("#teleport-below-toolbar");
        const headerEl = document.querySelector(".tox-editor-header");
        if (block && headerEl) {
            headerEl.after(block);
        }
        // Focus to title input
        const titleInput = document.querySelector(".editor-title-input");
        if (titleInput) {
            titleInput.focus();
            //titleInput.select();
        }
    }, 10);
};

const savePage = async () => {
    if (!pageTitle.value.trim()) {
        message.warning('请输入页面标题');
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
        message.error('保存失败，请重试');
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
    animation: fadeIn 0.2s ease-in-out;
    margin-top: 0;
    display: flex;
    flex-direction: column;
    min-height: calc(100vh - 40px);
}

.editor-breadcrumb {
    padding: 10px 40px 0;
    max-width: 900px;
}

.editor-breadcrumb :deep(.ant-breadcrumb) {
    font-size: 14px;
}

.editor-breadcrumb :deep(.ant-breadcrumb-link),
.editor-breadcrumb :deep(.ant-breadcrumb-separator) {
    color: #0052cc;
}

.editor-breadcrumb :deep(.ant-breadcrumb-link:hover) {
    text-decoration: underline;
    background: none;
}

#teleport-below-toolbar {
    max-width: 900px;
}

#teleport-title-dest {
    padding: 15px 40px 0;
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
    align-items: center;
    justify-content: flex-end;
}

.auto-save-hint {
    font-size: 12px;
    color: #6b778c;
}

/* 上传进度提示 */
.upload-progress {
    position: fixed;
    top: 50%;
    left: 50%;
    transform: translate(-50%, -50%);
    background: rgba(0, 0, 0, 0.8);
    color: white;
    padding: 16px 24px;
    border-radius: 8px;
    display: flex;
    align-items: center;
    gap: 12px;
    z-index: 9999;
    font-size: 14px;
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
