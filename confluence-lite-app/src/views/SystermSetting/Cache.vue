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
                        <Trash2 :size="14" style="vertical-align: middle" />
                        清空所有缓存
                    </a-button>
                    <a-button @click="refreshStats" :loading="loading">
                        <RotateCw :size="14" style="vertical-align: middle" />
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
                        <X :size="14" style="vertical-align: middle" />
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
                            <span v-if="record.ttl === '-1' || record.ttl === -1">永久</span>
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
    Trash2,
    RotateCw,
    X
} from 'lucide-vue-next'
import { systemSettingApi } from '@/api'

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
        // 加载缓存统计
        const statsData = await systemSettingApi.getCacheStats()
        if (statsData) {
            cacheStats.value = {
                keyCount: statsData.keyCount || 0,
                memoryUsed: statsData.memoryUsed || 0,
                memoryTotal: statsData.memoryTotal || 0,
                hitRate: statsData.hitRate || 0
            }
        }

        // 加载缓存类型列表
        const typesData = await fetch('/api/system/cache/types', {
            headers: {
                'Authorization': `Bearer ${localStorage.getItem('auth_token')}`
            }
        }).then(res => res.json()).then(data => data.success ? data.data : [])

        if (typesData) {
            cacheTypes.value = typesData.map((type, index) => ({
                ...type,
                name: getTypeName(type.key),
                description: getTypeDescription(type.key),
                keyCount: type.count || 0,
                memory: type.memory || 0,
                ttl: getTtlForType(type.key)
            }))
        }
    } catch (error) {
        console.error('Failed to refresh cache stats:', error)
        message.error('刷新统计失败')
    } finally {
        loading.value = false
    }
}

const getTypeName = (key) => {
    const names = {
        user: '用户缓存',
        page: '页面缓存',
        workspace: '空间缓存',
        attachment: '附件缓存',
        search: '搜索缓存',
        default: key
    }
    return names[key] || names.default
}

const getTypeDescription = (key) => {
    const descriptions = {
        user: '用户信息和权限',
        page: '页面内容和渲染结果',
        workspace: '空间配置和页面树',
        attachment: '附件元数据和预览',
        search: '搜索结果和索引',
        default: ''
    }
    return descriptions[key] || descriptions.default
}

const getTtlForType = (key) => {
    const ttls = {
        user: 3600,
        page: 1800,
        workspace: 7200,
        attachment: 86400,
        search: 600,
        default: 3600
    }
    return ttls[key] || ttls.default
}

const clearAllCache = async () => {
    clearing.value = true
    try {
        await systemSettingApi.clearAllCache()
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
        await systemSettingApi.clearCache(type)
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
        const data = await systemSettingApi.getCacheKeys(type)
        if (data) {
            cacheKeys.value = data.map(item => ({
                ...item,
                size: item.size || 0,
                ttl: item.ttl || '-1'
            })) || []
        }
    } catch (error) {
        console.error('Failed to load cache keys:', error)
        cacheKeys.value = []
    } finally {
        loadingKeys.value = false
    }
}

const deleteCacheKey = async (key) => {
    try {
        const type = selectedCacheType.value.key
        await systemSettingApi.deleteCacheKey(type, key)
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
    display: flex;
    align-items: center;
    justify-content: space-between;
}
</style>
