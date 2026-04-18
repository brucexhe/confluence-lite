<template>
    <div class="space-list-container">
        <div class="header-actions">
            <h2>Space Directory</h2>
            <a-button @click="showCreateModal" class="light-btn">Create space</a-button>
        </div>

        <!-- Search/Filter -->
        <div class="filter-bar">
            <a-input-search v-model:value="searchText" placeholder="Filter by space name..." style="width: 300px" />
        </div>

        <!-- Data Table -->
        <a-table
            :dataSource="filteredSpaces"
            :columns="columns"
            :pagination="pagination"
            :rowKey="(record) => record.id"
            :loading="loading"
        >
            <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'name'">
                    <div class="space-name-cell">
                        <div class="space-icon" :style="{ background: getSpaceColorById(record.id) }"></div>
                        <div>
                            <div class="space-name">
                                <router-link :to="`/${record.key}`">{{ record.name }}</router-link>
                            </div>
                            <span class="space-desc" v-if="record.description">{{ record.description }}</span>
                        </div>
                    </div>
                </template>
                <template v-else-if="column.key === 'key'">
                    <code class="space-key">{{ record.key }}</code>
                </template>
                <template v-else-if="column.key === 'pages'">
                    <span class="page-count">{{ record.pageCount || 0 }} pages</span>
                </template>
                <template v-else-if="column.key === 'action'">
                    <a-button type="link" size="small" @click="goToSettings(record.key)">Settings</a-button>
                    <a-popconfirm
                        title="Are you sure you want to delete this space?"
                        ok-text="Yes"
                        cancel-text="No"
                        @confirm="deleteSpace(record.id)"
                    >
                        <a-button type="link" danger size="small">Delete</a-button>
                    </a-popconfirm>
                </template>
            </template>
        </a-table>

        <!-- Create Modal -->
        <a-modal
            v-model:open="isCreateModalVisible"
            title="Create a new space"
            @ok="handleCreateSpace"
            okText="Create"
            cancelText="Cancel"
            :confirmLoading="creating"
            :okButtonProps="{ style: { backgroundColor: '#0052cc' } }"
        >
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item label="Space name" required>
                    <a-input
                        v-model:value="newSpace.name"
                        placeholder="E.g. Engineering Team"
                        :maxlength="100"
                        showCount
                    />
                </a-form-item>
                <a-form-item label="Space key" required>
                    <a-input
                        v-model:value="newSpace.key"
                        placeholder="E.g. ENG"
                        :maxlength="50"
                        style="width: 200px; text-transform: uppercase"
                    />
                    <div class="form-hint">2-50 characters, letters, numbers, - and _ only</div>
                </a-form-item>
                <a-form-item label="Description">
                    <a-textarea
                        v-model:value="newSpace.description"
                        :rows="3"
                        placeholder="What is this space about?"
                        :maxlength="1000"
                        showCount
                    />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { workspaceApi } from "../../api";
import { getSpaceColorById } from "../../utils/workspace";

const router = useRouter();
const spaces = ref([]);
const loading = ref(false);
const creating = ref(false);
const searchText = ref("");
const isCreateModalVisible = ref(false);
const newSpace = ref({ name: "", key: "", description: "" });

const columns = [
    { title: "Space", dataIndex: "name", key: "name" },
    { title: "Key", dataIndex: "key", key: "key", width: "15%" },
    { title: "Pages", dataIndex: "pages", key: "pages", width: "15%" },
    { title: "Action", key: "action", width: "20%" },
];

const pagination = { pageSize: 15 };

const filteredSpaces = computed(() => {
    if (!searchText.value) return spaces.value;
    return spaces.value.filter(
        (s) =>
            s.name.toLowerCase().includes(searchText.value.toLowerCase()) ||
            s.key.toLowerCase().includes(searchText.value.toLowerCase()),
    );
});

// Load workspaces from API
const loadWorkspaces = async () => {
    loading.value = true;
    try {
        const response = await workspaceApi.getList(1, 100);
        console.log('Workspace list response:', response);
        // request.js 自动提取了 data 字段，所以 response 是 PagedResponse<WorkspaceDto>
        if (response?.items && Array.isArray(response.items)) {
            spaces.value = response.items;
        } else if (Array.isArray(response)) {
            spaces.value = response;
        } else {
            console.warn('Unexpected response format:', response);
            spaces.value = [];
        }
    } catch (error) {
        console.error("Failed to load workspaces:", error);
        message.error("Failed to load workspaces");
        spaces.value = [];
    } finally {
        loading.value = false;
    }
};

const showCreateModal = () => {
    newSpace.value = { name: "", key: "", description: "" };
    isCreateModalVisible.value = true;
};

const handleCreateSpace = async () => {
    if (!newSpace.value.name || !newSpace.value.key) {
        message.warning("Name and Key are required.");
        return;
    }

    // Validate key format
    const keyRegex = /^[a-zA-Z0-9-_]+$/;
    if (!keyRegex.test(newSpace.value.key)) {
        message.warning("Key can only contain letters, numbers, hyphens and underscores.");
        return;
    }

    creating.value = true;
    try {
        await workspaceApi.create({
            name: newSpace.value.name,
            key: newSpace.value.key,
            description: newSpace.value.description,
        });
        message.success("Space created successfully");
        isCreateModalVisible.value = false;
        await loadWorkspaces();
    } catch (error) {
        console.error("Failed to create workspace:", error);
        message.error(error?.response?.data?.message || "Failed to create space");
    } finally {
        creating.value = false;
    }
};

const deleteSpace = async (id) => {
    try {
        await workspaceApi.remove(id);
        message.success("Space deleted successfully");
        await loadWorkspaces();
    } catch (error) {
        console.error("Failed to delete workspace:", error);
        message.error(error?.response?.data?.message || "Failed to delete space");
    }
};

const goToSettings = (key) => {
    router.push(`/${key}/settings`);
};

onMounted(() => {
    loadWorkspaces();
});
</script>

<style scoped>
.space-list-container {
    max-width: 1200px;
    margin: 0 auto;
    padding: 20px 2rem 0;
    animation: fadeIn 0.3s ease-in-out;
}

.header-actions {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
}

.header-actions h2 {
    font-size: 24px;
    font-weight: 500;
    color: #172b4d;
    margin: 0;
    letter-spacing: -0.01em;
}

.filter-bar {
    margin-bottom: 24px;
}

.space-name-cell {
    display: flex;
    align-items: flex-start;
    gap: 12px;
}

.space-icon {
    width: 32px;
    height: 32px;
    border-radius: 4px;
    flex-shrink: 0;
    margin-top: 2px;
}

.space-name {
    font-weight: 500;
    color: #172b4d;
}

.space-name a {
    color: #172b4d;
    text-decoration: none;
}

.space-name a:hover {
    color: #0052cc;
    text-decoration: underline;
}

.space-desc {
    font-size: 13px;
    color: #6b778c;
    margin-top: 2px;
    display: block;
}

.space-key {
    background-color: #f4f5f7;
    padding: 2px 8px;
    border-radius: 3px;
    font-size: 13px;
    color: #42526e;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
}

.page-count {
    font-size: 13px;
    color: #6b778c;
}

.form-hint {
    font-size: 12px;
    color: #6b778c;
    margin-top: 4px;
}

:deep(.ant-table-thead > tr > th) {
    background-color: #fafbfc;
    color: #42526e;
    font-weight: 600;
    font-size: 12px;
    border-bottom: 1px solid #dfe1e6;
    text-transform: uppercase;
}

:deep(.ant-table-tbody > tr:hover > td) {
    background-color: rgba(9, 30, 66, 0.02) !important;
}

@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(5px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}
</style>
