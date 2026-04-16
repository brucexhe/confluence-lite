<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>用户组管理</h1>
            <p class="page-description">管理系统用户组和权限</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-input-search
                    v-model:value="searchText"
                    placeholder="搜索用户组"
                    style="width: 250px"
                    @search="handleSearch"
                />
                <a-button type="primary" @click="showCreateModal">
                    <template #icon><PlusOutlined /></template>
                    添加用户组
                </a-button>
            </div>

            <a-table
                :columns="columns"
                :data-source="groups"
                :loading="loading"
                :pagination="pagination"
                @change="handleTableChange"
                row-key="id"
            >
                <template #bodyCell="{ column, record }">
                    <template v-else-if="column.key === 'userCount'">
                        <a-tag color="blue">{{ record.userCount || 0 }} 人</a-tag>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="showEditModal(record)">编辑</a-button>
                            <a-button type="link" size="small" @click="showMembersModal(record)">成员</a-button>
                            <a-popconfirm title="确定要删除该用户组吗？" @confirm="handleDelete(record.id)">
                                <a-button type="link" size="small" danger>删除</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 添加/编辑用户组弹窗 -->
        <a-modal
            v-model:open="modalVisible"
            :title="editingGroup ? '编辑用户组' : '添加用户组'"
            @ok="handleSubmit"
            :confirm-loading="submitting"
            width="500px"
        >
            <a-form ref="formRef" :model="formState" :label-col="{ span: 6 }" :wrapper-col="{ span: 16 }">
                <a-form-item label="组名称" name="name" :rules="[{ required: true, message: '请输入组名称' }]">
                    <a-input v-model:value="formState.name" />
                </a-form-item>
                <a-form-item label="描述" name="description">
                    <a-textarea v-model:value="formState.description" :rows="3" />
                </a-form-item>
                <a-form-item label="权限" name="permissions">
                    <a-select v-model:value="formState.permissions" mode="multiple" placeholder="选择权限">
                        <a-select-option value="page:read">阅读页面</a-select-option>
                        <a-select-option value="page:write">编辑页面</a-select-option>
                        <a-select-option value="page:delete">删除页面</a-select-option>
                        <a-select-option value="space:manage">管理空间</a-select-option>
                        <a-select-option value="user:manage">管理用户</a-select-option>
                        <a-select-option value="system:admin">系统管理</a-select-option>
                    </a-select>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- 成员管理弹窗 -->
        <a-modal
            v-model:open="membersModalVisible"
            title="成员管理"
            :footer="null"
            width="600px"
        >
            <div style="margin-bottom: 16px">
                <a-select
                    v-model:value="selectedUsers"
                    mode="multiple"
                    placeholder="选择要添加的用户"
                    style="width: 100%"
                    :filter-option="filterUser"
                    show-search
                >
                    <a-select-option v-for="user in availableUsers" :key="user.id" :value="user.id">
                        {{ user.name }} ({{ user.username }})
                    </a-select-option>
                </a-select>
                <a-button type="primary" style="margin-top: 8px" @click="addMembers">添加成员</a-button>
            </div>
            <a-list
                :data-source="currentMembers"
                :loading="loadingMembers"
                item-layout="horizontal"
            >
                <template #renderItem="{ item }">
                    <a-list-item>
                        <a-list-item-meta>
                            <template #title>{{ item.name }}</template>
                            <template #description>{{ item.username }}</template>
                        </a-list-item-meta>
                        <template #actions>
                            <a-button type="link" danger size="small" @click="removeMember(item.id)">移除</a-button>
                        </template>
                    </a-list-item>
                </template>
            </a-list>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { PlusOutlined } from '@ant-design/icons-vue'
import { userApi } from '@/api'

const loading = ref(false)
const submitting = ref(false)
const loadingMembers = ref(false)
const searchText = ref('')
const groups = ref([])
const modalVisible = ref(false)
const membersModalVisible = ref(false)
const editingGroup = ref(null)
const formRef = ref()
const selectedUsers = ref([])
const availableUsers = ref([])
const currentMembers = ref([])
const currentGroupId = ref(null)

const formState = reactive({
    name: '',
    description: '',
    permissions: []
})

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: '组名称', dataIndex: 'name', key: 'name' },
    { title: '描述', dataIndex: 'description', key: 'description' },
    { title: '成员数', key: 'userCount' },
    { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt' },
    { title: '操作', key: 'action', width: 180 }
]

const loadGroups = async () => {
    loading.value = true
    try {
        // TODO: 调用 API 获取用户组列表
        groups.value = [
            { id: 1, name: '管理员组', description: '系统管理员', userCount: 3, createdAt: '2024-01-01' },
            { id: 2, name: '编辑组', description: '内容编辑者', userCount: 10, createdAt: '2024-01-02' }
        ]
        pagination.total = groups.value.length
    } catch (error) {
        message.error('加载用户组列表失败')
    } finally {
        loading.value = false
    }
}

const loadAvailableUsers = async () => {
    try {
        const data = await userApi.getList(1, 100)
        availableUsers.value = data?.items || []
    } catch (error) {
        message.error('加载用户列表失败')
    }
}

const handleSearch = () => {
    pagination.current = 1
    loadGroups()
}

const handleTableChange = (pag) => {
    pagination.current = pag.current
    loadGroups()
}

const showCreateModal = () => {
    editingGroup.value = null
    Object.assign(formState, { name: '', description: '', permissions: [] })
    modalVisible.value = true
}

const showEditModal = (group) => {
    editingGroup.value = group
    Object.assign(formState, {
        name: group.name,
        description: group.description,
        permissions: group.permissions || []
    })
    modalVisible.value = true
}

const showMembersModal = async (group) => {
    currentGroupId.value = group.id
    membersModalVisible.value = true
    await loadAvailableUsers()
    // TODO: 加载组成员
    currentMembers.value = [
        { id: 1, name: 'Admin', username: 'admin' }
    ]
}

const handleSubmit = async () => {
    submitting.value = true
    try {
        // TODO: 调用 API 创建/更新用户组
        message.success(editingGroup.value ? '用户组更新成功' : '用户组创建成功')
        modalVisible.value = false
        loadGroups()
    } catch (error) {
        message.error('操作失败')
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (id) => {
    try {
        // TODO: 调用 API 删除用户组
        message.success('用户组删除成功')
        loadGroups()
    } catch (error) {
        message.error('删除用户组失败')
    }
}

const filterUser = (input, option) => {
    return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

const addMembers = async () => {
    if (selectedUsers.value.length === 0) return
    try {
        // TODO: 调用 API 添加成员
        message.success('成员添加成功')
        showMembersModal({ id: currentGroupId.value })
    } catch (error) {
        message.error('添加成员失败')
    }
}

const removeMember = async (userId) => {
    try {
        // TODO: 调用 API 移除成员
        message.success('成员移除成功')
        showMembersModal({ id: currentGroupId.value })
    } catch (error) {
        message.error('移除成员失败')
    }
}

onMounted(() => {
    loadGroups()
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
