<template>
  <div class="settings-page">
    <div class="page-header">
      <h1>从 Confluence 导入</h1>
      <p class="page-description">从 Confluence 7.x 备份文件导入数据到系统</p>
    </div>

    <div class="page-content">
      <!-- 数据类型选择 -->
      <div class="section">
        <h3 class="section-title">数据类型</h3>
        <a-space direction="vertical" style="width: 100%">
          <div class="type-selector">
            <a-button type="link" size="small" @click="selectAllTypes">全选</a-button>
            <a-button type="link" size="small" @click="clearAllTypes">清空</a-button>
          </div>
          <a-checkbox-group v-model:value="importOptions.types">
            <a-row :gutter="[16, 8]">
              <a-col :span="12">
                <a-checkbox value="spaces">空间数据</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="pages">页面数据</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="attachments">附件数据</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="comments">评论数据</a-checkbox>
              </a-col>
            </a-row>
          </a-checkbox-group>
        </a-space>
      </div>

      <!-- 文件上传 -->
      <div class="section">
        <h3 class="section-title">备份文件</h3>
        <a-upload-dragger
          :before-upload="handleBeforeUpload"
          :file-list="uploadFileList"
          @remove="handleRemoveFile"
          accept=".zip"
          :max-count="1"
          :disabled="uploading || importing"
        >
          <p class="ant-upload-drag-icon">
            <Upload :size="48" />
          </p>
          <p class="ant-upload-text">点击或拖拽文件到此处上传</p>
          <p class="ant-upload-hint">支持 .zip 格式，最大 1GB</p>
        </a-upload-dragger>
      </div>

      <!-- 高级选项 -->
      <div class="section">
        <h3 class="section-title">高级选项</h3>
        <a-checkbox v-model:checked="importOptions.overwriteExisting">
          覆盖现有数据
        </a-checkbox>
        <div v-if="importOptions.overwriteExisting" class="warning-text">
          <AlertCircle :size="14" />
          警告：启用后将覆盖已存在的数据，此操作不可撤销
        </div>
      </div>

      <!-- 导入按钮 -->
      <div class="section">
        <a-space direction="vertical" style="width: 100%">
          <a-button
            type="primary"
            :loading="uploading"
            :disabled="uploadFileList.length === 0 || importOptions.types.length === 0"
            @click="startImport"
            :style="{ width: '100%' }"
          >
            <template v-if="uploading">
              正在上传... {{ uploadProgress }}%
            </template>
            <template v-else>
              开始导入
            </template>
          </a-button>
          <a-progress
            v-if="uploading"
            :percent="uploadProgress"
            :stroke-color="{ '0%': '#108ee9', '100%': '#87d068' }"
          />
        </a-space>
      </div>

      <!-- 当前导入进度 -->
      <div v-if="currentImportTask" class="section">
        <h3 class="section-title">当前导入</h3>
        <a-space direction="vertical" style="width: 100%" size="large">
          <div>
            <a-progress
              :percent="importProgress"
              :status="importStatus"
              :stroke-color="{ '0%': '#108ee9', '100%': '#87d068' }"
            />
            <div class="step-text">
              <Loader2 v-if="importing" :size="14" class="spin-icon" />
              {{ importStepText }}
            </div>
          </div>

          <a-row :gutter="16">
            <a-col :span="8">
              <div class="stat-item">
                <div class="stat-value">{{ currentImportTask.progress?.totalItems || 0 }}</div>
                <div class="stat-label">总数</div>
              </div>
            </a-col>
            <a-col :span="8">
              <div class="stat-item">
                <div class="stat-value stat-success">{{ currentImportTask.progress?.processedItems || 0 }}</div>
                <div class="stat-label">已处理</div>
              </div>
            </a-col>
            <a-col :span="8">
              <div class="stat-item">
                <div class="stat-value stat-error">{{ currentImportTask.progress?.failedItems || 0 }}</div>
                <div class="stat-label">失败</div>
              </div>
            </a-col>
          </a-row>

          <a-alert
            v-if="currentImportTask.errorMessage"
            type="error"
            :message="currentImportTask.errorMessage"
            show-icon
            closable
          />
        </a-space>
      </div>

      <!-- 导入历史 -->
      <div class="section">
        <h3 class="section-title">导入历史</h3>
        <a-table
          :columns="historyColumns"
          :data-source="importTasks"
          :pagination="{ pageSize: 10 }"
          :loading="loadingTasks"
          row-key="id"
          size="small"
        >
          <template #bodyCell="{ column, record }">
            <template v-if="column.key === 'status'">
              <a-tag :color="getStatusColor(record.status)">
                {{ getStatusText(record.status) }}
              </a-tag>
            </template>
            <template v-else-if="column.key === 'progress'">
              <a-progress
                v-if="record.progress"
                :percent="record.progress.progressPercent || 0"
                :status="getProgressStatus(record.status)"
                size="small"
                :stroke-width="4"
              />
            </template>
            <template v-else-if="column.key === 'actions'">
              <a-space size="small">
                <a-button type="link" size="small" @click="viewDetails(record)">详情</a-button>
                <a-popconfirm
                  v-if="record.status === 'completed' || record.status === 'failed'"
                  title="确定要删除此导入任务吗？"
                  @confirm="deleteTask(record.id)"
                >
                  <a-button type="link" size="small" danger>删除</a-button>
                </a-popconfirm>
              </a-space>
            </template>
          </template>
        </a-table>
      </div>
    </div>

    <!-- 详情模态框 -->
    <a-modal
      v-model:open="detailModalVisible"
      title="导入详情"
      width="700px"
      :footer="null"
    >
      <div v-if="selectedTask" class="detail-content">
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item label="任务名称" :span="2">
            {{ selectedTask.name }}
          </a-descriptions-item>
          <a-descriptions-item label="状态">
            <a-tag :color="getStatusColor(selectedTask.status)">
              {{ getStatusText(selectedTask.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item label="创建时间">
            {{ formatTime(selectedTask.createdAt) }}
          </a-descriptions-item>
          <a-descriptions-item label="完成时间">
            {{ selectedTask.completedAt ? formatTime(selectedTask.completedAt) : '-' }}
          </a-descriptions-item>
          <a-descriptions-item label="总数" :span="2">
            {{ selectedTask.progress?.totalItems || 0 }}
          </a-descriptions-item>
          <a-descriptions-item label="已处理" :span="2">
            {{ selectedTask.progress?.processedItems || 0 }}
          </a-descriptions-item>
          <a-descriptions-item label="失败" :span="2">
            {{ selectedTask.progress?.failedItems || 0 }}
          </a-descriptions-item>
          <template v-if="selectedTask.errorMessage">
            <a-descriptions-item label="错误信息" :span="2">
              <a-typography-text type="danger">
                {{ selectedTask.errorMessage }}
              </a-typography-text>
            </a-descriptions-item>
          </template>
        </a-descriptions>
      </div>
    </a-modal>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { Upload, AlertCircle, Loader2 } from 'lucide-vue-next'
import { message, Modal } from 'ant-design-vue'
import { systemSettingApi } from '@/api'

const importOptions = ref({
  types: ['spaces', 'pages', 'attachments', 'comments'],
  overwriteExisting: false
})

const uploadFileList = ref([])
const uploading = ref(false)
const uploadProgress = ref(0)
const importing = ref(false)
const currentImportTask = ref(null)
const importTasks = ref([])
const loadingTasks = ref(false)
const detailModalVisible = ref(false)
const selectedTask = ref(null)

const historyColumns = [
  { title: '任务名称', dataIndex: 'name', key: 'name' },
  { title: '创建时间', dataIndex: 'createdAt', key: 'createdAt', customRender: ({ record }) => formatTime(record.createdAt) },
  { title: '状态', key: 'status', width: 100 },
  { title: '进度', key: 'progress', width: 150 },
  { title: '操作', key: 'actions', width: 120, align: 'right' }
]

const selectAllTypes = () => {
  importOptions.value.types = ['spaces', 'pages', 'attachments', 'comments']
}

const clearAllTypes = () => {
  importOptions.value.types = []
}

const importProgress = computed(() => {
  return currentImportTask.value?.progress?.progressPercent || 0
})

const importStatus = computed(() => {
  const status = currentImportTask.value?.status
  if (status === 'completed') return 'success'
  if (status === 'failed') return 'exception'
  return 'active'
})

const importStepText = computed(() => {
  return currentImportTask.value?.progress?.currentStep || '准备中...'
})

const handleBeforeUpload = (file) => {
  const isZip = file.name.endsWith('.zip')
  if (!isZip) {
    message.error('只能上传 .zip 格式的备份文件')
    return false
  }

  const isLt1G = file.size / 1024 / 1024 / 1024 < 1
  if (!isLt1G) {
    message.error('备份文件大小不能超过 1GB')
    return false
  }

  // 手动将文件添加到文件列表中
  uploadFileList.value = [{
    uid: file.uid || String(Date.now()),
    name: file.name,
    status: 'done',
    originFileObj: file
  }]

  // 返回 false 阻止自动上传
  return false
}

const handleRemoveFile = () => {
  uploadFileList.value = []
}

const startImport = async () => {
  if (uploadFileList.value.length === 0) {
    message.warning('请先选择备份文件')
    return
  }

  if (importOptions.value.types.length === 0) {
    message.warning('请至少选择一种数据类型')
    return
  }

  if (importOptions.value.overwriteExisting) {
    const confirmed = await new Promise((resolve) => {
      Modal.confirm({
        title: '确认覆盖现有数据',
        content: '启用覆盖后，已存在的空间、页面和用户数据将被覆盖，此操作不可撤销。确定要继续吗？',
        okText: '确定覆盖',
        okType: 'danger',
        okButtonProps: { danger: true },
        cancelText: '取消',
        onOk: () => resolve(true),
        onCancel: () => resolve(false)
      })
    })

    if (!confirmed) return
  }

  uploading.value = true
  uploadProgress.value = 0

  try {
    const file = uploadFileList.value[0].originFileObj

    const options = {
      importSpaces: importOptions.value.types.includes('spaces'),
      importPages: importOptions.value.types.includes('pages'),
      importAttachments: importOptions.value.types.includes('attachments'),
      importComments: importOptions.value.types.includes('comments'),
      overwriteExisting: importOptions.value.overwriteExisting
    }

    const response = await systemSettingApi.importFromConfluence(file, options, (progress) => {
      uploadProgress.value = progress
    })

    if (response) {
      message.success('导入任务已创建')
      currentImportTask.value = response
      importing.value = true
      startPolling(response.id)
      await loadImportTasks()
    }
  } catch (error) {
    console.error('导入失败:', error)
    message.error('导入失败: ' + (error.message || '未知错误'))
  } finally {
    uploading.value = false
    uploadProgress.value = 0
  }
}

let pollingInterval = null

const startPolling = (taskId) => {
  stopPolling()

  pollingInterval = setInterval(async () => {
    try {
      const response = await systemSettingApi.getConfluenceImportStatus(taskId)

      if (response) {
        currentImportTask.value = response

        if (response.status === 'completed' || response.status === 'failed') {
          stopPolling()
          await loadImportTasks()

          if (response.status === 'completed') {
            message.success('导入完成！')
          } else {
            message.error('导入失败: ' + (response.errorMessage || '未知错误'))
          }
        }
      }
    } catch (error) {
      console.error('获取导入状态失败:', error)
    }
  }, 2000)
}

const stopPolling = () => {
  if (pollingInterval) {
    clearInterval(pollingInterval)
    pollingInterval = null
  }
}

const loadImportTasks = async () => {
  loadingTasks.value = true

  try {
    const response = await systemSettingApi.getConfluenceImportList(1, 50)

    if (response) {
      importTasks.value = response.items || []
    }
  } catch (error) {
    console.error('加载导入任务失败:', error)
  } finally {
    loadingTasks.value = false
  }
}

const viewDetails = (task) => {
  selectedTask.value = task
  detailModalVisible.value = true
}

const deleteTask = async (taskId) => {
  try {
    await systemSettingApi.deleteConfluenceImportTask(taskId)
    message.success('删除成功')
    await loadImportTasks()
  } catch (error) {
    console.error('删除任务失败:', error)
    message.error('删除失败')
  }
}

const formatTime = (dateString) => {
  if (!dateString) return '-'
  const date = new Date(dateString)

  const now = new Date()
  const diffMs = now - date
  const diffMins = Math.floor(diffMs / 60000)
  const diffHours = Math.floor(diffMs / 3600000)
  const diffDays = Math.floor(diffMs / 86400000)

  if (diffMins < 1) return '刚刚'
  if (diffMins < 60) return `${diffMins} 分钟前`
  if (diffHours < 24) return `${diffHours} 小时前`
  if (diffDays < 7) return `${diffDays} 天前`

  return date.toLocaleDateString('zh-CN')
}

const getStatusColor = (status) => {
  const colors = {
    pending: 'default',
    processing: 'processing',
    completed: 'success',
    failed: 'error',
    cancelled: 'default'
  }
  return colors[status] || 'default'
}

const getStatusText = (status) => {
  const texts = {
    pending: '等待中',
    processing: '处理中',
    completed: '已完成',
    failed: '失败',
    cancelled: '已取消'
  }
  return texts[status] || status
}

const getProgressStatus = (status) => {
  if (status === 'completed') return 'success'
  if (status === 'failed') return 'exception'
  return 'active'
}

onMounted(() => {
  loadImportTasks()
})

onUnmounted(() => {
  stopPolling()
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

.type-selector {
  display: flex;
  gap: 8px;
  margin-bottom: 8px;
}

.warning-text {
  display: flex;
  align-items: center;
  margin-top: 8px;
  font-size: 12px;
  color: #de350b;
}

.warning-text :deep(.anticon) {
  margin-right: 4px;
}

.step-text {
  display: flex;
  align-items: center;
  margin-top: 12px;
  font-size: 13px;
  color: #6b778c;
}

.step-text :deep(.anticon) {
  margin-right: 6px;
}

.spin-icon {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.stat-item {
  text-align: center;
  padding: 12px;
  background: #f4f5f7;
  border-radius: 4px;
}

.stat-value {
  font-size: 18px;
  font-weight: 600;
  color: #172b4d;
  margin-bottom: 4px;
}

.stat-value.stat-success {
  color: #36b37e;
}

.stat-value.stat-error {
  color: #de350b;
}

.stat-label {
  font-size: 12px;
  color: #6b778c;
}

.detail-content {
  max-height: 60vh;
  overflow-y: auto;
}
</style>
