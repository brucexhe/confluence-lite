<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>邮件设置</h1>
            <p class="page-description">配置系统的邮件服务，用于通知和验证</p>
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
                    <h3 class="section-title">SMTP 服务器设置</h3>

                    <a-form-item
                        label="启用邮件"
                        name="enabled"
                    >
                        <a-switch
                            v-model:checked="formState.enabled"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">启用后系统将发送通知邮件</div>
                    </a-form-item>

                    <a-form-item
                        label="SMTP 主机"
                        name="smtpHost"
                        :rules="[{ required: formState.enabled, message: '请输入 SMTP 主机地址' }]"
                    >
                        <a-input
                            v-model:value="formState.smtpHost"
                            placeholder="例如: smtp.gmail.com"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">邮件服务器的主机地址</div>
                    </a-form-item>

                    <a-form-item
                        label="SMTP 端口"
                        name="smtpPort"
                        :rules="[{ required: formState.enabled, message: '请输入 SMTP 端口' }]"
                    >
                        <a-input-number
                            v-model:value="formState.smtpPort"
                            :min="1"
                            :max="65535"
                            style="width: 150px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">常用的端口：25 (非加密), 465 (SSL), 587 (TLS)</div>
                    </a-form-item>

                    <a-form-item
                        label="加密方式"
                        name="encryption"
                    >
                        <a-radio-group
                            v-model:value="formState.encryption"
                            :disabled="!formState.enabled"
                        >
                            <a-radio value="none">无加密</a-radio>
                            <a-radio value="ssl">SSL/TLS</a-radio>
                            <a-radio value="tls">STARTTLS</a-radio>
                        </a-radio-group>
                        <div class="form-hint">与 SMTP 服务器通信的加密方式</div>
                    </a-form-item>

                    <a-form-item
                        label="发件人邮箱"
                        name="fromEmail"
                        :rules="[
                            { required: formState.enabled, message: '请输入发件人邮箱' },
                            { type: 'email', message: '请输入有效的邮箱地址' }
                        ]"
                    >
                        <a-input
                            v-model:value="formState.fromEmail"
                            placeholder="noreply@example.com"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">系统发送邮件时使用的发件人地址</div>
                    </a-form-item>

                    <a-form-item
                        label="发件人名称"
                        name="fromName"
                    >
                        <a-input
                            v-model:value="formState.fromName"
                            placeholder="例如：Confluence Lite"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">系统发送邮件时显示的发件人名称</div>
                    </a-form-item>

                    <a-form-item
                        label="用户名"
                        name="username"
                        :rules="[{ required: formState.enabled, message: '请输入 SMTP 登录用户名' }]"
                    >
                        <a-input
                            v-model:value="formState.username"
                            placeholder="SMTP 登录用户名"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">SMTP 服务器的登录用户名（通常是邮箱地址）</div>
                    </a-form-item>

                    <a-form-item
                        label="密码"
                        name="password"
                    >
                        <a-input-password
                            v-model:value="formState.password"
                            placeholder="SMTP 登录密码（留空表示不修改）"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">SMTP 服务器的登录密码或应用专用密码</div>
                    </a-form-item>
                </div>

                <!-- 邮件模板 -->
                <div class="form-section">
                    <h3 class="section-title">邮件通知</h3>

                    <a-form-item label="新用户注册" name="notifyOnRegister">
                        <a-switch
                            v-model:checked="formState.notifyOnRegister"
                            checked-children="通知"
                            un-checked-children="不通知"
                        />
                        <div class="form-hint">有新用户注册时发送邮件通知管理员</div>
                    </a-form-item>

                    <a-form-item label="通知接收邮箱" name="adminEmail">
                        <a-input
                            v-model:value="formState.adminEmail"
                            placeholder="admin@example.com"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">接收系统通知的管理员邮箱地址</div>
                    </a-form-item>

                    <a-form-item label="邮件签名" name="emailSignature">
                        <a-textarea
                            v-model:value="formState.emailSignature"
                            placeholder="邮件底部的签名信息"
                            :rows="3"
                            style="max-width: 400px"
                        />
                        <div class="form-hint">添加到所有系统邮件底部的签名信息</div>
                    </a-form-item>
                </div>

                <!-- 提交和测试按钮 -->
                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                    <a-space>
                        <a-button type="primary" html-type="submit" :loading="saving">
                            保存设置
                        </a-button>
                        <a-button @click="handleReset">重置</a-button>
                        <a-button
                            @click="handleTest"
                            :loading="testing"
                            :disabled="!formState.enabled"
                        >
                            发送测试邮件
                        </a-button>
                    </a-space>
                </a-form-item>
            </a-form>
        </a-spin>

        <!-- 测试邮件对话框 -->
        <a-modal
            v-model:open="testModalVisible"
            title="发送测试邮件"
            @ok="sendTestEmail"
            :confirm-loading="testing"
        >
            <a-form layout="vertical">
                <a-form-item label="收件人邮箱">
                    <a-input
                        v-model:value="testEmail"
                        placeholder="输入用于接收测试邮件的邮箱地址"
                    />
                </a-form-item>
            </a-form>
        </a-modal>
    </div>
</template>

<script setup>
import { ref, onMounted, reactive } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi } from '@/api'

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
        message.success('邮件设置保存成功')
    } catch (error) {
        console.error('保存邮件设置失败:', error)
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

// 打开测试邮件对话框
const handleTest = async () => {
    try {
        await formRef.value.validate()
    } catch (error) {
        message.warning('请先填写完整的 SMTP 配置')
        return
    }

    // 如果有管理员邮箱，使用管理员邮箱作为默认测试邮箱
    testEmail.value = formState.adminEmail || formState.fromEmail
    testModalVisible.value = true
}

// 发送测试邮件
const sendTestEmail = async () => {
    if (!testEmail.value) {
        message.warning('请输入收件人邮箱')
        return
    }

    testing.value = true
    try {
        await systemSettingApi.testMail({
            ...formState,
            testEmail: testEmail.value
        })
        message.success(`测试邮件已发送到 ${testEmail.value}，请查收`)
        testModalVisible.value = false
    } catch (error) {
        console.error('发送测试邮件失败:', error)
        message.error('发送测试邮件失败，请检查配置')
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
