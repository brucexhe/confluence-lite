<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.jobs.title') }}</h1>
            <p class="page-description">{{ $t('settings.jobs.description') }}</p>
        </div>

        <div class="page-content">
            <!-- 作业统计 -->
            <a-row :gutter="16" class="stats-row">
                <a-col :span="6">
                    <a-card>
                        <a-statistic :title="$t('settings.jobs.running')" :value="stats.running" :value-style="{ color: '#1890ff' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic :title="$t('settings.jobs.completed')" :value="stats.completed" :value-style="{ color: '#52c41a' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic :title="$t('common.failed')" :value="stats.failed" :value-style="{ color: '#f5222d' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic :title="$t('settings.jobs.scheduled')" :value="stats.pending" />
                    </a-card>
                </a-col>
            </a-row>

            <!-- 作业列表 -->
            <div class="section">
                <h3 class="section-title">{{ $t('settings.jobs.scheduledTasks') }}</h3>
                <a-table
                    :columns="jobColumns"
                    :data-source="scheduledJobs"
                    :pagination="false"
                    row-key="id"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'enabled'">
                            <a-switch
                                :checked="record.enabled"
                                @change="toggleJob(record)"
                                :checked-children="$t('common.enable')"
                                :un-checked-children="$t('common.disable')"
                            />
                        </template>
                        <template v-else-if="column.key === 'cron'">
                            <a-tag color="blue">{{ record.cron }}</a-tag>
                        </template>
                        <template v-else-if="column.key === 'lastRun'">
                            <span v-if="record.lastRun">{{ formatDateTime(record.lastRun) }}</span>
                            <span v-else class="text-muted">{{ $t('settings.jobs.notExecuted') }}</span>
                        </template>
                        <template v-else-if="column.key === 'nextRun'">
                            <span v-if="record.nextRun">{{ formatDateTime(record.nextRun) }}</span>
                            <span v-else class="text-muted">-</span>
                        </template>
                        <template v-else-if="column.key === 'action'">
                            <a-space>
                                <a-button type="link" size="small" @click="runNow(record)">
                                    {{ $t('settings.jobs.runNow') }}
                                </a-button>
                                <a-button type="link" size="small" @click="showJobLog(record)">
                                    {{ $t('settings.jobs.executionLog') }}
                                </a-button>
                            </a-space>
                        </template>
                    </template>
                </a-table>
            </div>

            <!-- 执行历史 -->
            <div class="section">
                <h3 class="section-title">{{ $t('settings.jobs.executionHistory') }}</h3>
                <a-table
                    :columns="historyColumns"
                    :data-source="jobHistory"
                    :pagination="{ pageSize: 10 }"
                    row-key="id"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'status'">
                            <a-tag :color="getExecutionStatusColor(record.status)">
                                {{ getExecutionStatusText(record.status) }}
                            </a-tag>
                        </template>
                        <template v-else-if="column.key === 'startedAt'">
                            <span>{{ formatDateTime(record.startedAt) }}</span>
                        </template>
                        <template v-else-if="column.key === 'durationMs'">
                            <span v-if="record.durationMs">{{ record.durationMs }}ms</span>
                            <span v-else class="text-muted">-</span>
                        </template>
                        <template v-else-if="column.key === 'outputMessage'">
                            <span>{{ record.outputMessage || record.errorMessage || '-' }}</span>
                        </template>
                    </template>
                </a-table>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()

const stats = ref({
    running: 2,
    completed: 156,
    failed: 3,
    pending: 5
})

const scheduledJobs = ref([
    {
        id: 1,
        name: '数据库备份',
        description: '每日自动备份数据库',
        cron: '0 0 2 * * ?',
        enabled: true,
        lastRun: '2024-01-15 02:00:00',
        nextRun: '2024-01-16 02:00:00'
    },
    {
        id: 2,
        name: '清理过期日志',
        description: '清理30天前的系统日志',
        cron: '0 0 3 * * ?',
        enabled: true,
        lastRun: '2024-01-15 03:00:00',
        nextRun: '2024-01-16 03:00:00'
    },
    {
        id: 3,
        name: '缓存预热',
        description: '预热常用数据缓存',
        cron: '0 0/30 * * * ?',
        enabled: false,
        lastRun: '2024-01-14 18:30:00',
        nextRun: '-'
    },
    {
        id: 4,
        name: '索引更新',
        description: '更新全文搜索索引',
        cron: '0 0 4 * * ?',
        enabled: true,
        lastRun: '2024-01-15 04:00:00',
        nextRun: '2024-01-16 04:00:00'
    },
    {
        id: 5,
        name: '统计计算',
        description: '计算站点统计数据',
        cron: '0 0 5 * * ?',
        enabled: true,
        lastRun: '2024-01-15 05:00:00',
        nextRun: '2024-01-16 05:00:00'
    }
])

const jobHistory = ref([
    {
        id: 1,
        jobName: '数据库备份',
        status: 'success',
        startTime: '2024-01-15 02:00:00',
        duration: 5230,
        message: '备份成功'
    },
    {
        id: 2,
        jobName: '清理过期日志',
        status: 'success',
        startTime: '2024-01-15 03:00:00',
        duration: 1200,
        message: '清理了 1520 条日志'
    },
    {
        id: 3,
        jobName: '索引更新',
        status: 'success',
        startTime: '2024-01-15 04:00:00',
        duration: 8900,
        message: '更新了 120 个页面索引'
    },
    {
        id: 4,
        jobName: '数据库备份',
        status: 'failed',
        startTime: '2024-01-14 02:00:00',
        duration: 0,
        message: '磁盘空间不足'
    },
    {
        id: 5,
        jobName: '统计计算',
        status: 'running',
        startTime: '2024-01-15 05:00:00',
        duration: null,
        message: '执行中...'
    }
])

const jobColumns = computed(() => [
    { title: t('settings.jobs.jobName'), dataIndex: 'name', key: 'name' },
    { title: t('common.description'), dataIndex: 'description', key: 'description' },
    { title: t('settings.jobs.executionSchedule'), key: 'cron' },
    { title: t('common.status'), key: 'enabled' },
    { title: t('settings.jobs.lastRun'), key: 'lastRun' },
    { title: t('settings.jobs.nextRun'), key: 'nextRun' },
    { title: t('common.actions'), key: 'action', width: 180 }
])

const historyColumns = computed(() => [
    { title: t('common.status'), key: 'status' },
    { title: t('settings.jobs.startTime'), key: 'startedAt' },
    { title: t('settings.jobs.duration'), key: 'durationMs' },
    { title: t('settings.logsDetail.message'), dataIndex: 'outputMessage', key: 'outputMessage' }
])

const getExecutionStatusColor = (status) => {
    const colors = { success: 'green', failed: 'red', running: 'blue' }
    return colors[status] || 'default'
}

const getExecutionStatusText = (status) => {
    const texts = { success: t('common.success'), failed: t('common.failed'), running: t('settings.jobs.running') }
    return texts[status] || status
}

const toggleJob = async (job) => {
    try {
        if (job.enabled) {
            await systemSettingApi.pauseJob(job.id)
            job.enabled = false
            message.success(t('settings.jobs.jobDisabled'))
        } else {
            await systemSettingApi.resumeJob(job.id)
            job.enabled = true
            message.success(t('settings.jobs.jobEnabled'))
        }
    } catch (error) {
        message.error(t('common.operationFailed'))
    }
}

const runNow = async (job) => {
    try {
        await systemSettingApi.runJob(job.id)
        message.success(t('settings.jobs.jobQueued', { name: job.name }))
        // 刷新任务列表
        loadJobs()
    } catch (error) {
        message.error(t('settings.jobs.runFailed'))
    }
}

const showJobLog = async (job) => {
    try {
        const data = await systemSettingApi.getJobLogs(job.id)
        if (data) {
            jobHistory.value = data || []
        }
        message.info(t('settings.jobs.viewJobLog', { name: job.name, count: jobHistory.value.length }))
    } catch (error) {
        message.error(t('settings.jobs.loadJobLogFailed'))
    }
}

const loadJobs = async () => {
    try {
        // 加载定时任务列表
        const jobsData = await systemSettingApi.getJobs()
        if (jobsData) {
            scheduledJobs.value = jobsData || []
        }

        // 加载统计数据
        // Cookie 会自动发送，无需手动添加 Authorization header
        const statsData = await fetch('/api/system/jobs/stats')
            .then(res => res.json())
            .then(data => data.success ? data.data : null)

        if (statsData) {
            stats.value = {
                running: statsData.running || 0,
                completed: statsData.completed || 0,
                failed: statsData.failed || 0,
                pending: statsData.pending || 0
            }
        }
    } catch (error) {
        console.error('Failed to load jobs:', error)
    }
}

onMounted(() => {
    loadJobs()
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

.stats-row {
    margin-bottom: 24px;
}

.section {
    margin-bottom: 24px;
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
    color: #6b778c;
}
</style>
