<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.authenticationDetail.title') }}</h1>
            <p class="page-description">{{ $t('settings.authenticationDetail.description') }}</p>
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
                    <h3 class="section-title">{{ $t('settings.authenticationDetail.passwordAuth') }}</h3>

                    <a-form-item :label="$t('settings.authenticationDetail.enablePasswordLogin')" name="passwordEnabled">
                        <a-switch
                            v-model:checked="formState.passwordEnabled"
                            :checked-children="$t('common.enabled')"
                            :un-checked-children="$t('common.disabled')"
                        />
                        <div class="form-hint">{{ $t('settings.authenticationDetail.passwordLoginHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.authenticationDetail.allowEmailLogin')" name="emailLoginEnabled">
                        <a-switch
                            v-model:checked="formState.emailLoginEnabled"
                            :checked-children="$t('common.enable')"
                            :un-checked-children="$t('common.disable')"
                        />
                        <div class="form-hint">{{ $t('settings.authenticationDetail.emailLoginHint') }}</div>
                    </a-form-item>
                </div>

                <!-- OpenID Connect -->
                <div class="form-section">
                    <h3 class="section-title">OpenID Connect</h3>

                    <a-form-item :label="$t('settings.authenticationDetail.enableOidc')" name="oidcEnabled">
                        <a-switch
                            v-model:checked="formState.oidcEnabled"
                            :checked-children="$t('common.enabled')"
                            :un-checked-children="$t('common.disabled')"
                        />
                        <div class="form-hint">{{ $t('settings.authenticationDetail.oidcHint') }}</div>
                    </a-form-item>

                    <template v-if="formState.oidcEnabled">
                        <a-form-item :label="$t('settings.authenticationDetail.providerName')" name="oidcProviderName">
                            <a-input
                                v-model:value="formState.oidcProviderName"
                                :placeholder="$t('settings.authenticationDetail.providerNamePlaceholder')"
                                style="max-width: 300px"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.providerNameHint') }}</div>
                        </a-form-item>

                        <a-form-item label="Discovery URL" name="oidcDiscoveryUrl">
                            <a-input
                                v-model:value="formState.oidcDiscoveryUrl"
                                placeholder="https://accounts.google.com/.well-known/openid-configuration"
                                style="max-width: 500px"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.discoveryUrlHint') }}</div>
                        </a-form-item>

                        <a-form-item label="Client ID" name="oidcClientId">
                            <a-input
                                v-model:value="formState.oidcClientId"
                                :placeholder="$t('settings.authenticationDetail.clientIdPlaceholder')"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="Client Secret" name="oidcClientSecret">
                            <a-input-password
                                v-model:value="formState.oidcClientSecret"
                                :placeholder="$t('settings.authenticationDetail.clientSecretPlaceholder')"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item label="Scopes" name="oidcScopes">
                            <a-input
                                v-model:value="formState.oidcScopes"
                                placeholder="openid profile email"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.scopesHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.authenticationDetail.autoCreateUser')" name="oidcAutoCreateUser">
                            <a-switch
                                v-model:checked="formState.oidcAutoCreateUser"
                                :checked-children="$t('common.enabled')"
                                :un-checked-children="$t('common.disabled')"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.autoCreateUserHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.authenticationDetail.defaultRole')" name="oidcDefaultRole">
                            <a-select
                                v-model:value="formState.oidcDefaultRole"
                                style="max-width: 200px"
                                :options="roleOptions"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.defaultRoleHint') }}</div>
                        </a-form-item>
                    </template>
                </div>

                <!-- LDAP -->
                <div class="form-section">
                    <h3 class="section-title">LDAP / AD</h3>

                    <a-form-item :label="$t('settings.authenticationDetail.enableLdap')" name="ldapEnabled">
                        <a-switch
                            v-model:checked="formState.ldapEnabled"
                            :checked-children="$t('common.enabled')"
                            :un-checked-children="$t('common.disabled')"
                        />
                        <div class="form-hint">{{ $t('settings.authenticationDetail.ldapHint') }}</div>
                    </a-form-item>

                    <template v-if="formState.ldapEnabled">
                        <a-form-item :label="$t('settings.authenticationDetail.ldapServer')" name="ldapUrl">
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
                            <div class="form-hint">{{ $t('settings.authenticationDetail.bindDnHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.authenticationDetail.bindPassword')" name="ldapBindPassword">
                            <a-input-password
                                v-model:value="formState.ldapBindPassword"
                                :placeholder="$t('settings.authenticationDetail.bindPasswordPlaceholder')"
                                style="max-width: 400px"
                            />
                        </a-form-item>

                        <a-form-item :label="$t('settings.authenticationDetail.userSearchBase')" name="ldapBaseDn">
                            <a-input
                                v-model:value="formState.ldapBaseDn"
                                placeholder="ou=users,dc=example,dc=com"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.userSearchBaseHint') }}</div>
                        </a-form-item>

                        <a-form-item :label="$t('settings.authenticationDetail.userSearchFilter')" name="ldapUserFilter">
                            <a-input
                                v-model:value="formState.ldapUserFilter"
                                placeholder="(uid={username})"
                                style="max-width: 400px"
                            />
                            <div class="form-hint">{{ $t('settings.authenticationDetail.userSearchFilterHint') }}</div>
                        </a-form-item>
                    </template>
                </div>

                <!-- 测试连接 -->
                <div class="form-section" v-if="formState.oidcEnabled || formState.ldapEnabled">
                    <h3 class="section-title">{{ $t('settings.authenticationDetail.testConnection') }}</h3>
                    <a-form-item :label="$t('settings.authenticationDetail.testAuth')">
                        <a-space>
                            <a-button @click="testConnection" :loading="testing">
                                {{ $t('settings.authenticationDetail.testConnectionBtn') }}
                            </a-button>
                            <span v-if="testResult" :style="{ color: testResult.success ? 'green' : 'red' }">
                                {{ testResult.message }}
                            </span>
                        </a-space>
                    </a-form-item>
                </div>

                <!-- 提交按钮 -->
                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 140px">
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
import { ref, reactive, computed, onMounted } from 'vue'
import { message } from 'ant-design-vue'
import { useI18n } from 'vue-i18n'
import { systemSettingApi } from '@/api'

const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
const testing = ref(false)
const testResult = ref(null)

const formState = reactive({
    // 密码认证
    passwordEnabled: true,
    emailLoginEnabled: false,

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

const roleOptions = computed(() => [
    { label: t('profile.user'), value: 'user' },
    { label: t('settings.authenticationDetail.editorRole'), value: 'editor' },
    { label: t('profile.administrator'), value: 'admin' }
])

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
        message.success(t('settings.saveSuccess'))
    } catch (error) {
        message.error(t('settings.saveFailed'))
    } finally {
        saving.value = false
    }
}

const handleReset = () => {
    loadConfig()
    message.info(t('settings.resetToServer'))
}

const testConnection = async () => {
    testing.value = true
    testResult.value = null
    try {
        // TODO: 调用测试连接 API
        await new Promise(resolve => setTimeout(resolve, 1000))
        testResult.value = { success: true, message: t('settings.authenticationDetail.connectionSuccess') }
    } catch (error) {
        testResult.value = { success: false, message: t('settings.authenticationDetail.connectionFailed') + ': ' + error.message }
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
