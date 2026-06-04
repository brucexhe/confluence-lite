<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.mail.title') }}</h1>
            <p class="page-description">{{ $t('settings.mail.description') }}</p>
        </div>

        <a-spin :spinning="loading">
            <a-form
                ref="formRef"
                :model="formState"
                :label-col="{ style: { width: '120px' } }"
                :wrapper-col="{ span: 16 }"
                class="settings-form"
                @finish="handleSubmit"
            >
                <!-- SMTP 设置 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.mail.smtpSectionTitle') }}</h3>

                    <a-form-item
                        :label="$t('settings.mail.enableMail')"
                        name="enabled"
                    >
                        <a-switch
                            v-model:checked="formState.enabled"
                            :checked-children="$t('common.enabled')"
                            :un-checked-children="$t('common.disabled')"
                        />
                        <div class="form-hint">{{ $t('settings.mail.enableMailHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.smtpHost')"
                        name="smtpHost"
                        :rules="[{ required: formState.enabled, message: t('settings.mail.smtpHostRequired') }]"
                    >
                        <a-input
                            v-model:value="formState.smtpHost"
                            :placeholder="$t('settings.mail.smtpHostPlaceholder')"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.smtpHostHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.smtpPort')"
                        name="smtpPort"
                        :rules="[{ required: formState.enabled, message: t('settings.mail.smtpPortRequired') }]"
                    >
                        <a-input-number
                            v-model:value="formState.smtpPort"
                            :min="1"
                            :max="65535"
                            style="width: 150px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.smtpPortHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.encryption')"
                        name="encryption"
                    >
                        <a-radio-group
                            v-model:value="formState.encryption"
                            :disabled="!formState.enabled"
                        >
                            <a-radio value="none">{{ $t('settings.mail.encryptionNone') }}</a-radio>
                            <a-radio value="ssl">SSL/TLS</a-radio>
                            <a-radio value="tls">STARTTLS</a-radio>
                        </a-radio-group>
                        <div class="form-hint">{{ $t('settings.mail.encryptionHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.senderEmail')"
                        name="fromEmail"
                        :rules="[
                            { required: formState.enabled, message: t('settings.mail.senderEmailRequired') },
                            { type: 'email', message: t('profile.emailInvalid') }
                        ]"
                    >
                        <a-input
                            v-model:value="formState.fromEmail"
                            placeholder="noreply@example.com"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.senderEmailHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.senderName')"
                        name="fromName"
                    >
                        <a-input
                            v-model:value="formState.fromName"
                            :placeholder="$t('settings.mail.senderNamePlaceholder')"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.senderNameHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.smtpUsername')"
                        name="username"
                        :rules="[{ required: formState.enabled, message: t('settings.mail.smtpUsernameRequired') }]"
                    >
                        <a-input
                            v-model:value="formState.username"
                            :placeholder="$t('settings.mail.smtpUsernamePlaceholder')"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.smtpUsernameHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.mail.smtpPassword')"
                        name="password"
                    >
                        <a-input-password
                            v-model:value="formState.password"
                            :placeholder="$t('settings.mail.smtpPasswordPlaceholder')"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.mail.smtpPasswordHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 邮件模板 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.mail.notificationSectionTitle') }}</h3>

                    <a-form-item :label="$t('settings.mail.notifyOnRegister')" name="notifyOnRegister">
                        <a-switch
                            v-model:checked="formState.notifyOnRegister"
                            :checked-children="$t('settings.mail.notify')"
                            :un-checked-children="$t('settings.mail.noNotify')"
                        />
                        <div class="form-hint">{{ $t('settings.mail.notifyOnRegisterHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.mail.adminEmail')" name="adminEmail">
                        <a-input
                            v-model:value="formState.adminEmail"
                            placeholder="admin@example.com"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">{{ $t('settings.mail.adminEmailHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.mail.emailSignature')" name="emailSignature">
                        <a-textarea
                            v-model:value="formState.emailSignature"
                            :placeholder="$t('settings.mail.emailSignaturePlaceholder')"
                            :rows="3"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">{{ $t('settings.mail.emailSignatureHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 提交和测试按钮 -->
                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                    <a-space>
                        <a-button type="primary" html-type="submit" :loading="saving">
                            {{ $t('settings.saveSettings') }}
                        </a-button>
                        <a-button @click="handleReset">{{ $t('common.reset') }}</a-button>
                        <a-button
                            @click="handleTest"
                            :loading="testing"
                            :disabled="!formState.enabled"
                        >
                            {{ $t('settings.mail.testMail') }}
                        </a-button>
                    </a-space>
                </a-form-item>
            </a-form>
        </a-spin>

        <!-- 测试邮件对话框 -->
        <a-modal
            v-model:open="testModalVisible"
            :title="$t('settings.mail.testMail')"
            @ok="sendTestEmail"
            :confirm-loading="testing"
        >
            <a-form layout="vertical">
                <a-form-item :label="$t('settings.mail.recipientEmail')">
                    <a-input
                        v-model:value="testEmail"
                        :placeholder="$t('settings.mail.recipientEmailPlaceholder')"
                    />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { systemSettingApi } from '@/api'

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const testing = ref(false)
const formRef = ref()
const testModalVisible = ref(false)
const testEmail = ref('')

// 表单数据
const formState = reactive({
    enabled: false,
    smtpHost: '',
    smtpPort: 587,
    encryption: 'tls',
    fromEmail: '',
    fromName: '',
    username: '',
    password: '',
    notifyOnRegister: false,
    adminEmail: '',
    emailSignature: ''
})

// 常用邮件服务配置模板
const emailTemplates = {
    gmail: {
        smtpHost: 'smtp.gmail.com',
        smtpPort: 587,
        encryption: 'tls'
    },
    outlook: {
        smtpHost: 'smtp.office365.com',
        smtpPort: 587,
        encryption: 'tls'
    },
    qq: {
        smtpHost: 'smtp.qq.com',
        smtpPort: 587,
        encryption: 'tls'
    },
    '163': {
        smtpHost: 'smtp.163.com',
        smtpPort: 465,
        encryption: 'ssl'
    },
    '126': {
        smtpHost: 'smtp.126.com',
        smtpPort: 465,
        encryption: 'ssl'
    }
}

// 加载配置
const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getMailConfig()
        if (data) {
            Object.assign(formState, {
                ...formState,
                ...data,
                password: '' // 不显示已保存的密码
            })
        }
    } catch (error) {
        console.error('加载邮件配置失败:', error)
    } finally {
        loading.value = false
    }
}

// 提交表单
const handleSubmit = async () => {
    try {
        await formRef.value.validate()
    } catch (error) {
        return
    }

    saving.value = true
    try {
        await systemSettingApi.updateMailConfig(formState)
        message.success(t('settings.mail.saveSuccess'))
    } catch (error) {
        console.error('保存邮件设置失败:', error)
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

// 打开测试邮件对话框
const handleTest = async () => {
    try {
        await formRef.value.validate()
    } catch (error) {
        message.warning(t('settings.mail.smtpConfigRequired'))
        return
    }

    // 如果有管理员邮箱，使用管理员邮箱作为默认测试邮箱
    testEmail.value = formState.adminEmail || formState.fromEmail
    testModalVisible.value = true
}

// 发送测试邮件
const sendTestEmail = async () => {
    if (!testEmail.value) {
        message.warning(t('settings.mail.recipientEmailRequired'))
        return
    }

    testing.value = true
    try {
        await systemSettingApi.testMail({
            ...formState,
            testEmail: testEmail.value
        })
        message.success(t('settings.mail.testMailSent', { email: testEmail.value }))
        testModalVisible.value = false
    } catch (error) {
        console.error('发送测试邮件失败:', error)
        message.error(t('settings.mail.testMailFailed'))
    } finally {
        testing.value = false
    }
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
