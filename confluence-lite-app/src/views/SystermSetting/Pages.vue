<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>页面管理</h1>
            <p class="page-description">管理系统所有页面</p>
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
                        v-model:value="filterWorkspace"
                        placeholder="筛选空间"
                        style="width: 150px"
                        allow-clear
                        :options="workspaceOptions"
                        @change="handleSearch"
                    />
                    <a-select
                        v-model:value="filterStatus"
                        placeholder="筛选状态"
                        style="width: 120px"
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
                                查看
                            </a-button>
                            <a-button type="link" size="small" @click="$router.push(`/${record.workspace?.key}/page/${record.id}/edit`)">
                                编辑
                            </a-button>
                            <a-dropdown>
                                <template #overlay>
                                    <a-menu>
                                        <a-menu-item @click="copyPageLink(record)">
                                            复制链接
                                        </a-menu-item>
                                        <a-menu-item @click="viewHistory(record)">
                                            查看历史
                                        </a-menu-item>
                                        <a-menu-divider />
                                        <a-menu-item danger @click="handleDelete(record.id)">
                                            删除页面
                                        </a-menu-item>
                                    </a-menu>
                                </template>
                                <a-button type="link" size="small">
                                    更多
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
            :title="`页面历史 - ${historyPageTitle}`"
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
import { workspaceApi, pageApi } from '@/api'
import { formatDateTime } from '@/utils/format'

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
    showTotal: (total) => `共 ${total} 条`
})

const workspaceOptions = computed(() => {
    return workspaces.value.map(ws => ({
        label: ws.name,
        value: ws.id
    }))
})

const statusOptions = [
    { label: '已发布', value: 1 },
    { label: '草稿', value: 0 },
    { label: '已归档', value: 2 }
]

const rowSelection = computed(() => ({
    selectedRowKeys: selectedRowKeys.value,
    onChange: (keys) => {
        selectedRowKeys.value = keys
    }
}))

const columns = [
    { title: '页面标题', key: 'title' },
    { title: '所属空间', key: 'space' },
    { title: '创建者', key: 'creator' },
    { title: '状态', key: 'status' },
    { title: '更新时间', dataIndex: 'updatedAt', key: 'updatedAt' },
    { title: '操作', key: 'action', width: 180 }
]

const historyColumns = [
    { title: '版本', dataIndex: 'versionNumber', key: 'versionNumber', width: 60 },
    { title: '标题', dataIndex: 'title', key: 'title' },
    { title: '编辑者', key: 'editor', width: 120 },
    { title: '时间', key: 'createdAt', width: 160 }
]

const getStatusColor = (status) => {
    const colors = { 1: 'green', 0: 'orange', 2: 'default' }
    return colors[status] ?? 'default'
}

const getStatusText = (status) => {
    const texts = { 1: '已发布', 0: '草稿', 2: '已归档' }
    return texts[status] ?? '未知'
}

const copyPageLink = (page) => {
    const url = `${window.location.origin}/${page.workspace?.key}/page/${page.id}`
    navigator.clipboard.writeText(url)
    message.success('链接已复制到剪贴板')
}

const viewHistory = async (page) => {
    historyPageTitle.value = page.title
    historyVisible.value = true
    versionsLoading.value = true
    try {
        const data = await pageApi.getVersions(page.id)
        versions.value = data || []
    } catch {
        message.error('加载版本历史失败')
    } finally {
        versionsLoading.value = false
    }
}

const batchDelete = () => {
    if (selectedRowKeys.value.length === 0) {
        message.warning('请先选择要删除的页面')
        return
    }

    Modal.confirm({
        title: '确认删除',
        content: `确定要删除选中的 ${selectedRowKeys.value.length} 个页面吗？此操作不可恢复。`,
        okText: '删除',
        okType: 'danger',
        cancelText: '取消',
        onOk: async () => {
            try {
                for (const id of selectedRowKeys.value) {
                    await pageApi.remove(id)
                }
                message.success(`成功删除 ${selectedRowKeys.value.length} 个页面`)
                selectedRowKeys.value = []
                loadPages()
            } catch {
                message.error('批量删除失败')
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
        message.error('加载页面列表失败')
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
        title: '确认删除',
        content: '确定要删除该页面吗？子页面也会被一并删除，此操作不可恢复。',
        okText: '删除',
        okType: 'danger',
        cancelText: '取消',
        onOk: async () => {
            try {
                await pageApi.remove(id)
                message.success('页面删除成功')
                loadPages()
            } catch {
                message.error('删除页面失败')
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
