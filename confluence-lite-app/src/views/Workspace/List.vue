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
        >
            <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'name'">
                    <div class="space-name-cell">
                        <div class="space-icon" :style="{ background: record.color }"></div>
                        <strong>{{ record.name }}</strong>
                        <span class="space-desc" v-if="record.description">{{ record.description }}</span>
                    </div>
                </template>
                <template v-else-if="column.key === 'type'">
                    <a-tag :color="record.type === 'Team' ? 'blue' : record.type === 'Project' ? 'purple' : 'green'">
                        {{ record.type }}
                    </a-tag>
                </template>
                <template v-else-if="column.key === 'action'">
                    <a-button type="link" size="small">Settings</a-button>
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
            :okButtonProps="{ style: { backgroundColor: '#0052cc' } }"
        >
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item label="Space name" required>
                    <a-input v-model:value="newSpace.name" placeholder="E.g. Engineering Team" />
                </a-form-item>
                <a-form-item label="Space key" required>
                    <a-input
                        v-model:value="newSpace.key"
                        placeholder="E.g. ENG"
                        style="width: 150px; text-transform: uppercase"
                    />
                </a-form-item>
                <a-form-item label="Space Type">
                    <a-select v-model:value="newSpace.type">
                        <a-select-option value="Knowledge Base">Knowledge Base</a-select-option>
                        <a-select-option value="Team">Team Space</a-select-option>
                        <a-select-option value="Project">Project Space</a-select-option>
                    </a-select>
                </a-form-item>
                <a-form-item label="Description">
                    <a-textarea
                        v-model:value="newSpace.description"
                        :rows="3"
                        placeholder="What is this space about?"
                    />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed } from "vue";

const spaces = ref([
    {
        id: 1,
        name: "Engineering Space",
        key: "ENG",
        type: "Knowledge Base",
        color: "linear-gradient(135deg, #10b981, #059669)",
        description: "Tech docs and architecture.",
    },
    {
        id: 2,
        name: "Marketing & Design",
        key: "MKT",
        type: "Team",
        color: "linear-gradient(135deg, #3b82f6, #2563eb)",
        description: "Campaigns and brand assets.",
    },
    {
        id: 3,
        name: "Q4 Product Launch",
        key: "Q4L",
        type: "Project",
        color: "linear-gradient(135deg, #8B5CF6, #6D28D9)",
        description: "Temporary workspace for launch operations.",
    },
]);

const searchText = ref("");
const isCreateModalVisible = ref(false);
const newSpace = ref({ name: "", key: "", type: "Knowledge Base", description: "" });

const columns = [
    { title: "Space", dataIndex: "name", key: "name" },
    { title: "Key", dataIndex: "key", key: "key", width: "10%" },
    { title: "Type", dataIndex: "type", key: "type", width: "15%" },
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

const showCreateModal = () => {
    newSpace.value = { name: "", key: "", type: "Knowledge Base", description: "" };
    isCreateModalVisible.value = true;
};

const handleCreateSpace = () => {
    if (!newSpace.value.name || !newSpace.value.key) {
        alert("Name and Key are required.");
        return;
    }

    // Assign a random gradient
    const colors = [
        "linear-gradient(135deg, #F59E0B, #D97706)",
        "linear-gradient(135deg, #EF4444, #B91C1C)",
        "linear-gradient(135deg, #06B6D4, #0369A1)",
        "linear-gradient(135deg, #3B82F6, #2563EB)",
    ];
    const color = colors[Math.floor(Math.random() * colors.length)];

    spaces.value.push({
        id: Date.now(),
        name: newSpace.value.name,
        key: newSpace.value.key.toUpperCase(),
        type: newSpace.value.type,
        color,
        description: newSpace.value.description,
    });

    isCreateModalVisible.value = false;
};

const deleteSpace = (id) => {
    spaces.value = spaces.value.filter((s) => s.id !== id);
};
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
    align-items: center;
    gap: 12px;
}

.space-name-cell strong {
    font-weight: 500;
    color: #172b4d;
}

.space-desc {
    font-size: 13px;
    color: #6b778c;
    margin-left: 8px;
}

.space-icon {
    width: 24px;
    height: 24px;
    border-radius: 3px;
    background-color: #dfe1e6;
    flex-shrink: 0;
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
