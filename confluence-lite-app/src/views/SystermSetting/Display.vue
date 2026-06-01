<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>显示设置</h1>
            <p class="page-description">自定义系统界面的外观和显示方式</p>
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
                    <h3 class="section-title">主题设置</h3>

                    <a-form-item label="默认主题" name="defaultTheme">
                        <a-radio-group v-model:value="formState.defaultTheme" button-style="solid">
                            <a-radio-button value="light">浅色</a-radio-button>
                            <a-radio-button value="dark">深色</a-radio-button>
                            <a-radio-button value="auto">跟随系统</a-radio-button>
                        </a-radio-group>
                        <div class="form-hint">新用户的默认界面主题</div>
                    </a-form-item>

                    <a-form-item label="主题色" name="primaryColor">
                        <div class="color-picker-wrapper">
                            <input
                                type="color"
                                v-model="formState.primaryColor"
                                class="color-input"
                            />
                            <span class="color-value">{{ formState.primaryColor }}</span>
                            <a-button size="small" @click="resetColor">重置</a-button>
                        </div>
                        <div class="form-hint">系统主题的主色调</div>
                    </a-form-item>

                    <a-form-item label="紧凑模式" name="compactMode">
                        <a-switch
                            v-model:checked="formState.compactMode"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">启用后界面元素间距更紧凑</div>
                    </a-form-item>
                </div>

                <!-- 页面显示 -->
                <div class="form-section">
                    <h3 class="section-title">页面显示</h3>

                    <a-form-item label="每页显示条数" name="pageSize">
                        <a-select
                            v-model:value="formState.pageSize"
                            style="width: 120px"
                            :options="pageSizeOptions"
                        ></a-select>
                        <div class="form-hint">列表页面默认每页显示的条目数量</div>
                    </a-form-item>

                    <a-form-item label="页面树展开方式" name="pageTreeExpandMode">
                        <a-radio-group v-model:value="formState.pageTreeExpandMode">
                            <a-radio value="all">全部展开</a-radio>
                            <a-radio value="first">仅展开第一级</a-radio>
                            <a-radio value="none">全部折叠</a-radio>
                        </a-radio-group>
                        <div class="form-hint">侧边栏页面树的默认展开状态</div>
                    </a-form-item>

                    <a-form-item label="显示页面浏览量" name="showPageViews">
                        <a-switch
                            v-model:checked="formState.showPageViews"
                            checked-children="显示"
                            un-checked-children="隐藏"
                        />
                        <div class="form-hint">在页面底部显示浏览次数</div>
                    </a-form-item>

                    <a-form-item label="显示作者信息" name="showAuthorInfo">
                        <a-switch
                            v-model:checked="formState.showAuthorInfo"
                            checked-children="显示"
                            un-checked-children="隐藏"
                        />
                        <div class="form-hint">在页面显示创建者和最后更新者信息</div>
                    </a-form-item>

                    <a-form-item label="显示最后更新时间" name="showLastModified">
                        <a-switch
                            v-model:checked="formState.showLastModified"
                            checked-children="显示"
                            un-checked-children="隐藏"
                        />
                        <div class="form-hint">在页面显示最后更新时间</div>
                    </a-form-item>
                </div>

                <!-- 编辑器设置 -->
                <div class="form-section">
                    <h3 class="section-title">编辑器设置</h3>

                    <a-form-item label="默认编辑模式" name="defaultEditorMode">
                        <a-radio-group v-model:value="formState.defaultEditorMode">
                            <a-radio value="visual">可视化编辑器</a-radio>
                            <a-radio value="markdown">Markdown</a-radio>
                        </a-radio-group>
                        <div class="form-hint">创建或编辑页面时的默认编辑模式</div>
                    </a-form-item>

                    <a-form-item label="自动保存间隔" name="autoSaveInterval">
                        <a-input-number
                            v-model:value="formState.autoSaveInterval"
                            :min="10"
                            :max="300"
                            style="width: 120px"
                        />
                        <span style="margin-left: 8px">秒</span>
                        <div class="form-hint">编辑器自动保存草稿的时间间隔</div>
                    </a-form-item>

                    <a-form-item label="启用拼写检查" name="enableSpellCheck">
                        <a-switch
                            v-model:checked="formState.enableSpellCheck"
                            checked-children="开启"
                            un-checked-children="关闭"
                        />
                        <div class="form-hint">在编辑器中启用浏览器拼写检查</div>
                    </a-form-item>
                </div>

                <!-- 侧边栏设置 -->
                <div class="form-section">
                    <h3 class="section-title">侧边栏设置</h3>

                    <a-form-item label="默认侧边栏宽度" name="defaultSidebarWidth">
                        <a-slider
                            v-model:value="formState.defaultSidebarWidth"
                            :min="200"
                            :max="400"
                            :step="10"
                            style="width: 300px"
                        />
                        <span style="margin-left: 12px">{{ formState.defaultSidebarWidth }}px</span>
                        <div class="form-hint">页面树侧边栏的默认宽度</div>
                    </a-form-item>

                    <a-form-item label="显示空间图标" name="showSpaceIcon">
                        <a-switch
                            v-model:checked="formState.showSpaceIcon"
                            checked-children="显示"
                            un-checked-children="隐藏"
                        />
                        <div class="form-hint">在侧边栏空间头部显示图标</div>
                    </a-form-item>

                    <a-form-item label="允许折叠侧边栏" name="allowCollapseSidebar">
                        <a-switch
                            v-model:checked="formState.allowCollapseSidebar"
                            checked-children="允许"
                            un-checked-children="禁止"
                        />
                        <div class="form-hint">允许用户折叠/展开侧边栏</div>
                    </a-form-item>
                </div>

                <!-- 提交按钮 -->
                <a-form-item :wrapper-col="{ span: 16 }" style="margin-left: 120px">
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
    { label: '10 条/页', value: 10 },
    { label: '20 条/页', value: 20 },
    { label: '50 条/页', value: 50 },
    { label: '100 条/页', value: 100 }
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