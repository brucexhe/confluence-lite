<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.users.title') }}</h1>
            <p class="page-description">{{ $t('settings.users.description') }}</p>
        </div>

        <div class="page-content">
            <!-- 工具栏 -->
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        :placeholder="$t('settings.users.searchPlaceholder')"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                    <a-select
                        v-model:value="filterRole"
                        :placeholder="$t('settings.users.filterRole')"
                        style="width: 120px"
                        allow-clear
                        :options="roleOptions"
                        @change="handleSearch"
                    />
                </a-space>
                <a-button type="primary" @click="showCreateModal">
                    <Plus :size="14" style="vertical-align: middle" />
                    {{ $t('settings.users.addUser') }}
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
                        <a-tag v-if="record.isAdmin" color="red">{{ $t('settings.users.roleAdmin') }}</a-tag>
                        <a-tag v-else>{{ $t('settings.users.roleUser') }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'status'">
                        <a-tag :color="record.status === 1 ? 'green' : 'red'">
                            {{ record.status === 1 ? $t('settings.users.active') : $t('settings.users.disabled') }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span>{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="showEditModal(record)">{{ $t('common.edit') }}</a-button>
                            <a-button type="link" size="small" @click="showPasswordModal(record)">{{ $t('settings.users.resetPassword') }}</a-button>
                            <a-popconfirm
                                :title="$t('settings.users.confirmDelete')"
                                @confirm="handleDelete(record.id)"
                            >
                                <a-button type="link" size="small" danger>{{ $t('common.delete') }}</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 添加/编辑用户弹窗 -->
        <a-modal
            v-model:open="modalVisible"
            :title="editingUser ? $t('settings.users.editUser') : $t('settings.users.addUser')"
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
                <a-form-item :label="$t('settings.users.username')" name="username" :rules="[{ required: true, message: $t('settings.users.usernameRequired') }]">
                    <a-input v-model:value="formState.username" :disabled="!!editingUser" />
                </a-form-item>
                <a-form-item :label="$t('settings.users.displayName')" name="displayName" :rules="[{ required: true, message: $t('settings.users.displayNameRequired') }]">
                    <a-input v-model:value="formState.displayName" />
                </a-form-item>
                <a-form-item :label="$t('settings.users.email')" name="email" :rules="[
                    { required: true, message: $t('settings.users.emailRequired') },
                    { type: 'email', message: $t('settings.users.emailInvalid') }
                ]">
                    <a-input v-model:value="formState.email" />
                </a-form-item>
                <a-form-item v-if="!editingUser" :label="$t('settings.users.password')" name="password" :rules="[
                    { required: true, message: $t('settings.users.passwordRequired') },
                    { min: 6, message: $t('settings.users.passwordMinLength') }
                ]">
                    <a-input-password v-model:value="formState.password" />
                </a-form-item>
                <a-form-item :label="$t('settings.users.role')" name="isAdmin">
                    <a-radio-group v-model:value="formState.isAdmin">
                        <a-radio :value="true">{{ $t('settings.users.roleAdmin') }}</a-radio>
                        <a-radio :value="false">{{ $t('settings.users.roleUser') }}</a-radio>
                    </a-radio-group>
                </a-form-item>
                <a-form-item :label="$t('settings.users.status')" name="status">
                    <a-radio-group v-model:value="formState.status">
                        <a-radio :value="1">{{ $t('settings.users.active') }}</a-radio>
                        <a-radio :value="0">{{ $t('settings.users.disabled') }}</a-radio>
                    </a-radio-group>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- 重置密码弹窗 -->
        <a-modal
            v-model:open="passwordModalVisible"
            :title="$t('settings.users.resetPassword')"
            @ok="handleResetPassword"
            :confirm-loading="submitting"
        >
            <a-form :label-col="{ span: 6 }" :wrapper-col="{ span: 16 }">
                <a-form-item :label="$t('settings.users.newPassword')">
                    <a-input-password v-model:value="newPassword" :placeholder="$t('settings.users.newPasswordPlaceholder')" />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { Plus } from 'lucide-vue-next'
import { userApi } from '@/api'
import { formatDateTime } from '@/utils/format'
import UserAvatar from '@/components/UserAvatar.vue'

const { t } = useI18n()
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

// 与后端 UserDto/CreateUserRequest/UpdateUserRequest 对齐：
// 角色为 isAdmin（bool），状态为 status（int：0 禁用、1 正常）
const formState = reactive({
    username: '',
    displayName: '',
    email: '',
    password: '',
    isAdmin: false,
    status: 1
})

const roleOptions = [
    { label: t('settings.users.roleAdmin'), value: 'admin' },
    { label: t('settings.users.roleUser'), value: 'user' }
]

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: '', key: 'avatar', width: 60 },
    { title: t('settings.users.username'), dataIndex: 'username', key: 'username' },
    { title: t('settings.users.displayName'), dataIndex: 'displayName', key: 'displayName' },
    { title: t('settings.users.email'), dataIndex: 'email', key: 'email' },
    { title: t('settings.users.role'), key: 'roles' },
    { title: t('settings.users.status'), key: 'status' },
    { title: t('settings.users.createdAt'), key: 'createdAt' },
    { title: t('common.action'), key: 'action', width: 180 }
]

const loadUsers = async () => {
    loading.value = true
    try {
        const data = await userApi.getList(pagination.current, pagination.pageSize)
        users.value = data?.items || []
        pagination.total = data?.total || 0
    } catch (error) {
        message.error(t('settings.users.loadFailed'))
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
        displayName: '',
        email: '',
        password: '',
        isAdmin: false,
        status: 1
    })
    modalVisible.value = true
}

const showEditModal = (user) => {
    editingUser.value = user
    Object.assign(formState, {
        username: user.username || '',
        displayName: user.displayName || '',
        email: user.email || '',
        password: '',
        isAdmin: !!user.isAdmin,
        status: user.status === 0 ? 0 : 1
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
            await userApi.update(editingUser.value.id, {
                email: formState.email,
                displayName: formState.displayName,
                status: formState.status,
                isAdmin: formState.isAdmin
            })
            message.success(t('settings.users.updateSuccess'))
        } else {
            await userApi.register({
                username: formState.username,
                password: formState.password,
                email: formState.email,
                displayName: formState.displayName,
                isAdmin: formState.isAdmin
            })
            message.success(t('settings.users.createSuccess'))
        }
        modalVisible.value = false
        loadUsers()
    } catch (error) {
        message.error(editingUser.value ? t('settings.users.updateFailed') : t('settings.users.createFailed'))
    } finally {
        submitting.value = false
    }
}

const handleResetPassword = async () => {
    if (!newPassword.value || newPassword.value.length < 6) {
        message.warning(t('settings.users.passwordMinLength'))
        return
    }

    submitting.value = true
    try {
        await userApi.changePassword({
            userId: editingUser.value.id,
            newPassword: newPassword.value
        })
        message.success(t('settings.users.passwordResetSuccess'))
        passwordModalVisible.value = false
    } catch (error) {
        message.error(t('settings.users.passwordResetFailed'))
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (id) => {
    try {
        await userApi.remove(id)
        message.success(t('settings.users.deleteSuccess'))
        loadUsers()
    } catch (error) {
        message.error(t('settings.users.deleteFailed'))
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
