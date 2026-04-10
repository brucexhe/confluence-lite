<template>
  <div class="page-tree-container">
    <a-tree
      class="confluence-tree"
      :tree-data="treeData"
      :load-data="onLoadData"
      :show-icon="true"
      blockNode
      v-model:selectedKeys="selectedKeys"
      @select="onSelect"
    >
      <template #icon="{ dataRef }">
        <FileText v-if="dataRef.isLeaf" class="file-icon" />
        <Folder v-else class="folder-icon" />
      </template>
      <template #title="{ dataRef }">
        <span class="node-title" :title="dataRef.title">{{ dataRef.title }}</span>
      </template>
    </a-tree>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { FileText, Folder } from 'lucide-vue-next'

const router = useRouter()
const selectedKeys = ref(['1'])

// Mock initial root nodes (Lazy-loaded logic)
const treeData = ref([
  { title: 'Overview', key: '1', isLeaf: true },
  { title: 'Architecture (Click arrow to load)', key: '2', isLeaf: false },
  { title: 'Release Notes', key: '3', isLeaf: true },
  // Adding extra nodes to demonstrate virtual scrolling capabilities
  ...Array.from({ length: 50 }).map((_, i) => ({
    title: `Virtual Scroll Page ${i + 4}`,
    key: String(i + 4),
    isLeaf: true
  }))
])

// Lazy load children to support infinite hierarchies dynamically
const onLoadData = treeNode => {
  return new Promise(resolve => {
    if (treeNode.dataRef.children) {
      resolve()
      return
    }
    // Simulate API request delay
    setTimeout(() => {
      treeNode.dataRef.children = [
        { title: `${treeNode.dataRef.title} - Async Page 1`, key: `${treeNode.eventKey}-1`, isLeaf: true },
        { title: `${treeNode.dataRef.title} - Sub Folder`, key: `${treeNode.eventKey}-2`, isLeaf: false }
      ]
      treeData.value = [...treeData.value]
      resolve()
    }, 600)
  })
}

const onSelect = (selectedKeysValue, info) => {
  if (selectedKeysValue.length > 0) {
    router.push(`/page/${selectedKeysValue[0]}`)
  }
}
</script>

<style scoped>
.page-tree-container {
  padding: 0;
  margin-top: 0.5rem;
}

/* Confluence Tree Styling overrides */
:deep(.confluence-tree.ant-tree) {
  background: transparent;
  color: var(--color-text-primary);
}

:deep(.confluence-tree .ant-tree-node-content-wrapper) {
  border-radius: 3px;
  transition: background-color 0.1s;
  padding: 2px 6px;
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
  background-color: rgba(9, 30, 66, 0.06); /* Atlassian specific gray hover */
}

:deep(.confluence-tree .ant-tree-node-selected) {
  background-color: #E6EFFC !important; /* Confluence active item background */
  color: #0052CC !important; /* Confluence active blue text */
  font-weight: 500;
}

.file-icon {
  width: 14px;
  height: 14px;
  color: #0052CC; /* Confluence blue icon */
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
  line-height: 24px;
  margin-left: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  display: block;
}
</style>
