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
                        <a-descriptions-item label="环境">{{ systemInfo.environment }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 运行环境 -->
                <div class="info-section">
                    <h3 class="section-title">运行环境</h3>
                    <a-descriptions bordered :column="2">
                        <a-descriptions-item label="操作系统">{{ systemInfo.os }}</a-descriptions-item>
                        <a-descriptions-item label="架构">{{ systemInfo.arch }}</a-descriptions-item>
                        <a-descriptions-item label="Node.js 版本">{{ systemInfo.nodeVersion }}</a-descriptions-item>
                        <a-descriptions-item label="运行时间">{{ systemInfo.uptime }}</a-descriptions-item>
                    </a-descriptions>
                </div>

                <!-- 数据库 -->
                <div class="info-section">
                    <h3 class="section-title">数据库</h3>
                    <a-descriptions bordered :column="2">
                        <a-descriptions-item label="数据库类型">{{ systemInfo.database.type }}</a-descriptions-item>
                        <a-descriptions-item label="数据库版本">{{ systemInfo.database.version }}</a-descriptions-item>
                        <a-descriptions-item label="数据库名称">{{ systemInfo.database.name }}</a-descriptions-item>
                        <a-descriptions-item label="连接状态">
                            <a-tag :color="systemInfo.database.connected ? 'green' : 'red'">
                                {{ systemInfo.database.connected ? '正常' : '断开' }}
                            </a-tag>
                        </a-descriptions-item>
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
import { ref, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'

const loading = ref(false)

const systemInfo = ref({
    version: '1.0.0',
    buildTime: '2024-01-01 12:00:00',
    environment: 'production',
    os: 'Windows 10',
    arch: 'x64',
    nodeVersion: 'v18.17.0',
    uptime: '5天 3小时',
    database: {
        type: 'PostgreSQL',
        version: '14.0',
        name: 'confluence_lite',
        connected: true
    },
    memory: {
        used: 512 * 1024 * 1024,
        total: 2 * 1024 * 1024 * 1024
    },
    cpu: {
        usage: 25
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
    if (bytes === 0) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB', 'TB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
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
        // TODO: 调用 API 获取系统信息
        await new Promise(resolve => setTimeout(resolve, 500))
        stats.value = {
            userCount: 15,
            workspaceCount: 8,
            pageCount: 120,
            attachmentCount: 45
        }
    } catch (error) {
        message.error('加载系统信息失败')
    } finally {
        loading.value = false
    }
}

onMounted(() => {
    loadSystemInfo()
})
</script>

<style scoped>
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
