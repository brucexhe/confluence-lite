<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>备份与还原</h1>
            <p class="page-description">管理系统数据备份和恢复</p>
        </div>

        <div class="page-content">
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
                <h3 class="section-title">备份列表</h3>
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
import { Plus } from 'lucide-vue-next'
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

const downloadBackup = async (backup) => {
    try {
        // TODO: 实现下载功能（可能需要特殊的下载端点）
        message.success('开始下载: ' + backup.name)
    } catch (error) {
        message.error('下载失败')
    }
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
</style>
