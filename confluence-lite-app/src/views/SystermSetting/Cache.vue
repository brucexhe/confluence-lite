<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>缓存管理</h1>
            <p class="page-description">管理系统缓存和性能优化</p>
        </div>

        <div class="page-content">
            <!-- 缓存统计 -->
            <a-row :gutter="16" class="stats-row">
                <a-col :span="8">
                    <a-card>
                        <a-statistic title="缓存键数量" :value="cacheStats.keyCount" />
                    </a-card>
                </a-col>
                <a-col :span="8">
                    <a-card>
                        <a-statistic
                            title="内存使用"
                            :value="formatBytes(cacheStats.memoryUsed)"
                            :suffix="`/ ${formatBytes(cacheStats.memoryTotal)}`"
                        />
                    </a-card>
                </a-col>
                <a-col :span="8">
                    <a-card>
                        <a-statistic title="命中率" :value="cacheStats.hitRate" suffix="%" />
                    </a-card>
                </a-col>
            </a-row>

            <!-- 缓存操作 -->
            <div class="section">
                <h3 class="section-title">缓存操作</h3>
                <a-space>
                    <a-button type="primary" danger @click="clearAllCache" :loading="clearing">
                        <template #icon><DeleteOutlined /></template>
                        清空所有缓存
                    </a-button>
                    <a-button @click="refreshStats" :loading="loading">
                        <template #icon><ReloadOutlined /></template>
                        刷新统计
                    </a-button>
                </a-space>
            </div>

            <!-- 缓存类型 -->
            <div class="section">
                <h3 class="section-title">缓存分类</h3>
                <a-table
                    :columns="cacheTypeColumns"
                    :data-source="cacheTypes"
                    :pagination="false"
                    row-key="key"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'memory'">
                            {{ formatBytes(record.memory) }}
                        </template>
                        <template v-else-if="column.key === 'action'">
                            <a-space>
                                <a-button type="link" size="small" @click="clearCache(record.key)">
                                    清空
                                </a-button>
                                <a-button type="link" size="small" @click="viewCacheKeys(record.key)">
                                    查看键
                                </a-button>
                            </a-space>
                        </template>
                    </template>
                </a-table>
            </div>

            <!-- 缓存键列表 -->
            <div class="section" v-if="selectedCacheType">
                <h3 class="section-title">
                    {{ selectedCacheType.name }} - 缓存键
                    <a-button type="text" size="small" @click="selectedCacheType = null">
                        <template #icon><CloseOutlined /></template>
                    </a-button>
                </h3>
                <a-table
                    :columns="keyColumns"
                    :data-source="cacheKeys"
                    :loading="loadingKeys"
                    :pagination="{ pageSize: 20 }"
                    row-key="key"
                    size="small"
                >
                    <template #bodyCell="{ column, record }">
                        <template v-if="column.key === 'ttl'">
                            <span v-if="record.ttl === -1">永久</span>
                            <span v-else>{{ record.ttl }}秒</span>
                        </template>
                        <template v-else-if="column.key === 'action'">
                            <a-popconfirm
                                title="确定要删除该缓存键吗？"
                                @confirm="deleteCacheKey(record.key)"
                            >
                                <a-button type="link" size="small" danger>删除</a-button>
                            </a-popconfirm>
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
import {
    DeleteOutlined,
    ReloadOutlined,
    CloseOutlined
} from '@ant-design/icons-vue'

const loading = ref(false)
const clearing = ref(false)
const loadingKeys = ref(false)
const selectedCacheType = ref(null)

const cacheStats = ref({
    keyCount: 1250,
    memoryUsed: 104857600,
    memoryTotal: 524288000,
    hitRate: 85.6
})

const cacheTypes = ref([
    {
        key: 'user',
        name: '用户缓存',
        description: '用户信息和权限',
        keyCount: 150,
        memory: 20971520,
        ttl: 3600
    },
    {
        key: 'page',
        name: '页面缓存',
        description: '页面内容和渲染结果',
        keyCount: 500,
        memory: 52428800,
        ttl: 1800
    },
    {
        key: 'workspace',
        name: '空间缓存',
        description: '空间配置和页面树',
        keyCount: 100,
        memory: 10485760,
        ttl: 7200
    },
    {
        key: 'attachment',
        name: '附件缓存',
        description: '附件元数据和预览',
        keyCount: 300,
        memory: 20971520,
        ttl: 86400
    },
    {
        key: 'search',
        name: '搜索缓存',
        description: '搜索结果和索引',
        keyCount: 200,
        memory: 1048576,
        ttl: 600
    }
])

const cacheKeys = ref([])

const cacheTypeColumns = [
    { title: '缓存名称', dataIndex: 'name', key: 'name' },
    { title: '描述', dataIndex: 'description', key: 'description' },
    { title: '键数量', dataIndex: 'keyCount', key: 'keyCount' },
    { title: '内存占用', key: 'memory' },
    { title: 'TTL', dataIndex: 'ttl', key: 'ttl' },
    { title: '操作', key: 'action', width: 150 }
]

const keyColumns = [
    { title: '缓存键', dataIndex: 'key', key: 'key', ellipsis: true },
    { title: '大小', dataIndex: 'size', key: 'size' },
    { title: 'TTL', key: 'ttl' },
    { title: '操作', key: 'action', width: 80 }
]

const formatBytes = (bytes) => {
    if (!bytes) return '0 B'
    const k = 1024
    const sizes = ['B', 'KB', 'MB', 'GB']
    const i = Math.floor(Math.log(bytes) / Math.log(k))
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i]
}

const refreshStats = async () => {
    loading.value = true
    try {
        // TODO: 调用 API 获取缓存统计
        await new Promise(resolve => setTimeout(resolve, 500))
        message.success('缓存统计已刷新')
    } catch (error) {
        message.error('刷新统计失败')
    } finally {
        loading.value = false
    }
}

const clearAllCache = async () => {
    clearing.value = true
    try {
        // TODO: 调用 API 清空所有缓存
        await new Promise(resolve => setTimeout(resolve, 1000))
        message.success('所有缓存已清空')
        refreshStats()
    } catch (error) {
        message.error('清空缓存失败')
    } finally {
        clearing.value = false
    }
}

const clearCache = async (type) => {
    try {
        // TODO: 调用 API 清空指定类型的缓存
        message.success('缓存已清空')
        refreshStats()
    } catch (error) {
        message.error('清空缓存失败')
    }
}

const viewCacheKeys = async (type) => {
    selectedCacheType.value = cacheTypes.value.find(t => t.key === type)
    loadingKeys.value = true
    try {
        // TODO: 调用 API 获取缓存键列表
        await new Promise(resolve => setTimeout(resolve, 500))
        cacheKeys.value = [
            { key: `user:1`, size: 1024, ttl: 3600 },
            { key: `user:2`, size: 1024, ttl: 3600 },
            { key: `user:session:abc123`, size: 512, ttl: 1800 }
        ]
    } catch (error) {
        message.error('加载缓存键失败')
    } finally {
        loadingKeys.value = false
    }
}

const deleteCacheKey = async (key) => {
    try {
        // TODO: 调用 API 删除缓存键
        message.success('缓存键已删除')
        viewCacheKeys(selectedCacheType.value.key)
    } catch (error) {
        message.error('删除缓存键失败')
    }
}

onMounted(() => {
    refreshStats()
})
</script>

<style scoped>
.page-content {
    padding: 16px 24px 24px;
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
    display: flex;
    align-items: center;
    justify-content: space-between;
}
</style>
