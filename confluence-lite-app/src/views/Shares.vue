<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>分享管理</h1>
            <p class="page-description">管理我创建的所有页面分享链接</p>
        </div>

        <div class="page-content">
            <!-- 工具栏 -->
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        placeholder="搜索页面标题"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                    <a-select
                        v-model:value="filterStatus"
                        placeholder="筛选状态"
                        style="width: 130px"
                        allow-clear
                        :options="statusOptions"
                        @change="handleSearch"
                    />
                </a-space>
                <a-space v-if="selectedRowKeys.length > 0">
                    <a-button danger @click="batchDelete">
                        批量删除 ({{ selectedRowKeys.length }})
                    </a-button>
                    <a-button @click="selectedRowKeys = []">取消选择</a-button>
                </a-space>
            </div>

            <!-- 分享列表 -->
            <a-table
                :columns="columns"
                :data-source="filteredShares"
                :loading="loading"
                :row-selection="rowSelection"
                row-key="id"
                :pagination="{ pageSize: 20, showSizeChanger: true, showTotal: (total) => `共 ${total} 条` }"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'page'">
                        <a @click="copyShareLink(record)" style="font-weight: 500; color: #172b4d;">
                            {{ record.page?.title || '-' }}
                        </a>
                    </template>
                    <template v-else-if="column.key === 'code'">
                        <a-typography-paragraph
                            :copyable="{ text: getShareUrl(record), tooltip: ['复制链接', '已复制'] }"
                            style="margin: 0;"
                        >
                            <a :href="getShareUrl(record)" target="_blank" class="share-link">
                                {{ record.code }}
                            </a>
                        </a-typography-paragraph>
                    </template>
                    <template v-else-if="column.key === 'sharedWith'">
                        <template v-if="record.sharedWith">
                            <a-tag color="purple">{{ record.sharedWith.displayName || record.sharedWith.username }}</a-tag>
                        </template>
                        <template v-else>
                            <a-tag color="blue">任何人</a-tag>
                        </template>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-space>
                            <a-tag v-if="record.hasPassword" color="orange">🔒 密码保护</a-tag>
                            <a-tag v-if="record.allowEdit" color="green">✏️ 可编辑</a-tag>
                            <a-tag v-if="record.isExpired" color="red">已过期</a-tag>
                            <a-tag v-if="!record.isExpired" color="blue">有效</a-tag>
                        </a-space>
                    </template>
                    <template v-else-if="column.key === 'expireAt'">
                        <template v-if="record.expireAt">
                            <span :class="{ 'text-expired': record.isExpired }">
                                {{ formatDateTime(record.expireAt) }}
                            </span>
                        </template>
                        <template v-else>
                            <span class="text-muted">永不过期</span>
                        </template>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span class="text-muted">{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space split>
                            <a-button type="link" size="small" @click="copyShareLink(record)">
                                复制链接
                            </a-button>
                            <a-button type="link" size="small" @click="openEditModal(record)">
                                编辑
                            </a-button>
                            <a-button type="link" size="small" danger @click="handleDelete(record.id)">
                                删除
                            </a-button>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 编辑分享弹窗 -->
        <a-modal
            v-model:open="editVisible"
            title="编辑分享设置"
            ok-text="保存"
            cancel-text="取消"
            @ok="handleEditOk"
            :confirm-loading="editLoading"
        >
            <a-form :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }" style="margin-top: 16px;">
                <a-form-item label="允许编辑">
                    <a-switch v-model:checked="editForm.allowEdit" />
                </a-form-item>
                <a-form-item label="过期时间">
                    <a-date-picker
                        v-model:value="editForm.expireAt"
                        show-time
                        format="YYYY-MM-DD HH:mm:ss"
                        placeholder="选择过期时间"
                        style="width: 100%"
                        :disabled-date="disabledDate"
                    />
                    <div style="margin-top: 4px;">
                        <a-button type="link" size="small" @click="editForm.expireAt = null" :disabled="!editForm.expireAt">
                            设为永不过期
                        </a-button>
                    </div>
                </a-form-item>
                <a-form-item label="访问密码">
                    <a-input-password
                        v-model:value="editForm.visitPassword"
                        placeholder="留空表示无密码保护"
                        :maxlength="50"
                    />
                    <div v-if="editingShare?.hasPassword && !editForm.visitPassword" style="margin-top: 4px;">
                        <a-typography-text type="warning">
                            清空密码将移除密码保护
                        </a-typography-text>
                    </div>
                </a-form-item>
                <a-form-item v-if="editingShare" label="分享链接">
                    <a-typography-paragraph
                        :copyable="{ text: getShareUrl(editingShare) }"
                        :content="getShareUrl(editingShare)"
                        style="margin: 0;"
                    />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { shareApi } from '@/api'
import { formatDateTime } from '@/utils/format'
import dayjs from 'dayjs'

const loading = ref(false)
const searchText = ref('')
const filterStatus = ref(undefined)
const selectedRowKeys = ref([])
const shares = ref([])

// 编辑弹窗
const editVisible = ref(false)
const editLoading = ref(false)
const editingShare = ref(null)
const editForm = reactive({
    allowEdit: false,
    expireAt: null,
    visitPassword: ''
})

const statusOptions = [
    { label: '已过期', value: 'expired' },
    { label: '有效', value: 'active' },
    { label: '有密码', value: 'password' },
    { label: '可编辑', value: 'editable' }
]

const rowSelection = computed(() => ({
    selectedRowKeys: selectedRowKeys.value,
    onChange: (keys) => {
        selectedRowKeys.value = keys
    }
}))

const columns = [
    { title: '页面', key: 'page' }, 
    { title: '分享对象', key: 'sharedWith', width: 110 },
    { title: '状态', key: 'status', width: 200 },
    { title: '过期时间', key: 'expireAt', width: 170 },
    { title: '创建时间', key: 'createdAt', width: 170 },
    { title: '操作', key: 'action', width: 180 }
]

const getShareUrl = (share) => {
    return `${window.location.origin}/share/${share.code}`
}

const filteredShares = computed(() => {
    let result = shares.value

    // 搜索过滤
    if (searchText.value) {
        const keyword = searchText.value.toLowerCase()
        result = result.filter(s =>
            s.page?.title?.toLowerCase().includes(keyword) ||
            s.code?.toLowerCase().includes(keyword)
        )
    }

    // 状态过滤
    if (filterStatus.value) {
        switch (filterStatus.value) {
            case 'expired':
                result = result.filter(s => s.isExpired)
                break
            case 'active':
                result = result.filter(s => !s.isExpired)
                break
            case 'password':
                result = result.filter(s => s.hasPassword)
                break
            case 'editable':
                result = result.filter(s => s.allowEdit)
                break
        }
    }

    return result
})

const disabledDate = (current) => {
    return current && current < dayjs().startOf('day')
}

const copyShareLink = (share) => {
    const url = getShareUrl(share)
    navigator.clipboard.writeText(url)
    message.success('分享链接已复制到剪贴板')
}

const openEditModal = (share) => {
    editingShare.value = share
    editForm.allowEdit = share.allowEdit
    editForm.expireAt = share.expireAt ? dayjs(share.expireAt) : null
    editForm.visitPassword = ''
    editVisible.value = true
}

const handleEditOk = async () => {
    editLoading.value = true
    try {
        const data = {
            allowEdit: editForm.allowEdit,
            expireAt: editForm.expireAt ? editForm.expireAt.toISOString() : null,
            visitPassword: editForm.visitPassword || null
        }
        const updated = await shareApi.update(editingShare.value.id, data)
        message.success('分享设置已更新')

        // 更新列表中的对应项
        const idx = shares.value.findIndex(s => s.id === editingShare.value.id)
        if (idx !== -1) {
            shares.value[idx] = updated
        }

        editVisible.value = false
    } catch {
        message.error('更新分享设置失败')
    } finally {
        editLoading.value = false
    }
}

const handleSearch = () => {
    // filteredShares is computed, auto-updates
}

const batchDelete = () => {
    if (selectedRowKeys.value.length === 0) {
        message.warning('请先选择要删除的分享')
        return
    }

    Modal.confirm({
        title: '确认删除',
        content: `确定要删除选中的 ${selectedRowKeys.value.length} 个分享吗？`,
        okText: '删除',
        okType: 'danger',
        cancelText: '取消',
        onOk: async () => {
            try {
                for (const id of selectedRowKeys.value) {
                    await shareApi.remove(id)
                }
                message.success(`成功删除 ${selectedRowKeys.value.length} 个分享`)
                selectedRowKeys.value = []
                loadShares()
            } catch {
                message.error('批量删除失败')
            }
        }
    })
}

const handleDelete = (id) => {
    Modal.confirm({
        title: '确认删除',
        content: '确定要删除该分享吗？删除后分享链接将失效。',
        okText: '删除',
        okType: 'danger',
        cancelText: '取消',
        onOk: async () => {
            try {
                await shareApi.remove(id)
                message.success('分享已删除')
                loadShares()
            } catch {
                message.error('删除分享失败')
            }
        }
    })
}

const loadShares = async () => {
    loading.value = true
    try {
        const data = await shareApi.listMy()
        shares.value = data || []
    } catch {
        message.error('加载分享列表失败')
    } finally {
        loading.value = false
    }
}

onMounted(() => {
    loadShares()
})
</script>

<style scoped>
.settings-page {
    background-color: #ffffff;
    border-radius: 4px; 
    margin: 20px 2rem;
}

.page-header {
    padding: 20px 0 20px;
    border-bottom: 1px solid #dfe1e6;
}

.page-header h1 {
    font-size: 20px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 4px 0;
}

.page-description {
    font-size: 13px;
    color: #6b778c;
    margin: 0;
}

.page-content {
    padding: 20px 0 20px;
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;
    flex-wrap: wrap;
    gap: 12px;
}

.share-link {
    font-family: 'SFMono-Regular', Consolas, monospace;
    font-size: 13px;
}

.text-muted {
    color: #6b778c;
    font-size: 13px;
}

.text-expired {
    color: #ff4d4f;
    font-size: 13px;
}

:deep(.ant-table-wrapper .ant-table-tbody > tr > td) {
    padding: 10px 10px;
}
</style>
