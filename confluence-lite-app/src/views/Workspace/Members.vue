<template>
    <div class="members-container">
        <div class="header-actions">
            <div>
                <h2>Space members</h2>
                <div class="sub-title" v-if="spaceName">{{ spaceName }}</div>
            </div>
            <a-button type="primary" @click="openInviteModal">Invite members</a-button>
        </div>

        <a-alert v-if="!isAdmin" type="warning" show-icon message="You need space admin permission to manage members" />
        <template v-else>
            <!-- Search -->
            <div class="filter-bar">
                <a-input-search
                    v-model:value="searchText"
                    placeholder="Filter by name or email..."
                    style="width: 300px"
                    allow-clear
                    @search="loadMembers"
                />
            </div>

            <!-- Members Table -->
            <a-table
                :dataSource="members"
                :columns="columns"
                :rowKey="(record) => record.user.id"
                :loading="loading"
                :pagination="pagination"
                @change="handleTableChange"
            >
                <template #bodyCell="{ column, record }">
                    <template v-if="column.key === 'user'">
                        <div class="user-cell">
                            <a-avatar v-if="record.user.avatarUrl" :src="record.user.avatarUrl" size="small" />
                            <a-avatar v-else size="small" style="background-color: #0052cc">
                                {{ (record.user.displayName || record.user.username || '?').charAt(0).toUpperCase() }}
                            </a-avatar>
                            <div>
                                <div class="user-name">
                                    {{ record.user.displayName || record.user.username }}
                                    <a-tag v-if="record.isOwner" color="geekblue">Owner</a-tag>
                                    <a-tag v-else-if="record.adminSpace" color="orange">Admin</a-tag>
                                </div>
                                <div class="user-username">@{{ record.user.username }}</div>
                            </div>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'permissions'">
                        <div class="perm-tags">
                            <a-tag v-if="record.viewSpace" color="blue">View</a-tag>
                            <a-tag v-if="record.createPage" color="green">Create</a-tag>
                            <a-tag v-if="record.editPage" color="purple">Edit</a-tag>
                            <a-tag v-if="record.deletePage" color="red">Delete any</a-tag>
                            <a-tag v-else-if="record.deleteOwnPage" color="volcano">Delete own</a-tag>
                            <a-tag v-if="record.addComment" color="cyan">Comment</a-tag>
                            <a-tag v-if="record.exportPage" color="pink">Export</a-tag>
                            <span v-if="!hasAnyPermission(record) && !record.isOwner" class="no-perm">No access</span>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <template v-if="!record.isOwner">
                            <a-button type="link" size="small" @click="openEditModal(record)">Permissions</a-button>
                            <a-popconfirm
                                title="Remove this member from the space?"
                                ok-text="Yes"
                                cancel-text="No"
                                @confirm="removeMember(record)"
                            >
                                <a-button type="link" danger size="small">Remove</a-button>
                            </a-popconfirm>
                        </template>
                        <span v-else class="owner-hint">All permissions</span>
                    </template>
                </template>
            </a-table>
        </template>

        <!-- Invite Modal -->
        <a-modal
            v-model:open="inviteVisible"
            title="Invite members"
            @ok="handleInvite"
            okText="Invite"
            cancelText="Cancel"
            :confirmLoading="inviting"
            :width="isMobile ? '95%' : 560"
        >
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item label="Search users" required>
                    <a-select
                        v-model:value="inviteUserIds"
                        mode="multiple"
                        placeholder="Search by username, display name or email..."
                        :filter-option="false"
                        :options="userOptions"
                        @search="handleUserSearch"
                        style="width: 100%"
                    />
                    <div class="form-hint">Only users not already in this space are listed</div>
                </a-form-item>
                <a-form-item label="Permissions">
                    <div class="perm-grid">
                        <div v-for="p in permissionItems" :key="p.key" class="perm-item">
                            <a-checkbox v-model:checked="invitePerms[p.key]">{{ p.label }}</a-checkbox>
                        </div>
                    </div>
                    <div class="form-hint">New members need at least "View space" to see the space</div>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- Edit Permissions Modal -->
        <a-modal
            v-model:open="editVisible"
            title="Member permissions"
            @ok="handleUpdatePerms"
            okText="Save"
            cancelText="Cancel"
            :confirmLoading="updating"
            :width="isMobile ? '95%' : 560"
        >
            <div class="edit-user" v-if="editingMember">
                <a-avatar v-if="editingMember.user.avatarUrl" :src="editingMember.user.avatarUrl" size="small" />
                <a-avatar v-else size="small" style="background-color: #0052cc">
                    {{ (editingMember.user.displayName || editingMember.user.username || '?').charAt(0).toUpperCase() }}
                </a-avatar>
                <b>{{ editingMember.user.displayName || editingMember.user.username }}</b>
                <span class="user-username">@{{ editingMember.user.username }}</span>
            </div>
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item label="Permissions">
                    <div class="perm-grid">
                        <div v-for="p in permissionItems" :key="p.key" class="perm-item">
                            <a-checkbox v-model:checked="editPerms[p.key]">{{ p.label }}</a-checkbox>
                        </div>
                    </div>
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted, onUnmounted } from "vue";
import { useRoute } from "vue-router";
import { message } from "ant-design-vue";
import { workspaceApi } from "../../api";

const route = useRoute();

const isMobile = ref(false);
function checkMobile() {
    isMobile.value = window.innerWidth <= 768;
}
onMounted(() => { checkMobile(); window.addEventListener("resize", checkMobile); });
onUnmounted(() => { window.removeEventListener("resize", checkMobile); });

// 从 localStorage 找到当前空间
const space = computed(() => {
    const spaces = JSON.parse(localStorage.getItem("auth_spaces") || "[]");
    const key = route.params.spaceKey;
    return spaces.find((s) => s.key === key || s.key === key?.toUpperCase()) || null;
});
const spaceName = computed(() => space.value?.name || route.params.spaceKey || "");
const workspaceId = computed(() => space.value?.id || null);

const isAdmin = ref(false);
const loading = ref(false);
const members = ref([]);
const searchText = ref("");
const pagination = reactive({ current: 1, pageSize: 20, total: 0 });

const columns = [
    { title: "Member", key: "user" },
    { title: "Permissions", key: "permissions" },
    { title: "Action", key: "action", width: "220px" },
];

const permissionItems = [
    { key: "viewSpace", label: "View space" },
    { key: "createPage", label: "Create page" },
    { key: "editPage", label: "Edit page" },
    { key: "deletePage", label: "Delete any page" },
    { key: "deleteOwnPage", label: "Delete own page" },
    { key: "addComment", label: "Add comment" },
    { key: "deleteComment", label: "Delete comment" },
    { key: "exportPage", label: "Export page" },
    { key: "adminSpace", label: "Admin space" },
    { key: "setPermissions", label: "Set permissions" },
];

const defaultPerms = () => {
    const perms = {};
    permissionItems.forEach((p) => { perms[p.key] = false; });
    perms.viewSpace = true;
    return perms;
};

const inviteVisible = ref(false);
const inviting = ref(false);
const inviteUserIds = ref([]);
const userOptions = ref([]);
const invitePerms = reactive(defaultPerms());

const editVisible = ref(false);
const updating = ref(false);
const editingMember = ref(null);
const editPerms = reactive(defaultPerms());

const hasAnyPermission = (record) => {
    return permissionItems.some((p) => record[p.key]);
};

const loadMembers = async () => {
    if (!workspaceId.value) return;
    loading.value = true;
    try {
        const response = await workspaceApi.getMembers(
            workspaceId.value,
            pagination.current,
            pagination.pageSize,
            searchText.value || ""
        );
        members.value = response?.items || [];
        pagination.total = response?.total || 0;
    } catch (error) {
        members.value = [];
        pagination.total = 0;
    } finally {
        loading.value = false;
    }
};

const handleTableChange = (pag) => {
    pagination.current = pag.current;
    pagination.pageSize = pag.pageSize;
    loadMembers();
};

// 加载我的权限，非空间管理员不展示管理界面
const loadMyPermissions = async () => {
    if (!workspaceId.value) return;
    try {
        const perms = await workspaceApi.getMyPermissions(workspaceId.value);
        isAdmin.value = !!perms?.isSpaceAdmin;
    } catch {
        isAdmin.value = false;
    }
};

// 搜索可邀请的用户
let searchTimer = null;
const handleUserSearch = (keyword) => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(async () => {
        if (!workspaceId.value || !keyword || keyword.trim().length < 1) {
            userOptions.value = [];
            return;
        }
        try {
            const users = await workspaceApi.searchMembers(workspaceId.value, keyword.trim());
            userOptions.value = (users || []).map((u) => ({
                label: `${u.displayName || u.username} (@${u.username})`,
                value: u.id
            }));
        } catch {
            userOptions.value = [];
        }
    }, 300);
};

const openInviteModal = () => {
    inviteUserIds.value = [];
    userOptions.value = [];
    Object.assign(invitePerms, defaultPerms());
    inviteVisible.value = true;
};

const handleInvite = async () => {
    if (inviteUserIds.value.length === 0) {
        message.warning("Please select at least one user.");
        return;
    }
    inviting.value = true;
    try {
        for (const userId of inviteUserIds.value) {
            await workspaceApi.addMember(workspaceId.value, { userId, ...invitePerms });
        }
        message.success("Members invited");
        inviteVisible.value = false;
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || "Failed to invite members");
    } finally {
        inviting.value = false;
    }
};

const openEditModal = (record) => {
    editingMember.value = record;
    permissionItems.forEach((p) => {
        editPerms[p.key] = !!record[p.key];
    });
    editVisible.value = true;
};

const handleUpdatePerms = async () => {
    if (!editingMember.value) return;
    updating.value = true;
    try {
        await workspaceApi.updateMember(workspaceId.value, editingMember.value.user.id, {
            userId: editingMember.value.user.id,
            ...editPerms
        });
        message.success("Permissions updated");
        editVisible.value = false;
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || "Failed to update permissions");
    } finally {
        updating.value = false;
    }
};

const removeMember = async (record) => {
    try {
        await workspaceApi.removeMember(workspaceId.value, record.user.id);
        message.success("Member removed");
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || "Failed to remove member");
    }
};

onMounted(async () => {
    await loadMyPermissions();
    if (isAdmin.value) {
        await loadMembers();
    }
});
</script>

<style scoped>
.members-container {
    max-width: 1100px;
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

.sub-title {
    font-size: 13px;
    color: #6b778c;
    margin-top: 2px;
}

.filter-bar {
    margin-bottom: 16px;
}

.user-cell {
    display: flex;
    align-items: center;
    gap: 10px;
}

.user-name {
    font-weight: 500;
    color: #172b4d;
    display: flex;
    align-items: center;
    gap: 6px;
}

.user-username {
    font-size: 12px;
    color: #6b778c;
}

.perm-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.no-perm {
    font-size: 12px;
    color: #6b778c;
}

.owner-hint {
    font-size: 12px;
    color: #6b778c;
}

.perm-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
}

.edit-user {
    display: flex;
    align-items: center;
    gap: 10px;
}

.form-hint {
    font-size: 12px;
    color: #6b778c;
    margin-top: 4px;
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

@media (max-width: 768px) {
    .members-container {
        padding: 12px 1rem 0;
    }

    .header-actions h2 {
        font-size: 20px;
    }

    .perm-grid {
        grid-template-columns: 1fr;
    }
}
</style>
