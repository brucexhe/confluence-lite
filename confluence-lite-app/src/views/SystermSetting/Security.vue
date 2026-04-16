<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>安全设置</h1>
            <p class="page-description">配置用户账号和访问控制相关的安全选项</p>
        </div>

        <a-spin :spinning="loading">
            <a-form
                :model="formState"
                :label-col="{ style: { width: '120px' } }"
                :wrapper-col="{ span: 16 }"
                class="settings-form"
                @finish="handleSubmit"
            >
                <!-- 用户注册 -->
                <div class="form-section">
                    <h3 class="section-title">用户注册</h3>

                    <a-form-item label="允许公开注册" name="allowPublicRegistration">
                        <a-switch
                            v-model:checked="formState.allowPublicRegistration"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">是否允许新用户自行注册账号</div>
                    </a-form-item>

                    <a-form-item label="需要邮箱验证" name="requireEmailVerification">
                        <a-switch
                            v-model:checked="formState.requireEmailVerification"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">注册时需要验证邮箱地址</div>
                    </a-form-item>

                    <a-form-item label="默认用户角色" name="defaultUserRole">
                        <a-select
                            v-model:value="formState.defaultUserRole"
                            style="max-width: 200px"
                            :options="roleOptions"
                        ></a-select>
                        <div class="form-hint">新注册用户的默认角色</div>
                    </a-form-item>
                </div>

                <!-- 密码策略 -->
                <div class="form-section">
                    <h3 class="section-title">密码策略</h3>

                    <a-form-item label="最小密码长度" name="minPasswordLength">
                        <a-input-number
                            v-model:value="formState.minPasswordLength"
                            :min="4"
                            :max="32"
                            style="width: 120px"
                        />
                        <div class="form-hint">用户密码的最小长度限制</div>
                    </a-form-item>

                    <a-form-item label="密码复杂度" name="passwordComplexity">
                        <a-select
                            v-model:value="formState.passwordComplexity"
                            style="max-width: 200px"
                            :options="complexityOptions"
                        ></a-select>
                        <div class="form-hint">密码复杂度要求</div>
                    </a-form-item>

                    <a-form-item label="密码过期天数" name="passwordExpireDays">
                        <a-input-number
                            v-model:value="formState.passwordExpireDays"
                            :min="0"
                            :max="365"
                            style="width: 120px"
                        />
                        <span style="margin-left: 8px">天（设置为 0 表示永不过期）</span>
                        <div class="form-hint">密码过期后用户需要修改密码</div>
                    </a-form-item>
                </div>

                <!-- 会话管理 -->
                <div class="form-section">
                    <h3 class="section-title">会话管理</h3>

                    <a-form-item label="会话超时时间" name="sessionTimeout">
                        <a-input-number
                            v-model:value="formState.sessionTimeout"
                            :min="5"
                            :max="1440"
                            style="width: 120px"
                        />
                        <span style="margin-left: 8px">分钟</span>
                        <div class="form-hint">用户无操作后自动退出登录的时间</div>
                    </a-form-item>

                    <a-form-item label="允许同时登录" name="allowConcurrentSessions">
                        <a-switch
                            v-model:checked="formState.allowConcurrentSessions"
                            checked-children="允许"
                            un-checked-children="禁止"
                        />
                        <div class="form-hint">是否允许同一账号在多个设备同时登录</div>
                    </a-form-item>

                    <a-form-item label="记住登录" name="allowRememberMe">
                        <a-switch
                            v-model:checked="formState.allowRememberMe"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">是否允许用户选择"记住我"保持登录状态</div>
                    </a-form-item>
                </div>

                <!-- 访问控制 -->
                <div class="form-section">
                    <h3 class="section-title">访问控制</h3>

                    <a-form-item label="IP 白名单" name="ipWhitelist">
                        <a-textarea
                            v-model:value="formState.ipWhitelist"
                            placeholder="每行一个 IP 地址或 IP 段，例如：&#10;192.168.1.100&#10;192.168.1.0/24"
                            :rows="5"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">仅允许白名单中的 IP 访问系统（留空表示不限制）</div>
                    </a-form-item>

                    <a-form-item label="启用两步验证" name="enableTwoFactor">
                        <a-switch
                            v-model:checked="formState.enableTwoFactor"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">开启后用户可启用两步验证增强账号安全</div>
                    </a-form-item>
                </div>

                <!-- 提交按钮 -->
                <a-form-item :wrapper-col="{ offset: 0 }">
                    <a-space>
                        <a-button type="primary" html-type="submit" :loading="saving">
                            保存设置
                        </a-button>
                        <a-button @click="handleReset">重置</a-button>
                    </a-space>
                </a-form-item>
            </a-form>
        </a-spin>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi } from '@/api'

const loading = ref(false)
const saving = ref(false)

// 表单数据
const formState = ref({
    // 用户注册
    allowPublicRegistration: true,
    requireEmailVerification: false,
    defaultUserRole: 'user',

    // 密码策略
    minPasswordLength: 8,
    passwordComplexity: 'medium',
    passwordExpireDays: 0,

    // 会话管理
    sessionTimeout: 60,
    allowConcurrentSessions: true,
    allowRememberMe: true,

    // 访问控制
    ipWhitelist: '',
    enableTwoFactor: false
})

// 角色选项
const roleOptions = ref([
    { label: '普通用户', value: 'user' },
    { label: '编辑者', value: 'editor' },
    { label: '管理员', value: 'admin' }
])

// 密码复杂度选项
const complexityOptions = ref([
    { label: '低（仅长度要求）', value: 'low' },
    { label: '中（需包含字母和数字）', value: 'medium' },
    { label: '高（需包含大小写字母、数字和符号）', value: 'high' }
])

// 加载配置
const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getSecurityConfig()
        if (data) {
            formState.value = {
                ...formState.value,
                ...data
            }
        }
    } catch (error) {
        console.error('加载安全配置失败:', error)
    } finally {
        loading.value = false
    }
}

// 提交表单
const handleSubmit = async () => {
    saving.value = true
    try {
        await systemSettingApi.updateSecurityConfig(formState.value)
        message.success('安全设置保存成功')
    } catch (error) {
        console.error('保存安全设置失败:', error)
        message.error('保存设置失败，请稍后重试')
    } finally {
        saving.value = false
    }
}

// 重置表单
const handleReset = () => {
    loadConfig()
    message.info('已重置为服务器配置')
}

onMounted(() => {
    loadConfig()
})
</script>

<style scoped>
.settings-page {
    background-color: #ffffff;
    border-radius: 4px;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
    margin: 16px;
}

.page-header {
    padding: 20px 24px 16px;
    border-bottom: 1px solid #dfe1e6;
}

.page-header h1 {
    font-size: 20px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 4px 0;
}

.page-description {
    font-size: 13px;
    color: #6b778c;
    margin: 0;
}

.settings-form {
    padding: 20px 24px 24px;
}

.form-section {
    margin-bottom: 24px;
    padding-bottom: 20px;
    border-bottom: 1px solid #ebecf0;
}

.form-section:last-child {
    border-bottom: none;
    margin-bottom: 0;
    padding-bottom: 0;
}

.section-title {
    font-size: 14px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 16px 0;
    display: flex;
    align-items: center;
}

.section-title::before {
    content: '';
    width: 3px;
    height: 14px;
    background-color: #0052cc;
    margin-right: 8px;
    border-radius: 2px;
}

.settings-form :deep(.ant-form-item) {
    margin-bottom: 16px;
}

.settings-form :deep(.ant-form-item-label > label) {
    font-weight: 500;
    color: #42526e;
    font-size: 14px;
}

.form-hint {
    margin-top: 2px;
    font-size: 12px;
    color: #6b778c;
    line-height: 1.4;
}
</style> 