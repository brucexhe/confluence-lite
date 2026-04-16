<template>
  <div class="workspace-home">
    <!-- 欢迎头部 -->
    <div class="welcome-header">
      <h1 class="workspace-title">{{ workspaceName }}</h1>
      <p class="workspace-desc">{{ workspaceDescription }}</p>
    </div>

    <!-- 内容区域 -->
    <div class="content-section">
      <div class="activity-section">
        <a-card class="activity-card" :bordered="false">
          <template #title>
            <span class="card-title">最近活动</span>
          </template>
          <template #extra>
            <a-button type="link" size="small" @click="loadActivities">刷新</a-button>
          </template>

          <a-spin :spinning="loading">
            <a-list
              v-if="activities.length > 0"
              class="activity-list"
              :data-source="activities"
              size="small"
            >
              <template #renderItem="{ item }">
                <a-list-item class="activity-item">
                  <template #avatar>
                    <a-avatar
                      :size="24"
                      :style="{
                        backgroundColor: getUserColor(item.user?.id),
                        fontSize: '12px'
                      }"
                    >
                      {{ getUserInitial(item.user) }}
                    </a-avatar>
                  </template>
                  <template #default>
                    <div class="activity-content">
                      <div class="activity-page">
                        <router-link
                          v-if="item.workspaceKey && item.pageId"
                          :to="`/${item.workspaceKey}/page/${item.pageId}`"
                          class="page-link"
                        >
                          <FileText :size="15" color="#0066ff" :stroke-width="1.5" class="page-icon" />
                          {{ item.pageTitle }}
                        </router-link>
                      </div>
                      <div class="activity-meta">
                        <span class="activity-user">{{ item.user?.displayName || item.user?.username || 'Unknown' }}</span>
                        <span class="activity-separator">•</span>
                        <span class="activity-time">{{ formatTime(item.createdAt) }}</span>
                      </div>
                    </div>
                  </template>
                </a-list-item>
              </template>
            </a-list>
            <a-empty
              v-else-if="!loading"
              description="暂无活动"
              class="empty-state"
            >
              <template #description>
                <span style="color: #6b778c; font-size: 13px;">暂无活动记录</span>
              </template>
            </a-empty>
          </a-spin>
        </a-card>
      </div>

      <!-- 快速操作 -->
      <div class="quick-actions">
        <a-card :bordered="false" class="quick-card">
          <template #title>
            <span class="card-title">快速操作</span>
          </template>
          <div class="action-buttons">
            <a-button block @click="createPage">
              <span style="font-size: 14px;">+ 创建页面</span>
            </a-button>
            <a-button block @click="goToSettings">
              <span style="font-size: 14px;">⚙ 空间设置</span>
            </a-button>
          </div>
        </a-card>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { FileText } from 'lucide-vue-next'
import { activityApi, workspaceApi } from '@/api'

const route = useRoute()
const router = useRouter()

const loading = ref(false)
const activities = ref([])

const workspaceName = ref('')
const workspaceDescription = ref('')

// 用户颜色池
const userColors = [
  '#3b82f6', '#8b5cf6', '#ec4899', '#f59e0b', '#10b981', '#06b6d4',
  '#6366f1', '#14b8a6', '#f97316', '#84cc16'
]

function getUserColor(userId) {
  if (!userId) return '#6b778c'
  return userColors[userId % userColors.length]
}

function getUserInitial(user) {
  if (!user) return '?'
  const name = user.displayName || user.username
  return name?.charAt(0)?.toUpperCase() || '?'
}

function formatTime(dateStr) {
  if (!dateStr) return ''
  const date = new Date(dateStr)
  const now = new Date()
  const diff = now - date

  const minutes = Math.floor(diff / 60000)
  if (minutes < 1) return '刚刚'
  if (minutes < 60) return `${minutes} 分钟前`

  const hours = Math.floor(minutes / 60)
  if (hours < 24) return `${hours} 小时前`

  const days = Math.floor(hours / 24)
  if (days < 7) return `${days} 天前`

  return date.toLocaleDateString('zh-CN')
}

async function loadWorkspaceInfo() {
  try {
    const key = route.params.spaceKey
    if (!key) return

    const data = await workspaceApi.getByKey(key)
    if (data) {
      workspaceName.value = data.name || key
      workspaceDescription.value = data.description || '工作空间首页'
    }
  } catch (error) {
    console.error('加载工作空间信息失败:', error)
  }
}

async function loadActivities() {
  loading.value = true
  try {
    const key = route.params.spaceKey
    const data = await activityApi.getRecent({
      workspaceId: null, // 获取所有工作空间的活动
      count: 20
    })

    // 确保返回的是数组
    if (Array.isArray(data)) {
      activities.value = data
    } else {
      console.warn('活动 API 返回格式不正确:', data)
      activities.value = []
    }
  } catch (error) {
    console.error('加载活动失败:', error)
    activities.value = []
  } finally {
    loading.value = false
  }
}

function createPage() {
  const key = route.params.spaceKey
  router.push(`/${key}/page/new`)
}

function goToSettings() {
  router.push('/settings/workspaces')
}

onMounted(() => {
  loadWorkspaceInfo()
  loadActivities()
})
</script>

<style scoped>
.workspace-home {
  max-width: 1000px; 
  padding: 20px 40px 0;
}

/* 欢迎头部 */
.welcome-header {
  margin-bottom: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid #dfe1e6;
}

.workspace-title {
  font-size: 24px;
  font-weight: 600;
  color: #172b4d;
  margin: 0 0 4px 0;
}

.workspace-desc {
  font-size: 13px;
  color: #6b778c;
  margin: 0;
}

/* 内容区域 */
.content-section {
  display: flex;
  gap: 16px;
}

.activity-section {
  flex: 1;
  min-width: 0;
}

.quick-actions {
  width: 200px;
  flex-shrink: 0;
}

 

/* 卡片样式 */
.activity-card,
.quick-card {
  border-radius: 4px;
  box-shadow: none !important;
}

.activity-card :deep(.ant-card-head),
.quick-card :deep(.ant-card-head) {
  padding: 12px 16px;
  min-height: auto;
  border-bottom: 1px solid #dfe1e6;
}

.activity-card :deep(.ant-card-body),
.quick-card :deep(.ant-card-body) {
  padding: 12px 6px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  color: #172b4d;
}

/* 活动列表 */
.activity-list {
  margin: 0;
}

.activity-list :deep(.ant-list-item) {
  border-bottom: none;
  padding: 4px 0;
}

.activity-list :deep(.ant-list-item:last-child) {
  border-bottom: none;
}

.activity-item {
  display: flex;
  align-items: flex-start;
}

.activity-content {
  flex: 1;
  min-width: 0;
  padding-left: 8px;
}

.activity-page {
  font-size: 14px; 
  line-height: 1.4;
}

.page-link {
  color: #0052cc;
  text-decoration: none;
  font-weight: 400;
  display: flex;
  align-items: center;
  gap: 4px;
}

.page-icon {
  opacity: 0.6;
  flex-shrink: 0;
}

.page-link:hover {
  text-decoration: underline;
  color: #0052cc;
}

.activity-meta {
  font-size: 12px;
  color: #6b778c;
  display: flex;
  align-items: center;
  padding-left: 20px;;
}

.activity-user {
  color: #42526e;
  font-weight: 400;
}

.activity-separator {
  margin: 0 4px;
  color: #dfe1e6;
}

.activity-time {
  color: #6b778c;
}

/* 空状态 */
.empty-state {
  padding: 32px 0;
}

/* 快速操作 */
.action-buttons {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.action-buttons :deep(.ant-btn) {
  height: 36px;
  font-size: 13px;
  text-align: left;
  padding: 0 12px;
  border-radius: 3px;
}

/* 响应式 */
@media (max-width: 768px) {
  .content-section {
    flex-direction: column;
  }

  .quick-actions {
    width: 100%;
  }

  .workspace-home {
    padding: 12px 16px 0;
  }
}
</style>
