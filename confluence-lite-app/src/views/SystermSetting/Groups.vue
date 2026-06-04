<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.groups.title') }}</h1>
            <p class="page-description">{{ $t('settings.groups.description') }}</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-input-search
                    v-model:value="searchText"
                    :placeholder="$t('settings.groups.searchPlaceholder')"
                    style="width: 250px"
                    @search="handleSearch"
                />
                <a-button type="primary" @click="showCreateModal">
                    <Plus :size="14" style="vertical-align: middle" />
                    {{ $t('settings.groups.addGroup') }}
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
                    <template v-if="column.key === 'memberCount'">
                        <a-tag color="blue">{{ record.memberCount || 0 }} {{ $t('settings.groups.memberCountUnit') }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span>{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="showEditModal(record)">{{ $t('common.edit') }}</a-button>
                            <a-button type="link" size="small" @click="showMembersModal(record)">{{ $t('settings.groups.members') }}</a-button>
                            <a-popconfirm :title="$t('settings.groups.confirmDelete')" @confirm="handleDelete(record.id)">
                                <a-button type="link" size="small" danger>{{ $t('common.delete') }}</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 添加/编辑用户组弹窗 -->
        <a-modal
            v-model:open="modalVisible"
            :title="editingGroup ? $t('settings.groups.editGroup') : $t('settings.groups.addGroup')"
            @ok="handleSubmit"
            :confirm-loading="submitting"
            width="500px"
        >
            <a-form ref="formRef" :model="formState" :label-col="{ span: 6 }" :wrapper-col="{ span: 16 }">
                <a-form-item :label="$t('settings.groups.groupName')" name="name" :rules="[{ required: true, message: $t('settings.groups.groupNameRequired') }]">
                    <a-input v-model:value="formState.name" />
                </a-form-item>
                <a-form-item :label="$t('settings.groups.groupDescription')" name="description">
                    <a-textarea v-model:value="formState.description" :rows="3" />
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- 成员管理弹窗 -->
        <a-modal
            v-model:open="membersModalVisible"
            :title="$t('settings.groups.memberManagement')"
            :footer="null"
            width="600px"
        >
            <div style="margin-bottom: 16px">
                <a-select
                    v-model:value="selectedUsers"
                    mode="multiple"
                    :placeholder="$t('settings.groups.selectUsersToAdd')"
                    style="width: 100%"
                    :filter-option="filterUser"
                    :options="availableUserOptions"
                    show-search
                />
                <a-button type="primary" style="margin-top: 8px" @click="addMembers">{{ $t('settings.groups.addMembers') }}</a-button>
            </div>
            <a-list
                :data-source="currentMembers"
                :loading="loadingMembers"
            >
                <template #renderItem="{ item }">
                    <a-list-item>
                        <a-list-item-meta :title="item.name" :description="item.username" />
                        <template #actions>
                            <a-button type="link" danger size="small" @click="removeMember(item.id)">{{ $t('settings.groups.remove') }}</a-button>
                        </template>
                    </a-list-item>
                </template>
            </a-list>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { Plus } from 'lucide-vue-next'
import { userApi, userGroupApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
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
    description: ''
})

const availableUserOptions = computed(() => {
    return availableUsers.value.map(user => ({
        label: `${user.name} (${user.username})`,
        value: user.id
    }))
})

const pagination = reactive({
    current: 1,
    pageSize: 20,
    total: 0
})

const columns = [
    { title: t('settings.groups.groupName'), dataIndex: 'name', key: 'name' },
    { title: t('settings.groups.groupDescription'), dataIndex: 'description', key: 'description' },
    { title: t('settings.groups.memberCount'), key: 'memberCount' },
    { title: t('settings.groups.createdAt'), key: 'createdAt' },
    { title: t('common.action'), key: 'action', width: 180 }
]

const loadGroups = async () => {
    loading.value = true
    try {
        const data = await userGroupApi.getList(pagination.current, pagination.pageSize, searchText.value)
        groups.value = data?.items || []
        pagination.total = data?.total || 0
    } catch (error) {
        message.error(t('settings.groups.loadFailed'))
    } finally {
        loading.value = false
    }
}

const loadAvailableUsers = async () => {
    try {
        const data = await userApi.getList(1, 100)
        availableUsers.value = data?.items || []
    } catch (error) {
        message.error(t('settings.users.loadFailed'))
    }
}

const loadGroupMembers = async (groupId) => {
    loadingMembers.value = true
    try {
        const data = await userGroupApi.getMembers(groupId)
        currentMembers.value = data?.members?.map(m => ({
            id: m.userId,
            name: m.displayName || m.username,
            username: m.username
        })) || []
    } catch (error) {
        message.error(t('settings.groups.loadMembersFailed'))
    } finally {
        loadingMembers.value = false
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
    Object.assign(formState, { name: '', description: '' })
    modalVisible.value = true
}

const showEditModal = (group) => {
    editingGroup.value = group
    Object.assign(formState, {
        name: group.name,
        description: group.description
    })
    modalVisible.value = true
}

const showMembersModal = async (group) => {
    currentGroupId.value = group.id
    membersModalVisible.value = true
    await loadAvailableUsers()
    await loadGroupMembers(group.id)
}

const handleSubmit = async () => {
    submitting.value = true
    try {
        if (editingGroup.value) {
            await userGroupApi.update(editingGroup.value.id, {
                name: formState.name,
                description: formState.description
            })
        } else {
            await userGroupApi.create({
                name: formState.name,
                description: formState.description
            })
        }
        message.success(editingGroup.value ? t('settings.groups.updateSuccess') : t('settings.groups.createSuccess'))
        modalVisible.value = false
        loadGroups()
    } catch (error) {
        message.error(error?.message || t('common.operationFailed'))
    } finally {
        submitting.value = false
    }
}

const handleDelete = async (id) => {
    try {
        await userGroupApi.remove(id)
        message.success(t('settings.groups.deleteSuccess'))
        loadGroups()
    } catch (error) {
        message.error(error?.message || t('settings.groups.deleteFailed'))
    }
}

const filterUser = (input, option) => {
    return option.children[0].children.toLowerCase().includes(input.toLowerCase())
}

const addMembers = async () => {
    if (selectedUsers.value.length === 0) return
    try {
        await userGroupApi.addMembers(currentGroupId.value, selectedUsers.value)
        message.success(t('settings.groups.addMembersSuccess'))
        selectedUsers.value = []
        await loadGroupMembers(currentGroupId.value)
    } catch (error) {
        message.error(error?.message || t('settings.groups.addMembersFailed'))
    }
}

const removeMember = async (userId) => {
    try {
        await userGroupApi.removeMember(currentGroupId.value, userId)
        message.success(t('settings.groups.removeMemberSuccess'))
        await loadGroupMembers(currentGroupId.value)
    } catch (error) {
        message.error(error?.message || t('settings.groups.removeMemberFailed'))
    }
}

onMounted(() => {
    loadGroups()
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
