<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>空间管理</h1>
            <p class="page-description">管理系统所有空间</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        placeholder="搜索空间名称或Key"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                </a-space>
                <a-button type="primary" @click="$router.push('/spaces')">
                    <template #icon><PlusOutlined /></template>
                    创建空间
                </a-button>
            </div>

            <a-table
                :columns="columns"
                :data-source="workspaces"
                :loading="loading"
                :pagination="pagination"
                @change="handleTableChange"
                row-key="id"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'icon'">
                        <div class="space-icon" :style="{ background: getSpaceColor(record.id) }">
                            {{ record.key?.charAt(0)?.toUpperCase() }}
                        </div>
                    </template>
                    <template v-else-if="column.key === 'name'">
                        <a @click="$router.push(`/${record.key}`)">{{ record.name }}</a>
                    </template>
                    <template v-else-if="column.key === 'pageCount'">
                        <a-tag color="blue">{{ record.pageCount || 0 }} 页面</a-tag>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="$router.push(`/${record.key}`)">查看</a-button>
                            <a-button type="link" size="small" @click="showEditModal(record)">编辑</a-button>
                            <a-popconfirm title="确定要删除该空间吗？" @confirm="handleDelete(record.id)">
                                <a-button type="link" size="small" danger>删除</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import { workspaceApi } from '@/api'

const loading = ref(false)
const searchText = ref('')
const workspaces = ref([])

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: '', key: 'icon', width: 50 },
    { title: '空间名称', dataIndex: 'name', key: 'name' },
    { title: 'Key', dataIndex: 'key', key: 'key' },
    { title: '描述', dataIndex: 'description', key: 'description' },
    { title: '页面数', key: 'pageCount' },
    { title: '创建者', dataIndex: 'creatorName', key: 'creatorName' },
    { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt' },
    { title: '操作', key: 'action', width: 150 }
]

const spaceColors = [
    'linear-gradient(135deg, #10b981, #059669)',
    'linear-gradient(135deg, #3b82f6, #2563eb)',
    'linear-gradient(135deg, #8b5cf6, #7c3aed)',
    'linear-gradient(135deg, #f59e0b, #d97706)',
    'linear-gradient(135deg, #ef4444, #dc2626)',
    'linear-gradient(135deg, #06b6d4, #0891b2)',
]

const getSpaceColor = (id) => {
    return spaceColors[(id || 0) % spaceColors.length]
}

const loadWorkspaces = async () => {
    loading.value = true
    try {
        const data = await workspaceApi.getList(pagination.current, pagination.pageSize)
        workspaces.value = data?.items || []
        pagination.total = data?.total || 0
    } catch (error) {
        message.error('加载空间列表失败')
    } finally {
        loading.value = false
    }
}

const handleSearch = () => {
    pagination.current = 1
    loadWorkspaces()
}

const handleTableChange = (pag) => {
    pagination.current = pag.current
    pagination.pageSize = pag.pageSize
    loadWorkspaces()
}

const showEditModal = (workspace) => {
    // TODO: 显示编辑弹窗
    message.info('编辑功能: ' + workspace.name)
}

const handleDelete = async (id) => {
    try {
        await workspaceApi.remove(id)
        message.success('空间删除成功')
        loadWorkspaces()
    } catch (error) {
        message.error('删除空间失败')
    }
}

onMounted(() => {
    loadWorkspaces()
})
</script>

<style scoped>
.page-content {
    padding: 16px 24px 24px;
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;
}

.space-icon {
    width: 32px;
    height: 32px;
    border-radius: 3px;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #fff;
    font-weight: 600;
    font-size: 14px;
}
</style>
