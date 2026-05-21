<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>用户管理</h1>
            <p class="page-description">管理系统用户和权限</p>
        </div>

        <div class="page-content">
            <!-- 工具栏 -->
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        placeholder="搜索用户名或邮箱"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                    <a-select
                        v-model:value="filterRole"
                        placeholder="筛选角色"
                        style="width: 120px"
                        allow-clear
                        :options="roleOptions"
                        @change="handleSearch"
                    />
                </a-space>
                <a-button type="primary" @click="showCreateModal">
                    <Plus :size="14" style="vertical-align: middle" />
                    添加用户
                </a-button>
            </div>

            <!-- 用户列表 -->
            <a-table
                :columns="columns"
                :data-source="users"
                :loading="loading"
                :pagination="pagination"
                @change="handleTableChange"
                row-key="id"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'avatar'">
                        <UserAvatar :user="record" />
                    </template>
                    <template v-else-if="column.key === 'roles'">
                        <a-tag v-for="role in record.roles" :key="role" :color="getRoleColor(role)">
                            {{ getRoleLabel(role) }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-tag :color="record.status === 'active' ? 'green' : 'red'">
                            {{ record.status === 'active' ? '正常' : '禁用' }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span>{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="showEditModal(record)">编辑</a-button>
                            <a-button type="link" size="small" @click="showPasswordModal(record)">重置密码</a-button>
                            <a-popconfirm
                                title="确定要删除该用户吗？"
                                @confirm="handleDelete(record.id)"
                            >
                                <a-button type="link" size="small" danger>删除</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 添加/编辑用户弹窗 -->
        <a-modal
            v-model:open="modalVisible"
            :title="editingUser ? '编辑用户' : '添加用户'"
            @ok="handleSubmit"
            :confirm-loading="submitting"
            width="500px"
        >
            <a-form
                ref="formRef"
                :model="formState"
                :label-col="{ span: 6 }"
                :wrapper-col="{ span: 16 }"
            >
                <a-form-item label="用户名" name="username" :rules="[{ required: true, message: '请输入用户名' }]">
                    <a-input v-model:value="formState.username" :disabled="!!editingUser" />
                </a-form-item>
                <a-form-item label="姓名" name="displayName" :rules="[{ required: true, message: '请输入姓名' }]">
                    <a-input v-model:value="formState.displayName" />
                </a-form-item>
                <a-form-item label="邮箱" name="email" :rules="[
                    { required: true, message: '请输入邮箱' },
                    { type: 'email', message: '邮箱格式不正确' }
                ]">
                    <a-input v-model:value="formState.email" />
                </a-form-item>
                <a-form-item v-if="!editingUser" label="密码" name="password" :rules="[
                    { required: true, message: '请输入密码' },
                    { min: 6, message: '密码至少6位' }
                ]">
                    <a-input-password v-model:value="formState.password" />
                </a-form-item>
                <a-form-item label="角色" name="roles" :rules="[{ required: true, message: '请选择角色' }]">
                    <a-select
                        v-model:value="formState.roles"
                        mode="multiple"
                        :options="roleOptions"
                    />
                </a-form-item>
                <a-form-item label="状态" name="status">
                    <a-radio-group v-model:value="formState.status">
                        <a-radio value="active">正常</a-radio>
                        <a-radio value="inactive">禁用</a-radio>
                    </a-radio-group>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- 重置密码弹窗 -->
        <a-modal
            v-model:open="passwordModalVisible"
            title="重置密码"
            @ok="handleResetPassword"
            :confirm-loading="submitting"
        >
            <a-form :label-col="{ span: 6 }" :wrapper-col="{ span: 16 }">
                <a-form-item label="新密码">
                    <a-input-password v-model:value="newPassword" placeholder="请输入新密码（至少6位）" />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { Plus } from 'lucide-vue-next'
import { userApi } from '@/api'
import { formatDateTime } from '@/utils/format'
import UserAvatar from '@/components/UserAvatar.vue'

const loading = ref(false)
const submitting = ref(false)
const searchText = ref('')
const filterRole = ref()
const users = ref([])
const modalVisible = ref(false)
const passwordModalVisible = ref(false)
const editingUser = ref(null)
const newPassword = ref('')
const formRef = ref()

const formState = reactive({
    username: '',
    name: '',
    email: '',
    password: '',
    roles: ['user'],
    status: 'active'
})

const roleOptions = [
    { label: '管理员', value: 'admin' },
    { label: '编辑者', value: 'editor' },
    { label: '普通用户', value: 'user' }
]

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: '', key: 'avatar', width: 60 },
    { title: '用户名', dataIndex: 'username', key: 'username' },
    { title: '姓名', dataIndex: 'displayName', key: 'displayName' },
    { title: '邮箱', dataIndex: 'email', key: 'email' },
    { title: '角色', key: 'roles' },
    { title: '状态', key: 'status' },
    { title: '创建时间', key: 'createdAt' },
    { title: '操作', key: 'action', width: 180 }
]

const getRoleColor = (role) => {
    const colors = { admin: 'red', editor: 'blue', user: 'default' }
    return colors[role] || 'default'
}

const getRoleLabel = (role) => {
    const labels = { admin: '管理员', editor: '编辑者', user: '普通用户' }
    return labels[role] || role
}

const loadUsers = async () => {
    loading.value = true
    try {
        const data = await userApi.getList(pagination.current, pagination.pageSize)
        users.value = data?.items || []
        pagination.total = data?.total || 0
    } catch (error) {
        message.error('加载用户列表失败')
    } finally {
        loading.value = false
    }
}

const handleSearch = () => {
    pagination.current = 1
    loadUsers()
}

const handleTableChange = (pag) => {
    pagination.current = pag.current
    pagination.pageSize = pag.pageSize
    loadUsers()
}

const showCreateModal = () => {
    editingUser.value = null
    Object.assign(formState, {
        username: '',
        name: '',
        email: '',
        password: '',
        roles: ['user'],
        status: 'active'
    })
    modalVisible.value = true
}

const showEditModal = (user) => {
    editingUser.value = user
    Object.assign(formState, {
        username: user.username,
        name: user.name,
        email: user.email,
        roles: user.roles || ['user'],
        status: user.status
    })
    modalVisible.value = true
}

const showPasswordModal = (user) => {
    editingUser.value = user
    newPassword.value = ''
    passwordModalVisible.value = true
}

const handleSubmit = async () => {
    try {
        await formRef.value.validate()
    } catch {
        return
    }

    submitting.value = true
    try {
        if (editingUser.value) {
            await userApi.update(editingUser.value.id, formState)
            message.success('用户更新成功')
        } else {
            await userApi.register(formState)
            message.success('用户创建成功')
        }
        modalVisible.value = false
        loadUsers()
    } catch (error) {
        message.error(editingUser.value ? '更新用户失败' : '创建用户失败')
    } finally {
        submitting.value = false
    }
}

const handleResetPassword = async () => {
    if (!newPassword.value || newPassword.value.length < 6) {
        message.warning('密码至少6位')
        return
    }

    submitting.value = true
    try {
        await userApi.changePassword({
            userId: editingUser.value.id,
            newPassword: newPassword.value
        })
        message.success('密码重置成功')
        passwordModalVisible.value = false
    } catch (error) {
        message.error('密码重置失败')
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (id) => {
    try {
        await userApi.remove(id)
        message.success('用户删除成功')
        loadUsers()
    } catch (error) {
        message.error('删除用户失败')
    }
}

onMounted(() => {
    loadUsers()
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
</style>
