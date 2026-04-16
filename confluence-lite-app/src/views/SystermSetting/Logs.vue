<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>系统日志</h1>
            <p class="page-description">查看系统运行日志和审计记录</p>
        </div>

        <div class="page-content">
            <div class="toolbar">
                <a-space>
                    <a-select v-model:value="logLevel" style="width: 120px" @change="loadLogs">
                        <a-select-option value="">全部级别</a-select-option>
                        <a-select-option value="ERROR">ERROR</a-select-option>
                        <a-select-option value="WARN">WARN</a-select-option>
                        <a-select-option value="INFO">INFO</a-select-option>
                        <a-select-option value="DEBUG">DEBUG</a-select-option>
                    </a-select>
                    <a-range-picker v-model:value="dateRange" @change="loadLogs" />
                    <a-input-search
                        v-model:value="searchText"
                        placeholder="搜索日志内容"
                        style="width: 250px"
                        @search="loadLogs"
                    />
                </a-space>
                <a-space>
                    <a-button @click="loadLogs" :loading="loading">
                        <template #icon><ReloadOutlined /></template>
                        刷新
                    </a-button>
                    <a-button @click="exportLogs">
                        <template #icon><DownloadOutlined /></template>
                        导出
                    </a-button>
                </a-space>
            </div>

            <div class="log-container" ref="logContainer">
                <div v-for="(log, index) in logs" :key="index" class="log-entry" :class="'log-' + log.level.toLowerCase()">
                    <div class="log-header">
                        <span class="log-time">{{ log.timestamp }}</span>
                        <span class="log-level">{{ log.level }}</span>
                        <span class="log-source">{{ log.source }}</span>
                    </div>
                    <div class="log-message">{{ log.message }}</div>
                    <div v-if="log.details" class="log-details">
                        <pre>{{ log.details }}</pre>
                    </div>
                </div>
                <a-empty v-if="logs.length === 0 && !loading" description="暂无日志" />
            </div>

            <div class="pagination-wrapper">
                <a-pagination
                    v-model:current="pagination.current"
                    v-model:pageSize="pagination.pageSize"
                    :total="pagination.total"
                    :show-size-changer="true"
                    :show-total="(total) => `共 ${total} 条`"
                    @change="loadLogs"
                />
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, reactive, onMounted, nextTick } from 'vue'
import { message } from 'ant-design-vue'
import { ReloadOutlined, DownloadOutlined } from '@ant-design/icons-vue'

const loading = ref(false)
const logLevel = ref('')
const dateRange = ref()
const searchText = ref('')
const logs = ref([])
const logContainer = ref()

const pagination = reactive({
    current: 1,
    pageSize: 50,
    total: 0
})

const loadLogs = async () => {
    loading.value = true
    try {
        // TODO: 调用 API 获取日志
        await new Promise(resolve => setTimeout(resolve, 500))

        // 模拟数据
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
    } catch (error) {
        message.error('加载日志失败')
    } finally {
        loading.value = false
    }
}

const exportLogs = async () => {
    try {
        // TODO: 调用 API 导出日志
        message.success('日志导出成功')
    } catch (error) {
        message.error('导出日志失败')
    }
}

onMounted(() => {
    loadLogs()
})
</script>

<style scoped>
.page-content {
    padding: 16px 24px 24px;
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
