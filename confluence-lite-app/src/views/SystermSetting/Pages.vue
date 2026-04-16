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
                            <a @click="$router.push(`/${record.spaceKey}/page/${record.id}`)">
                                {{ record.title }}
                            </a>
                            <div class="page-path">/${{ record.spaceKey }}/{{ record.id }}</div>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'space'">
                        <a-tag color="blue">{{ record.spaceName }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-tag :color="getStatusColor(record.status)">
                            {{ getStatusText(record.status) }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'viewCount'">
                        <span>{{ formatNumber(record.viewCount) }}</span>
                    </template>
                    <template v-else-if="column.key === 'updatedAt'">
                        <span class="text-muted">{{ formatDateTime(record.updatedAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space split>
                            <a-button type="link" size="small" @click="$router.push(`/${record.spaceKey}/page/${record.id}`)">
                                查看
                            </a-button>
                            <a-button type="link" size="small" @click="$router.push(`/${record.spaceKey}/page/${record.id}/edit`)">
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
    </div>
</template>

<script setup>
import { ref, computed, onMounted, reactive } from 'vue'
import { message } from 'ant-design-vue'
import { workspaceApi, pageApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const loading = ref(false)
const searchText = ref('')
const filterWorkspace = ref()
const filterStatus = ref()
const selectedRowKeys = ref([])
const pages = ref([])
const workspaces = ref([])

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const workspaceOptions = computed(() => {
    return workspaces.value.map(ws => ({
        label: ws.name,
        value: ws.id
    }))
})

const statusOptions = [
    { label: '已发布', value: 'published' },
    { label: '草稿', value: 'draft' },
    { label: '已归档', value: 'archived' }
]

const rowSelection = {
    selectedRowKeys: selectedRowKeys,
    onChange: (keys) => {
        selectedRowKeys.value = keys
    }
}

const columns = [
    { title: '页面标题', key: 'title' },
    { title: '所属空间', key: 'space' },
    { title: '创建者', dataIndex: 'creatorName', key: 'creatorName' },
    { title: '状态', key: 'status' },
    { title: '浏览次数', dataIndex: 'viewCount', key: 'viewCount' },
    { title: '更新时间', dataIndex: 'updatedAt', key: 'updatedAt' },
    { title: '操作', key: 'action', width: 180 }
]

const getStatusColor = (status) => {
    const colors = { published: 'green', draft: 'orange', archived: 'default' }
    return colors[status] || 'default'
}

const getStatusText = (status) => {
    const texts = { published: '已发布', draft: '草稿', archived: '已归档' }
    return texts[status] || status
}

const formatNumber = (num) => {
    if (!num) return '0'
    return num >= 1000 ? (num / 1000).toFixed(1) + 'k' : num.toString()
}

const copyPageLink = (page) => {
    const url = `${window.location.origin}/${page.spaceKey}/page/${page.id}`
    navigator.clipboard.writeText(url)
    message.success('链接已复制到剪贴板')
}

const viewHistory = (page) => {
    message.info('查看历史: ' + page.title)
    // TODO: 跳转到历史页面
}

const batchDelete = async () => {
    if (selectedRowKeys.value.length === 0) {
        message.warning('请先选择要删除的页面')
        return
    }

    try {
        for (const id of selectedRowKeys.value) {
            await pageApi.remove(id)
        }
        message.success(`成功删除 ${selectedRowKeys.value.length} 个页面`)
        selectedRowKeys.value = []
        loadPages()
    } catch (error) {
        message.error('批量删除失败')
    }
}

const loadWorkspaces = async () => {
    try {
        const data = await workspaceApi.getMy()
        workspaces.value = data || []
    } catch (error) {
        console.error('加载空间列表失败')
    }
}

const loadPages = async () => {
    loading.value = true
    try {
        // TODO: 调用 API 获取所有页面列表
        await new Promise(resolve => setTimeout(resolve, 500))

        // 模拟数据
        const mockPages = []
        workspaces.value.forEach(ws => {
            for (let i = 1; i <= 5; i++) {
                mockPages.push({
                    id: `${ws.key}-${i}`,
                    title: `${ws.name} - 示例页面 ${i}`,
                    spaceName: ws.name,
                    spaceKey: ws.key,
                    status: 'published',
                    creatorName: 'Admin',
                    viewCount: Math.floor(Math.random() * 500),
                    updatedAt: new Date(Date.now() - Math.random() * 30 * 24 * 60 * 60 * 1000).toISOString()
                })
            }
        })

        // 应用筛选
        let filtered = mockPages
        if (searchText.value) {
            filtered = filtered.filter(p => p.title.toLowerCase().includes(searchText.value.toLowerCase()))
        }
        if (filterWorkspace.value) {
            filtered = filtered.filter(p => p.spaceKey === workspaces.value.find(w => w.id === filterWorkspace.value)?.key)
        }
        if (filterStatus.value) {
            filtered = filtered.filter(p => p.status === filterStatus.value)
        }

        pages.value = filtered
        pagination.total = filtered.length
    } catch (error) {
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

const handleDelete = async (id) => {
    try {
        await pageApi.remove(id)
        message.success('页面删除成功')
        loadPages()
    } catch (error) {
        message.error('删除页面失败')
    }
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
</style>
