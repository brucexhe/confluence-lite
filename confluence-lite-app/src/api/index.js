/**
 * 业务 API 接口
 * 按模块组织所有后端 API 调用
 */
import { request } from './request'

// ========== 站点信息（公开，无需认证） ==========

export const siteInfoApi = {
  /** 获取站点信息（含安装状态、站点名称、LOGO等） */
  get() {
    return request('/api/siteinfo', { auth: false })
  }
}

// ========== 安装向导 ==========

export const setupApi = {
  /** 获取安装状态 */
  getStatus() {
    return request('/api/setup/status', { auth: false })
  },

  /** 测试数据库连接 */
  testConnection(config) {
    return request('/api/setup/test-connection', {
      method: 'POST',
      body: config,
      auth: false
    })
  },

  /** 执行安装 */
  install(payload) {
    return request('/api/setup/install', {
      method: 'POST',
      body: payload,
      auth: false
    })
  }
}

// ========== 用户 ==========

export const userApi = {
  /** 登录 */
  login(username, password) {
    return request('/api/user/login', {
      method: 'POST',
      body: { username, password },
      auth: false
    })
  },

  /** 登出 */
  logout() {
    return request('/api/user/logout', { method: 'POST' })
  },

  /** 注册 */
  register(data) {
    return request('/api/user/register', {
      method: 'POST',
      body: data,
      auth: false
    })
  },

  /** 获取当前用户 */
  getMe() {
    return request('/api/user/me')
  },

  /** 获取用户信息 */
  getById(id) {
    return request(`/api/user/${id}`)
  },

  /** 获取用户列表 */
  getList(page = 1, pageSize = 20) {
    return request(`/api/user/list?page=${page}&pageSize=${pageSize}`)
  },

  /** 更新用户 */
  update(id, data) {
    return request(`/api/user/${id}`, { method: 'PUT', body: data })
  },

  /** 修改密码 */
  changePassword(data) {
    return request('/api/user/change-password', { method: 'POST', body: data })
  },

  /** 删除用户 */
  remove(id) {
    return request(`/api/user/${id}`, { method: 'DELETE' })
  }
}

// ========== 工作空间 ==========

export const workspaceApi = {
  /** 创建工作空间 */
  create(data) {
    return request('/api/workspace/', { method: 'POST', body: data })
  },

  /** 获取工作空间详情 */
  getById(id) {
    return request(`/api/workspace/${id}`)
  },

  /** 根据 Key 获取 */
  getByKey(key) {
    return request(`/api/workspace/key/${key}`)
  },

  /** 获取列表 */
  getList(page = 1, pageSize = 20) {
    return request(`/api/workspace/list?page=${page}&pageSize=${pageSize}`)
  },

  /** 获取我的工作空间 */
  getMy() {
    return request('/api/workspace/my')
  },

  /** 更新 */
  update(id, data) {
    return request(`/api/workspace/${id}`, { method: 'PUT', body: data })
  },

  /** 删除 */
  remove(id) {
    return request(`/api/workspace/${id}`, { method: 'DELETE' })
  }
}

// ========== 页面 ==========

export const pageApi = {
  /** 创建页面 */
  create(data) {
    return request('/api/page/', { method: 'POST', body: data })
  },

  /** 获取页面详情 */
  getById(id) {
    return request(`/api/page/${id}`)
  },

  /** 获取空间页面列表 */
  getListByWorkspace(workspaceId, page = 1, pageSize = 20) {
    return request(`/api/page/workspace/${workspaceId}?page=${page}&pageSize=${pageSize}`)
  },

  /** 获取页面树 */
  getTree(workspaceId) {
    return request(`/api/page/workspace/${workspaceId}/tree`)
  },

  /** 获取子页面 */
  getChildren(parentId) {
    return request(`/api/page/${parentId}/children`)
  },

  /** 更新 */
  update(id, data) {
    return request(`/api/page/${id}`, { method: 'PUT', body: data })
  },

  /** 删除 */
  remove(id) {
    return request(`/api/page/${id}`, { method: 'DELETE' })
  },

  /** 获取页面版本列表 */
  getVersions(pageId) {
    return request(`/api/page/${pageId}/versions`)
  },

  /** 获取单个版本详情 */
  getVersion(versionId) {
    return request(`/api/page/versions/${versionId}`)
  },

  /** 删除版本 */
  deleteVersion(versionId) {
    return request(`/api/page/versions/${versionId}`, { method: 'DELETE' })
  }
}

// ========== 评论 ==========

export const commentApi = {
  /** 获取页面评论 */
  getList(pageId) {
    return request(`/api/page/${pageId}/comments`)
  },

  /** 创建评论 */
  create(pageId, data) {
    return request(`/api/page/${pageId}/comments`, { method: 'POST', body: data })
  },

  /** 更新评论 */
  update(id, data) {
    return request(`/api/page/comments/${id}`, { method: 'PUT', body: data })
  },

  /** 删除评论 */
  remove(id) {
    return request(`/api/page/comments/${id}`, { method: 'DELETE' })
  }
}

// ========== 附件 ==========

export const attachmentApi = {
  /** 上传附件 */
  upload(pageId, file, comment) {
    const formData = new FormData()
    formData.append('file', file)
    if (comment) formData.append('comment', comment)
    return request(`/api/attachment/upload?pageId=${pageId}`, {
      method: 'POST',
      body: formData,
      isFormData: true
    })
  },

  /** 获取页面附件列表 */
  getListByPage(pageId) {
    return request(`/api/attachment/page/${pageId}`)
  },

  /** 获取附件详情 */
  getById(id) {
    return request(`/api/attachment/${id}`)
  },

  /** 删除附件 */
  remove(id) {
    return request(`/api/attachment/${id}`, { method: 'DELETE' })
  }
}

// ========== 通用上传 ==========

export const uploadApi = {
  /** 通用文件上传（用于空间 icon 等） */
  upload(file) {
    const formData = new FormData()
    formData.append('file', file)
    return request('/api/upload', {
      method: 'POST',
      body: formData,
      isFormData: true
    })
  }
}

// ========== 活动记录 ==========

export const activityApi = {
  /** 获取最近活动 */
  getRecent(params = {}) {
    const queryParams = new URLSearchParams()
    if (params.workspaceId) queryParams.append('workspaceId', params.workspaceId)
    if (params.type) queryParams.append('type', params.type)
    queryParams.append('count', params.count || 20)
    queryParams.append('offset', params.offset || 0)

    return request(`/api/activity/recent?${queryParams.toString()}`)
  }
}

// ========== 用户组 ==========

export const userGroupApi = {
  /** 获取用户组列表 */
  getList(page = 1, pageSize = 20, search = '') {
    const params = new URLSearchParams({ page, pageSize })
    if (search) params.append('search', search)
    return request(`/api/user-groups?${params.toString()}`)
  },

  /** 获取用户组详情 */
  getById(id) {
    return request(`/api/user-groups/${id}`)
  },

  /** 创建用户组 */
  create(data) {
    return request('/api/user-groups/', { method: 'POST', body: data })
  },

  /** 更新用户组 */
  update(id, data) {
    return request(`/api/user-groups/${id}`, { method: 'PUT', body: data })
  },

  /** 删除用户组 */
  remove(id) {
    return request(`/api/user-groups/${id}`, { method: 'DELETE' })
  },

  /** 获取用户组成员列表 */
  getMembers(id) {
    return request(`/api/user-groups/${id}/members`)
  },

  /** 添加用户组成员 */
  addMembers(id, userIds) {
    return request(`/api/user-groups/${id}/members`, {
      method: 'POST',
      body: { userIds }
    })
  },

  /** 移除用户组成员 */
  removeMember(id, userId) {
    return request(`/api/user-groups/${id}/members/${userId}`, { method: 'DELETE' })
  }
}

// ========== 系统设置 ==========

export const systemSettingApi = {
  /** 获取站点配置 */
  getSiteConfig() {
    return request('/api/system/site-config')
  },

  /** 更新站点配置 */
  updateSiteConfig(data) {
    return request('/api/system/site-config', { method: 'PUT', body: data })
  },

  /** 获取安全设置 */
  getSecurityConfig() {
    return request('/api/system/security-config')
  },

  /** 更新安全设置 */
  updateSecurityConfig(data) {
    return request('/api/system/security-config', { method: 'PUT', body: data })
  },

  /** 获取显示设置 */
  getDisplayConfig() {
    return request('/api/system/display-config')
  },

  /** 更新显示设置 */
  updateDisplayConfig(data) {
    return request('/api/system/display-config', { method: 'PUT', body: data })
  },

  /** 获取邮件设置 */
  getMailConfig() {
    return request('/api/system/mail-config')
  },

  /** 更新邮件设置 */
  updateMailConfig(data) {
    return request('/api/system/mail-config', { method: 'PUT', body: data })
  },

  /** 测试邮件配置 */
  testMail(config) {
    return request('/api/system/mail-config/test', { method: 'POST', body: config })
  },

  /** 获取身份验证配置 */
  getAuthConfig() {
    return request('/api/system/auth-config')
  },

  /** 更新身份验证配置 */
  updateAuthConfig(data) {
    return request('/api/system/auth-config', { method: 'PUT', body: data })
  },

  /** 测试身份验证配置 */
  testAuthConfig(config) {
    return request('/api/system/auth-config/test', { method: 'POST', body: config })
  },

  /** 获取系统信息 */
  getSystemInfo() {
    return request('/api/system/info')
  },

  /** 获取日志列表 */
  getLogs(params) {
    return request('/api/system/logs', { params })
  },

  /** 导出日志 */
  exportLogs(params) {
    return request('/api/system/logs/export', { params })
  },

  /** 获取缓存统计 */
  getCacheStats() {
    return request('/api/system/cache/stats')
  },

  /** 清空所有缓存 */
  clearAllCache() {
    return request('/api/system/cache/clear-all', { method: 'POST' })
  },

  /** 清空指定类型缓存 */
  clearCache(type) {
    return request(`/api/system/cache/${type}/clear`, { method: 'POST' })
  },

  /** 获取缓存键列表 */
  getCacheKeys(type) {
    return request(`/api/system/cache/${type}/keys`)
  },

  /** 删除缓存键 */
  deleteCacheKey(type, key) {
    return request(`/api/system/cache/${type}/keys/${key}`, { method: 'DELETE' })
  },

  /** 创建备份 */
  createBackup(data) {
    return request('/api/system/backup', { method: 'POST', body: data })
  },

  /** 获取备份列表 */
  getBackups() {
    return request('/api/system/backup/list')
  },

  /** 删除备份 */
  deleteBackup(id) {
    return request(`/api/system/backup/${id}`, { method: 'DELETE' })
  },

  /** 恢复备份 */
  restoreBackup(id, data) {
    return request(`/api/system/backup/${id}/restore`, { method: 'POST', body: data })
  },

  /** 从 Confluence 导入 - 上传备份文件并开始导入 */
  importFromConfluence(file, options) {
    const formData = new FormData()
    formData.append('file', file)
    formData.append('importUsers', options.importUsers)
    formData.append('importSpaces', options.importSpaces)
    formData.append('ImportPages', options.importPages)
    formData.append('ImportAttachments', options.importAttachments)
    formData.append('ImportComments', options.importComments)
    formData.append('overwriteExisting', options.overwriteExisting)

    return request('/api/system/backup/import-confluence', {
      method: 'POST',
      body: formData,
      headers: {
        'Content-Type': 'multipart/form-data'
      }
    })
  },

  /** 获取 Confluence 导入任务状态 */
  getConfluenceImportStatus(id) {
    return request(`/api/system/backup/import-status/${id}`)
  },

  /** 获取 Confluence 导入任务列表 */
  getConfluenceImportList(page = 1, pageSize = 20) {
    return request(`/api/system/backup/import-list?page=${page}&pageSize=${pageSize}`)
  },

  /** 删除 Confluence 导入任务 */
  deleteConfluenceImportTask(id) {
    return request(`/api/system/backup/import/${id}`, { method: 'DELETE' })
  },

  /** 删除缓存键 */
  deleteCacheKey(type, key) {
    return request(`/api/system/cache/${type}/keys/${key}`, { method: 'DELETE' })
  },

  /** 获取定时任务列表 */
  getJobs() {
    return request('/api/system/jobs')
  },

  /** 获取任务详情 */
  getJob(id) {
    return request(`/api/system/jobs/${id}`)
  },

  /** 执行任务 */
  runJob(id) {
    return request(`/api/system/jobs/${id}/run`, { method: 'POST' })
  },

  /** 暂停任务 */
  pauseJob(id) {
    return request(`/api/system/jobs/${id}/pause`, { method: 'POST' })
  },

  /** 恢复任务 */
  resumeJob(id) {
    return request(`/api/system/jobs/${id}/resume`, { method: 'POST' })
  },

  /** 获取任务执行日志 */
  getJobLogs(id, page = 1, pageSize = 20) {
    return request(`/api/system/jobs/${id}/logs?page=${page}&pageSize=${pageSize}`)
  },

  /** 获取 Office 预览配置 */
  getOfficePreviewConfig() {
    return request('/api/system/office-preview-config')
  },

  /** 更新 Office 预览配置 */
  updateOfficePreviewConfig(data) {
    return request('/api/system/office-preview-config', { method: 'PUT', body: data })
  },

  /** 测试 Office 预览连接 */
  testOfficePreviewConfig(data) {
    return request('/api/system/office-preview-config/test', { method: 'POST', body: data })
  }
}
