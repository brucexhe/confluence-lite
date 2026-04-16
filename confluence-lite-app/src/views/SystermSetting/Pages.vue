<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>页面管理</h1>
            <p class="page-description">管理系统所有页面</p>
        </div>

        <div class="page-content">
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
                        @change="handleSearch"
                    >
                        <a-select-option v-for="ws in workspaces" :key="ws.id" :value="ws.id">
                            {{ ws.name }}
                        </a-select-option>
                    </a-select>
                </a-space>
            </div>

            <a-table
                :columns="columns"
                :data-source="pages"
                :loading="loading"
                :pagination="pagination"
                @change="handleTableChange"
                row-key="id"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'title'">
                        <a @click="$router.push(`/${record.spaceKey}/page/${record.id}`)">
                            {{ record.title }}
                        </a>
                    </template>
                    <template v-else-if="column.key === 'space'">
                        <a-tag color="blue">{{ record.spaceName }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-tag :color="record.status === 'published' ? 'green' : 'orange'">
                            {{ record.status === 'published' ? '已发布' : '草稿' }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="$router.push(`/${record.spaceKey}/page/${record.id}`)">
                                查看
                            </a-button>
                            <a-button type="link" size="small" @click="$router.push(`/${record.spaceKey}/page/${record.id}/edit`)">
                                编辑
                            </a-button>
                            <a-popconfirm title="确定要删除该页面吗？" @confirm="handleDelete(record.id)">
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
import { workspaceApi, pageApi } from '@/api'

const loading = ref(false)
const searchText = ref('')
const filterWorkspace = ref()
const pages = ref([])
const workspaces = ref([])

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: '页面标题', dataIndex: 'title', key: 'title' },
    { title: '空间', key: 'space' },
    { title: '创建者', dataIndex: 'creatorName', key: 'creatorName' },
    { title: '状态', key: 'status' },
    { title: '浏览次数', dataIndex: 'viewCount', key: 'viewCount' },
    { title: '更新时间', dataIndex: 'updatedAt', key: 'updatedAt' },
    { title: '操作', key: 'action', width: 180 }
]

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
        pages.value = [
            { id: 1, title: '示例页面', spaceName: 'TEST', spaceKey: 'TEST', status: 'published', creatorName: 'Admin', viewCount: 100, updatedAt: '2024-01-01' }
        ]
        pagination.total = pages.value.length
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
.page-content {
    padding: 16px 24px 24px;
}

.toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 16px;
}
</style>
