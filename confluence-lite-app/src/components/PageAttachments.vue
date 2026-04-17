<template>
    <a-drawer
        :open="open"
        @update:open="$emit('update:open', $event)"
        title="附件"
        width="520"
        :body-style="{ padding: '0' }"
    > 
        <a-spin v-if="loading" style="display:block;padding:2rem;text-align:center;" />
        <div v-else-if="attachments.length === 0" style="padding:2rem;text-align:center;color:#6b778c;">暂无附件</div>
        <div v-else>
            <div v-for="item in attachments" :key="item.id" class="attachment-item">
                <div class="attachment-icon">
                    <FileIcon :content-type="item.contentType" />
                </div>
                <div class="attachment-info">
                    <a :href="`/${item.storagePath}`" target="_blank" class="attachment-name">{{ item.fileName }}</a>
                    <span class="attachment-meta">
                        {{ formatSize(item.fileSize) }} · {{ formatTime(item.createdAt) }}
                    </span>
                </div>
                <a-button type="text" size="small" danger @click="deleteAttachment(item.id)">
                    <template #icon><Trash2 :size="14" /></template>
                </a-button>
            </div>
        </div>
    </a-drawer>
</template>

<script setup>
import { ref, watch, h } from 'vue'
import { message } from 'ant-design-vue'
import { Trash2, FileText, Image, FileArchive, FileSpreadsheet, File, Film, Music } from 'lucide-vue-next'
import { attachmentApi } from '../api'

const props = defineProps({
    open: { type: Boolean, default: false },
    pageId: { type: [String, Number], required: true }
})

const emit = defineEmits(['update:open', 'changed'])

const attachments = ref([])
const loading = ref(false)

const FileIcon = {
    props: { contentType: String },
    setup(props) {
        const iconMap = {
            'image': Image,
            'pdf': FileText,
            'zip': FileArchive,
            'word': FileText,
            'excel': FileSpreadsheet,
            'ppt': FileText,
            'text': FileText,
            'video': Film,
            'audio': Music,
        }
        return () => {
            const ct = props.contentType || ''
            const key = Object.keys(iconMap).find(k => ct.includes(k)) || 'text'
            const Icon = iconMap[key] || FileText
            return h(Icon, { size: 20, color: '#6b778c' })
        }
    }
}

function formatSize(bytes) {
    if (!bytes) return '0 B'
    const units = ['B', 'KB', 'MB', 'GB']
    let i = 0
    let size = bytes
    while (size >= 1024 && i < units.length - 1) {
        size /= 1024
        i++
    }
    return `${size.toFixed(i === 0 ? 0 : 1)} ${units[i]}`
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

async function loadAttachments() {
    loading.value = true
    try {
        attachments.value = await attachmentApi.getListByPage(props.pageId) || []
    } catch (e) {
        console.error('加载附件列表失败:', e)
    } finally {
        loading.value = false
    }
}

async function handleUpload({ file }) {
    try {
        await attachmentApi.upload(props.pageId, file)
        message.success('上传成功')
        await loadAttachments()
        emit('changed')
    } catch (e) {
        console.error('上传失败:', e)
        message.error('上传失败')
    }
}

async function deleteAttachment(id) {
    if (!confirm('确定要删除此附件吗？')) return
    try {
        await attachmentApi.remove(id)
        await loadAttachments()
        emit('changed')
    } catch (e) {
        console.error('删除附件失败:', e)
    }
}

watch(() => props.open, (val) => {
    if (val) loadAttachments()
})
</script>

<style scoped>
.upload-area {
    padding: 12px 16px;
    border-bottom: 1px solid #f0f0f0;
}
.attachment-item {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 10px 16px;
    border-bottom: 1px solid #f0f0f0;
}
.attachment-item:last-child {
    border-bottom: none;
}
.attachment-icon {
    flex-shrink: 0;
    width: 32px;
    height: 32px;
    display: flex;
    align-items: center;
    justify-content: center;
}
.attachment-info {
    display: flex;
    flex-direction: column;
    gap: 2px;
    flex: 1;
    min-width: 0;
}
.attachment-name {
    font-size: 14px;
    color: #0052cc;
    font-weight: 500;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
.attachment-name:hover {
    text-decoration: underline;
}
.attachment-meta {
    font-size: 12px;
    color: #6b778c;
}
</style>
