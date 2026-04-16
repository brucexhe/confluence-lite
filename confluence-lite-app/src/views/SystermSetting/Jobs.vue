<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>作业管理</h1>
            <p class="page-description">管理系统定时任务和后台作业</p>
        </div>

        <div class="page-content">
            <!-- 作业统计 -->
            <a-row :gutter="16" class="stats-row">
                <a-col :span="6">
                    <a-card>
                        <a-statistic title="运行中" :value="stats.running" :value-style="{ color: '#1890ff' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic title="已完成" :value="stats.completed" :value-style="{ color: '#52c41a' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic title="失败" :value="stats.failed" :value-style="{ color: '#f5222d' }" />
                    </a-card>
                </a-col>
                <a-col :span="6">
                    <a-card>
                        <a-statistic title="等待中" :value="stats.pending" />
                    </a-card>
                </a-col>
            </a-row>

            <!-- 作业列表 -->
            <div class="section">
                <h3 class="section-title">定时任务</h3>
                <a-table
                    :columns="jobColumns"
                    :data-source="scheduledJobs"
                    :pagination="false"
                    row-key="id"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'status'">
                            <a-switch
                                :checked="record.enabled"
                                @change="toggleJob(record)"
                                checked-children="启用"
                                un-checked-children="禁用"
                            />
                        </template>
                        <template v-else-if="column.key === 'schedule'">
                            <a-tag color="blue">{{ record.cron }}</a-tag>
                        </template>
                        <template v-else-if="column.key === 'lastRun'">
                            <span v-if="record.lastRun">{{ formatDateTime(record.lastRun) }}</span>
                            <span v-else class="text-muted">未执行</span>
                        </template>
                        <template v-else-if="column.key === 'nextRun'">
                            <span v-if="record.nextRun">{{ formatDateTime(record.nextRun) }}</span>
                            <span v-else class="text-muted">-</span>
                        </template>
                        <template v-else-if="column.key === 'action'">
                            <a-space>
                                <a-button type="link" size="small" @click="runNow(record)">
                                    立即执行
                                </a-button>
                                <a-button type="link" size="small" @click="showJobLog(record)">
                                    执行日志
                                </a-button>
                            </a-space>
                        </template>
                    </template>
                </a-table>
            </div>

            <!-- 执行历史 -->
            <div class="section">
                <h3 class="section-title">执行历史</h3>
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
                        <template v-else-if="column.key === 'startTime'">
                            <span>{{ formatDateTime(record.startTime) }}</span>
                        </template>
                        <template v-else-if="column.key === 'duration'">
                            {{ record.duration }}ms
                        </template>
                    </template>
                </a-table>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

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

const jobColumns = [
    { title: '任务名称', dataIndex: 'name', key: 'name' },
    { title: '描述', dataIndex: 'description', key: 'description' },
    { title: '执行计划', key: 'schedule' },
    { title: '状态', key: 'status' },
    { title: '上次执行', key: 'lastRun' },
    { title: '下次执行', key: 'nextRun' },
    { title: '操作', key: 'action', width: 180 }
]

const historyColumns = [
    { title: '任务名称', dataIndex: 'jobName', key: 'jobName' },
    { title: '状态', key: 'status' },
    { title: '开始时间', key: 'startTime' },
    { title: '耗时', key: 'duration' },
    { title: '消息', dataIndex: 'message', key: 'message' }
]

const getExecutionStatusColor = (status) => {
    const colors = { success: 'green', failed: 'red', running: 'blue' }
    return colors[status] || 'default'
}

const getExecutionStatusText = (status) => {
    const texts = { success: '成功', failed: '失败', running: '运行中' }
    return texts[status] || status
}

const toggleJob = async (job) => {
    try {
        if (job.enabled) {
            await systemSettingApi.pauseJob(job.id)
            job.enabled = false
            message.success('任务已禁用')
        } else {
            await systemSettingApi.resumeJob(job.id)
            job.enabled = true
            message.success('任务已启用')
        }
    } catch (error) {
        message.error('操作失败')
    }
}

const runNow = async (job) => {
    try {
        await systemSettingApi.runJob(job.id)
        message.success('任务 ' + job.name + ' 已加入执行队列')
        // 刷新任务列表
        loadJobs()
    } catch (error) {
        message.error('执行任务失败')
    }
}

const showJobLog = async (job) => {
    try {
        const data = await systemSettingApi.getJobLogs(job.id)
        // TODO: 显示任务执行日志弹窗
        message.info('查看任务日志: ' + job.name + '，共 ' + (data?.total || 0) + ' 条记录')
    } catch (error) {
        message.error('加载任务日志失败')
    }
}

const loadJobs = async () => {
    try {
        const data = await systemSettingApi.getJobs()
        if (data) {
            scheduledJobs.value = data.scheduledJobs || scheduledJobs.value
            jobHistory.value = data.jobHistory || jobHistory.value
            stats.value = data.stats || stats.value
        }
    } catch (error) {
        // 如果 API 调用失败，使用默认模拟数据
        console.log('Using mock data for jobs')
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
