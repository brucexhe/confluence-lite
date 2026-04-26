<template>
    <div v-if="visible" class="office-preview-overlay" @click.self="close" @keydown.esc="close">
        <!-- 顶部工具栏 -->
        <div class="top-toolbar-bar">
            <div class="toolbar-left">
                <span class="file-name">{{ fileName }}</span>
            </div>
            <div class="toolbar-right">
                <a-space :size="4">
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="handleDownload"
                        title="下载原文件"
                    >
                        <template #icon><DownloadIcon :size="16" /></template>
                    </a-button>
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="close"
                        title="关闭 (ESC)"
                    >
                        <template #icon><XIcon :size="16" /></template>
                    </a-button>
                </a-space>
            </div>
        </div>

        <!-- 内容区域 -->
        <div class="preview-content">
            <!-- 加载中 -->
            <div v-if="loading" class="preview-loading">
                <a-spin size="large" tip="正在转换文档..." />
            </div>

            <!-- 未配置 -->
            <div v-else-if="notConfigured" class="preview-error">
                <SettingsIcon :size="48" />
                <p class="error-text">转换 API 未配置</p>
                <p class="error-sub-text">请联系管理员在系统设置中启用 Office 预览功能</p>
                <a-button type="primary" @click="handleDownload">
                    <template #icon><DownloadIcon :size="14" /></template>
                    下载原文件
                </a-button>
            </div>

            <!-- 加载失败 -->
            <div v-else-if="error" class="preview-error">
                <FileXIcon :size="48" />
                <p class="error-text">{{ error }}</p>
                <a-button type="primary" @click="handleDownload">
                    <template #icon><DownloadIcon :size="14" /></template>
                    下载原文件
                </a-button>
            </div>

            <!-- PDF 预览 -->
            <div v-else-if="pdfData" class="pdf-container" @click.stop>
                <VuePdfEmbed :source="pdfData" />
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue';
import VuePdfEmbed from 'vue-pdf-embed';
import { XIcon, DownloadIcon, FileXIcon, SettingsIcon } from 'lucide-vue-next';
import { systemSettingApi } from '@/api';

const props = defineProps({
    open: Boolean,
    filePath: String,
    fileName: String
});

const emit = defineEmits(['update:open']);

const visible = ref(false);
const loading = ref(false);
const error = ref('');
const pdfData = ref(null);
const notConfigured = ref(false);

const isPdfFile = (path) => {
    return path?.toLowerCase().endsWith('.pdf');
};

const loadPdf = async () => {
    if (!props.filePath) return;

    loading.value = true;
    error.value = '';
    pdfData.value = null;
    notConfigured.value = false;

    try {
        const token = localStorage.getItem('auth_token');
        const headers = { 'Authorization': `Bearer ${token}` };

        if (isPdfFile(props.filePath)) {
            // PDF 文件直接加载，无需转换
            const response = await fetch(props.filePath, { headers });

            if (!response.ok) {
                throw new Error(`加载失败 (HTTP ${response.status})`);
            }

            const arrayBuffer = await response.arrayBuffer();
            pdfData.value = new Uint8Array(arrayBuffer);
        } else {
            // Office 文件需要通过转换 API
            const config = await systemSettingApi.getOfficePreviewConfig();
            if (!config?.enabled) {
                notConfigured.value = true;
                loading.value = false;
                return;
            }

            const relativePath = props.filePath.replace(/^\//, '');
            const url = `/api/office/preview?path=${encodeURIComponent(relativePath)}`;

            const response = await fetch(url, { headers });

            if (!response.ok) {
                throw new Error(response.status === 503
                    ? '文档转换服务不可用，请稍后再试'
                    : `加载失败 (HTTP ${response.status})`);
            }

            const arrayBuffer = await response.arrayBuffer();
            pdfData.value = new Uint8Array(arrayBuffer);
        }
    } catch (e) {
        error.value = e.message || '文档加载失败';
    } finally {
        loading.value = false;
    }
};

const close = () => {
    visible.value = false;
};

const handleDownload = () => {
    if (props.filePath) {
        window.open(props.filePath, '_blank');
    }
};

watch(() => props.open, (val) => {
    visible.value = val;
    if (val) {
        loadPdf();
    } else {
        pdfData.value = null;
        error.value = '';
        notConfigured.value = false;
    }
});

watch(visible, (val) => {
    emit('update:open', val);
});

const handleKeydown = (e) => {
    if (!visible.value) return;
    if (e.key === 'Escape') {
        close();
    }
};

onMounted(() => {
    window.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
    window.removeEventListener('keydown', handleKeydown);
});
</script>

<style scoped>
.office-preview-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.85);
    z-index: 1000;
    display: flex;
    flex-direction: column;
}

.top-toolbar-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 16px;
    background: rgba(0, 0, 0, 0.6);
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    flex-shrink: 0;
}

.toolbar-left {
    flex: 1;
    overflow: hidden;
}

.file-name {
    color: rgba(255, 255, 255, 0.9);
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.toolbar-right {
    flex-shrink: 0;
}

.toolbar-btn {
    color: rgba(255, 255, 255, 0.8);
    background: transparent;
    border: none;
    width: 32px;
    height: 32px;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}

.toolbar-btn:hover {
    color: #fff;
    background: rgba(255, 255, 255, 0.1);
}

.preview-content {
    flex: 1;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
}

.preview-loading {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
}

.preview-error {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 16px;
    color: rgba(255, 255, 255, 0.7);
}

.error-text {
    font-size: 14px;
    margin: 0;
}

.error-sub-text {
    font-size: 12px;
    color: rgba(255, 255, 255, 0.5);
    margin: -8px 0 0 0;
}

.pdf-container {
    flex: 1;
    overflow-y: auto;
    padding: 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 100%;
    height: 100%;
}

.pdf-container :deep(.vue-pdf-embed) {
    width: 100%;
    max-width: 900px;
}

.pdf-container :deep(.vue-pdf-embed > div) {
    width: 100%;
}

.pdf-container :deep(canvas) {
    width: 100% !important;
    height: auto !important;
}
</style>
