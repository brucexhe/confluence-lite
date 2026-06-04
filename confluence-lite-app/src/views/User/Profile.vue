<template>
    <div class="profile-page">
        <div class="profile-header">
            <h1 class="profile-title">{{ $t('profile.title') }}</h1>
            <p class="profile-desc">{{ $t('profile.description') }}</p>
        </div>

        <div class="profile-body">
            <a-tabs v-model:activeKey="activeTab" class="profile-tabs">
                <!-- 基本资料 -->
                <a-tab-pane key="basic" :tab="$t('profile.basicInfo')">
                    <div class="tab-content">
                        <a-spin :spinning="loading">
                            <div class="profile-layout">
                                <!-- 左侧头像 -->
                                <div class="avatar-section">
                                    <div class="avatar-wrapper">
                                        <UserAvatar :user="userForm" :size="120" shape="square" class="avatar-img" />
                                        <div class="avatar-overlay" @click="triggerAvatarUpload">
                                            <Camera :size="20" />
                                            <span>{{ $t('profile.changeAvatar') }}</span>
                                        </div>
                                    </div>
                                    <input
                                        ref="avatarInput"
                                        type="file"
                                        accept="image/*"
                                        style="display: none"
                                        @change="handleAvatarChange"
                                    />
                                    <div class="avatar-tips">
                                        <p>{{ $t('profile.avatarTip1') }}</p>
                                        <p>{{ $t('profile.avatarTip2') }}</p>
                                    </div>
                                </div>

                                <!-- 右侧表单 -->
                                <div class="form-section">
                                    <a-form :model="userForm" layout="vertical" @finish="handleUpdateProfile">
                                        <a-form-item :label="$t('profile.username')" name="username">
                                            <a-input
                                                v-model:value="userForm.username"
                                                disabled
                                                class="premium-input"
                                            ></a-input>
                                            <div class="field-hint">{{ $t('profile.usernameHint') }}</div>
                                        </a-form-item>

                                        <a-form-item
                                            :label="$t('profile.displayName')"
                                            name="displayName"
                                            :rules="[{ required: true, message: t('profile.displayNameRequired') }]"
                                        >
                                            <a-input
                                                v-model:value="userForm.displayName"
                                                :placeholder="$t('profile.displayNamePlaceholder')"
                                                class="premium-input"
                                            ></a-input>
                                        </a-form-item>

                                        <a-form-item
                                            :label="$t('profile.email')"
                                            name="email"
                                            :rules="[{ type: 'email', message: t('profile.emailInvalid') }]"
                                        >
                                            <a-input
                                                v-model:value="userForm.email"
                                                placeholder="email@example.com"
                                                class="premium-input"
                                            ></a-input>
                                        </a-form-item>

                                        <a-form-item class="form-actions">
                                            <a-button
                                                type="primary"
                                                html-type="submit"
                                                :loading="saving"
                                                class="premium-btn"
                                            >
                                                {{ $t('profile.saveBasicInfo') }}
                                            </a-button>
                                        </a-form-item>
                                    </a-form>
                                </div>
                            </div>
                        </a-spin>
                    </div>
                </a-tab-pane>

                <!-- 安全设置 -->
                <a-tab-pane key="security" :tab="$t('profile.securitySettings')">
                    <div class="tab-content security-tab">
                        <div class="section-info">
                            <h3>{{ $t('profile.changePassword') }}</h3>
                            <p>{{ $t('profile.changePasswordHint') }}</p>
                        </div>

                        <a-form
                            :model="passwordForm"
                            layout="vertical"
                            @finish="handleChangePassword"
                            class="security-form"
                        >
                            <a-form-item
                                :label="$t('profile.currentPassword')"
                                name="oldPassword"
                                :rules="[{ required: true, message: t('profile.currentPasswordRequired') }]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.oldPassword"
                                    :placeholder="$t('profile.verifyIdentity')"
                                    class="premium-input"
                                >
                                    <template #prefix><Lock :size="16" class="input-icon" /></template>
                                </a-input-password>
                            </a-form-item>

                            <a-divider />

                            <a-form-item
                                :label="$t('profile.newPassword')"
                                name="newPassword"
                                :rules="[
                                    { required: true, message: t('profile.newPasswordRequired') },
                                    { min: 6, message: t('profile.passwordMinLength') },
                                ]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.newPassword"
                                    :placeholder="$t('profile.setNewPassword')"
                                    class="premium-input"
                                >
                                    <template #prefix><Key :size="16" class="input-icon" /></template>
                                </a-input-password>
                            </a-form-item>

                            <a-form-item
                                :label="$t('profile.confirmNewPassword')"
                                name="confirmPassword"
                                :rules="[
                                    { required: true, message: t('profile.confirmNewPasswordRequired') },
                                    { validator: validateConfirmPassword },
                                ]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.confirmPassword"
                                    :placeholder="$t('profile.repeatNewPassword')"
                                    class="premium-input"
                                >
                                    <template #prefix><Key :size="16" class="input-icon" /></template>
                                </a-input-password>
                            </a-form-item>

                            <a-form-item class="form-actions">
                                <a-button
                                    type="primary"
                                    html-type="submit"
                                    :loading="changingPassword"
                                    class="premium-btn"
                                >
                                    {{ $t('profile.updatePassword') }}
                                </a-button>
                            </a-form-item>
                        </a-form>
                    </div>
                </a-tab-pane>
            </a-tabs>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, reactive } from "vue";
import { message } from "ant-design-vue";
import { useI18n } from "vue-i18n";
import { User, Mail, Lock, Key, Camera, Smile, Upload } from "lucide-vue-next";
import { userApi, uploadApi } from "@/api";
import { useAuthStore } from "@/store/auth";
import UserAvatar from "@/components/UserAvatar.vue";

const { t } = useI18n();
const authStore = useAuthStore();
const activeTab = ref("basic");
const loading = ref(false);
const saving = ref(false);
const changingPassword = ref(false);
const uploading = ref(false);
const avatarInput = ref(null);

// 用户信息表单
const userForm = reactive({
    id: null,
    username: "",
    displayName: "",
    email: "",
    avatarUrl: "",
});

// 密码修改表单
const passwordForm = reactive({
    oldPassword: "",
    newPassword: "",
    confirmPassword: "",
});

// 加载当前用户信息
const loadUserData = async () => {
    loading.value = true;
    try {
        const data = await userApi.getMe();
        if (data) {
            userForm.id = data.id;
            userForm.username = data.username;
            userForm.displayName = data.displayName || "";
            userForm.email = data.email || "";
            userForm.avatarUrl = data.avatarUrl || "";
        }
    } catch (error) {
        console.error("Failed to load user info:", error);
        message.error(t('profile.loadUserFailed'));
    } finally {
        loading.value = false;
    }
};

// 保存个人资料
const handleUpdateProfile = async () => {
    saving.value = true;
    try {
        const updateData = {
            displayName: userForm.displayName,
            email: userForm.email,
            avatarUrl: userForm.avatarUrl,
        };
        const result = await userApi.update(userForm.id, updateData);

        // 更新本地 store
        if (authStore.user) {
            authStore.user.name = userForm.displayName;
            authStore.user.avatarUrl = userForm.avatarUrl;
            // 持久化到 localStorage
            localStorage.setItem("auth_user", JSON.stringify(authStore.user));
        }

        message.success(t('profile.profileUpdated'));
    } catch (error) {
        console.error("Failed to update profile:", error);
        message.error(error.message || t('profile.updateFailed'));
    } finally {
        saving.value = false;
    }
};

// 修改密码
const handleChangePassword = async () => {
    changingPassword.value = true;
    try {
        await userApi.changePassword({
            oldPassword: passwordForm.oldPassword,
            newPassword: passwordForm.newPassword,
        });
        message.success(t('profile.passwordChanged'));
        // 重置表单
        passwordForm.oldPassword = "";
        passwordForm.newPassword = "";
        passwordForm.confirmPassword = "";
    } catch (error) {
        console.error("Failed to change password:", error);
        message.error(error.message || t('profile.passwordChangeFailed'));
    } finally {
        changingPassword.value = false;
    }
};

// 密码确认验证
const validateConfirmPassword = async (_rule, value) => {
    if (value && value !== passwordForm.newPassword) {
        throw new Error(t('profile.passwordMismatch'));
    }
};

// 触发头像上传
const triggerAvatarUpload = () => {
    avatarInput.value?.click();
};

// 处理头像文件选择
const handleAvatarChange = async (event) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!file.type.startsWith("image/")) {
        message.error(t('profile.selectImageFile'));
        return;
    }

    if (file.size > 2 * 1024 * 1024) {
        message.error(t('profile.imageTooLarge'));
        return;
    }

    uploading.value = true;
    try {
        // 使用通用上传接口
        const url = await uploadApi.upload(file);
        userForm.avatarUrl = url;
        message.success(t('profile.avatarUploadSuccess'));
    } catch (error) {
        console.error("Failed to upload avatar:", error);
        message.error(t('profile.avatarUploadFailed'));
    } finally {
        uploading.value = false;
        // 清空 input 方便下次选择同一文件
        if (avatarInput.value) avatarInput.value.value = "";
    }
};

onMounted(() => {
    loadUserData();
});
</script>

<style scoped>
.profile-page {
    padding: 20px 2rem;
    min-height: 100vh;
}

.profile-header {
    margin-bottom: 32px;
}

.profile-title {
    font-size: 28px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 4px 0;
    letter-spacing: -0.01em;
}

.profile-desc {
    font-size: 14px;
    color: #6b778c;
    margin: 0;
}

.profile-tabs :deep(.ant-tabs-nav) {
    margin-bottom: 0;
}

.tab-content {
    padding: 32px 0;
    min-height: 400px;
}

/* 基本资料布局 */
.profile-layout {
    display: flex;
    gap: 60px;
}

.avatar-section {
    display: flex;
    flex-direction: column;
    align-items: center;
    width: 160px;
    flex-shrink: 0;
}

.avatar-wrapper {
    position: relative;
    width: 120px;
    height: 120px;
    border-radius: 3px;
    overflow: hidden;
    background: #f4f5f7;
    border: 1px solid #dfe1e6;
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    color: #6b778c;
}

.avatar-img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.avatar-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background: rgba(9, 30, 66, 0.4);
    display: flex;
    flex-direction: column;
    align-items: center;
    justify-content: center;
    color: white;
    opacity: 0;
    transition: opacity 0.2s;
    gap: 4px;
}

.avatar-overlay span {
    font-size: 12px;
    font-weight: 500;
}

.avatar-wrapper:hover .avatar-overlay {
    opacity: 1;
}

.avatar-tips {
    margin-top: 12px;
    text-align: center;
}

.avatar-tips p {
    font-size: 12px;
    color: #6b778c;
    margin: 2px 0;
}

.form-section {
    flex: 1;
    max-width: 480px;
}

.form-section :deep(.ant-form-item-label > label) {
    font-weight: 600;
    color: #42526e;
    font-size: 12px;
    text-transform: uppercase;
}

.field-hint {
    font-size: 12px;
    color: #6b778c;
    margin-top: 4px;
}

/* 安全设置 */
.security-tab {
    max-width: 500px;
}

.section-info {
    margin-bottom: 32px;
}

.section-info h3 {
    font-size: 18px;
    font-weight: 700;
    margin-bottom: 4px;
}

.section-info p {
    color: var(--color-text-muted);
    font-size: 14px;
}

.security-form :deep(.ant-divider) {
    margin: 24px 0;
}

.form-actions {
    margin-top: 32px;
}

.input-icon {
    color: var(--color-text-muted);
    margin-right: 4px;
}

/* 响应式调整 */
@media (max-width: 768px) {
    .profile-layout {
        flex-direction: column;
        gap: 40px;
        align-items: center;
    }

    .form-section {
        width: 100%;
    }

    .tab-content {
        padding: 24px;
    }
}
</style>
