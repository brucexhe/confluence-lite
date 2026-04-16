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
                    <Plus :size="14" style="vertical-align: middle" />
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
                        <div class="space-icon" :style="{ background: getSpaceColor(record.id, record.iconStyle) }">
                            {{ record.key?.charAt(0)?.toUpperCase() }}
                        </div>
                    </template>
                    <template v-else-if="column.key === 'name'">
                        <a @click="$router.push(`/${record.key}`)">{{ record.name }}</a>
                    </template>
                    <template v-else-if="column.key === 'pageCount'">
                        <a-tag color="blue">{{ record.pageCount || 0 }} 页面</a-tag>
                    </template>
                    <template v-else-if="column.key === 'type'">
                        <a-tag :color="record.isPublic ? 'green' : 'default'">
                            {{ record.isPublic ? '公开' : '私有' }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span>{{ formatDateTime(record.createdAt) }}</span>
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

        <!-- 编辑空间弹窗 -->
        <a-modal
            v-model:open="editModalVisible"
            title="编辑空间"
            @ok="handleUpdate"
            :confirm-loading="updating"
            width="600px"
        >
            <a-form
                ref="formRef"
                :model="formState"
                :label-col="{ span: 6 }"
                :wrapper-col="{ span: 16 }"
            >
                <a-form-item label="空间名称" name="name" :rules="[{ required: true, message: '请输入空间名称' }]">
                    <a-input v-model:value="formState.name" placeholder="输入空间名称" />
                </a-form-item>
                <a-form-item label="空间Key" name="key" :rules="[
                    { required: true, message: '请输入空间Key' },
                    { pattern: /^[a-z0-9_-]+$/, message: '只能包含小写字母、数字、下划线和连字符' }
                ]">
                    <a-input
                        v-model:value="formState.key"
                        placeholder="小写字母、数字、下划线或连字符"
                        :disabled="!!editingWorkspace"
                    />
                </a-form-item>
                <a-form-item label="描述" name="description">
                    <a-textarea v-model:value="formState.description" :rows="3" placeholder="简要描述该空间的用途" />
                </a-form-item>
                <a-form-item label="图标样式" name="iconStyle">
                    <a-select v-model:value="formState.iconStyle" style="width: 200px">
                        <a-select-option value="green">绿色</a-select-option>
                        <a-select-option value="blue">蓝色</a-select-option>
                        <a-select-option value="purple">紫色</a-select-option>
                        <a-select-option value="orange">橙色</a-select-option>
                        <a-select-option value="red">红色</a-select-option>
                        <a-select-option value="cyan">青色</a-select-option>
                    </a-select>
                </a-form-item>
                <a-form-item label="公开访问" name="isPublic">
                    <a-switch
                        v-model:checked="formState.isPublic"
                        checked-children="公开"
                        un-checked-children="私有"
                    />
                    <div class="form-hint">公开空间对所有用户可见，私有空间仅对成员可见</div>
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { Plus } from 'lucide-vue-next'
import { workspaceApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const loading = ref(false)
const updating = ref(false)
const searchText = ref('')
const workspaces = ref([])
const editModalVisible = ref(false)
const editingWorkspace = ref(null)
const formRef = ref()

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const formState = reactive({
    id: null,
    name: '',
    key: '',
    description: '',
    iconStyle: 'blue',
    isPublic: true
})

const columns = [
    { title: '', key: 'icon', width: 50 },
    { title: '空间名称', dataIndex: 'name', key: 'name' },
    { title: 'Key', dataIndex: 'key', key: 'key' },
    // { title: '描述', dataIndex: 'description', key: 'description' },
    { title: '页面数', key: 'pageCount' },
    { title: '类型', key: 'type' },
    { title: '创建者', dataIndex: 'creatorName', key: 'creatorName' },
    { title: '创建时间', key: 'createdAt' },
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

const iconStyleColors = {
    green: 'linear-gradient(135deg, #10b981, #059669)',
    blue: 'linear-gradient(135deg, #3b82f6, #2563eb)',
    purple: 'linear-gradient(135deg, #8b5cf6, #7c3aed)',
    orange: 'linear-gradient(135deg, #f59e0b, #d97706)',
    red: 'linear-gradient(135deg, #ef4444, #dc2626)',
    cyan: 'linear-gradient(135deg, #06b6d4, #0891b2)',
}

const getSpaceColor = (id, iconStyle) => {
    return iconStyleColors[iconStyle] || spaceColors[(id || 0) % spaceColors.length]
}

const loadWorkspaces = async () => {
    loading.value = true
    try {
        const data = await workspaceApi.getList(pagination.current, pagination.pageSize)
        workspaces.value = (data?.items || []).map(ws => ({
            ...ws,
            iconStyle: ws.iconStyle || 'blue',
            isPublic: ws.isPublic !== false
        }))
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
    editingWorkspace.value = workspace
    Object.assign(formState, {
        id: workspace.id,
        name: workspace.name,
        key: workspace.key,
        description: workspace.description || '',
        iconStyle: workspace.iconStyle || 'blue',
        isPublic: workspace.isPublic !== false
    })
    editModalVisible.value = true
}

const handleUpdate = async () => {
    try {
        await formRef.value.validate()
    } catch {
        return
    }

    updating.value = true
    try {
        await workspaceApi.update(formState.id, {
            name: formState.name,
            description: formState.description,
            iconStyle: formState.iconStyle,
            isPublic: formState.isPublic
        })
        message.success('空间更新成功')
        editModalVisible.value = false
        loadWorkspaces()
    } catch (error) {
        message.error('更新空间失败')
    } finally {
        updating.value = false
    }
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
