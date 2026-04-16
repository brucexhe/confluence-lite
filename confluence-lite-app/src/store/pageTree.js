import { defineStore } from 'pinia'
import { ref } from 'vue'
import { pageApi } from '../api'

export const usePageTreeStore = defineStore('pageTree', () => {
  // 缓存的工作空间 ID 和对应的树数据
  const cache = new Map() // workspaceId -> treeData

  const currentWorkspaceId = ref(null)
  const currentTreeData = ref([])

  /**
   * 获取工作空间的页面树数据
   * 如果缓存中存在且未过期，直接返回缓存
   * @param {number} workspaceId - 工作空间 ID
   * @param {boolean} forceRefresh - 是否强制刷新
   * @returns {Promise<Array>} 页面树数据
   */
  async function getTree(workspaceId, forceRefresh = false) {
    if (!workspaceId) {
      return []
    }

    // 检查缓存
    if (!forceRefresh && cache.has(workspaceId)) {
      const cached = cache.get(workspaceId)
      // 检查缓存是否在30分钟内
      if (Date.now() - cached.timestamp < 30 * 60 * 1000) {
        currentWorkspaceId.value = workspaceId
        currentTreeData.value = cached.data
        return cached.data
      } else {
        cache.delete(workspaceId)
      }
    }

    // 加载新数据
    try {
      const data = await pageApi.getTree(workspaceId)
      const treeData = data || []

      // 缓存数据
      cache.set(workspaceId, {
        data: treeData,
        timestamp: Date.now()
      })

      currentWorkspaceId.value = workspaceId
      currentTreeData.value = treeData

      return treeData
    } catch (error) {
      console.error('[PageTreeStore] Failed to load tree:', error)
      return []
    }
  }

  /**
   * 根据页面 ID 查找节点信息
   * @param {number} pageId - 页面 ID
   * @returns {Object|null} 节点信息
   */
  function findNode(pageId) {
    if (!currentTreeData.value || currentTreeData.value.length === 0) {
      return null
    }

    function search(nodes) {
      for (const node of nodes) {
        if (node.id === pageId) {
          return node
        }
        if (node.children && node.children.length > 0) {
          const found = search(node.children)
          if (found) return found
        }
      }
      return null
    }

    return search(currentTreeData.value)
  }

  /**
   * 获取页面的所有上级节点 ID（包括自己）
   * @param {number} pageId - 页面 ID
   * @returns {Array<number>} 上级节点 ID 数组
   */
  function getParentIds(pageId) {
    const ids = []

    function collect(nodes, targetId) {
      for (const node of nodes) {
        if (node.id === targetId) {
          ids.push(node.id)
          return true
        }
        if (node.children && node.children.length > 0) {
          ids.push(node.id)
          if (collect(node.children, targetId)) {
            return true
          }
          ids.pop()
        }
      }
      return false
    }

    collect(currentTreeData.value, pageId)
    return ids
  }

  /**
   * 清除指定工作空间的缓存
   * @param {number} workspaceId - 工作空间 ID
   */
  function clearCache(workspaceId) {
    cache.delete(workspaceId)
    if (currentWorkspaceId.value === workspaceId) {
      currentWorkspaceId.value = null
      currentTreeData.value = []
    }
  }

  /**
   * 清除所有缓存
   */
  function clearAllCache() {
    cache.clear()
    currentWorkspaceId.value = null
    currentTreeData.value = []
  }

  /**
   * 当页面被创建、更新或删除时，清除对应工作空间的缓存
   * @param {number} workspaceId - 工作空间 ID
   */
  function invalidateWorkspace(workspaceId) {
    clearCache(workspaceId)
  }

  return {
    currentWorkspaceId,
    currentTreeData,
    getTree,
    findNode,
    getParentIds,
    clearCache,
    clearAllCache,
    invalidateWorkspace
  }
})
