/**
 * 业务 API 接口
 * 按模块组织所有后端 API 调用
 */
import { request } from './request'

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
