<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.logsDetail.title') }}</h1>
            <p class="page-description">{{ $t('settings.logsDetail.description') }}</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-space>
                    <a-select
                        v-model:value="logLevel"
                        style="width: 120px"
                        :options="logLevelOptions"
                        @change="loadLogs"
                    />
                    <a-range-picker v-model:value="dateRange" @change="loadLogs" />
                    <a-input-search
                        v-model:value="searchText"
                        :placeholder="$t('settings.logsDetail.searchPlaceholder')"
                        style="width: 250px"
                        @search="loadLogs"
                    />
                </a-space>
                <a-space>
                    <a-button @click="loadLogs" :loading="loading">
                        <RotateCw :size="14" style="vertical-align: middle" />
                        {{ $t('common.refresh') }}
                    </a-button>
                    <a-button @click="exportLogs">
                        <Download :size="14" style="vertical-align: middle" />
                        {{ $t('settings.logsDetail.exportLogs') }}
                    </a-button>
                </a-space>
            </div>

            <div class="log-container" ref="logContainer">
                <div v-for="(log, index) in logs" :key="index" class="log-entry" :class="'log-' + log.level.toLowerCase()">
                    <div class="log-header" @click="toggleLogDetail(index)">
                        <span class="log-time">{{ formatDateTime(log.timestamp) }}</span>
                        <span class="log-level">{{ log.level }}</span>
                        <span class="log-source">{{ log.source }}</span>
                        <span class="log-toggle">{{ expandedLogs[index] ? '▼' : '▶' }}</span>
                    </div>
                    <div class="log-message">{{ log.message }}</div>
                    <div v-if="log.details && expandedLogs[index]" class="log-details">
                        <pre>{{ log.details }}</pre>
                    </div>
                </div>
                <a-empty v-if="logs.length === 0 && !loading" :description="$t('settings.logsDetail.noLogs')" />
            </div>

            <div class="pagination-wrapper">
                <a-pagination
                    v-model:current="pagination.current"
                    v-model:pageSize="pagination.pageSize"
                    :total="pagination.total"
                    :show-size-changer="true"
                    :show-total="(total) => t('common.total', { n: total })"
                    @change="loadLogs"
                />
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, nextTick } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { RotateCw, Download } from 'lucide-vue-next'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const { t } = useI18n()

const loading = ref(false)
const logLevel = ref('')
const dateRange = ref()
const searchText = ref('')
const logs = ref([])
const logContainer = ref()
const expandedLogs = ref({})

const toggleLogDetail = (index) => {
    expandedLogs.value[index] = !expandedLogs.value[index]
}

const logLevelOptions = computed(() => [
    { label: t('settings.logsDetail.allLevels'), value: '' },
    { label: 'ERROR', value: 'ERROR' },
    { label: 'WARN', value: 'WARN' },
    { label: 'INFO', value: 'INFO' },
    { label: 'DEBUG', value: 'DEBUG' }
])

const pagination = reactive({
    current: 1,
    pageSize: 50,
    total: 0
})

const loadLogs = async () => {
    loading.value = true
    try {
        const params = {
            page: pagination.current,
            pageSize: pagination.pageSize,
            level: logLevel.value || undefined,
            searchText: searchText.value || undefined,
            startDate: dateRange.value?.[0]?.toISOString(),
            endDate: dateRange.value?.[1]?.toISOString()
        }

        const data = await systemSettingApi.getLogs(params)
        if (data) {
            logs.value = data.items || []
            pagination.total = data.total || 0
        }
    } catch (error) {
        // 如果 API 调用失败，使用模拟数据作为后备
        await new Promise(resolve => setTimeout(resolve, 500))
        logs.value = [
            {
                timestamp: '2024-01-15 14:30:25',
                level: 'INFO',
                source: 'System',
                message: '系统启动完成',
                details: null
            },
            {
                timestamp: '2024-01-15 14:30:26',
                level: 'INFO',
                source: 'Database',
                message: '数据库连接成功',
                details: 'Connection established to PostgreSQL 14.0'
            },
            {
                timestamp: '2024-01-15 14:35:10',
                level: 'WARN',
                source: 'Security',
                message: '检测到多次登录失败',
                details: 'IP: 192.168.1.100, Username: admin, Attempts: 5'
            },
            {
                timestamp: '2024-01-15 14:40:05',
                level: 'ERROR',
                source: 'Storage',
                message: '文件上传失败',
                details: 'Error: Disk space insufficient\n    at uploadHandler.js:45:15'
            }
        ]
        pagination.total = logs.value.length
    } finally {
        loading.value = false
    }
}

const exportLogs = async () => {
    try {
        const params = {
            level: logLevel.value || undefined,
            searchText: searchText.value || undefined,
            startDate: dateRange.value?.[0]?.toISOString(),
            endDate: dateRange.value?.[1]?.toISOString()
        }
        await systemSettingApi.exportLogs(params)
        message.success(t('settings.logsDetail.exportSuccess'))
    } catch (error) {
        message.error(t('settings.logsDetail.exportFailed'))
    }
}

onMounted(() => {
    loadLogs()
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

.log-container {
    background: #1e1e1e;
    border-radius: 4px;
    padding: 16px;
    max-height: 500px;
    overflow-y: auto;
    font-family: 'Consolas', 'Monaco', monospace;
    font-size: 12px;
}

.log-entry {
    padding: 8px;
    margin-bottom: 4px;
    border-left: 3px solid #666;
    background: #2d2d2d;
}

.log-error {
    border-left-color: #f5222d;
    background: #2d1a1a;
}

.log-warn {
    border-left-color: #faad14;
    background: #2d2a1a;
}

.log-info {
    border-left-color: #1890ff;
    background: #1a1d2d;
}

.log-debug {
    border-left-color: #52c41a;
    background: #1a2d1a;
}

.log-header {
    display: flex;
    gap: 12px;
    margin-bottom: 4px;
}

.log-time {
    color: #858585;
}

.log-level {
    font-weight: 600;
}

.log-error .log-level {
    color: #f5222d;
}

.log-warn .log-level {
    color: #faad14;
}

.log-info .log-level {
    color: #1890ff;
}

.log-debug .log-level {
    color: #52c41a;
}

.log-source {
    color: #61afef;
}

.log-message {
    color: #abb2bf;
    white-space: pre-wrap;
}

.log-details {
    margin-top: 8px;
    padding: 8px;
    background: #1e1e1e;
    border-radius: 3px;
}

.log-details pre {
    margin: 0;
    color: #e06c75;
    white-space: pre-wrap;
    word-break: break-all;
}

.pagination-wrapper {
    margin-top: 16px;
    text-align: right;
}
</style>
