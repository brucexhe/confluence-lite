<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>备份与还原</h1>
            <p class="page-description">管理系统数据备份和恢复</p>
        </div>

        <div class="page-content">
            <!-- 自动备份配置 -->
            <div class="section">
                <h3 class="section-title">自动备份配置</h3>
                <a-spin :spinning="configLoading">
                    <a-form
                        :label-col="{ style: { width: '120px' } }"
                        :wrapper-col="{ span: 16 }"
                    >
                        <a-form-item label="启用自动备份">
                            <a-switch
                                v-model:checked="backupConfig.enabled"
                                checked-children="开启"
                                un-checked-children="关闭"
                            />
                            <div class="form-hint">定期自动创建系统备份</div>
                        </a-form-item>

                        <a-form-item label="备份间隔">
                            <a-input-number
                                v-model:value="backupConfig.intervalDays"
                                :min="1"
                                :max="365"
                                style="width: 120px"
                            />
                            <span style="margin-left: 8px">天</span>
                            <div class="form-hint">自动备份的时间间隔</div>
                        </a-form-item>

                        <a-form-item label="备份内容">
                            <a-checkbox-group v-model:value="backupConfig.content">
                                <a-checkbox value="database">数据库</a-checkbox>
                                <a-checkbox value="attachments">附件文件</a-checkbox>
                                <a-checkbox value="config">系统配置</a-checkbox>
                            </a-checkbox-group>
                            <div class="form-hint">选择自动备份时包含的内容</div>
                        </a-form-item>

                        <a-form-item label="保留天数">
                            <a-input-number
                                v-model:value="backupConfig.retentionDays"
                                :min="1"
                                :max="365"
                                style="width: 120px"
                            />
                            <span style="margin-left: 8px">天</span>
                            <div class="form-hint">超过此天数的备份将自动删除</div>
                        </a-form-item>

                        <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                            <a-button type="primary" :loading="configSaving" @click="saveBackupConfig">
                                保存配置
                            </a-button>
                        </a-form-item>
                    </a-form>
                </a-spin>
            </div>

            <!-- 创建备份 -->
            <div class="section">
                <h3 class="section-title">创建备份</h3>
                <a-space direction="vertical" style="width: 100%">
                    <a-checkbox-group v-model:value="backupOptions">
                        <a-checkbox value="database">数据库</a-checkbox>
                        <a-checkbox value="attachments">附件文件</a-checkbox>
                        <a-checkbox value="config">系统配置</a-checkbox>
                    </a-checkbox-group>
                    <a-space>
                        <a-input
                            v-model:value="backupName"
                            placeholder="备份名称（可选）"
                            style="width: 250px"
                        />
                        <a-button type="primary" :loading="creating" @click="createBackup">
                            <Plus :size="14" style="vertical-align: middle" />
                            创建备份
                        </a-button>
                    </a-space>
                </a-space>
            </div>

            <!-- 备份列表 -->
            <div class="section">
                <div class="section-title-row">
                    <h3 class="section-title" style="margin: 0">备份列表</h3>
                    <a-button class="refresh" size="small" :loading="loading" @click="loadBackups">
                        <RefreshCw :size="14" style="vertical-align: middle" />
                        刷新状态
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
                                    还原
                                </a-button>
                                <a-button
                                    type="link"
                                    size="small"
                                    :disabled="record.status !== 'completed'"
                                    @click="downloadBackup(record)"
                                >
                                    下载
                                </a-button>
                                <a-popconfirm
                                    title="确定要删除该备份吗？"
                                    @confirm="deleteBackup(record.id)"
                                >
                                    <a-button type="link" size="small" danger>删除</a-button>
                                </a-popconfirm>
                            </a-space>
                        </template>
                    </template>
                </a-table>
            </div>

            <!-- 还原弹窗 -->
            <a-modal
                v-model:open="restoreModalVisible"
                title="还原备份"
                @ok="confirmRestoreBackup"
                :confirm-loading="restoring"
            >
                <a-alert
                    message="警告"
                    description="还原操作将覆盖当前数据，请确保已创建当前数据的备份。"
                    type="warning"
                    show-icon
                    style="margin-bottom: 16px"
                />
                <p>确定要还原备份 <strong>{{ selectedBackup?.name }}</strong> 吗？</p>
                <a-checkbox v-model:checked="confirmRestore">
                    我已了解风险，确认还原
                </a-checkbox>
            </a-modal>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { Plus, RefreshCw } from 'lucide-vue-next'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

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

const columns = [
    { title: '备份名称', dataIndex: 'name', key: 'name' },
    { title: '类型', dataIndex: 'type', key: 'type' },
    { title: '大小', key: 'size' },
    { title: '状态', key: 'status' },
    { title: '创建时间', key: 'createdAt' },
    { title: '操作', key: 'action', width: 200 }
]

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
    const texts = { completed: '完成', processing: '处理中', failed: '失败' }
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
        message.warning('请至少选择一种备份内容')
        return
    }

    configSaving.value = true
    try {
        await systemSettingApi.updateBackupConfig(backupConfig.value)
        message.success('备份配置保存成功')
    } catch (error) {
        message.error('保存备份配置失败')
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
                type: item.type || '完整'
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
        message.warning('请至少选择一种备份类型')
        return
    }

    creating.value = true
    try {
        await systemSettingApi.createBackup({
            name: backupName.value || undefined,
            options: backupOptions.value
        })
        message.success('备份创建成功')
        backupName.value = ''
        loadBackups()
    } catch (error) {
        message.error('创建备份失败')
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
        message.warning('请确认还原操作')
        return
    }

    restoring.value = true
    try {
        await systemSettingApi.restoreBackup(selectedBackup.value.id, {
            confirmed: true
        })
        message.success('备份还原成功，系统将重启')
        setTimeout(() => {
            window.location.href = '/'
        }, 2000)
    } catch (error) {
        message.error('还原备份失败')
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
        message.success('备份删除成功')
        loadBackups()
    } catch (error) {
        message.error('删除备份失败')
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
