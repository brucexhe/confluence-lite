<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.pages.title') }}</h1>
            <p class="page-description">{{ $t('settings.pages.description') }}</p>
        </div>

        <div class="page-content">
            <!-- 工具栏 -->
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        :placeholder="$t('settings.pages.searchPlaceholder')"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                    <a-select
                        v-model:value="filterWorkspace"
                        :placeholder="$t('settings.pages.filterWorkspace')"
                        style="width: 150px"
                        allow-clear
                        :options="workspaceOptions"
                        @change="handleSearch"
                    />
                    <a-select
                        v-model:value="filterStatus"
                        :placeholder="$t('settings.pages.filterStatus')"
                        style="width: 120px"
                        allow-clear
                        :options="statusOptions"
                        @change="handleSearch"
                    />
                </a-space>
                <a-space v-if="selectedRowKeys.length > 0">
                    <a-button danger @click="batchDelete">
                        {{ $t('settings.pages.batchDelete') }} ({{ selectedRowKeys.length }})
                    </a-button>
                    <a-button @click="selectedRowKeys = []">{{ $t('settings.pages.cancelSelect') }}</a-button>
                </a-space>
            </div>

            <!-- 页面列表 -->
            <a-table
                :columns="columns"
                :data-source="pages"
                :loading="loading"
                :pagination="pagination"
                :row-selection="rowSelection"
                @change="handleTableChange"
                row-key="id"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'title'">
                        <div class="page-title">
                            <a @click="$router.push(`/${record.workspace?.key}/page/${record.id}`)">
                                {{ record.title }}
                            </a>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'space'">
                        <a-tag color="blue">{{ record.workspace?.name }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'creator'">
                        <span>{{ record.creator?.displayName || record.creator?.username || '-' }}</span>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-tag :color="getStatusColor(record.status)">
                            {{ getStatusText(record.status) }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'updatedAt'">
                        <span class="text-muted">{{ formatDateTime(record.updatedAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space split>
                            <a-button type="link" size="small" @click="$router.push(`/${record.workspace?.key}/page/${record.id}`)">
                                {{ $t('common.view') }}
                            </a-button>
                            <a-button type="link" size="small" @click="$router.push(`/${record.workspace?.key}/page/${record.id}/edit`)">
                                {{ $t('common.edit') }}
                            </a-button>
                            <a-dropdown>
                                <template #overlay>
                                    <a-menu>
                                        <a-menu-item @click="copyPageLink(record)">
                                            {{ $t('settings.pages.copyLink') }}
                                        </a-menu-item>
                                        <a-menu-item @click="viewHistory(record)">
                                            {{ $t('settings.pages.viewHistory') }}
                                        </a-menu-item>
                                        <a-menu-divider />
                                        <a-menu-item danger @click="handleDelete(record.id)">
                                            {{ $t('settings.pages.deletePage') }}
                                        </a-menu-item>
                                    </a-menu>
                                </template>
                                <a-button type="link" size="small">
                                    {{ $t('common.more') }}
                                </a-button>
                            </a-dropdown>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 版本历史弹窗 -->
        <a-modal
            v-model:open="historyVisible"
            :title="$t('settings.pages.pageHistory') + ' - ' + historyPageTitle"
            footer={null}
            width="700px"
        >
            <a-table
                :columns="historyColumns"
                :data-source="versions"
                :loading="versionsLoading"
                size="small"
                row-key="id"
                :pagination="false"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'editor'">
                        {{ record.editor?.displayName || record.editor?.username || '-' }}
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        {{ formatDateTime(record.createdAt) }}
                    </template>
                </template>
            </a-table>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { message, Modal } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { workspaceApi, pageApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
const loading = ref(false)
const searchText = ref('')
const filterWorkspace = ref(undefined)
const filterStatus = ref(undefined)
const selectedRowKeys = ref([])
const pages = ref([])
const workspaces = ref([])

// 版本历史
const historyVisible = ref(false)
const historyPageTitle = ref('')
const versions = ref([])
const versionsLoading = ref(false)

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0,
    showSizeChanger: true,
    showTotal: (total) => t('common.total', { n: total })
})

const workspaceOptions = computed(() => {
    return workspaces.value.map(ws => ({
        label: ws.name,
        value: ws.id
    }))
})

const statusOptions = [
    { label: t('settings.pages.published'), value: 1 },
    { label: t('settings.pages.draft'), value: 0 },
    { label: t('settings.pages.archived'), value: 2 }
]

const rowSelection = computed(() => ({
    selectedRowKeys: selectedRowKeys.value,
    onChange: (keys) => {
        selectedRowKeys.value = keys
    }
}))

const columns = [
    { title: t('settings.pages.pageTitle'), key: 'title' },
    { title: t('settings.pages.space'), key: 'space' },
    { title: t('settings.pages.creator'), key: 'creator' },
    { title: t('settings.pages.status'), key: 'status' },
    { title: t('settings.pages.updatedAt'), dataIndex: 'updatedAt', key: 'updatedAt' },
    { title: t('common.action'), key: 'action', width: 180 }
]

const historyColumns = [
    { title: t('settings.pages.version'), dataIndex: 'versionNumber', key: 'versionNumber', width: 60 },
    { title: t('settings.pages.historyTitle'), dataIndex: 'title', key: 'title' },
    { title: t('settings.pages.editor'), key: 'editor', width: 120 },
    { title: t('settings.pages.time'), key: 'createdAt', width: 160 }
]

const getStatusColor = (status) => {
    const colors = { 1: 'green', 0: 'orange', 2: 'default' }
    return colors[status] ?? 'default'
}

const getStatusText = (status) => {
    const texts = { 1: t('settings.pages.published'), 0: t('settings.pages.draft'), 2: t('settings.pages.archived') }
    return texts[status] ?? t('common.unknown')
}

const copyPageLink = (page) => {
    const url = `${window.location.origin}/${page.workspace?.key}/page/${page.id}`
    navigator.clipboard.writeText(url)
    message.success(t('settings.pages.linkCopied'))
}

const viewHistory = async (page) => {
    historyPageTitle.value = page.title
    historyVisible.value = true
    versionsLoading.value = true
    try {
        const data = await pageApi.getVersions(page.id)
        versions.value = data || []
    } catch {
        message.error(t('settings.pages.loadVersionsFailed'))
    } finally {
        versionsLoading.value = false
    }
}

const batchDelete = () => {
    if (selectedRowKeys.value.length === 0) {
        message.warning(t('settings.pages.selectPagesFirst'))
        return
    }

    Modal.confirm({
        title: t('common.confirmDelete'),
        content: t('settings.pages.batchDeleteConfirm', { count: selectedRowKeys.value.length }),
        okText: t('common.delete'),
        okType: 'danger',
        cancelText: t('common.cancel'),
        onOk: async () => {
            try {
                for (const id of selectedRowKeys.value) {
                    await pageApi.remove(id)
                }
                message.success(t('settings.pages.batchDeleteSuccess', { count: selectedRowKeys.value.length }))
                selectedRowKeys.value = []
                loadPages()
            } catch {
                message.error(t('settings.pages.batchDeleteFailed'))
            }
        }
    })
}

const loadWorkspaces = async () => {
    try {
        const data = await workspaceApi.getMy()
        workspaces.value = data || []
    } catch {
        console.error('加载空间列表失败')
    }
}

const loadPages = async () => {
    loading.value = true
    try {
        const data = await pageApi.getList(
            pagination.current,
            pagination.pageSize,
            searchText.value,
            filterWorkspace.value,
            filterStatus.value
        )
        pages.value = data?.items || []
        pagination.total = data?.total || 0
    } catch {
        message.error(t('settings.pages.loadFailed'))
    } finally {
        loading.value = false
    }
}

const handleSearch = () => {
    pagination.current = 1
    loadPages()
}

const handleTableChange = (pag) => {
    pagination.current = pag.current
    pagination.pageSize = pag.pageSize
    loadPages()
}

const handleDelete = (id) => {
    Modal.confirm({
        title: t('common.confirmDelete'),
        content: t('settings.pages.deleteConfirm'),
        okText: t('common.delete'),
        okType: 'danger',
        cancelText: t('common.cancel'),
        onOk: async () => {
            try {
                await pageApi.remove(id)
                message.success(t('settings.pages.deleteSuccess'))
                loadPages()
            } catch {
                message.error(t('settings.pages.deleteFailed'))
            }
        }
    })
}

onMounted(() => {
    loadWorkspaces()
    loadPages()
})
</script>

<style scoped>
.settings-page {
    background-color: #ffffff;
    border-radius: 4px;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
    margin: 16px;
}

.page-header {
    padding: 20px 24px 16px;
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
    padding: 20px 24px 24px;
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;
    flex-wrap: wrap;
    gap: 12px;
}

.page-title {
    display: flex;
    flex-direction: column;
}

.page-title a {
    font-weight: 500;
    color: #172b4d;
}

.page-title a:hover {
    color: #0052cc;
}

.page-path {
    font-size: 12px;
    color: #6b778c;
    margin-top: 2px;
}

.text-muted {
    color: #6b778c;
    font-size: 13px;
}
:deep(.ant-table-wrapper .ant-table-tbody>tr>td){
    padding: 10px 10px;
}
</style>
