<template>
    <div class="members-container">
        <div class="header-actions">
            <div>
                <h2>{{ $t('spaceMembers.title') }}</h2>
                <div class="sub-title" v-if="spaceName">{{ spaceName }}</div>
            </div>
            <a-button type="primary" @click="openInviteModal">{{ $t('spaceMembers.invite') }}</a-button>
        </div>

        <a-alert v-if="!isAdmin" type="warning" show-icon :message="$t('spaceMembers.noPermission')" />
        <template v-else>
            <!-- Search -->
            <div class="filter-bar">
                <a-input-search
                    v-model:value="searchText"
                    :placeholder="$t('spaceMembers.filterPlaceholder')"
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
                                    <a-tag v-if="record.isOwner" color="geekblue">{{ $t('spaceMembers.owner') }}</a-tag>
                                    <a-tag v-else-if="record.adminSpace" color="orange">{{ $t('spaceMembers.admin') }}</a-tag>
                                </div>
                                <div class="user-username">@{{ record.user.username }}</div>
                            </div>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'permissions'">
                        <div class="perm-tags">
                            <a-tag v-if="record.viewSpace" color="blue">{{ $t('spaceMembers.tagView') }}</a-tag>
                            <a-tag v-if="record.createPage" color="green">{{ $t('spaceMembers.tagCreate') }}</a-tag>
                            <a-tag v-if="record.editPage" color="purple">{{ $t('spaceMembers.tagEdit') }}</a-tag>
                            <a-tag v-if="record.deletePage" color="red">{{ $t('spaceMembers.tagDeleteAny') }}</a-tag>
                            <a-tag v-else-if="record.deleteOwnPage" color="volcano">{{ $t('spaceMembers.tagDeleteOwn') }}</a-tag>
                            <a-tag v-if="record.addComment" color="cyan">{{ $t('spaceMembers.tagComment') }}</a-tag>
                            <a-tag v-if="record.exportPage" color="pink">{{ $t('spaceMembers.tagExport') }}</a-tag>
                            <span v-if="!hasAnyPermission(record) && !record.isOwner" class="no-perm">{{ $t('spaceMembers.noAccess') }}</span>
                        </div>
                    </template>
                    <template v-else-if="column.key === 'action'">
                        <template v-if="!record.isOwner">
                            <a-button type="link" size="small" @click="openEditModal(record)">{{ $t('spaceMembers.permissions') }}</a-button>
                            <a-popconfirm
                                :title="$t('spaceMembers.confirmRemove')"
                                :ok-text="$t('common.yes')"
                                :cancel-text="$t('common.no')"
                                @confirm="removeMember(record)"
                            >
                                <a-button type="link" danger size="small">{{ $t('spaceMembers.remove') }}</a-button>
                            </a-popconfirm>
                        </template>
                        <span v-else class="owner-hint">{{ $t('spaceMembers.allPermissions') }}</span>
                    </template>
                </template>
            </a-table>
        </template>

        <!-- Invite Modal -->
        <a-modal
            v-model:open="inviteVisible"
            :title="$t('spaceMembers.invite')"
            @ok="handleInvite"
            :okText="$t('spaceMembers.invite')"
            :cancelText="$t('common.cancel')"
            :confirmLoading="inviting"
            :width="isMobile ? '95%' : 560"
        >
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item :label="$t('spaceMembers.searchUsers')" required>
                    <a-select
                        v-model:value="inviteUserIds"
                        mode="multiple"
                        :placeholder="$t('spaceMembers.searchPlaceholder')"
                        :filter-option="false"
                        :options="userOptions"
                        @search="handleUserSearch"
                        style="width: 100%"
                    />
                    <div class="form-hint">{{ $t('spaceMembers.searchHint') }}</div>
                </a-form-item>
                <a-form-item :label="$t('spaceMembers.permissions')">
                    <div class="perm-grid">
                        <div v-for="p in permissionItems" :key="p.key" class="perm-item">
                            <a-checkbox v-model:checked="invitePerms[p.key]">{{ p.label }}</a-checkbox>
                        </div>
                    </div>
                    <div class="form-hint">{{ $t('spaceMembers.viewHint') }}</div>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- Edit Permissions Modal -->
        <a-modal
            v-model:open="editVisible"
            :title="$t('spaceMembers.memberPermissions')"
            @ok="handleUpdatePerms"
            :okText="$t('common.save')"
            :cancelText="$t('common.cancel')"
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
                <a-form-item :label="$t('spaceMembers.permissions')">
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
import { useI18n } from "vue-i18n";
import { workspaceApi } from "../../api";

const route = useRoute();
const { t } = useI18n();

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

// 列定义与权限项均用 computed 包装，切换语言时响应式更新
const columns = computed(() => [
    { title: t("spaceMembers.colMember"), key: "user" },
    { title: t("spaceMembers.colPermissions"), key: "permissions" },
    { title: t("spaceMembers.colAction"), key: "action", width: "220px" },
]);

const permissionItems = computed(() => [
    { key: "viewSpace", label: t("spaceMembers.permViewSpace") },
    { key: "createPage", label: t("spaceMembers.permCreatePage") },
    { key: "editPage", label: t("spaceMembers.permEditPage") },
    { key: "deletePage", label: t("spaceMembers.permDeletePage") },
    { key: "deleteOwnPage", label: t("spaceMembers.permDeleteOwnPage") },
    { key: "addComment", label: t("spaceMembers.permAddComment") },
    { key: "deleteComment", label: t("spaceMembers.permDeleteComment") },
    { key: "exportPage", label: t("spaceMembers.permExportPage") },
    { key: "adminSpace", label: t("spaceMembers.permAdminSpace") },
    { key: "setPermissions", label: t("spaceMembers.permSetPermissions") },
]);

const defaultPerms = () => {
    const perms = {};
    permissionItems.value.forEach((p) => { perms[p.key] = false; });
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
    return permissionItems.value.some((p) => record[p.key]);
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
        message.warning(t("spaceMembers.selectUserRequired"));
        return;
    }
    inviting.value = true;
    try {
        for (const userId of inviteUserIds.value) {
            await workspaceApi.addMember(workspaceId.value, { userId, ...invitePerms });
        }
        message.success(t("spaceMembers.inviteSuccess"));
        inviteVisible.value = false;
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || t("spaceMembers.inviteFailed"));
    } finally {
        inviting.value = false;
    }
};

const openEditModal = (record) => {
    editingMember.value = record;
    permissionItems.value.forEach((p) => {
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
        message.success(t("spaceMembers.updateSuccess"));
        editVisible.value = false;
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || t("spaceMembers.updateFailed"));
    } finally {
        updating.value = false;
    }
};

const removeMember = async (record) => {
    try {
        await workspaceApi.removeMember(workspaceId.value, record.user.id);
        message.success(t("spaceMembers.removeSuccess"));
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || t("spaceMembers.removeFailed"));
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
