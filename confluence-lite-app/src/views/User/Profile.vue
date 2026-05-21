<template>
    <div class="profile-page">
        <div class="profile-header">
            <h1 class="profile-title">个人中心</h1>
            <p class="profile-desc">管理您的个人账号信息与安全设置</p>
        </div>

        <div class="profile-body">
            <a-tabs v-model:activeKey="activeTab" class="profile-tabs">
                <!-- 基本资料 -->
                <a-tab-pane key="basic" tab="基本资料">
                    <div class="tab-content">
                        <a-spin :spinning="loading">
                            <div class="profile-layout">
                                <!-- 左侧头像 -->
                                <div class="avatar-section">
                                    <div class="avatar-wrapper">
                                        <UserAvatar :user="userForm" :size="120" shape="square" class="avatar-img" />
                                        <div class="avatar-overlay" @click="triggerAvatarUpload">
                                            <Camera :size="20" />
                                            <span>更换头像</span>
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
                                        <p>支持 JPG, PNG 格式</p>
                                        <p>建议尺寸 200x200，且不超过 2MB</p>
                                    </div>
                                </div>

                                <!-- 右侧表单 -->
                                <div class="form-section">
                                    <a-form :model="userForm" layout="vertical" @finish="handleUpdateProfile">
                                        <a-form-item label="用户名" name="username">
                                            <a-input
                                                v-model:value="userForm.username"
                                                disabled
                                                class="premium-input"
                                            ></a-input>
                                            <div class="field-hint">用户名作为唯一标识，不可修改</div>
                                        </a-form-item>

                                        <a-form-item
                                            label="昵称"
                                            name="displayName"
                                            :rules="[{ required: true, message: '请输入昵称' }]"
                                        >
                                            <a-input
                                                v-model:value="userForm.displayName"
                                                placeholder="起个好听的名字吧"
                                                class="premium-input"
                                            ></a-input>
                                        </a-form-item>

                                        <a-form-item
                                            label="电子邮箱"
                                            name="email"
                                            :rules="[{ type: 'email', message: '请输入正确的邮箱格式' }]"
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
                                                保存基本信息
                                            </a-button>
                                        </a-form-item>
                                    </a-form>
                                </div>
                            </div>
                        </a-spin>
                    </div>
                </a-tab-pane>

                <!-- 安全设置 -->
                <a-tab-pane key="security" tab="安全设置">
                    <div class="tab-content security-tab">
                        <div class="section-info">
                            <h3>修改密码</h3>
                            <p>定期更换密码有助于保护账户安全</p>
                        </div>

                        <a-form
                            :model="passwordForm"
                            layout="vertical"
                            @finish="handleChangePassword"
                            class="security-form"
                        >
                            <a-form-item
                                label="当前密码"
                                name="oldPassword"
                                :rules="[{ required: true, message: '请输入当前密码' }]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.oldPassword"
                                    placeholder="验证身份"
                                    class="premium-input"
                                >
                                    <template #prefix><Lock :size="16" class="input-icon" /></template>
                                </a-input-password>
                            </a-form-item>

                            <a-divider />

                            <a-form-item
                                label="新密码"
                                name="newPassword"
                                :rules="[
                                    { required: true, message: '请输入新密码' },
                                    { min: 6, message: '密码长度至少 6 位' },
                                ]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.newPassword"
                                    placeholder="设置新密码"
                                    class="premium-input"
                                >
                                    <template #prefix><Key :size="16" class="input-icon" /></template>
                                </a-input-password>
                            </a-form-item>

                            <a-form-item
                                label="确认新密码"
                                name="confirmPassword"
                                :rules="[
                                    { required: true, message: '请再次确认新密码' },
                                    { validator: validateConfirmPassword },
                                ]"
                            >
                                <a-input-password
                                    v-model:value="passwordForm.confirmPassword"
                                    placeholder="重复新密码"
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
                                    更新登录密码
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
import { User, Mail, Lock, Key, Camera, Smile, Upload } from "lucide-vue-next";
import { userApi, uploadApi } from "@/api";
import { useAuthStore } from "@/store/auth";
import UserAvatar from "@/components/UserAvatar.vue";

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
        console.error("加载用户信息失败:", error);
        message.error("无法获取用户信息");
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

        message.success("资料已更新");
    } catch (error) {
        console.error("更新资料失败:", error);
        message.error(error.message || "更新失败，请重试");
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
        message.success("密码修改成功，请下次登录时使用新密码");
        // 重置表单
        passwordForm.oldPassword = "";
        passwordForm.newPassword = "";
        passwordForm.confirmPassword = "";
    } catch (error) {
        console.error("修改密码失败:", error);
        message.error(error.message || "原密码错误或更新失败");
    } finally {
        changingPassword.value = false;
    }
};

// 密码确认验证
const validateConfirmPassword = async (_rule, value) => {
    if (value && value !== passwordForm.newPassword) {
        throw new Error("两次输入的密码不一致");
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
        message.error("请选择图片文件");
        return;
    }

    if (file.size > 2 * 1024 * 1024) {
        message.error("图片过大，不能超过 2MB");
        return;
    }

    uploading.value = true;
    try {
        // 使用通用上传接口
        const url = await uploadApi.upload(file);
        userForm.avatarUrl = url;
        message.success("头像上传成功，点击保存按钮生效");
    } catch (error) {
        console.error("上传头像失败:", error);
        message.error("头像上传失败");
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
