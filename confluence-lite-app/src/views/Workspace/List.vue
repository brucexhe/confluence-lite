<template>
    <div class="space-list-container">
        <div class="header-actions">
            <h2>Space Directory</h2>
            <a-button type="primary" @click="showCreateModal">Create space</a-button>
        </div>

        <!-- Search -->
        <div class="filter-bar">
            <a-input-search
                v-model:value="searchText"
                placeholder="Search public spaces..."
                style="width: 300px"
                allow-clear
                @search="loadDiscover"
            />
        </div>

        <!-- My Spaces -->
        <div class="section">
            <div class="section-title">My spaces</div>
            <a-spin :spinning="loadingMine">
                <div v-if="mySpaces.length > 0" class="space-grid">
                    <div v-for="space in mySpaces" :key="space.id" class="space-card">
                        <div class="space-card-head" @click="goSpace(space)">
                            <img
                                v-if="space.icon && isImageUrl(space.icon)"
                                class="space-icon-img"
                                :src="space.icon"
                                alt=""
                            />
                            <div
                                v-else
                                class="space-icon"
                                :style="{ background: space.icon || getSpaceColorById(space.id) }"
                            >
                                <span>{{ getSpaceInitial(space) }}</span>
                            </div>
                            <div class="space-card-title">
                                <div class="space-name">{{ space.name }}</div>
                                <code class="space-key">{{ space.key }}</code>
                            </div>
                        </div>
                        <div class="space-desc" v-if="space.description">{{ space.description }}</div>
                        <div class="space-card-foot">
                            <span class="page-count">{{ space.pageCount || 0 }} pages</span>
                            <a-tag v-if="space.isPublic" color="green">Public</a-tag>
                            <a-tag v-if="space.isDefault" color="blue">Default</a-tag>
                            <span class="foot-actions">
                                <a-button v-if="isOwnerOf(space)" type="link" size="small" @click="showEditModal(space)">Edit</a-button>
                                <a-popconfirm
                                    v-if="isOwnerOf(space)"
                                    title="Are you sure you want to delete this space?"
                                    ok-text="Yes"
                                    cancel-text="No"
                                    @confirm="deleteSpace(space.id)"
                                >
                                    <a-button type="link" danger size="small">Delete</a-button>
                                </a-popconfirm>
                                <a-popconfirm
                                    v-else
                                    title="Leave this space?"
                                    ok-text="Yes"
                                    cancel-text="No"
                                    @confirm="leaveSpace(space)"
                                >
                                    <a-button type="link" size="small">Leave</a-button>
                                </a-popconfirm>
                            </span>
                        </div>
                    </div>
                </div>
                <a-empty v-else-if="!loadingMine" description="You have no spaces yet. Create one or join a public space below." />
            </a-spin>
        </div>

        <!-- Discover Public Spaces -->
        <div class="section">
            <div class="section-title">Discover public spaces</div>
            <a-spin :spinning="loadingDiscover">
                <div v-if="discoverSpaces.length > 0" class="space-grid">
                    <div v-for="space in discoverSpaces" :key="space.id" class="space-card">
                        <div class="space-card-head" @click="space.isJoined && goSpace(space)">
                            <img
                                v-if="space.icon && isImageUrl(space.icon)"
                                class="space-icon-img"
                                :src="space.icon"
                                alt=""
                            />
                            <div
                                v-else
                                class="space-icon"
                                :style="{ background: space.icon || getSpaceColorById(space.id) }"
                            >
                                <span>{{ getSpaceInitial(space) }}</span>
                            </div>
                            <div class="space-card-title">
                                <div class="space-name">{{ space.name }}</div>
                                <code class="space-key">{{ space.key }}</code>
                            </div>
                        </div>
                        <div class="space-desc" v-if="space.description">{{ space.description }}</div>
                        <div class="space-card-foot">
                            <span class="page-count">{{ space.memberCount }} members · {{ space.pageCount || 0 }} pages</span>
                            <span class="foot-actions">
                                <a-tag v-if="space.isOwner" color="geekblue">Owner</a-tag>
                                <a-tag v-else-if="space.isJoined" color="cyan">Joined</a-tag>
                                <a-button v-else type="primary" size="small" :loading="joiningId === space.id" @click="joinSpace(space)">Join</a-button>
                            </span>
                        </div>
                    </div>
                </div>
                <a-empty v-else-if="!loadingDiscover" description="No public spaces found" />
            </a-spin>
        </div>

        <!-- Create Modal -->
        <a-modal
            v-model:open="isCreateModalVisible"
            title="Create a new space"
            @ok="handleCreateSpace"
            okText="Create"
            cancelText="Cancel"
            :confirmLoading="creating"
            :okButtonProps="{ style: { backgroundColor: '#0052cc' } }"
            :width="isMobile ? '95%' : 600"
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
                <a-form-item label="Visibility">
                    <a-switch v-model:checked="newSpace.isPublic" />
                    <span class="form-hint">Public spaces appear in the directory and can be joined by anyone</span>
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
            :width="isMobile ? '95%' : 600"
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
                <a-form-item label="Space key">
                    <a-input
                        v-model:value="editSpace.key"
                        disabled
                        style="width: 200px"
                    />
                    <div class="form-hint">Space keys cannot be changed</div>
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
                <a-form-item label="Visibility">
                    <a-switch v-model:checked="editSpace.isPublic" />
                    <span class="form-hint">Public spaces appear in the directory and can be joined by anyone</span>
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
import { ref, computed, onMounted, onUnmounted } from "vue";
import { useRouter } from "vue-router";
import { message } from "ant-design-vue";
import { workspaceApi } from "../../api";
import { useAuthStore } from "../../store/auth";
import { getSpaceColorById, getSpaceInitial } from "../../utils/workspace";

// Mobile detection
const isMobile = ref(false);
function checkMobile() {
    isMobile.value = window.innerWidth <= 768;
}
onMounted(() => { checkMobile(); window.addEventListener("resize", checkMobile); });
onUnmounted(() => { window.removeEventListener("resize", checkMobile); });

function isImageUrl(icon) {
    if (!icon) return false;
    return /^(https?:\/\/|data:image\/|\/)/.test(icon);
}

const router = useRouter();
const authStore = useAuthStore();

const mySpaces = ref([]);
const discoverSpaces = ref([]);
const loadingMine = ref(false);
const loadingDiscover = ref(false);
const creating = ref(false);
const editing = ref(false);
const searchText = ref("");
const joiningId = ref(null);
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

const newSpace = ref({
    name: "",
    key: "",
    description: "",
    iconType: "gradient",
    iconGradient: presetColors[0].value,
    iconUrl: "",
    isDefault: false,
    isPublic: false
});

const editSpace = ref({
    id: null,
    name: "",
    key: "",
    description: "",
    iconType: "gradient",
    iconGradient: presetColors[0].value,
    iconUrl: "",
    isDefault: false,
    isPublic: false
});

const currentUserId = computed(() => authStore.user?.id || null);

const isOwnerOf = (space) => {
    return currentUserId.value != null && space.ownerId === currentUserId.value;
};

// 加载"我的空间"（Owner ∪ 被授权空间）
const loadMySpaces = async () => {
    loadingMine.value = true;
    try {
        const data = await workspaceApi.getMy();
        mySpaces.value = (data || []).map((ws) => ({
            ...ws,
            key: ws.key?.toUpperCase() || ""
        }));
    } catch (error) {
        console.error("Failed to load my workspaces:", error);
        mySpaces.value = [];
    } finally {
        loadingMine.value = false;
    }
};

// 加载公开空间目录
const loadDiscover = async () => {
    loadingDiscover.value = true;
    try {
        const response = await workspaceApi.discover(1, 60, searchText.value || "");
        discoverSpaces.value = (response?.items || []).map((ws) => ({
            ...ws,
            key: ws.key?.toUpperCase() || ""
        }));
    } catch (error) {
        console.error("Failed to load public spaces:", error);
        discoverSpaces.value = [];
    } finally {
        loadingDiscover.value = false;
    }
};

const goSpace = (space) => {
    router.push(`/${space.key}`);
};

// 加入公开空间
const joinSpace = async (space) => {
    joiningId.value = space.id;
    try {
        await workspaceApi.join(space.id);
        // 先把最新空间列表写入 localStorage，再整页刷新，
        // 确保顶部导航、页面树等直接读缓存的组件同步到新空间
        await authStore.refreshSpaces();
        window.location.reload();
    } catch (error) {
        joiningId.value = null;
        message.error(error?.response?.data?.message || "Failed to join space");
    }
};

// 退出空间
const leaveSpace = async (space) => {
    try {
        await workspaceApi.leave(space.id);
        // 与加入空间一致：先写入最新空间列表，再整页刷新同步所有缓存
        await authStore.refreshSpaces();
        window.location.reload();
    } catch (error) {
        message.error(error?.response?.data?.message || "Failed to leave space");
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
        isDefault: false,
        isPublic: false
    };
    isCreateModalVisible.value = true;
};

const showEditModal = (space) => {
    const iconType = isImageUrl(space.icon) ? "image" : "gradient";
    editSpace.value = {
        id: space.id,
        name: space.name,
        key: space.key,
        description: space.description || "",
        iconType: space.icon ? iconType : "gradient",
        iconUrl: iconType === "image" ? space.icon || "" : "",
        iconGradient: iconType === "gradient" ? space.icon || presetColors[0].value : presetColors[0].value,
        isDefault: space.isDefault || false,
        isPublic: space.isPublic || false
    };
    isEditModalVisible.value = true;
};

const handleBeforeUpload = async (file) => {
    const isImage = file.type.startsWith("image/");
    if (!isImage) {
        message.error("You can only upload image files!");
        return false;
    }
    const isLt2M = file.size / 1024 / 1024 < 2;
    if (!isLt2M) {
        message.error("Image must smaller than 2MB!");
        return false;
    }

    try {
        const { uploadApi } = await import("../../api");
        const filePath = await uploadApi.upload(file);
        const target = isCreateModalVisible.value ? newSpace.value : editSpace.value;
        target.iconUrl = filePath;
        target.iconType = "image";
        message.success("Image uploaded successfully");
    } catch (error) {
        console.error("Upload failed:", error);
        message.error("Failed to upload image");
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
            isDefault: newSpace.value.isDefault,
            isPublic: newSpace.value.isPublic
        };

        // Handle icon
        if (newSpace.value.iconType === "image" && newSpace.value.iconUrl) {
            data.icon = newSpace.value.iconUrl;
        } else if (newSpace.value.iconType === "gradient") {
            data.icon = newSpace.value.iconGradient;
        }

        await workspaceApi.create(data);
        // 先写入最新空间列表，再整页刷新，避免停留在旧页面（与加入/退出空间一致）
        await authStore.refreshSpaces();
        window.location.reload();
    } catch (error) {
        console.error("Failed to create workspace:", error);
        message.error(error?.message || "Failed to create space");
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
            isDefault: editSpace.value.isDefault,
            isPublic: editSpace.value.isPublic
        };

        // Handle icon
        if (editSpace.value.iconType === "image" && editSpace.value.iconUrl) {
            data.icon = editSpace.value.iconUrl;
        } else if (editSpace.value.iconType === "gradient") {
            data.icon = editSpace.value.iconGradient;
        }

        await workspaceApi.update(editSpace.value.id, data);
        message.success("Space updated successfully");
        isEditModalVisible.value = false;
        await Promise.all([loadMySpaces(), loadDiscover(), authStore.refreshSpaces()]);
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
        // 先写入最新空间列表，再整页刷新，避免停留在旧页面（与加入/退出空间一致）
        await authStore.refreshSpaces();
        window.location.reload();
    } catch (error) {
        console.error("Failed to delete workspace:", error);
        message.error(error?.response?.data?.message || "Failed to delete space");
    }
};

onMounted(() => {
    loadMySpaces();
    loadDiscover();
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

.section {
    margin-bottom: 32px;
}

.section-title {
    font-size: 16px;
    font-weight: 600;
    color: #172b4d;
    margin-bottom: 12px;
}

.space-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(300px, 1fr));
    gap: 16px;
}

.space-card {
    border: 1px solid #dfe1e6;
    border-radius: 8px;
    padding: 16px;
    background: #fff;
    transition: box-shadow 0.2s;
    display: flex;
    flex-direction: column;
    gap: 8px;
}

.space-card:hover {
    box-shadow: 0 4px 12px rgba(9, 30, 66, 0.12);
}

.space-card-head {
    display: flex;
    align-items: center;
    gap: 12px;
    cursor: pointer;
}

.space-icon {
    width: 40px;
    height: 40px;
    border-radius: 6px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 16px;
    font-weight: 600;
}

.space-icon-img {
    width: 40px;
    height: 40px;
    border-radius: 6px;
    flex-shrink: 0;
    object-fit: cover;
}

.space-card-title {
    min-width: 0;
}

.space-name {
    font-weight: 600;
    color: #172b4d;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.space-desc {
    font-size: 13px;
    color: #6b778c;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.space-card-foot {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-top: auto;
}

.foot-actions {
    margin-left: auto;
    display: flex;
    align-items: center;
    gap: 4px;
}

.space-key {
    background-color: #f4f5f7;
    padding: 1px 6px;
    border-radius: 3px;
    font-size: 12px;
    color: #42526e;
    font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', Menlo, monospace;
}

.page-count {
    font-size: 12px;
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

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
    .space-list-container {
        padding: 12px 1rem 0;
    }

    .header-actions h2 {
        font-size: 20px;
    }

    .filter-bar :deep(.ant-input-search) {
        width: 100% !important;
    }

    .space-grid {
        grid-template-columns: 1fr;
    }
}
</style>
