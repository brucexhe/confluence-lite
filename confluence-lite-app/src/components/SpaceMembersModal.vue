<template>
    <a-modal
        :open="open"
        :title="$t('spaceMembers.title')"
        :footer="null"
        :width="isMobile ? '95%' : 860"
        destroy-on-close
        @cancel="close"
    >
        <div class="sm-sub-title" v-if="spaceName">{{ spaceName }}</div>

        <!-- 搜索 + 邀请 -->
        <div class="sm-toolbar">
            <a-input-search
                v-model:value="searchText"
                :placeholder="$t('spaceMembers.filterPlaceholder')"
                style="width: 260px"
                allow-clear
                @search="reload"
            />
            <a-button type="primary" @click="openInviteModal">{{ $t('spaceMembers.invite') }}</a-button>
        </div>

        <!-- 成员表格 -->
        <a-table
            :dataSource="members"
            :columns="columns"
            :rowKey="(record) => record.user.id"
            :loading="loading"
            :pagination="pagination"
            size="small"
            @change="handleTableChange"
        >
            <template #bodyCell="{ column, record }">
                <template v-if="column.key === 'user'">
                    <div class="sm-user-cell">
                        <a-avatar v-if="record.user.avatarUrl" :src="record.user.avatarUrl" size="small" />
                        <a-avatar v-else size="small" style="background-color: #0052cc">
                            {{ (record.user.displayName || record.user.username || '?').charAt(0).toUpperCase() }}
                        </a-avatar>
                        <div>
                            <div class="sm-user-name">
                                {{ record.user.displayName || record.user.username }}
                                <a-tag v-if="record.isOwner" color="geekblue">{{ $t('spaceMembers.owner') }}</a-tag>
                                <a-tag v-else-if="record.adminSpace" color="orange">{{ $t('spaceMembers.admin') }}</a-tag>
                            </div>
                            <div class="sm-user-username">@{{ record.user.username }}</div>
                        </div>
                    </div>
                </template>
                <template v-else-if="column.key === 'permissions'">
                    <div class="sm-perm-tags">
                        <a-tag v-if="record.viewSpace" color="blue">{{ $t('spaceMembers.tagView') }}</a-tag>
                        <a-tag v-if="record.createPage" color="green">{{ $t('spaceMembers.tagCreate') }}</a-tag>
                        <a-tag v-if="record.editPage" color="purple">{{ $t('spaceMembers.tagEdit') }}</a-tag>
                        <a-tag v-if="record.deletePage" color="red">{{ $t('spaceMembers.tagDeleteAny') }}</a-tag>
                        <a-tag v-else-if="record.deleteOwnPage" color="volcano">{{ $t('spaceMembers.tagDeleteOwn') }}</a-tag>
                        <a-tag v-if="record.addComment" color="cyan">{{ $t('spaceMembers.tagComment') }}</a-tag>
                        <a-tag v-if="record.exportPage" color="pink">{{ $t('spaceMembers.tagExport') }}</a-tag>
                        <span v-if="!hasAnyPermission(record) && !record.isOwner" class="sm-no-perm">{{ $t('spaceMembers.noAccess') }}</span>
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
                    <span v-else class="sm-owner-hint">{{ $t('spaceMembers.allPermissions') }}</span>
                </template>
            </template>
        </a-table>

        <!-- 邀请成员子弹窗 -->
        <a-modal
            v-model:open="inviteVisible"
            :title="$t('spaceMembers.invite')"
            :okText="$t('spaceMembers.invite')"
            :cancelText="$t('common.cancel')"
            :confirmLoading="inviting"
            :width="isMobile ? '95%' : 520"
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
                    <div class="sm-form-hint">{{ $t('spaceMembers.searchHint') }}</div>
                </a-form-item>
                <a-form-item :label="$t('spaceMembers.permissions')">
                    <div class="sm-perm-grid">
                        <div v-for="p in permissionItems" :key="p.key" class="sm-perm-item">
                            <a-checkbox v-model:checked="invitePerms[p.key]">{{ p.label }}</a-checkbox>
                        </div>
                    </div>
                    <div class="sm-form-hint">{{ $t('spaceMembers.viewHint') }}</div>
                </a-form-item>
            </a-form>
        </a-modal>

        <!-- 编辑权限子弹窗 -->
        <a-modal
            v-model:open="editVisible"
            :title="$t('spaceMembers.memberPermissions')"
            :okText="$t('common.save')"
            :cancelText="$t('common.cancel')"
            :confirmLoading="updating"
            :width="isMobile ? '95%' : 520"
        >
            <div class="sm-edit-user" v-if="editingMember">
                <a-avatar v-if="editingMember.user.avatarUrl" :src="editingMember.user.avatarUrl" size="small" />
                <a-avatar v-else size="small" style="background-color: #0052cc">
                    {{ (editingMember.user.displayName || editingMember.user.username || '?').charAt(0).toUpperCase() }}
                </a-avatar>
                <b>{{ editingMember.user.displayName || editingMember.user.username }}</b>
                <span class="sm-user-username">@{{ editingMember.user.username }}</span>
            </div>
            <a-form layout="vertical" style="margin-top: 1rem">
                <a-form-item :label="$t('spaceMembers.permissions')">
                    <div class="sm-perm-grid">
                        <div v-for="p in permissionItems" :key="p.key" class="sm-perm-item">
                            <a-checkbox v-model:checked="editPerms[p.key]">{{ p.label }}</a-checkbox>
                        </div>
                    </div>
                </a-form-item>
            </a-form>
        </a-modal>
    </a-modal>
</template>

<script setup>
import { ref, reactive, computed, watch, onUnmounted } from "vue";
import { message } from "ant-design-vue";
import { useI18n } from "vue-i18n";
import { workspaceApi } from "../api";

const props = defineProps({
    open: { type: Boolean, default: false },
    workspaceId: { type: Number, default: null },
    spaceName: { type: String, default: "" }
});

const emit = defineEmits(["update:open", "changed"]);

const { t } = useI18n();

const isMobile = ref(false);
function checkMobile() {
    isMobile.value = window.innerWidth <= 768;
}
onUnmounted(() => { window.removeEventListener("resize", checkMobile); });

// 弹窗打开时加载成员列表
watch(() => props.open, (visible) => {
    if (visible) {
        checkMobile();
        window.addEventListener("resize", checkMobile);
        reload();
    }
});

const loading = ref(false);
const members = ref([]);
const searchText = ref("");
const pagination = reactive({ current: 1, pageSize: 10, total: 0 });

// 列定义与权限项用 computed 包装，切换语言时响应式更新
const columns = computed(() => [
    { title: t("spaceMembers.colMember"), key: "user" },
    { title: t("spaceMembers.colPermissions"), key: "permissions" },
    { title: t("spaceMembers.colAction"), key: "action", width: "180px" },
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

const close = () => emit("update:open", false);

const loadMembers = async () => {
    if (!props.workspaceId) return;
    loading.value = true;
    try {
        const response = await workspaceApi.getMembers(
            props.workspaceId,
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

const reload = () => {
    pagination.current = 1;
    loadMembers();
};

const handleTableChange = (pag) => {
    pagination.current = pag.current;
    pagination.pageSize = pag.pageSize;
    loadMembers();
};

// 搜索可邀请的用户
let searchTimer = null;
const handleUserSearch = (keyword) => {
    clearTimeout(searchTimer);
    searchTimer = setTimeout(async () => {
        if (!props.workspaceId || !keyword || keyword.trim().length < 1) {
            userOptions.value = [];
            return;
        }
        try {
            const users = await workspaceApi.searchMembers(props.workspaceId, keyword.trim());
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
            await workspaceApi.addMember(props.workspaceId, { userId, ...invitePerms });
        }
        message.success(t("spaceMembers.inviteSuccess"));
        inviteVisible.value = false;
        emit("changed");
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
        await workspaceApi.updateMember(props.workspaceId, editingMember.value.user.id, {
            userId: editingMember.value.user.id,
            ...editPerms
        });
        message.success(t("spaceMembers.updateSuccess"));
        editVisible.value = false;
        emit("changed");
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || t("spaceMembers.updateFailed"));
    } finally {
        updating.value = false;
    }
};

const removeMember = async (record) => {
    try {
        await workspaceApi.removeMember(props.workspaceId, record.user.id);
        message.success(t("spaceMembers.removeSuccess"));
        emit("changed");
        await loadMembers();
    } catch (error) {
        message.error(error?.response?.data?.message || t("spaceMembers.removeFailed"));
    }
};
</script>

<style scoped>
.sm-sub-title {
    font-size: 13px;
    color: #6b778c;
    margin-bottom: 12px;
}

.sm-toolbar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 12px;
    gap: 8px;
    flex-wrap: wrap;
}

.sm-user-cell {
    display: flex;
    align-items: center;
    gap: 8px;
}

.sm-user-name {
    font-weight: 500;
    color: #172b4d;
    display: flex;
    align-items: center;
    gap: 6px;
}

.sm-user-username {
    font-size: 12px;
    color: #6b778c;
}

.sm-perm-tags {
    display: flex;
    flex-wrap: wrap;
    gap: 4px;
}

.sm-no-perm,
.sm-owner-hint {
    font-size: 12px;
    color: #6b778c;
}

.sm-perm-grid {
    display: grid;
    grid-template-columns: repeat(2, minmax(0, 1fr));
    gap: 8px;
}

.sm-edit-user {
    display: flex;
    align-items: center;
    gap: 10px;
}

.sm-form-hint {
    font-size: 12px;
    color: #6b778c;
    margin-top: 4px;
}

@media (max-width: 768px) {
    .sm-perm-grid {
        grid-template-columns: 1fr;
    }
}
</style>
