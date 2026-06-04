<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.security.title') }}</h1>
            <p class="page-description">{{ $t('settings.security.description') }}</p>
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
                    <h3 class="section-title">{{ $t('settings.security.userRegistration') }}</h3>

                    <a-form-item :label="$t('settings.security.allowPublicRegistration')" name="allowPublicRegistration">
                        <a-switch
                            v-model:checked="formState.allowPublicRegistration"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.security.allowPublicRegistrationHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.requireEmailVerification')" name="requireEmailVerification">
                        <a-switch
                            v-model:checked="formState.requireEmailVerification"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.security.requireEmailVerificationHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.defaultUserRole')" name="defaultUserRole">
                        <a-select
                            v-model:value="formState.defaultUserRole"
                            style="max-width: 200px"
                            :options="roleOptions"
                        ></a-select>
                        <div class="form-hint">{{ $t('settings.security.defaultUserRoleHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 密码策略 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.security.passwordPolicy') }}</h3>

                    <a-form-item :label="$t('settings.security.minPasswordLength')" name="minPasswordLength">
                        <a-input-number
                            v-model:value="formState.minPasswordLength"
                            :min="4"
                            :max="32"
                            style="width: 120px"
                        />
                        <div class="form-hint">{{ $t('settings.security.minPasswordLengthHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.passwordComplexity')" name="passwordComplexity">
                        <a-select
                            v-model:value="formState.passwordComplexity"
                            style="max-width: 200px"
                            :options="complexityOptions"
                        ></a-select>
                        <div class="form-hint">{{ $t('settings.security.passwordComplexityHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.passwordExpireDays')" name="passwordExpireDays">
                        <a-input-number
                            v-model:value="formState.passwordExpireDays"
                            :min="0"
                            :max="365"
                            style="width: 120px"
                        />
                        <span style="margin-left: 8px">{{ $t('settings.security.passwordExpireDaysUnit') }}</span>
                        <div class="form-hint">{{ $t('settings.security.passwordExpireDaysHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 会话管理 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.security.sessionManagement') }}</h3>

                    <a-form-item :label="$t('settings.security.allowConcurrentSessions')" name="allowConcurrentSessions">
                        <a-switch
                            v-model:checked="formState.allowConcurrentSessions"
                            :checked-children="$t('common.allow')"
                            :un-checked-children="$t('common.forbid')"
                        />
                        <div class="form-hint">{{ $t('settings.security.allowConcurrentSessionsHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.rememberMe')" name="allowRememberMe">
                        <a-switch
                            v-model:checked="formState.allowRememberMe"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.security.rememberMeHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 访问控制 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.security.accessControl') }}</h3>

                    <a-form-item :label="$t('settings.security.ipWhitelist')" name="ipWhitelist">
                        <a-textarea
                            v-model:value="formState.ipWhitelist"
                            :placeholder="$t('settings.security.ipWhitelistPlaceholder')"
                            :rows="5"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">{{ $t('settings.security.ipWhitelistHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.enableTwoFactor')" name="enableTwoFactor">
                        <a-switch
                            v-model:checked="formState.enableTwoFactor"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.security.enableTwoFactorHint') }}</div>
                    </a-form-item>
                </div>

                <!-- JWT Token 配置 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.security.jwtTokenConfig') }}</h3>

                    <a-form-item :label="$t('settings.security.jwtSecret')" name="jwtSecret">
                        <div style="display: flex; gap: 8px; max-width: 500px">
                            <a-input-password
                                v-model:value="formState.jwtSecret"
                                :placeholder="$t('settings.security.jwtSecretPlaceholder')"
                                style="flex: 1"
                            />
                            <a-button @click="handleGenerateSecret" :loading="generatingSecret">
                                {{ $t('settings.security.generateSecret') }}
                            </a-button>
                        </div>
                        <div class="form-hint">{{ $t('settings.security.jwtSecretHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.jwtIssuer')" name="jwtIssuer">
                        <a-input
                            v-model:value="formState.jwtIssuer"
                            style="max-width: 300px"
                            :placeholder="$t('settings.security.jwtIssuerPlaceholder')"
                        />
                        <div class="form-hint">{{ $t('settings.security.jwtIssuerHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.jwtAudience')" name="jwtAudience">
                        <a-input
                            v-model:value="formState.jwtAudience"
                            style="max-width: 300px"
                            :placeholder="$t('settings.security.jwtAudiencePlaceholder')"
                        />
                        <div class="form-hint">{{ $t('settings.security.jwtAudienceHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.security.jwtExpiration')" name="jwtExpirationMinutes">
                        <a-input-number
                            v-model:value="formState.jwtExpirationMinutes"
                            :min="1"
                            :max="525600"
                            style="width: 160px"
                        />
                        <span style="margin-left: 8px">{{ $t('settings.security.jwtExpirationUnit') }}</span>
                        <div class="form-hint">{{ $t('settings.security.jwtExpirationHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 提交按钮 -->
                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                    <a-space>
                        <a-button type="primary" html-type="submit" :loading="saving">
                            {{ $t('settings.saveSettings') }}
                        </a-button>
                        <a-button @click="handleReset">{{ $t('common.reset') }}</a-button>
                    </a-space>
                </a-form-item>
            </a-form>
        </a-spin>
    </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { systemSettingApi } from '@/api'

const { t } = useI18n()
const loading = ref(false)
const saving = ref(false)
const generatingSecret = ref(false)

// 表单数据
const formState = ref({
    // 用户注册
    allowPublicRegistration: false,
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
    enableTwoFactor: false,

    // JWT Token 配置
    jwtSecret: '',
    jwtIssuer: 'ConfluenceLite',
    jwtAudience: 'ConfluenceLiteUsers',
    jwtExpirationMinutes: 10080
})

// 角色选项
const roleOptions = ref([
    { label: t('settings.users.roleUser'), value: 'user' },
    { label: t('settings.users.roleEditor'), value: 'editor' },
    { label: t('settings.users.roleAdmin'), value: 'admin' }
])

// 密码复杂度选项
const complexityOptions = ref([
    { label: t('settings.security.complexityLow'), value: 'low' },
    { label: t('settings.security.complexityMedium'), value: 'medium' },
    { label: t('settings.security.complexityHigh'), value: 'high' }
])

// 生成密钥
const handleGenerateSecret = async () => {
    generatingSecret.value = true
    try {
        const secret = await systemSettingApi.generateJwtSecret()
        if (secret) {
            formState.value.jwtSecret = secret
            message.success(t('settings.security.secretGenerated'))
        }
    } catch (error) {
        console.error('生成密钥失败:', error)
        message.error(t('settings.security.generateSecretFailed'))
    } finally {
        generatingSecret.value = false
    }
}

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
        message.success(t('settings.security.saveSuccess'))
    } catch (error) {
        console.error('保存安全设置失败:', error)
        message.error(t('settings.saveFailed'))
    } finally {
        saving.value = false
    }
}

// 重置表单
const handleReset = () => {
    loadConfig()
    message.info(t('settings.resetToServer'))
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