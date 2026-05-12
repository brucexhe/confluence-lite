<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>系统信息</h1>
            <p class="page-description">查看系统运行状态和版本信息</p>
        </div>

        <a-spin :spinning="loading">
            <div class="info-content">
                <!-- 系统概览 -->
                <div class="info-section">
                    <h3 class="section-title">系统概览</h3>
                    <a-descriptions bordered :column="2">
                        <a-descriptions-item label="产品名称">Confluence Lite</a-descriptions-item>
                        <a-descriptions-item label="系统版本">{{ systemInfo.version }}</a-descriptions-item>
                        <a-descriptions-item label="构建时间">{{ systemInfo.buildTime }}</a-descriptions-item>
                        <a-descriptions-item label="运行环境">{{ systemInfo.environment }}</a-descriptions-item>
                        <a-descriptions-item label="Git 提交">{{ systemInfo.gitCommit }}</a-descriptions-item>
                        <a-descriptions-item label="启动时间">{{ systemInfo.startTime }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 服务器信息 -->
                <div class="info-section">
                    <h3 class="section-title">服务器信息</h3>
                    <a-descriptions bordered :column="2">
                        <a-descriptions-item label="主机名">{{ systemInfo.hostname }}</a-descriptions-item>
                        <a-descriptions-item label="平台">{{ systemInfo.platform }}</a-descriptions-item>
                        <a-descriptions-item label="架构">{{ systemInfo.arch }}</a-descriptions-item>
                        <a-descriptions-item label="CPU 核心数">{{ systemInfo.cpu.cores }} 核心</a-descriptions-item>
                        <a-descriptions-item label="总内存">{{ formatBytes(systemInfo.memory.total) }}</a-descriptions-item>
                        <a-descriptions-item label="可用内存">{{ formatBytes(systemInfo.memory.free) }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 运行环境 -->
                <div class="info-section">
                    <h3 class="section-title">运行环境</h3>
                    <a-descriptions bordered :column="2">
                        <a-descriptions-item label="Node.js 版本">{{ systemInfo.nodeVersion }}</a-descriptions-item>
                        <a-descriptions-item label="V8 版本">{{ systemInfo.v8Version }}</a-descriptions-item>
                        <a-descriptions-item label="运行时间">{{ systemInfo.uptime }}</a-descriptions-item>
                        <a-descriptions-item label="进程 ID">{{ systemInfo.pid }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 数据库 -->
                <div class="info-section">
                    <h3 class="section-title">数据库</h3>
                    <a-descriptions bordered :column="3">
                        <a-descriptions-item label="数据库类型">{{ systemInfo.database.type }}</a-descriptions-item>
                        <a-descriptions-item label="数据库版本">{{ systemInfo.database.version }}</a-descriptions-item>
                        <a-descriptions-item label="数据库名称">{{ systemInfo.database.name }}</a-descriptions-item>
                        <a-descriptions-item label="连接状态">
                            <a-tag :color="systemInfo.database.connected ? 'green' : 'red'">
                                {{ systemInfo.database.connected ? '正常' : '断开' }}
                            </a-tag>
                        </a-descriptions-item>
                        <a-descriptions-item label="连接池">{{ systemInfo.database.pool }}</a-descriptions-item>
                        <a-descriptions-item label="查询总数">{{ systemInfo.database.queries }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 统计信息 -->
                <div class="info-section">
                    <h3 class="section-title">统计信息</h3>
                    <a-row :gutter="16">
                        <a-col :span="6">
                            <a-statistic title="用户数" :value="stats.userCount" />
                        </a-col>
                        <a-col :span="6">
                            <a-statistic title="空间数" :value="stats.workspaceCount" />
                        </a-col>
                        <a-col :span="6">
                            <a-statistic title="页面数" :value="stats.pageCount" />
                        </a-col>
                        <a-col :span="6">
                            <a-statistic title="附件数" :value="stats.attachmentCount" />
                        </a-col>
                    </a-row>
                </div>

                <!-- 资源使用 -->
                <div class="info-section">
                    <h3 class="section-title">资源使用</h3>
                    <div class="resource-item">
                        <div class="resource-label">
                            <span>内存使用</span>
                            <span>{{ formatBytes(systemInfo.memory.used) }} / {{ formatBytes(systemInfo.memory.total) }}</span>
                        </div>
                        <a-progress :percent="memoryPercent" :stroke-color="getMemoryColor(memoryPercent)" />
                    </div>
                    <div class="resource-item">
                        <div class="resource-label">
                            <span>CPU 使用</span>
                            <span>{{ systemInfo.cpu.usage }}%</span>
                        </div>
                        <a-progress :percent="systemInfo.cpu.usage" :stroke-color="getCpuColor(systemInfo.cpu.usage)" />
                    </div>
                    <div class="resource-item">
                        <div class="resource-label">
                            <span>磁盘使用</span>
                            <span>{{ formatBytes(systemInfo.disk.used) }} / {{ formatBytes(systemInfo.disk.total) }}</span>
                        </div>
                        <a-progress :percent="diskPercent" :stroke-color="getDiskColor(diskPercent)" />
                    </div>
                </div>

                <!-- 刷新按钮 -->
                <div class="action-bar">
                    <a-button type="primary" @click="loadSystemInfo" :loading="loading">
                        刷新信息
                    </a-button>
                </div>
            </div>
        </a-spin>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi } from '@/api'
import { formatDateTime } from '@/utils/format'

const loading = ref(false)
// 定时器引用，用于清理
let infoRefreshTimer = null

const systemInfo = ref({
    version: '1.0.0',
    buildTime: '2024-01-01 12:00:00',
    environment: import.meta.env.MODE || 'development',
    gitCommit: 'abc123',
    startTime: new Date().toISOString(),
    hostname: typeof window !== 'undefined' ? window.location.hostname : 'localhost',
    platform: navigator?.platform || 'unknown',
    arch: 'x64',
    nodeVersion: 'v20.11.0',
    v8Version: '11.0.226.13',
    pid: 12345,
    uptime: '0分钟',
    database: {
        type: 'PostgreSQL',
        version: '14.0',
        name: 'confluence_lite',
        connected: true,
        pool: '5/10',
        queries: '1,234'
    },
    memory: {
        used: 512 * 1024 * 1024,
        total: 2 * 1024 * 1024 * 1024,
        free: 1.5 * 1024 * 1024 * 1024
    },
    cpu: {
        usage: 25,
        cores: 8,
        model: 'Intel Core i7'
    },
    disk: {
        used: 50 * 1024 * 1024 * 1024,
        total: 200 * 1024 * 1024 * 1024
    }
})

const stats = ref({
    userCount: 0,
    workspaceCount: 0,
    pageCount: 0,
    attachmentCount: 0
})

const memoryPercent = computed(() => {
    return Math.round((systemInfo.value.memory.used / systemInfo.value.memory.total) * 100)
})

const diskPercent = computed(() => {
    return Math.round((systemInfo.value.disk.used / systemInfo.value.disk.total) * 100)
})

const formatBytes = (bytes) => {
    if (!bytes || bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
}

const formatUptime = (seconds) => {
    if (!seconds) return '0分钟'
    const days = Math.floor(seconds / 86400)
    const hours = Math.floor((seconds % 86400) / 3600)
    const minutes = Math.floor((seconds % 3600) / 60)

    const parts = []
    if (days > 0) parts.push(`${days}天`)
    if (hours > 0) parts.push(`${hours}小时`)
    if (minutes > 0 || parts.length === 0) parts.push(`${minutes}分钟`)

    return parts.join(' ')
}

const getMemoryColor = (percent) => {
    if (percent < 50) return '#52c41a'
    if (percent < 80) return '#faad14'
    return '#f5222d'
}

const getCpuColor = (percent) => {
    if (percent < 50) return '#52c41a'
    if (percent < 80) return '#faad14'
    return '#f5222d'
}

const getDiskColor = (percent) => {
    if (percent < 50) return '#52c41a'
    if (percent < 80) return '#faad14'
    return '#f5222d'
}

const loadSystemInfo = async () => {
    loading.value = true
    try {
        // 调用系统信息接口
        const infoData = await systemSettingApi.getSystemInfo()
        if (infoData) {
            systemInfo.value = {
                ...systemInfo.value,
                version: infoData.version || systemInfo.value.version,
                buildTime: infoData.buildTime ? formatDateTime(infoData.buildTime) : systemInfo.value.buildTime,
                environment: infoData.environment || systemInfo.value.environment,
                startTime: infoData.startTime ? formatDateTime(infoData.startTime) : formatDateTime(new Date()),
                uptime: infoData.uptimeSeconds ? formatUptime(infoData.uptimeSeconds) : '0分钟',
                hostname: infoData.hostname || systemInfo.value.hostname,
                platform: infoData.platform || systemInfo.value.platform,
                arch: infoData.arch || systemInfo.value.arch,
                cpu: {
                    ...systemInfo.value.cpu,
                    cores: infoData.cpu?.cores || systemInfo.value.cpu.cores,
                    usage: infoData.cpu?.usage ?? systemInfo.value.cpu.usage
                },
                memory: {
                    ...systemInfo.value.memory,
                    used: infoData.memory?.used ?? systemInfo.value.memory.used,
                    total: infoData.memory?.total ?? systemInfo.value.memory.total,
                    free: infoData.memory?.free ?? systemInfo.value.memory.free
                },
                database: {
                    ...systemInfo.value.database,
                    type: infoData.database?.type || systemInfo.value.database.type,
                    version: infoData.database?.version || systemInfo.value.database.version,
                    name: infoData.database?.name || systemInfo.value.database.name,
                    connected: infoData.database?.connected ?? systemInfo.value.database.connected
                }
            }
        }
    } catch (error) {
        console.error('Failed to load system info:', error)
    }

    try {
        // 调用统计数据接口
        // Cookie 会自动发送，无需手动添加 Authorization header
        const statsData = await fetch('/api/system/stats')
            .then(res => res.json())
            .then(data => data.success ? data.data : null)

        if (statsData) {
            stats.value = {
                userCount: statsData.userCount || 0,
                workspaceCount: statsData.workspaceCount || 0,
                pageCount: statsData.pageCount || 0,
                attachmentCount: statsData.attachmentCount || 0
            }
        }
    } catch (error) {
        console.error('Failed to load stats:', error)
    }

    loading.value = false
}

onMounted(() => {
    loadSystemInfo()
    // 每30秒自动刷新
    infoRefreshTimer = setInterval(() => {
        if (!loading.value) {
            loadSystemInfo()
        }
    }, 30000)
})

// 组件卸载时清理定时器
onUnmounted(() => {
    if (infoRefreshTimer) {
        clearInterval(infoRefreshTimer)
        infoRefreshTimer = null
    }
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

.info-content {
    padding: 20px 24px 24px;
}

.info-section {
    margin-bottom: 24px;
    padding-bottom: 24px;
    border-bottom: 1px solid #ebecf0;
}

.info-section:last-child {
    border-bottom: none;
}

.section-title {
    font-size: 14px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 16px 0;
}

.resource-item {
    margin-bottom: 20px;
}

.resource-label {
    display: flex;
    justify-content: space-between;
    margin-bottom: 8px;
    font-size: 13px;
    color: #42526e;
}

.action-bar {
    margin-top: 24px;
}
</style>
