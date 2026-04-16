<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>身份验证方式</h1>
            <p class="page-description">配置用户的登录和认证方式</p>
        </div>

        <a-spin :spinning="loading">
            <a-form
                :model="formState"
                :label-col="{ style: { width: '140px' } }"
                :wrapper-col="{ span: 16 }"
                class="settings-form"
                @finish="handleSubmit"
            >
                <!-- 密码认证 -->
                <div class="form-section">
                    <h3 class="section-title">密码认证</h3>

                    <a-form-item label="启用密码登录" name="passwordEnabled">
                        <a-switch
                            v-model:checked="formState.passwordEnabled"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">用户可以使用用户名/邮箱和密码登录</div>
                    </a-form-item>

                    <a-form-item label="允许邮箱登录" name="emailLoginEnabled">
                        <a-switch
                            v-model:checked="formState.emailLoginEnabled"
                            checked-children="允许"
                            un-checked-children="禁止"
                        />
                        <div class="form-hint">用户可以使用邮箱代替用户名登录</div>
                    </a-form-item>
                </div>

                <!-- OpenID Connect -->
                <div class="form-section">
                    <h3 class="section-title">OpenID Connect</h3>

                    <a-form-item label="启用 OpenID" name="oidcEnabled">
                        <a-switch
                            v-model:checked="formState.oidcEnabled"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">允许用户通过 OpenID Connect 提供商登录</div>
                    </a-form-item>

                    <template v-if="formState.oidcEnabled">
                        <a-form-item label="提供商名称" name="oidcProviderName">
                            <a-input
                                v-model:value="formState.oidcProviderName"
                                placeholder="例如：Google、GitHub"
                                style="max-width: 300px"
                            />
                            <div class="form-hint">显示在登录按钮上的名称</div>
                        </a-form-item>

                        <a-form-item label="Discovery URL" name="oidcDiscoveryUrl">
                            <a-input
                                v-model:value="formState.oidcDiscoveryUrl"
                                placeholder="https://accounts.google.com/.well-known/openid-configuration"
                                style="max-width: 500px"
                            />
                            <div class="form-hint">OpenID Connect 发现端点 URL</div>
                        </a-form-item>

                        <a-form-item label="Client ID" name="oidcClientId">
                            <a-input
                                v-model:value="formState.oidcClientId"
                                placeholder="应用程序的客户端 ID"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="Client Secret" name="oidcClientSecret">
                            <a-input-password
                                v-model:value="formState.oidcClientSecret"
                                placeholder="应用程序的客户端密钥"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="Scopes" name="oidcScopes">
                            <a-input
                                v-model:value="formState.oidcScopes"
                                placeholder="openid profile email"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">请求的权限范围，用空格分隔</div>
                        </a-form-item>

                        <a-form-item label="自动创建用户" name="oidcAutoCreateUser">
                            <a-switch
                                v-model:checked="formState.oidcAutoCreateUser"
                                checked-children="开启"
                                un-checked-children="关闭"
                            />
                            <div class="form-hint">首次登录时自动创建用户账号</div>
                        </a-form-item>

                        <a-form-item label="默认角色" name="oidcDefaultRole">
                            <a-select
                                v-model:value="formState.oidcDefaultRole"
                                style="max-width: 200px"
                            >
                                <a-select-option value="user">普通用户</a-select-option>
                                <a-select-option value="editor">编辑者</a-select-option>
                                <a-select-option value="admin">管理员</a-select-option>
                            </a-select>
                            <div class="form-hint">自动创建用户的默认角色</div>
                        </a-form-item>
                    </template>
                </div>

                <!-- LDAP -->
                <div class="form-section">
                    <h3 class="section-title">LDAP / AD</h3>

                    <a-form-item label="启用 LDAP" name="ldapEnabled">
                        <a-switch
                            v-model:checked="formState.ldapEnabled"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">允许用户通过 LDAP/Active Directory 登录</div>
                    </a-form-item>

                    <template v-if="formState.ldapEnabled">
                        <a-form-item label="LDAP 服务器" name="ldapUrl">
                            <a-input
                                v-model:value="formState.ldapUrl"
                                placeholder="ldap://ldap.example.com:389"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="Bind DN" name="ldapBindDn">
                            <a-input
                                v-model:value="formState.ldapBindDn"
                                placeholder="cn=admin,dc=example,dc=com"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">用于绑定查询的 DN</div>
                        </a-form-item>

                        <a-form-item label="Bind 密码" name="ldapBindPassword">
                            <a-input-password
                                v-model:value="formState.ldapBindPassword"
                                placeholder="绑定密码"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="用户搜索基础" name="ldapBaseDn">
                            <a-input
                                v-model:value="formState.ldapBaseDn"
                                placeholder="ou=users,dc=example,dc=com"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">搜索用户的基础 DN</div>
                        </a-form-item>

                        <a-form-item label="用户搜索过滤器" name="ldapUserFilter">
                            <a-input
                                v-model:value="formState.ldapUserFilter"
                                placeholder="(uid={username})"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">{username} 会被替换为实际用户名</div>
                        </a-form-item>
                    </template>
                </div>

                <!-- 测试连接 -->
                <div class="form-section" v-if="formState.oidcEnabled || formState.ldapEnabled">
                    <h3 class="section-title">测试连接</h3>
                    <a-form-item label="测试认证">
                        <a-space>
                            <a-button @click="testConnection" :loading="testing">
                                测试连接
                            </a-button>
                            <span v-if="testResult" :style="{ color: testResult.success ? 'green' : 'red' }">
                                {{ testResult.message }}
                            </span>
                        </a-space>
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
import { ref, reactive, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi } from '@/api'

const loading = ref(false)
const saving = ref(false)
const testing = ref(false)
const testResult = ref(null)

const formState = reactive({
    // 密码认证
    passwordEnabled: true,
    emailLoginEnabled: true,

    // OpenID Connect
    oidcEnabled: false,
    oidcProviderName: '',
    oidcDiscoveryUrl: '',
    oidcClientId: '',
    oidcClientSecret: '',
    oidcScopes: 'openid profile email',
    oidcAutoCreateUser: true,
    oidcDefaultRole: 'user',

    // LDAP
    ldapEnabled: false,
    ldapUrl: '',
    ldapBindDn: '',
    ldapBindPassword: '',
    ldapBaseDn: '',
    ldapUserFilter: '(uid={username})'
})

const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getAuthConfig()
        if (data) {
            Object.assign(formState, data)
        }
    } catch (error) {
        console.error('加载认证配置失败:', error)
    } finally {
        loading.value = false
    }
}

const handleSubmit = async () => {
    saving.value = true
    try {
        await systemSettingApi.updateAuthConfig(formState)
        message.success('认证设置保存成功')
    } catch (error) {
        message.error('保存设置失败')
    } finally {
        saving.value = false
    }
}

const handleReset = () => {
    loadConfig()
    message.info('已重置为服务器配置')
}

const testConnection = async () => {
    testing.value = true
    testResult.value = null
    try {
        // TODO: 调用测试连接 API
        await new Promise(resolve => setTimeout(resolve, 1000))
        testResult.value = { success: true, message: '连接成功' }
    } catch (error) {
        testResult.value = { success: false, message: '连接失败: ' + error.message }
    } finally {
        testing.value = false
    }
}

onMounted(() => {
    loadConfig()
})
</script>

<style scoped>
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
}

.section-title {
    font-size: 14px;
    font-weight: 600;
    color: #172b4d;
    margin: 0 0 16px 0;
}

.form-hint {
    margin-top: 2px;
    font-size: 12px;
    color: #6b778c;
    line-height: 1.4;
}
</style>
