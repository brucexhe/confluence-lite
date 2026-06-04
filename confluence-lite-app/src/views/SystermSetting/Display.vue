<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>{{ $t('settings.display.title') }}</h1>
            <p class="page-description">{{ $t('settings.display.description') }}</p>
        </div>

        <a-spin :spinning="loading">
            <a-form
                :model="formState"
                :label-col="{ style: { width: '120px' } }"
                :wrapper-col="{ span: 16 }"
                class="settings-form"
                @finish="handleSubmit"
            >
                <!-- 主题设置 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.display.themeSettings') }}</h3>

                    <a-form-item :label="$t('settings.display.defaultTheme')" name="defaultTheme">
                        <a-radio-group v-model:value="formState.defaultTheme" button-style="solid">
                            <a-radio-button value="light">{{ $t('settings.display.themeLight') }}</a-radio-button>
                            <a-radio-button value="dark">{{ $t('settings.display.themeDark') }}</a-radio-button>
                            <a-radio-button value="auto">{{ $t('settings.display.themeAuto') }}</a-radio-button>
                        </a-radio-group>
                        <div class="form-hint">{{ $t('settings.display.defaultThemeHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.primaryColor')" name="primaryColor">
                        <div class="color-picker-wrapper">
                            <input
                                type="color"
                                v-model="formState.primaryColor"
                                class="color-input"
                            />
                            <span class="color-value">{{ formState.primaryColor }}</span>
                            <a-button size="small" @click="resetColor">{{ $t('common.reset') }}</a-button>
                        </div>
                        <div class="form-hint">{{ $t('settings.display.primaryColorHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.compactMode')" name="compactMode">
                        <a-switch
                            v-model:checked="formState.compactMode"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.display.compactModeHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 页面显示 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.display.pageDisplay') }}</h3>

                    <a-form-item :label="$t('settings.display.pageSize')" name="pageSize">
                        <a-select
                            v-model:value="formState.pageSize"
                            style="width: 120px"
                            :options="pageSizeOptions"
                        ></a-select>
                        <div class="form-hint">{{ $t('settings.display.pageSizeHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.pageTreeExpandMode')" name="pageTreeExpandMode">
                        <a-radio-group v-model:value="formState.pageTreeExpandMode">
                            <a-radio value="all">{{ $t('settings.display.expandAll') }}</a-radio>
                            <a-radio value="first">{{ $t('settings.display.expandFirst') }}</a-radio>
                            <a-radio value="none">{{ $t('settings.display.collapseAll') }}</a-radio>
                        </a-radio-group>
                        <div class="form-hint">{{ $t('settings.display.pageTreeExpandModeHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.showPageViews')" name="showPageViews">
                        <a-switch
                            v-model:checked="formState.showPageViews"
                            :checked-children="$t('common.show')"
                            :un-checked-children="$t('common.hide')"
                        />
                        <div class="form-hint">{{ $t('settings.display.showPageViewsHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.showAuthorInfo')" name="showAuthorInfo">
                        <a-switch
                            v-model:checked="formState.showAuthorInfo"
                            :checked-children="$t('common.show')"
                            :un-checked-children="$t('common.hide')"
                        />
                        <div class="form-hint">{{ $t('settings.display.showAuthorInfoHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.showLastModified')" name="showLastModified">
                        <a-switch
                            v-model:checked="formState.showLastModified"
                            :checked-children="$t('common.show')"
                            :un-checked-children="$t('common.hide')"
                        />
                        <div class="form-hint">{{ $t('settings.display.showLastModifiedHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 编辑器设置 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.display.editorSettings') }}</h3>

                    <a-form-item :label="$t('settings.display.defaultEditorMode')" name="defaultEditorMode">
                        <a-radio-group v-model:value="formState.defaultEditorMode">
                            <a-radio value="visual">{{ $t('settings.display.visualEditor') }}</a-radio>
                            <a-radio value="markdown">Markdown</a-radio>
                        </a-radio-group>
                        <div class="form-hint">{{ $t('settings.display.defaultEditorModeHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.autoSaveInterval')" name="autoSaveInterval">
                        <a-input-number
                            v-model:value="formState.autoSaveInterval"
                            :min="10"
                            :max="300"
                            style="width: 120px"
                        />
                        <span style="margin-left: 8px">{{ $t('settings.display.seconds') }}</span>
                        <div class="form-hint">{{ $t('settings.display.autoSaveIntervalHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.enableSpellCheck')" name="enableSpellCheck">
                        <a-switch
                            v-model:checked="formState.enableSpellCheck"
                            :checked-children="$t('common.on')"
                            :un-checked-children="$t('common.off')"
                        />
                        <div class="form-hint">{{ $t('settings.display.enableSpellCheckHint') }}</div>
                    </a-form-item>
                </div>

                <!-- 侧边栏设置 -->
                <div class="form-section">
                    <h3 class="section-title">{{ $t('settings.display.sidebarSettings') }}</h3>

                    <a-form-item :label="$t('settings.display.defaultSidebarWidth')" name="defaultSidebarWidth">
                        <a-slider
                            v-model:value="formState.defaultSidebarWidth"
                            :min="200"
                            :max="400"
                            :step="10"
                            style="width: 300px"
                        />
                        <span style="margin-left: 12px">{{ formState.defaultSidebarWidth }}px</span>
                        <div class="form-hint">{{ $t('settings.display.defaultSidebarWidthHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.showSpaceIcon')" name="showSpaceIcon">
                        <a-switch
                            v-model:checked="formState.showSpaceIcon"
                            :checked-children="$t('common.show')"
                            :un-checked-children="$t('common.hide')"
                        />
                        <div class="form-hint">{{ $t('settings.display.showSpaceIconHint') }}</div>
                    </a-form-item>

                    <a-form-item :label="$t('settings.display.allowCollapseSidebar')" name="allowCollapseSidebar">
                        <a-switch
                            v-model:checked="formState.allowCollapseSidebar"
                            :checked-children="$t('common.allow')"
                            :un-checked-children="$t('common.forbid')"
                        />
                        <div class="form-hint">{{ $t('settings.display.allowCollapseSidebarHint') }}</div>
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

const DEFAULT_PRIMARY_COLOR = '#0052cc'

// 表单数据
const formState = ref({
    // 主题设置
    defaultTheme: 'light',
    primaryColor: DEFAULT_PRIMARY_COLOR,
    compactMode: false,

    // 页面显示
    pageSize: 20,
    pageTreeExpandMode: 'none',
    showPageViews: true,
    showAuthorInfo: true,
    showLastModified: true,

    // 编辑器设置
    defaultEditorMode: 'visual',
    autoSaveInterval: 60,
    enableSpellCheck: true,

    // 侧边栏设置
    defaultSidebarWidth: 260,
    showSpaceIcon: true,
    allowCollapseSidebar: true
})

// 每页条数选项
const pageSizeOptions = ref([
    { label: t('settings.display.pageSizeOption', { n: 10 }), value: 10 },
    { label: t('settings.display.pageSizeOption', { n: 20 }), value: 20 },
    { label: t('settings.display.pageSizeOption', { n: 50 }), value: 50 },
    { label: t('settings.display.pageSizeOption', { n: 100 }), value: 100 }
])

// 重置主题色
const resetColor = () => {
    formState.value.primaryColor = DEFAULT_PRIMARY_COLOR
}

// 加载配置
const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getDisplayConfig()
        if (data) {
            formState.value = {
                ...formState.value,
                ...data
            }
        }
    } catch (error) {
        console.error('加载显示配置失败:', error)
    } finally {
        loading.value = false
    }
}

// 提交表单
const handleSubmit = async () => {
    saving.value = true
    try {
        await systemSettingApi.updateDisplayConfig(formState.value)
        message.success('显示设置保存成功')
    } catch (error) {
        console.error('保存显示设置失败:', error)
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

.color-picker-wrapper {
    display: flex;
    align-items: center;
    gap: 8px;
}

.color-input {
    width: 40px;
    height: 28px;
    border: 1px solid #dfe1e6;
    border-radius: 3px;
    cursor: pointer;
    padding: 2px;
}

.color-value {
    font-family: 'Monaco', 'Consolas', monospace;
    font-size: 12px;
    color: #42526e;
    min-width: 60px;
}
</style> 