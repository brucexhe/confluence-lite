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
                <a-button type="link" size="small" @click="viewingVersion = null">← 返回列表</a-button>
                <span style="font-weight:500;">v{{ viewingVersion.versionNumber }} {{ viewingVersion.title }}</span>
            </div>
            <div style="padding: 0 16px 16px; font-size: 12px; color: #6b778c;">
                {{ editorName(viewingVersion) }}
                · {{ formatTime(viewingVersion.createdAt) }}
            </div>
            <div class="version-actions" v-if="!isCurrentVersion(viewingVersion)">
                <a-button type="primary" @click="restoreVersion" :loading="restoring">
                    恢复此版本
                </a-button>
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

.version-actions {
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
