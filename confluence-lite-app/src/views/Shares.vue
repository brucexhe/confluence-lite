<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('shares.title') }}</h1>
            <p class="page-description">{{ $t('shares.description') }}</p>
        </div>

        <div class="page-content">
            <!-- 工具栏 -->
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        :placeholder="$t('shares.searchPlaceholder')"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                    <a-select
                        v-model:value="filterStatus"
                        :placeholder="$t('shares.filterStatus')"
                        style="width: 130px"
                        allow-clear
                        :options="statusOptions"
                        @change="handleSearch"
                    />
                </a-space>
                <a-space v-if="selectedRowKeys.length > 0">
                    <a-button danger @click="batchDelete">
                        {{ $t('shares.batchDelete', { count: selectedRowKeys.length }) }}
                    </a-button>
                    <a-button @click="selectedRowKeys = []">{{ $t('shares.cancelSelection') }}</a-button>
                </a-space>
            </div>

            <!-- 分享列表 -->
            <a-table
                :columns="columns"
                :data-source="filteredShares"
                :loading="loading"
                :row-selection="rowSelection"
                row-key="id"
                :pagination="{ pageSize: 20, showSizeChanger: true, showTotal: (total) => t('common.total', { n: total }) }"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'page'">
                        <a @click="copyShareLink(record)" style="font-weight: 500; color: #172b4d;">
                            {{ record.page?.title || '-' }}
                        </a>
                    </template>
                    <template v-else-if="column.key === 'code'">
                        <a-typography-paragraph
                            :copyable="{ text: getShareUrl(record), tooltip: [t('shares.copyShareLink'), t('shares.copyLinkCopied')] }"
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
                            <a-tag color="blue">{{ $t('shares.anyone') }}</a-tag>
                        </template>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-space>
                            <a-tag v-if="record.hasPassword" color="orange">{{ $t('shares.passwordProtect') }}</a-tag>
                            <a-tag v-if="record.allowEdit" color="green">{{ $t('shares.editable') }}</a-tag>
                            <a-tag v-if="record.isExpired" color="red">{{ $t('shares.expired') }}</a-tag>
                            <a-tag v-if="!record.isExpired" color="blue">{{ $t('shares.active') }}</a-tag>
                        </a-space>
                    </template>
                    <template v-else-if="column.key === 'expireAt'">
                        <template v-if="record.expireAt">
                            <span :class="{ 'text-expired': record.isExpired }">
                                {{ formatDateTime(record.expireAt) }}
                            </span>
                        </template>
                        <template v-else>
                            <span class="text-muted">{{ $t('shares.neverExpires') }}</span>
                        </template>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span class="text-muted">{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space split>
                            <a-button type="link" size="small" @click="copyShareLink(record)">
                                {{ $t('shares.copyLink') }}
                            </a-button>
                            <a-button type="link" size="small" @click="openEditModal(record)">
                                {{ $t('shares.editBtn') }}
                            </a-button>
                            <a-button type="link" size="small" danger @click="handleDelete(record.id)">
                                {{ $t('shares.deleteBtn') }}
                            </a-button>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 编辑分享弹窗 -->
        <a-modal
            v-model:open="editVisible"
            :title="$t('shares.editShare')"
            :ok-text="$t('shares.saveBtn')"
            :cancel-text="$t('shares.cancelBtn')"
            @ok="handleEditOk"
            :confirm-loading="editLoading"
        >
            <a-form :label-col="{ span: 6 }" :wrapper-col="{ span: 18 }" style="margin-top: 16px;">
                <a-form-item :label="$t('shares.allowEdit')">
                    <a-switch v-model:checked="editForm.allowEdit" />
                </a-form-item>
                <a-form-item :label="$t('shares.expireTime')">
                    <a-date-picker
                        v-model:value="editForm.expireAt"
                        show-time
                        format="YYYY-MM-DD HH:mm:ss"
                        :placeholder="$t('shares.selectExpireTime')"
                        style="width: 100%"
                        :disabled-date="disabledDate"
                    />
                    <div style="margin-top: 4px;">
                        <a-button type="link" size="small" @click="editForm.expireAt = null" :disabled="!editForm.expireAt">
                            {{ $t('shares.setNeverExpire') }}
                        </a-button>
                    </div>
                </a-form-item>
                <a-form-item :label="$t('shares.visitPassword')">
                    <a-input-password
                        v-model:value="editForm.visitPassword"
                        :placeholder="$t('shares.passwordPlaceholder')"
                        :maxlength="50"
                    />
                    <div v-if="editingShare?.hasPassword && !editForm.visitPassword" style="margin-top: 4px;">
                        <a-typography-text type="warning">
                            {{ $t('shares.clearPasswordWarning') }}
                        </a-typography-text>
                    </div>
                </a-form-item>
                <a-form-item v-if="editingShare" :label="$t('shares.shareLink')">
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
import { useI18n } from 'vue-i18n'

const { t } = useI18n()
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
    { label: t('shares.statusExpired'), value: 'expired' },
    { label: t('shares.statusActive'), value: 'active' },
    { label: t('shares.statusPassword'), value: 'password' },
    { label: t('shares.statusEditable'), value: 'editable' }
]

const rowSelection = computed(() => ({
    selectedRowKeys: selectedRowKeys.value,
    onChange: (keys) => {
        selectedRowKeys.value = keys
    }
}))

const columns = [
    { title: t('shares.columnPage'), key: 'page' },
    { title: t('shares.columnSharedWith'), key: 'sharedWith', width: 110 },
    { title: t('shares.columnStatus'), key: 'status', width: 200 },
    { title: t('shares.columnExpireAt'), key: 'expireAt', width: 170 },
    { title: t('shares.columnCreatedAt'), key: 'createdAt', width: 170 },
    { title: t('shares.columnAction'), key: 'action', width: 180 }
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
    message.success(t('shares.linkCopied'))
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
        message.success(t('shares.shareUpdated'))

        // 更新列表中的对应项
        const idx = shares.value.findIndex(s => s.id === editingShare.value.id)
        if (idx !== -1) {
            shares.value[idx] = updated
        }

        editVisible.value = false
    } catch {
        message.error(t('shares.updateFailed'))
    } finally {
        editLoading.value = false
    }
}

const handleSearch = () => {
    // filteredShares is computed, auto-updates
}

const batchDelete = () => {
    if (selectedRowKeys.value.length === 0) {
        message.warning(t('shares.selectToDelete'))
        return
    }

    Modal.confirm({
        title: t('shares.confirmDeleteTitle'),
        content: t('shares.confirmBatchDelete', { count: selectedRowKeys.value.length }),
        okText: t('common.delete'),
        okType: 'danger',
        cancelText: t('common.cancel'),
        onOk: async () => {
            try {
                for (const id of selectedRowKeys.value) {
                    await shareApi.remove(id)
                }
                message.success(t('shares.batchDeleteSuccess', { count: selectedRowKeys.value.length }))
                selectedRowKeys.value = []
                loadShares()
            } catch {
                message.error(t('shares.batchDeleteFailed'))
            }
        }
    })
}

const handleDelete = (id) => {
    Modal.confirm({
        title: t('shares.confirmDeleteTitle'),
        content: t('shares.confirmSingleDelete'),
        okText: t('common.delete'),
        okType: 'danger',
        cancelText: t('common.cancel'),
        onOk: async () => {
            try {
                await shareApi.remove(id)
                message.success(t('shares.deleteSuccess'))
                loadShares()
            } catch {
                message.error(t('shares.deleteFailed'))
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
        message.error(t('shares.loadFailed'))
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

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
    .settings-page {
        margin: 12px 1rem;
    }

    .toolbar {
        flex-direction: column;
        align-items: stretch;
    }

    .toolbar :deep(.ant-space) {
        width: 100%;
    }

    .toolbar :deep(.ant-space .ant-input-search) {
        width: 100% !important;
    }

    .toolbar :deep(.ant-space .ant-select) {
        width: 100% !important;
    }

    /* Table horizontal scroll on mobile */
    :deep(.ant-table) {
        overflow-x: auto;
    }

    :deep(.ant-table-wrapper) {
        overflow-x: auto;
    }
}
</style>
