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
                        <img
                            v-if="record.icon && isImageUrl(record.icon)"
                            class="space-icon-img"
                            :src="record.icon"
                            alt=""
                        />
                        <div
                            v-else
                            class="space-icon"
                            :style="{
                                background: record.icon || getSpaceColorById(record.id)
                            }"
                        >
                            <span>{{ getSpaceInitial(record) }}</span>
                        </div>
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
                <template v-else-if="column.key === 'isDefault'">
                    <a-tag v-if="record.isDefault" color="blue">Default</a-tag>
                    <span v-else style="color: #dfe1e6">-</span>
                </template>
                <template v-else-if="column.key === 'action'">
                    <a-button type="link" size="small" @click="showEditModal(record)">Edit</a-button>
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
            width="600px"
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
                <a-form-item label="Icon">
                    <div class="icon-selector">
                        <div class="icon-type-tabs">
                            <a-radio-group v-model:value="newSpace.iconType" button-style="solid">
                                <a-radio-button value="gradient">Color</a-radio-button>
                                <a-radio-button value="image">Image</a-radio-button>
                            </a-radio-group>
                        </div>
                        <div v-if="newSpace.iconType === 'gradient'" class="color-picker">
                            <div
                                v-for="color in presetColors"
                                :key="color.value"
                                class="color-option"
                                :class="{ selected: newSpace.iconGradient === color.value }"
                                :style="{ background: color.value }"
                                @click="newSpace.iconGradient = color.value"
                            ></div>
                        </div>
                        <div v-else class="image-uploader">
                            <a-upload
                                :before-upload="handleBeforeUpload"
                                :show-upload-list="false"
                                accept="image/*"
                            >
                                <div v-if="newSpace.iconUrl" class="image-preview">
                                    <img :src="newSpace.iconUrl" alt="Space icon" />
                                    <div class="image-overlay">
                                        <span>Change</span>
                                    </div>
                                </div>
                                <div v-else class="upload-placeholder">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                        <path d="M12 5V19M5 12H19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                                    </svg>
                                    <span>Upload Image</span>
                                </div>
                            </a-upload>
                            <a-button v-if="newSpace.iconUrl" danger size="small" @click="newSpace.iconUrl = ''">Remove</a-button>
                        </div>
                    </div>
                </a-form-item>
                <a-form-item label="Set as default space">
                    <a-switch v-model:checked="newSpace.isDefault" />
                    <span class="form-hint">This will be your primary space</span>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- Edit Modal -->
        <a-modal
            v-model:open="isEditModalVisible"
            title="Edit space"
            @ok="handleEditSpace"
            okText="Save"
            cancelText="Cancel"
            :confirmLoading="editing"
            :okButtonProps="{ style: { backgroundColor: '#0052cc' } }"
            width="600px"
        >
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item label="Space name" required>
                    <a-input
                        v-model:value="editSpace.name"
                        placeholder="E.g. Engineering Team"
                        :maxlength="100"
                        showCount
                    />
                </a-form-item>
                <a-form-item label="Description">
                    <a-textarea
                        v-model:value="editSpace.description"
                        :rows="3"
                        placeholder="What is this space about?"
                        :maxlength="1000"
                        showCount
                    />
                </a-form-item>
                <a-form-item label="Icon">
                    <div class="icon-selector">
                        <div class="icon-type-tabs">
                            <a-radio-group v-model:value="editSpace.iconType" button-style="solid">
                                <a-radio-button value="gradient">Color</a-radio-button>
                                <a-radio-button value="image">Image</a-radio-button>
                            </a-radio-group>
                        </div>
                        <div v-if="editSpace.iconType === 'gradient'" class="color-picker">
                            <div
                                v-for="color in presetColors"
                                :key="color.value"
                                class="color-option"
                                :class="{ selected: editSpace.iconGradient === color.value }"
                                :style="{ background: color.value }"
                                @click="editSpace.iconGradient = color.value"
                            ></div>
                        </div>
                        <div v-else class="image-uploader">
                            <a-upload
                                :before-upload="handleBeforeUpload"
                                :show-upload-list="false"
                                accept="image/*"
                            >
                                <div v-if="editSpace.iconUrl" class="image-preview">
                                    <img :src="editSpace.iconUrl" alt="Space icon" />
                                    <div class="image-overlay">
                                        <span>Change</span>
                                    </div>
                                </div>
                                <div v-else class="upload-placeholder">
                                    <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                        <path d="M12 5V19M5 12H19" stroke="currentColor" stroke-width="2" stroke-linecap="round"/>
                                    </svg>
                                    <span>Upload Image</span>
                                </div>
                            </a-upload>
                            <a-button v-if="editSpace.iconUrl" danger size="small" @click="editSpace.iconUrl = ''">Remove</a-button>
                        </div>
                    </div>
                </a-form-item>
                <a-form-item label="Set as default space">
                    <a-switch v-model:checked="editSpace.isDefault" />
                    <span class="form-hint">This will be your primary space</span>
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { workspaceApi, uploadApi } from "../../api";
import { getSpaceColorById, getSpaceInitial } from "../../utils/workspace";

function isImageUrl(icon) {
    if (!icon) return false;
    return /^(https?:\/\/|data:image\/|\/)/.test(icon);
}

const router = useRouter();
const spaces = ref([]);
const loading = ref(false);
const creating = ref(false);
const editing = ref(false);
const searchText = ref("");
const isCreateModalVisible = ref(false);
const isEditModalVisible = ref(false);

const presetColors = [
    { name: "Green", value: "linear-gradient(135deg, #10b981, #059669)" },
    { name: "Blue", value: "linear-gradient(135deg, #3b82f6, #2563eb)" },
    { name: "Purple", value: "linear-gradient(135deg, #8b5cf6, #7c3aed)" },
    { name: "Orange", value: "linear-gradient(135deg, #f59e0b, #d97706)" },
    { name: "Red", value: "linear-gradient(135deg, #ef4444, #dc2626)" },
    { name: "Cyan", value: "linear-gradient(135deg, #06b6d4, #0891b2)" },
];

const columns = [
    { title: "Space", dataIndex: "name", key: "name" },
    { title: "Key", dataIndex: "key", key: "key", width: "15%" },
    { title: "Pages", dataIndex: "pages", key: "pages", width: "12%" },
    { title: "Default", dataIndex: "isDefault", key: "isDefault", width: "10%" },
    { title: "Action", key: "action", width: "20%" },
];

const pagination = { pageSize: 15 };

const newSpace = ref({
    name: "",
    key: "",
    description: "",
    iconType: "gradient",
    iconGradient: presetColors[0].value,
    iconUrl: "",
    isDefault: false
});

const editSpace = ref({
    id: null,
    name: "",
    description: "",
    iconType: "gradient",
    iconGradient: presetColors[0].value,
    iconUrl: "",
    isDefault: false
});

const filteredSpaces = computed(() => {
    if (!searchText.value) return spaces.value;
    return spaces.value.filter(
        (s) =>
            s.name.toLowerCase().includes(searchText.value.toLowerCase()) ||
            s.key.toLowerCase().includes(searchText.value.toLowerCase()),
    );
});

// 判断 icon 是图片URL还是颜色渐变
const getIconType = (icon) => {
    if (!icon) return 'none';
    // 检查是否是URL（http/https 或 base64）
    if (icon.startsWith('http') || icon.startsWith('data:image') || icon.startsWith('/')) {
        return 'image';
    }
    // 否则是颜色渐变
    return 'gradient';
};

// Load workspaces from API
const loadWorkspaces = async () => {
    loading.value = true;
    try {
        const response = await workspaceApi.getList(1, 100);
        console.log('Workspace list response:', response);
        if (response?.items && Array.isArray(response.items)) {
            spaces.value = response.items.map(ws => ({
                ...ws,
                iconType: getIconType(ws.icon),
                iconUrl: ws.icon || '',
                iconGradient: ws.icon || ''
            }));
        } else if (Array.isArray(response)) {
            spaces.value = response.map(ws => ({
                ...ws,
                iconType: getIconType(ws.icon),
                iconUrl: ws.icon || '',
                iconGradient: ws.icon || ''
            }));
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
    newSpace.value = {
        name: "",
        key: "",
        description: "",
        iconType: "gradient",
        iconGradient: presetColors[0].value,
        iconUrl: "",
        isDefault: false
    };
    isCreateModalVisible.value = true;
};

const showEditModal = (space) => {
    const iconType = getIconType(space.icon);
    editSpace.value = {
        id: space.id,
        name: space.name,
        description: space.description || "",
        iconType: iconType,
        iconUrl: iconType === 'image' ? space.icon || '' : '',
        iconGradient: iconType === 'gradient' ? space.icon : presetColors[0].value,
        isDefault: space.isDefault || false
    };
    isEditModalVisible.value = true;
};

const handleBeforeUpload = async (file) => {
    const isImage = file.type.startsWith('image/');
    if (!isImage) {
        message.error('You can only upload image files!');
        return false;
    }
    const isLt2M = file.size / 1024 / 1024 < 2;
    if (!isLt2M) {
        message.error('Image must smaller than 2MB!');
        return false;
    }

    // Upload to server
    try {
        const filePath = await uploadApi.upload(file);
        const target = isCreateModalVisible.value ? newSpace.value : editSpace.value;
        target.iconUrl = filePath;
        target.iconType = 'image';
        message.success('Image uploaded successfully');
    } catch (error) {
        console.error('Upload failed:', error);
        message.error('Failed to upload image');
    }
    return false; // Prevent auto upload
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
        const data = {
            name: newSpace.value.name,
            key: newSpace.value.key,
            description: newSpace.value.description,
            isDefault: newSpace.value.isDefault
        };

        // Handle icon
        if (newSpace.value.iconType === 'image' && newSpace.value.iconUrl) {
            data.icon = newSpace.value.iconUrl;
        } else if (newSpace.value.iconType === 'gradient') {
            data.icon = newSpace.value.iconGradient;
        }

        await workspaceApi.create(data);
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

const handleEditSpace = async () => {
    if (!editSpace.value.name) {
        message.warning("Name is required.");
        return;
    }

    editing.value = true;
    try {
        const data = {
            name: editSpace.value.name,
            description: editSpace.value.description,
            isDefault: editSpace.value.isDefault
        };

        // Handle icon
        if (editSpace.value.iconType === 'image' && editSpace.value.iconUrl) {
            data.icon = editSpace.value.iconUrl;
        } else if (editSpace.value.iconType === 'gradient') {
            data.icon = editSpace.value.iconGradient;
        }

        await workspaceApi.update(editSpace.value.id, data);
        message.success("Space updated successfully");
        isEditModalVisible.value = false;
        await loadWorkspaces();
    } catch (error) {
        console.error("Failed to update workspace:", error);
        message.error(error?.response?.data?.message || "Failed to update space");
    } finally {
        editing.value = false;
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
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 14px;
    font-weight: 600;
}

.space-icon-img {
    width: 32px;
    height: 32px;
    border-radius: 4px;
    flex-shrink: 0;
    margin-top: 2px;
    object-fit: cover;
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
    margin-left: 8px;
}

/* Icon Selector */
.icon-selector {
    border: 1px solid #dfe1e6;
    border-radius: 4px;
    padding: 16px;
}

.icon-type-tabs {
    margin-bottom: 16px;
}

.color-picker {
    display: flex;
    gap: 8px;
    flex-wrap: wrap;
}

.color-option {
    width: 40px;
    height: 40px;
    border-radius: 4px;
    cursor: pointer; 
    transition: all 0.2s;
}

.color-option:hover {
    transform: scale(1.1);
}

.color-option.selected {
    border-color: #0052cc;
    box-shadow: 0 0 0 3px rgba(6, 79, 187, 0.2);
}

.image-uploader {
    display: flex;
    align-items: center;
    gap: 12px;
}

.image-preview {
    width: 80px;
    height: 80px;
    border-radius: 4px;
    overflow: hidden;
    position: relative;
    cursor: pointer;
}

.image-preview img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.image-overlay {
    position: absolute;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.5);
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 12px;
    opacity: 0;
    transition: opacity 0.2s;
}

.image-preview:hover .image-overlay {
    opacity: 1;
}

.upload-placeholder {
    width: 80px;
    height: 80px;
    border: 2px dashed #dfe1e6;
    border-radius: 4px;
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    color: #6b778c;
    font-size: 12px;
    transition: all 0.2s;
}

.upload-placeholder:hover {
    border-color: #0052cc;
    color: #0052cc;
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
