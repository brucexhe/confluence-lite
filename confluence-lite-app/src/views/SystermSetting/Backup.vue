<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.backup.title') }}</h1>
            <p class="page-description">{{ $t('settings.backup.description') }}</p>
        </div>

        <div class="page-content">
            <!-- 自动备份配置 -->
            <div class="section">
                <h3 class="section-title">{{ $t('settings.backup.autoBackupConfig') }}</h3>
                <a-spin :spinning="configLoading">
                    <a-form
                        :label-col="{ style: { width: '120px' } }"
                        :wrapper-col="{ span: 16 }"
                    >
                        <a-form-item :label="$t('settings.backup.enableAutoBackup')">
                            <a-switch
                                v-model:checked="backupConfig.enabled"
                                :checked-children="$t('common.enabled')"
                                :un-checked-children="$t('common.disabled')"
                            />
                            <div class="form-hint">{{ $t('settings.backup.autoBackupHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.backup.backupInterval')">
                            <a-input-number
                                v-model:value="backupConfig.intervalDays"
                                :min="1"
                                :max="365"
                                style="width: 120px"
                            />
                            <span style="margin-left: 8px">{{ $t('settings.backup.days') }}</span>
                            <div class="form-hint">{{ $t('settings.backup.backupIntervalHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.backup.backupContent')">
                            <a-checkbox-group v-model:value="backupConfig.content">
                                <a-checkbox value="database">{{ $t('settings.backup.database') }}</a-checkbox>
                                <a-checkbox value="attachments">{{ $t('settings.backup.attachments') }}</a-checkbox>
                                <a-checkbox value="config">{{ $t('settings.backup.systemConfig') }}</a-checkbox>
                            </a-checkbox-group>
                            <div class="form-hint">{{ $t('settings.backup.backupContentHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.backup.retentionDays')">
                            <a-input-number
                                v-model:value="backupConfig.retentionDays"
                                :min="1"
                                :max="365"
                                style="width: 120px"
                            />
                            <span style="margin-left: 8px">{{ $t('settings.backup.days') }}</span>
                            <div class="form-hint">{{ $t('settings.backup.retentionDaysHint') }}</div>
                        </a-form-item>

                        <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                            <a-button type="primary" :loading="configSaving" @click="saveBackupConfig">
                                {{ $t('settings.backup.saveConfig') }}
                            </a-button>
                        </a-form-item>
                    </a-form>
                </a-spin>
            </div>

            <!-- 创建备份 -->
            <div class="section">
                <h3 class="section-title">{{ $t('settings.backup.createBackup') }}</h3>
                <a-space direction="vertical" style="width: 100%">
                    <a-checkbox-group v-model:value="backupOptions">
                        <a-checkbox value="database">{{ $t('settings.backup.database') }}</a-checkbox>
                        <a-checkbox value="attachments">{{ $t('settings.backup.attachments') }}</a-checkbox>
                        <a-checkbox value="config">{{ $t('settings.backup.systemConfig') }}</a-checkbox>
                    </a-checkbox-group>
                    <a-space>
                        <a-input
                            v-model:value="backupName"
                            :placeholder="$t('settings.backup.backupNamePlaceholder')"
                            style="width: 250px"
                        />
                        <a-button type="primary" :loading="creating" @click="createBackup">
                            <Plus :size="14" style="vertical-align: middle" />
                            {{ $t('settings.backup.createBackup') }}
                        </a-button>
                    </a-space>
                </a-space>
            </div>

            <!-- 备份列表 -->
            <div class="section">
                <div class="section-title-row">
                    <h3 class="section-title" style="margin: 0">{{ $t('settings.backup.backupList') }}</h3>
                    <a-button class="refresh" size="small" :loading="loading" @click="loadBackups">
                        <RefreshCw :size="14" style="vertical-align: middle" />
                        {{ $t('settings.backup.refreshStatus') }}
                    </a-button>
                </div>
                <a-table
                    :columns="columns"
                    :data-source="backups"
                    :loading="loading"
                    :pagination="false"
                    row-key="id"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'name'">
                            <div>
                                <div>{{ record.name }}</div>
                                <div class="text-muted">{{ record.description }}</div>
                            </div>
                        </template>
                        <template v-else-if="column.key === 'size'">
                            {{ formatBytes(record.size) }}
                        </template>
                        <template v-else-if="column.key === 'status'">
                            <a-tag :color="getStatusColor(record.status)">
                                {{ getStatusText(record.status) }}
                            </a-tag>
                        </template>
                        <template v-else-if="column.key === 'createdAt'">
                            <span>{{ formatDateTime(record.createdAt) }}</span>
                        </template>
                        <template v-else-if="column.key === 'action'">
                            <a-space>
                                <a-button
                                    type="link"
                                    size="small"
                                    :disabled="record.status !== 'completed'"
                                    @click="restoreBackup(record)"
                                >
                                    {{ $t('settings.backup.restore') }}
                                </a-button>
                                <a-button
                                    type="link"
                                    size="small"
                                    :disabled="record.status !== 'completed'"
                                    @click="downloadBackup(record)"
                                >
                                    {{ $t('settings.backup.download') }}
                                </a-button>
                                <a-popconfirm
                                    :title="$t('settings.backup.confirmDelete')"
                                    @confirm="deleteBackup(record.id)"
                                >
                                    <a-button type="link" size="small" danger>{{ $t('common.delete') }}</a-button>
                                </a-popconfirm>
                            </a-space>
                        </template>
                    </template>
                </a-table>
            </div>

            <!-- 还原弹窗 -->
            <a-modal
                v-model:open="restoreModalVisible"
                :title="$t('settings.backup.restoreBackup')"
                @ok="confirmRestoreBackup"
                :confirm-loading="restoring"
            >
                <a-alert
                    :message="$t('settings.backup.warning')"
                    :description="$t('settings.backup.restoreWarning')"
                    type="warning"
                    show-icon
                    style="margin-bottom: 16px"
                />
                <p>{{ $t('settings.backup.confirmRestoreText') }} <strong>{{ selectedBackup?.name }}</strong>？</p>
                <a-checkbox v-model:checked="confirmRestore">
                    {{ $t('settings.backup.confirmRestoreCheck') }}
                </a-checkbox>
            </a-modal>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { Plus, RefreshCw } from 'lucide-vue-next'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()

const loading = ref(false)
const creating = ref(false)
const restoring = ref(false)
const backupName = ref('')
const backupOptions = ref(['database', 'attachments', 'config'])
const restoreModalVisible = ref(false)
const selectedBackup = ref(null)
const confirmRestore = ref(false)

const configLoading = ref(false)
const configSaving = ref(false)
const backupConfig = ref({
    enabled: false,
    intervalDays: 1,
    content: ['database', 'attachments', 'config'],
    retentionDays: 30
})

const backups = ref([])

const columns = computed(() => [
    { title: t('settings.backup.fileName'), dataIndex: 'name', key: 'name' },
    { title: t('common.type'), dataIndex: 'type', key: 'type' },
    { title: t('settings.backup.size'), key: 'size' },
    { title: t('common.status'), key: 'status' },
    { title: t('settings.backup.createdAt'), key: 'createdAt' },
    { title: t('common.actions'), key: 'action', width: 200 }
])

const formatBytes = (bytes) => {
    if (!bytes) return '-'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
}

const getStatusColor = (status) => {
    const colors = { completed: 'green', processing: 'blue', failed: 'red' }
    return colors[status] || 'default'
}

const getStatusText = (status) => {
    const texts = { completed: t('common.success'), processing: t('settings.backup.processing'), failed: t('common.failed') }
    return texts[status] || status
}

const loadBackupConfig = async () => {
    configLoading.value = true
    try {
        const data = await systemSettingApi.getBackupConfig()
        if (data) {
            backupConfig.value = {
                enabled: data.enabled || false,
                intervalDays: data.intervalDays || 1,
                content: data.content || ['database', 'attachments', 'config'],
                retentionDays: data.retentionDays || 30
            }
        }
    } catch (error) {
        console.error('Failed to load backup config:', error)
    } finally {
        configLoading.value = false
    }
}

const saveBackupConfig = async () => {
    if (backupConfig.value.content.length === 0) {
        message.warning(t('settings.backup.selectContentWarning'))
        return
    }

    configSaving.value = true
    try {
        await systemSettingApi.updateBackupConfig(backupConfig.value)
        message.success(t('settings.backup.configSaveSuccess'))
    } catch (error) {
        message.error(t('settings.backup.configSaveFailed'))
    } finally {
        configSaving.value = false
    }
}

const loadBackups = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getBackups()
        if (data) {
            backups.value = data.map(item => ({
                ...item,
                size: item.fileSize,
                description: item.description || '',
                type: item.type || t('settings.backup.fullBackup')
            })) || []
        }
    } catch (error) {
        console.error('Failed to load backups:', error)
        backups.value = []
    } finally {
        loading.value = false
    }
}

const createBackup = async () => {
    if (backupOptions.value.length === 0) {
        message.warning(t('settings.backup.selectTypeWarning'))
        return
    }

    creating.value = true
    try {
        await systemSettingApi.createBackup({
            name: backupName.value || undefined,
            options: backupOptions.value
        })
        message.success(t('settings.backup.backupCreated'))
        backupName.value = ''
        loadBackups()
    } catch (error) {
        message.error(t('settings.backup.backupCreateFailed'))
    } finally {
        creating.value = false
    }
}

const restoreBackup = (backup) => {
    selectedBackup.value = backup
    confirmRestore.value = false
    restoreModalVisible.value = true
}

const confirmRestoreBackup = async () => {
    if (!confirmRestore.value) {
        message.warning(t('settings.backup.confirmRestoreWarning'))
        return
    }

    restoring.value = true
    try {
        await systemSettingApi.restoreBackup(selectedBackup.value.id, {
            confirmed: true
        })
        message.success(t('settings.backup.restoreSuccess'))
        setTimeout(() => {
            window.location.href = '/'
        }, 2000)
    } catch (error) {
        message.error(t('settings.backup.restoreFailed'))
    } finally {
        restoring.value = false
        restoreModalVisible.value = false
    }
}

const downloadBackup = (backup) => {
    systemSettingApi.downloadBackup(backup.id, backup.name)
}

const deleteBackup = async (id) => {
    try {
        await systemSettingApi.deleteBackup(id)
        message.success(t('settings.backup.deleteSuccess'))
        loadBackups()
    } catch (error) {
        message.error(t('settings.backup.deleteFailed'))
    }
}

onMounted(() => {
    loadBackupConfig()
    loadBackups()
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

.section {
    margin-bottom: 32px;
    padding-bottom: 24px;
    border-bottom: 1px solid #ebecf0;
}

.section:last-child {
    border-bottom: none;
}

.section-title {
    font-size: 14px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 16px 0;
}

.text-muted {
    font-size: 12px;
    color: #6b778c;
}

.form-hint {
    margin-top: 2px;
    font-size: 12px;
    color: #6b778c;
    line-height: 1.4;
}

.section-title-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 16px;
}
.refresh{
    display: flex;
    align-items: center;
    gap: 4px; 
}
</style>
