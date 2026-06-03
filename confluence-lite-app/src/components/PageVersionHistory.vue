<template>
    <a-drawer
        :open="open"
        @update:open="$emit('update:open', $event)"
        title="版本历史"
        width="520"
        :body-style="{ padding: '0' }"
    >
        <a-spin v-if="loading" style="display:block;padding:2rem;text-align:center;" />
        <template v-else-if="viewingVersion">
            <div class="version-detail-header">

                <span style="font-weight:500;" class="version-number">v{{ viewingVersion.versionNumber }} </span>
                <span class="bold">{{ viewingVersion.title }}</span>
            </div>
            <div  class="page-meta">
                {{ editorName(viewingVersion) }}
                · {{ formatTime(viewingVersion.createdAt) }}
            </div>

            <div class="version-content" v-html="viewingVersion.content"></div>

            <div class="version-actions" v-if="!isCurrentVersion(viewingVersion)">
                <a-button type="link" size="small" @click="viewingVersion = null">← 返回列表</a-button>
                <a-button type="primary" @click="restoreVersion" :loading="restoring">
                    恢复此版本
                </a-button>
            </div>
        </template>
        <template v-else>
            <div v-if="versions.length === 0" style="padding:2rem;text-align:center;color:#6b778c;">暂无历史版本</div>
            <div v-for="v in versions" :key="v.id" class="version-item">
                <div class="version-item-main" @click="viewVersion(v.id)">
                    <span class="version-number">v{{ v.versionNumber }}</span>
                    <div class="version-item-info">
                        <span class="version-title">{{ v.title }}</span>
                        <span class="version-meta">
                            {{ editorName(v) }}
                            · {{ formatTime(v.createdAt) }}
                        </span>
                    </div>
                </div>
                <a-button type="text" size="small" danger @click="deleteVersion(v.id)">删除</a-button>
            </div>
        </template>
    </a-drawer>
</template>

<script setup>
import { ref, watch } from 'vue'
import { message } from 'ant-design-vue'
import { pageApi } from '../api'

const props = defineProps({
    open: { type: Boolean, default: false },
    pageId: { type: [String, Number], required: true }
})

const emit = defineEmits(['update:open', 'restored'])

const versions = ref([])
const loading = ref(false)
const viewingVersion = ref(null)
const restoring = ref(false)

function editorName(v) {
    return v.editor?.displayName || v.editor?.username || 'Unknown'
}

function isCurrentVersion(v) {
    if (!v || versions.value.length === 0) return false
    return v.versionNumber === versions.value[0].versionNumber
}

function formatTime(dateStr) {
    if (!dateStr) return ''
    const date = new Date(dateStr)
    const now = new Date()
    const diff = now - date
    const minutes = Math.floor(diff / 60000)
    if (minutes < 1) return '刚刚'
    if (minutes < 60) return `${minutes} 分钟前`
    const hours = Math.floor(minutes / 60)
    if (hours < 24) return `${hours} 小时前`
    const days = Math.floor(hours / 24)
    if (days < 30) return `${days} 天前`
    return date.toLocaleDateString('zh-CN')
}

async function loadVersions() {
    loading.value = true
    viewingVersion.value = null
    try {
        const data = await pageApi.getVersions(props.pageId)
        versions.value = data || []
    } catch (e) {
        console.error('加载版本历史失败:', e)
    } finally {
        loading.value = false
    }
}

async function viewVersion(versionId) {
    try {
        const data = await pageApi.getVersion(versionId)
        viewingVersion.value = data
    } catch (e) {
        console.error('加载版本详情失败:', e)
    }
}

async function deleteVersion(versionId) {
    if (!confirm('确定要删除此版本吗？')) return
    try {
        await pageApi.deleteVersion(versionId)
        await loadVersions()
    } catch (e) {
        console.error('删除版本失败:', e)
    }
}

async function restoreVersion() {
    if (!viewingVersion.value) return
    restoring.value = true
    try {
        await pageApi.update(props.pageId, {
            title: viewingVersion.value.title,
            content: viewingVersion.value.content,
        })
        message.success('已恢复到该版本')
        emit('update:open', false)
        emit('restored')
    } catch (e) {
        console.error('恢复版本失败:', e)
        message.error('恢复版本失败')
    } finally {
        restoring.value = false
    }
}

watch(() => props.open, (val) => {
    if (val) loadVersions()
})
</script>

<style scoped>
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

.page-meta{
    padding: 0 55px 10px; font-size: 12px; color: #6b778c;border-bottom: 1px solid #f0f0f0;
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
}

.version-actions {
    position: sticky;
    bottom: 0;
    padding: 12px 16px;
    background: #fff;
    border-top: 1px solid #f0f0f0;
    display: flex;
    align-items: center;
    justify-content: space-between;
    z-index: 10;
}

.version-content {
    padding: 16px;
    font-size: 14px;
    line-height: 1.714;
    color: #172b4d;
    word-break: break-word;
}

.version-content :deep(img) {
    max-width: 100%;
    height: auto;
}

.version-content :deep(.lead-text) {
    font-size: 14px;
    color: #172b4d;
    line-height: 1.714;
    margin-bottom: 16px;
}

.version-content :deep(ul),
.version-content :deep(ol) {
    padding-left: 1.5em;
    margin-bottom: 12px;
}

.version-content :deep(li) {
    font-size: 14px;
    line-height: 1.714;
    color: #172b4d;
    margin-bottom: 4px;
}

.version-content :deep(h2) {
    font-size: 20px;
    font-weight: 500;
    color: #172b4d;
    margin-top: 24px;
    margin-bottom: 12px;
}

.version-content :deep(p) {
    font-size: 14px;
    margin-bottom: 12px;
    line-height: 1.714;
    color: #172b4d;
    word-break: break-word;
}

.version-content :deep(pre) {
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

.version-content :deep(pre code) {
    background: none;
    padding: 0;
    border-radius: 0;
    font-size: inherit;
    color: inherit;
}

.version-content :deep(pre[class*="language-"]) {
    padding-left: 3.8em;
}

.version-content :deep(pre[class*="language-"] .line-numbers-rows) {
    border-right: 1px solid #dfe1e6;
}

.version-content :deep(pre .code-copy-btn) {
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

.version-content :deep(pre:hover .code-copy-btn) {
    opacity: 1;
}

.version-content :deep(:not(pre) > code) {
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

.version-content :deep(table) {
    border-collapse: collapse !important;
    margin: 16px 0;
    border: 1px solid #dfe1e6 !important;
    font-size: 14px;
}

.version-content :deep(table th),
.version-content :deep(table td) {
    border: 1px solid #dfe1e6 !important;
    padding: 8px 12px;
    text-align: left;
    vertical-align: top;
    line-height: 1.5;
}

.version-content :deep(table th) {
    background: #f4f5f7 center right no-repeat;
    color: #172b4d;
    font-weight: 600;
    cursor: pointer;
    padding-right: 24px;
}
</style>
