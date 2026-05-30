<template>
  <a-modal
    :open="open"
    title="页面排序设置"
    :width="700"
    :maskClosable="false"
    :keyboard="false"
    @cancel="handleClose"
  >
    <template #footer>
      <a-button @click="handleClose">关闭</a-button>
    </template>
    <div class="tree-settings-hint">拖拽页面可调整排序或更改层级</div>
    <a-spin v-if="loading" size="small" style="padding: 16px; display: block" />
    <div v-else-if="treeData.length === 0" class="empty-hint">暂无页面</div>
    <div v-else class="tree-scroll-container">
      <a-tree
        class="confluence-tree draggable-tree"
        :tree-data="treeData"
        :show-icon="false"
        draggable
        blockNode
        @drop="onDrop"
      >
        <template #switcherIcon="{ expanded }">
          <ChevronRight :class="['tree-switcher-icon', { expanded }]" />
        </template>
        <template #title="{ dataRef }">
          <div class="flex">
            <Dot
              v-if="!dataRef.children || dataRef.children.length === 0"
              class="leaf-icon"
            />
            <span class="node-title" :title="dataRef.title">{{
              dataRef.title
            }}</span>
          </div>
        </template>
      </a-tree>
    </div>
  </a-modal>
</template>

<script setup>
import { ref, watch } from "vue";
import { message } from "ant-design-vue";
import { Dot, ChevronRight } from "lucide-vue-next";
import { pageApi } from "../api";

const props = defineProps({
  open: { type: Boolean, default: false },
  workspaceId: { type: Number, default: null },
  spaceKey: { type: String, default: "" },
});

const emit = defineEmits(["update:open"]);

const loading = ref(false);
const treeData = ref([]);

// 将后端数据递归转为 a-tree 格式
function mapNode(node) {
  const item = {
    title: node.title,
    key: String(node.id),
    isLeaf: !node.children || node.children.length === 0,
  };
  if (node.children && node.children.length > 0) {
    item.children = node.children.map(mapNode);
  }
  return item;
}

// 弹窗打开时加载数据
watch(
  () => props.open,
  async (val) => {
    if (val && props.workspaceId) {
      loading.value = true;
      try {
        const data = await pageApi.getTree(props.workspaceId);
        treeData.value = (data || []).map(mapNode);
      } catch (e) {
        message.error("加载页面树失败");
      } finally {
        loading.value = false;
      }
    }
  }
);

// ========== 拖拽处理 ==========

// 在树中递归查找并移除指定 key 的节点
function removeNode(nodes, key) {
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].key === key) {
      return nodes.splice(i, 1)[0];
    }
    if (nodes[i].children && nodes[i].children.length > 0) {
      const found = removeNode(nodes[i].children, key);
      if (found) return found;
    }
  }
  return null;
}

// 在树中递归查找指定 key 的节点
function findNode(nodes, key) {
  for (const node of nodes) {
    if (node.key === key) return node;
    if (node.children && node.children.length > 0) {
      const found = findNode(node.children, key);
      if (found) return found;
    }
  }
  return null;
}

// 查找节点的父节点及其在 children 中的索引
function findParentInfo(nodes, key, parent = null) {
  for (let i = 0; i < nodes.length; i++) {
    if (nodes[i].key === key) {
      return { parent: parent ? parent : { children: nodes }, index: i };
    }
    if (nodes[i].children && nodes[i].children.length > 0) {
      const found = findParentInfo(nodes[i].children, key, nodes[i]);
      if (found) return found;
    }
  }
  return null;
}

async function onDrop(info) {
  const dragKey = info.dragNode.key;
  const dropKey = info.node.key;
  const dropToGap = info.dropToGap;
  const dropPosition = info.dropPosition;

  // 不能拖拽到自身
  if (dragKey === dropKey) return;

  // 从树中移除被拖拽的节点
  const dragNode = removeNode(treeData.value, dragKey);
  if (!dragNode) return;

  let targetParentId = null;
  let targetSortOrder = 0;

  if (dropToGap) {
    // 放在目标节点的上方或下方（作为兄弟节点）
    const dropParentInfo = findParentInfo(treeData.value, dropKey);
    if (!dropParentInfo) {
      // 目标在根级别
      const dropIndex = treeData.value.findIndex((n) => n.key === dropKey);
      if (dropPosition === -1) {
        treeData.value.splice(dropIndex, 0, dragNode);
        targetSortOrder = dropIndex;
      } else {
        treeData.value.splice(dropIndex + 1, 0, dragNode);
        targetSortOrder = dropIndex + 1;
      }
      targetParentId = null;
    } else {
      const { parent, index } = dropParentInfo;
      targetParentId = Number(parent.key);
      if (dropPosition === -1) {
        parent.children.splice(index, 0, dragNode);
        targetSortOrder = index;
      } else {
        parent.children.splice(index + 1, 0, dragNode);
        targetSortOrder = index + 1;
      }
    }
  } else {
    // 放在目标节点内部（作为子节点）
    const dropNode = findNode(treeData.value, dropKey);
    if (dropNode) {
      if (!dropNode.children) {
        dropNode.children = [];
      }
      targetSortOrder = dropNode.children.length;
      targetParentId = Number(dropNode.key);
      dropNode.children.push(dragNode);
      dropNode.isLeaf = false;
    }
  }

  // 即时保存到后端
  try {
    await pageApi.movePage(Number(dragKey), targetParentId, targetSortOrder);
  } catch (e) {
    message.error(e.message || "移动页面失败，正在恢复...");
    // 保存失败，重新加载树恢复原状
    await reloadTree();
  }
}

async function reloadTree() {
  if (!props.workspaceId) return;
  try {
    const data = await pageApi.getTree(props.workspaceId);
    treeData.value = (data || []).map(mapNode);
  } catch (e) {
    // ignore
  }
}

function handleClose() {
  emit("update:open", false);
}
</script>

<style scoped>
.tree-settings-hint {
  padding: 0 0 12px;
  color: var(--color-text-secondary, #6b778c);
  font-size: 13px;
}

.empty-hint {
  padding: 16px;
  color: var(--color-text-secondary, #6b778c);
  font-size: 13px;
  text-align: center;
}

.tree-scroll-container {
  height: 400px;
  overflow-y: auto;
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
  margin-left: 2px;
}

:deep(.confluence-tree .ant-tree-node-content-wrapper:hover) {
  background-color: rgba(9, 30, 66, 0.06);
}

:deep(.confluence-tree .ant-tree-switcher) {
  display: flex;
  align-items: center;
  justify-content: center;
}

.tree-switcher-icon {
  width: 16px;
  height: 16px;
  color: #42526e;
  transition: transform 0.2s ease;
}

.tree-switcher-icon.expanded {
  transform: rotate(90deg);
}

:deep(.confluence-tree .ant-tree-switcher:hover .tree-switcher-icon) {
  color: #0052cc;
}

/* 隐藏默认图标容器 */
:deep(.confluence-tree .ant-tree-switcher-icon) {
  display: none;
}

.leaf-icon {
  width: 21px;
  height: 21px;
  color: #42526e;
  flex-shrink: 0;
  transform: scale(1.2);
  margin-left: -6px;
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

/* 拖拽相关样式 */
:deep(.draggable-tree .ant-tree-draggable-icon) {
  opacity: 0.4;
  cursor: grab;
}

:deep(.draggable-tree .ant-tree-node-content-wrapper-draggable) {
  cursor: grab;
}

:deep(.draggable-tree .ant-tree-drop-indicator) {
  background-color: #0052cc;
}
</style>
