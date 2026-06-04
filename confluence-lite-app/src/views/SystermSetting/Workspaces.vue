<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.workspaces.title') }}</h1>
            <p class="page-description">{{ $t('settings.workspaces.description') }}</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-space>
                    <a-input-search
                        v-model:value="searchText"
                        :placeholder="$t('settings.workspaces.searchPlaceholder')"
                        style="width: 250px"
                        @search="handleSearch"
                    />
                </a-space>
                <a-button type="primary" @click="$router.push('/spaces')">
                    <Plus :size="14" style="vertical-align: middle" />
                    {{ $t('settings.workspaces.createSpace') }}
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
                        <a-tag color="blue">{{ record.pageCount || 0 }} {{ $t('settings.workspaces.pages') }}</a-tag>
                    </template>
                    <template v-else-if="column.key === 'type'">
                        <a-tag :color="record.isPublic ? 'green' : 'default'">
                            {{ record.isPublic ? $t('settings.workspaces.public') : $t('settings.workspaces.private') }}
                        </a-tag>
                    </template>
                    <template v-else-if="column.key === 'createdAt'">
                        <span>{{ formatDateTime(record.createdAt) }}</span>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <a-space>
                            <a-button type="link" size="small" @click="$router.push(`/${record.key}`)">{{ $t('common.view') }}</a-button>
                            <a-button type="link" size="small" @click="showEditModal(record)">{{ $t('common.edit') }}</a-button>
                            <a-popconfirm :title="$t('settings.workspaces.confirmDelete')" @confirm="handleDelete(record.id)">
                                <a-button type="link" size="small" danger>{{ $t('common.delete') }}</a-button>
                            </a-popconfirm>
                        </a-space>
                    </template>
                </template>
            </a-table>
        </div>

        <!-- 编辑空间弹窗 -->
        <a-modal
            v-model:open="editModalVisible"
            :title="$t('settings.workspaces.editSpace')"
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
                <a-form-item :label="$t('settings.workspaces.spaceName')" name="name" :rules="[{ required: true, message: $t('settings.workspaces.spaceNameRequired') }]">
                    <a-input v-model:value="formState.name" :placeholder="$t('settings.workspaces.spaceNamePlaceholder')" />
                </a-form-item>
                <a-form-item :label="$t('settings.workspaces.spaceKey')" name="key" :rules="[
                    { required: true, message: $t('settings.workspaces.spaceKeyRequired') },
                    { pattern: /^[a-z0-9_-]+$/, message: $t('settings.workspaces.spaceKeyPattern') }
                ]">
                    <a-input
                        v-model:value="formState.key"
                        :placeholder="$t('settings.workspaces.spaceKeyPlaceholder')"
                        :disabled="!!editingWorkspace"
                    />
                </a-form-item>
                <a-form-item :label="$t('settings.workspaces.description')" name="description">
                    <a-textarea v-model:value="formState.description" :rows="3" :placeholder="$t('settings.workspaces.descriptionPlaceholder')" />
                </a-form-item>
                <a-form-item :label="$t('settings.workspaces.iconStyle')" name="iconStyle">
                    <a-select v-model:value="formState.iconStyle" style="width: 200px">
                        <a-select-option value="green">{{ $t('settings.workspaces.colorGreen') }}</a-select-option>
                        <a-select-option value="blue">{{ $t('settings.workspaces.colorBlue') }}</a-select-option>
                        <a-select-option value="purple">{{ $t('settings.workspaces.colorPurple') }}</a-select-option>
                        <a-select-option value="orange">{{ $t('settings.workspaces.colorOrange') }}</a-select-option>
                        <a-select-option value="red">{{ $t('settings.workspaces.colorRed') }}</a-select-option>
                        <a-select-option value="cyan">{{ $t('settings.workspaces.colorCyan') }}</a-select-option>
                    </a-select>
                </a-form-item>
                <a-form-item :label="$t('settings.workspaces.publicAccess')" name="isPublic">
                    <a-switch
                        v-model:checked="formState.isPublic"
                        :checked-children="$t('settings.workspaces.public')"
                        :un-checked-children="$t('settings.workspaces.private')"
                    />
                    <div class="form-hint">{{ $t('settings.workspaces.publicAccessHint') }}</div>
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
import { workspaceApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()
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
    { title: t('settings.workspaces.spaceName'), dataIndex: 'name', key: 'name' },
    { title: 'Key', dataIndex: 'key', key: 'key' },
    // { title: t('settings.workspaces.description'), dataIndex: 'description', key: 'description' },
    { title: t('settings.workspaces.pageCount'), key: 'pageCount' },
    { title: t('settings.workspaces.type'), key: 'type' },
    { title: t('settings.workspaces.creator'), dataIndex: 'creatorName', key: 'creatorName' },
    { title: t('settings.workspaces.createdAt'), key: 'createdAt' },
    { title: t('common.action'), key: 'action', width: 150 }
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
        message.error(t('settings.workspaces.loadFailed'))
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
        message.success(t('settings.workspaces.updateSuccess'))
        editModalVisible.value = false
        loadWorkspaces()
    } catch (error) {
        message.error(t('settings.workspaces.updateFailed'))
    } finally {
        updating.value = false
    }
}

const handleDelete = async (id) => {
    try {
        await workspaceApi.remove(id)
        message.success(t('settings.workspaces.deleteSuccess'))
        loadWorkspaces()
    } catch (error) {
        message.error(t('settings.workspaces.deleteFailed'))
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
