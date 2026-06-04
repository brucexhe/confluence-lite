<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.officePreview.title') }}</h1>
            <p class="page-description">{{ $t('settings.officePreview.description') }}</p>
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
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.officePreview.serviceConfig') }}</h3>

                    <a-form-item :label="$t('settings.officePreview.enablePreview')" name="enabled">
                        <a-switch
                            v-model:checked="formState.enabled"
                            :checked-children="$t('common.enabled')"
                            :un-checked-children="$t('common.disabled')"
                        />
                        <div class="form-hint">{{ $t('settings.officePreview.enablePreviewHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.officePreview.serviceUrl')"
                        name="baseUrl"
                        :rules="[{ required: formState.enabled, message: t('settings.officePreview.serviceUrlRequired') }]"
                    >
                        <a-input
                            v-model:value="formState.baseUrl"
                            :placeholder="$t('settings.officePreview.serviceUrlPlaceholder')"
                            style="max-width: 400px"
                            :disabled="!formState.enabled"
                        />
                        <div class="form-hint">{{ $t('settings.officePreview.serviceUrlHint') }}</div>
                    </a-form-item>

                    <a-form-item
                        :label="$t('settings.officePreview.timeout')"
                        name="timeoutSeconds"
                    >
                        <a-input-number
                            v-model:value="formState.timeoutSeconds"
                            :min="30"
                            :max="600"
                            style="width: 150px"
                            :disabled="!formState.enabled"
                        />
                        <span style="margin-left: 8px; color: #6b778c; font-size: 13px">{{ $t('settings.officePreview.seconds') }}</span>
                        <div class="form-hint">{{ $t('settings.officePreview.timeoutHint') }}</div>
                    </a-form-item>
                </div>

                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
                    <a-space>
                        <a-button type="primary" html-type="submit" :loading="saving">
                            {{ $t('settings.saveSettings') }}
                        </a-button>
                        <a-button @click="handleReset">{{ $t('common.reset') }}</a-button>
                        <a-button
                            @click="handleTest"
                            :loading="testing"
                            :disabled="!formState.enabled || !formState.baseUrl"
                        >
                            {{ $t('settings.officePreview.testConnection') }}
                        </a-button>
                    </a-space>
                </a-form-item>
            </a-form>
        </a-spin>
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

const formState = reactive({
    enabled: false,
    baseUrl: 'http://gotenberg:3000',
    timeoutSeconds: 120
})

const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getOfficePreviewConfig()
        if (data) {
            Object.assign(formState, data)
        }
    } catch (error) {
        console.error('加载配置失败:', error)
    } finally {
        loading.value = false
    }
}

const handleSubmit = async () => {
    try {
        await formRef.value.validate()
    } catch {
        return
    }

    saving.value = true
    try {
        await systemSettingApi.updateOfficePreviewConfig(formState)
        message.success(t('settings.officePreview.saveSuccess'))
    } catch (error) {
        console.error('保存设置失败:', error)
        message.error(t('settings.saveFailed'))
    } finally {
        saving.value = false
    }
}

const handleReset = () => {
    loadConfig()
    message.info(t('settings.resetToServer'))
}

const handleTest = async () => {
    testing.value = true
    try {
        const result = await systemSettingApi.testOfficePreviewConfig(formState)
        if (result === true) {
            message.success(t('settings.officePreview.connectionSuccess'))
        }
    } catch (error) {
        console.error('测试连接失败:', error)
        message.error(t('settings.officePreview.connectionFailed'))
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
