/**
 * 统一 HTTP 请求封装
 * 对接后端 ApiResponse<T> 格式: { success, data, message, errorCode, timestamp }
 */

class ApiError extends Error {
  constructor(message, code, data) {
    super(message)
    this.code = code
    this.data = data
  }
}

/**
 * 发起 API 请求
 * @param {string} url - 请求路径 (如 '/api/user/login')
 * @param {object} options - 请求配置
 * @param {string} [options.method='GET'] - 请求方法
 * @param {object} [options.body] - 请求体 (自动 JSON 序列化)
 * @param {object} [options.headers] - 自定义请求头
 * @param {boolean} [options.auth=true] - 是否自动附加 Authorization header
 * @returns {Promise<any>} 返回 data 字段的内容
 */
async function request(url, options = {}) {
  const {
    method = 'GET',
    body,
    headers: customHeaders = {},
    auth = true
  } = options

  const headers = {
    'Content-Type': 'application/json',
    ...customHeaders
  }

  if (auth) {
    const token = localStorage.getItem('auth_token')
    if (token) {
      headers['Authorization'] = `Bearer ${token}`
    }
  }

  const config = { method, headers }
  if (body !== undefined) {
    config.body = JSON.stringify(body)
  }

  const res = await fetch(url, config)

  // 尝试解析 JSON
  let json
  try {
    json = await res.json()
  } catch {
    throw new ApiError(`请求失败: HTTP ${res.status}`, res.status)
  }

  // 后端返回了 ApiResponse 格式
  if (json && typeof json.success === 'boolean') {
    if (json.success) {
      return json.data
    }
    throw new ApiError(json.message || '操作失败', res.status, json)
  }

  // 非标准格式，直接返回
  return json
}

export { request, ApiError }
