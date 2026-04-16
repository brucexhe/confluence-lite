<template>
    <div class="page-tree-container">
        <a-spin v-if="loading" size="small" style="padding: 8px 16px; display: block;"/>
        <div v-else-if="treeData.length === 0" class="empty-hint">暂无页面</div>
        <a-tree
            v-else
            class="confluence-tree"
            :tree-data="treeData"
            :show-icon="false"
            blockNode
            v-model:selectedKeys="selectedKeys"
            v-model:expandedKeys="expandedKeys"
            @select="onSelect"
        >
            <template #switcherIcon="{ expanded }">
                <ChevronRight :class="['tree-switcher-icon', { expanded }]"/>
            </template>
            <template #icon="{ dataRef }">
                <FileText v-if="!dataRef.children || dataRef.children.length === 0" class="file-icon"/>
                <Folder v-else class="folder-icon"/>
            </template>
            <template #title="{ dataRef }">
                <span class="node-title" :title="dataRef.title">{{ dataRef.title }}</span>
            </template>
        </a-tree>
    </div>
</template>

<script setup>
import {ref, watch, nextTick} from 'vue'
import {useRoute, useRouter} from 'vue-router'
import {Modal} from 'ant-design-vue'
import {FileText, Folder, ChevronRight} from 'lucide-vue-next'
import {pageApi} from '../api'

const props = defineProps({
    workspaceId: {type: Number, default: null},
    spaceKey: {type: String, default: ''}
})

const route = useRoute()
const router = useRouter()
const loading = ref(false)
const treeData = ref([])
const selectedKeys = ref([])
const expandedKeys = ref([])

// 将后端 PageTreeNodeDto 递归转为 ant-design Tree 格式
function mapNode(node) {
    const item = {
        title: node.title,
        key: String(node.id),
        isLeaf: !node.children || node.children.length === 0,
        hasChildren: node.children && node.children.length > 0,
    }
    if (node.children && node.children.length > 0) {
        item.children = node.children.map(mapNode)
    }
    return item
}

async function loadTree() {
    if (!props.workspaceId) return
    loading.value = true
    try {
        const data = await pageApi.getTree(props.workspaceId)
        treeData.value = (data || []).map(mapNode)
    } catch {
        treeData.value = []
    } finally {
        loading.value = false
    }
}

// workspaceId 变化时重新加载
watch(() => props.workspaceId, loadTree, {immediate: true})

// treeData 加载完成后，自动展开当前页面的父级和子级
watch(() => treeData, (newTree) => {
    if (newTree && newTree.length > 0 && route.params.id) {
        nextTick(() => {
            const id = String(route.params.id)
            selectedKeys.value = [id]

            // 展开所有上级节点
            const parentKeys = getParentKeys(id, newTree, [])

            // 展开第一级子节点
            const childKeys = getChildKeys(id, newTree)

            // 合并所有需要展开的 key
            expandedKeys.value = [...new Set([...parentKeys, ...childKeys])]
        })
    }
}, { deep: true })

// 获取节点的所有上级 key
function getParentKeys(nodeId, nodes, parents = []) {
    for (const node of nodes) {
        if (node.key === String(nodeId)) {
            return [...parents, node.key]
        }
        if (node.children && node.children.length > 0) {
            const result = getParentKeys(nodeId, node.children, [...parents, node.key])
            if (result) return result
        }
    }
    return null
}

// 获取节点的子级 key
function getChildKeys(nodeId, nodes) {
    for (const node of nodes) {
        if (node.key === String(nodeId)) {
            if (node.children && node.children.length > 0) {
                return node.children.map(child => child.key)
            }
            return []
        }
        if (node.children && node.children.length > 0) {
            const result = getChildKeys(nodeId, node.children)
            if (result) return result
        }
    }
    return []
}

// 同步路由中的 page id 到 selectedKeys
watch(() => route.params.id, (id) => {
    if (id) {
        selectedKeys.value = [String(id)]
    }
}, {immediate: true})

function isEditing() {
    return route.path.endsWith('/edit') || route.path.includes('/page/new')
}

const onSelect = (keys, info) => {
    const clickedKey = String(info.node?.key)

    // 如果节点有子节点，切换展开状态
    if (info.node?.hasChildren) {
        const index = expandedKeys.value.indexOf(clickedKey)
        if (index > -1) {
            // 收起：创建不包含该 key 的新数组
            expandedKeys.value = expandedKeys.value.filter(k => k !== clickedKey)
        } else {
            // 展开：创建包含该 key 的新数组
            expandedKeys.value = [...expandedKeys.value, clickedKey]
        }
    }

    // 点击已选中项时，保持选中状态不取消
    if (keys.length === 0) {
        selectedKeys.value = [clickedKey]
        return
    }
    selectedKeys.value = keys
    if (props.spaceKey) {
        const target = `/${props.spaceKey}/page/${keys[0]}`
        const doNavigate = () => router.push(target)
        if (isEditing()) {
            Modal.confirm({
                title: '未保存的更改',
                content: '当前编辑未保存，确认离开？',
                okText: '离开',
                cancelText: '留下',
                onOk: doNavigate,
                onCancel: () => {
                    selectedKeys.value = [String(route.params.id)]
                },
            })
        } else {
            doNavigate()
        }
    }
}
</script>

<style scoped>
.page-tree-container {
    padding: 0;
    margin-top: 0.5rem;
}

.empty-hint {
    padding: 8px 16px;
    color: var(--color-text-secondary, #6b778c);
    font-size: 13px;
}

/* Confluence Tree Styling overrides */
:deep(.confluence-tree.ant-tree) {
    background: transparent;
    color: var(--color-text-primary);
}

:deep(.confluence-tree .ant-tree-node-content-wrapper) {
    border-radius: 3px;
    transition: background-color 0.1s;
    padding: 2px 2px;
    display: flex !important;
    align-items: center;
    overflow: hidden;
}

:deep(.confluence-tree .ant-tree-title) {
    overflow: hidden;
    display: block;
    flex: 1;
}

:deep(.confluence-tree .ant-tree-node-content-wrapper:hover) {
    background-color: rgba(9, 30, 66, 0.06);
}

:deep(.confluence-tree .ant-tree-node-selected) {
    background-color: #E6EFFC !important;
    color: #0052CC !important;
    font-weight: 500;
}

/* Confluence-style Chevron 箭头 */
:deep(.confluence-tree .ant-tree-switcher) {
    display: flex;
    align-items: center;
    justify-content: center;
}

.tree-switcher-icon {
    width: 16px;
    height: 16px;
    color: #42526E;
    transition: transform 0.2s ease;
}

.tree-switcher-icon.expanded {
    transform: rotate(90deg);
}

:deep(.confluence-tree .ant-tree-switcher:hover .tree-switcher-icon) {
    color: #0052CC;
}

/* 隐藏默认图标容器 */
:deep(.confluence-tree .ant-tree-switcher-icon) {
    display: none;
}

.file-icon {
    width: 14px;
    height: 14px;
    color: #0052CC;
    margin-top: 5px;
}

.folder-icon {
    width: 14px;
    height: 14px;
    color: #6B778C;
    margin-top: 5px;
}

.node-title {
    font-size: 14px;
    line-height: 22px;
    margin-left: 0;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    display: block;
    color: #42526e;
}
</style>
