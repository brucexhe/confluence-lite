<template>
  <div class="settings-page">
    <div class="page-header">
      <h1>{{ $t('settings.confluenceImportDetail.title') }}</h1>
      <p class="page-description">{{ $t('settings.confluenceImportDetail.description') }}</p>
    </div>

    <div class="page-content">
      <!-- 数据类型选择 -->
      <div class="section">
        <h3 class="section-title">{{ $t('settings.confluenceImportDetail.dataTypes') }}</h3>
        <a-space direction="vertical" style="width: 100%">
          <div class="type-selector">
            <a-button type="link" size="small" @click="selectAllTypes">{{ $t('settings.confluenceImportDetail.selectAll') }}</a-button>
            <a-button type="link" size="small" @click="clearAllTypes">{{ $t('settings.confluenceImportDetail.clearAll') }}</a-button>
          </div>
          <a-checkbox-group v-model:value="importOptions.types">
            <a-row :gutter="[16, 8]">
              <a-col :span="12">
                <a-checkbox value="spaces">{{ $t('settings.confluenceImportDetail.spaceData') }}</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="pages">{{ $t('settings.confluenceImportDetail.pageData') }}</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="attachments">{{ $t('settings.confluenceImportDetail.attachmentData') }}</a-checkbox>
              </a-col>
              <a-col :span="12">
                <a-checkbox value="comments">{{ $t('settings.confluenceImportDetail.commentData') }}</a-checkbox>
              </a-col>
            </a-row>
          </a-checkbox-group>
        </a-space>
      </div>

      <!-- 文件上传 -->
      <div class="section">
        <h3 class="section-title">{{ $t('settings.confluenceImportDetail.backupFile') }}</h3>
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
          <p class="ant-upload-text">{{ $t('settings.confluenceImportDetail.uploadHint') }}</p>
          <p class="ant-upload-hint">{{ $t('settings.confluenceImportDetail.uploadFormatHint') }}</p>
        </a-upload-dragger>
      </div>

      <!-- 高级选项 -->
      <div class="section">
        <h3 class="section-title">{{ $t('settings.confluenceImportDetail.advancedOptions') }}</h3>
        <a-checkbox v-model:checked="importOptions.overwriteExisting">
          {{ $t('settings.confluenceImportDetail.overwriteExisting') }}
        </a-checkbox>
        <div v-if="importOptions.overwriteExisting" class="warning-text">
          <AlertCircle :size="14" />
          {{ $t('settings.confluenceImportDetail.overwriteWarning') }}
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
              {{ $t('settings.confluenceImportDetail.uploading') }} {{ uploadProgress }}%
            </template>
            <template v-else>
              {{ $t('settings.confluenceImportDetail.startImport') }}
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
        <h3 class="section-title">{{ $t('settings.confluenceImportDetail.currentImport') }}</h3>
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
                <div class="stat-label">{{ $t('settings.confluenceImportDetail.total') }}</div>
              </div>
            </a-col>
            <a-col :span="8">
              <div class="stat-item">
                <div class="stat-value stat-success">{{ currentImportTask.progress?.processedItems || 0 }}</div>
                <div class="stat-label">{{ $t('settings.confluenceImportDetail.processed') }}</div>
              </div>
            </a-col>
            <a-col :span="8">
              <div class="stat-item">
                <div class="stat-value stat-error">{{ currentImportTask.progress?.failedItems || 0 }}</div>
                <div class="stat-label">{{ $t('settings.confluenceImportDetail.failed') }}</div>
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
        <h3 class="section-title">{{ $t('settings.confluenceImportDetail.importHistory') }}</h3>
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
                <a-button type="link" size="small" @click="viewDetails(record)">{{ $t('settings.confluenceImportDetail.details') }}</a-button>
                <a-popconfirm
                  v-if="record.status === 'completed' || record.status === 'failed'"
                  :title="$t('settings.confluenceImportDetail.confirmDeleteTask')"
                  @confirm="deleteTask(record.id)"
                >
                  <a-button type="link" size="small" danger>{{ $t('common.delete') }}</a-button>
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
      :title="$t('settings.confluenceImportDetail.importDetails')"
      width="700px"
      :footer="null"
    >
      <div v-if="selectedTask" class="detail-content">
        <a-descriptions :column="2" bordered size="small">
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.taskName')" :span="2">
            {{ selectedTask.name }}
          </a-descriptions-item>
          <a-descriptions-item :label="$t('common.status')">
            <a-tag :color="getStatusColor(selectedTask.status)">
              {{ getStatusText(selectedTask.status) }}
            </a-tag>
          </a-descriptions-item>
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.createdAt')">
            {{ formatTime(selectedTask.createdAt) }}
          </a-descriptions-item>
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.completedAt')">
            {{ selectedTask.completedAt ? formatTime(selectedTask.completedAt) : '-' }}
          </a-descriptions-item>
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.total')" :span="2">
            {{ selectedTask.progress?.totalItems || 0 }}
          </a-descriptions-item>
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.processed')" :span="2">
            {{ selectedTask.progress?.processedItems || 0 }}
          </a-descriptions-item>
          <a-descriptions-item :label="$t('settings.confluenceImportDetail.failed')" :span="2">
            {{ selectedTask.progress?.failedItems || 0 }}
          </a-descriptions-item>
          <template v-if="selectedTask.errorMessage">
            <a-descriptions-item :label="$t('settings.confluenceImportDetail.errorMessage')" :span="2">
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
import { useI18n } from 'vue-i18n'
import { systemSettingApi } from '@/api'

const { t, locale } = useI18n()

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

const historyColumns = computed(() => [
  { title: t('settings.confluenceImportDetail.taskName'), dataIndex: 'name', key: 'name' },
  { title: t('settings.confluenceImportDetail.createdAt'), dataIndex: 'createdAt', key: 'createdAt', customRender: ({ record }) => formatTime(record.createdAt) },
  { title: t('common.status'), key: 'status', width: 100 },
  { title: t('settings.confluenceImportDetail.progress'), key: 'progress', width: 150 },
  { title: t('common.actions'), key: 'actions', width: 120, align: 'right' }
])

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
  return currentImportTask.value?.progress?.currentStep || t('settings.confluenceImportDetail.preparing')
})

const handleBeforeUpload = (file) => {
  const isZip = file.name.endsWith('.zip')
  if (!isZip) {
    message.error(t('settings.confluenceImportDetail.zipOnly'))
    return false
  }

  const isLt1G = file.size / 1024 / 1024 / 1024 < 1
  if (!isLt1G) {
    message.error(t('settings.confluenceImportDetail.fileTooLarge'))
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
    message.warning(t('settings.confluenceImportDetail.selectFileFirst'))
    return
  }

  if (importOptions.value.types.length === 0) {
    message.warning(t('settings.confluenceImportDetail.selectAtLeastOneType'))
    return
  }

  if (importOptions.value.overwriteExisting) {
    const confirmed = await new Promise((resolve) => {
      Modal.confirm({
        title: t('settings.confluenceImportDetail.confirmOverwriteTitle'),
        content: t('settings.confluenceImportDetail.confirmOverwriteContent'),
        okText: t('settings.confluenceImportDetail.confirmOverwrite'),
        okType: 'danger',
        okButtonProps: { danger: true },
        cancelText: t('common.cancel'),
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
      message.success(t('settings.confluenceImportDetail.taskCreated'))
      currentImportTask.value = response
      importing.value = true
      startPolling(response.id)
      await loadImportTasks()
    }
  } catch (error) {
    console.error('导入失败:', error)
    message.error(t('settings.confluenceImportDetail.importFailed') + ': ' + (error.message || t('settings.confluenceImportDetail.unknownError')))
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
            message.success(t('settings.confluenceImportDetail.importSuccess'))
          } else {
            message.error(t('settings.confluenceImportDetail.importFailed') + ': ' + (response.errorMessage || t('settings.confluenceImportDetail.unknownError')))
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
    message.success(t('common.deleteSuccess'))
    await loadImportTasks()
  } catch (error) {
    console.error('删除任务失败:', error)
    message.error(t('common.deleteFailed'))
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

  if (diffMins < 1) return t('common.justNow')
  if (diffMins < 60) return t('common.minutesAgo', { n: diffMins })
  if (diffHours < 24) return t('common.hoursAgo', { n: diffHours })
  if (diffDays < 7) return t('common.daysAgo', { n: diffDays })

  return date.toLocaleDateString(locale.value)
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
    pending: t('settings.confluenceImportDetail.statusPending'),
    processing: t('settings.confluenceImportDetail.statusProcessing'),
    completed: t('settings.confluenceImportDetail.statusCompleted'),
    failed: t('common.failed'),
    cancelled: t('settings.confluenceImportDetail.statusCancelled')
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
