<template>
    <div class="settings-page">
        <div class="page-header">
            <h1>常规设置</h1>
            <p class="page-description">配置站点的基本信息和默认选项</p>
        </div>

        <a-spin :spinning="loading">
            <a-form
                :model="formState"
                :label-col="{ style: { width: '100px' } }"
                :wrapper-col="{ span: 16 }"
                class="settings-form"
                @finish="handleSubmit"
            >
                <!-- 站点名称 -->
                <a-form-item label="站点名称" name="siteName" :rules="[{ required: true, message: '请输入站点名称' }]">
                    <a-input
                        v-model:value="formState.siteName"
                        placeholder="例如：我的知识库"
                        style="max-width: 400px"
                    />
                    <div class="form-hint">显示在浏览器标题和页面头部的名称</div>
                </a-form-item>

                <!-- 站点描述 -->
                <a-form-item label="站点描述" name="siteDescription">
                    <a-textarea
                        v-model:value="formState.siteDescription"
                        placeholder="简要描述您的站点用途"
                        :rows="3"
                        style="max-width: 400px"
                    />
                    <div class="form-hint">用于 SEO 和站点说明</div>
                </a-form-item>

                <!-- 站点标签 -->
                <a-form-item label="站点标签" name="siteTags">
                    <a-select
                        v-model:value="formState.siteTags"
                        mode="tags"
                        placeholder="添加标签"
                        style="max-width: 400px"
                        :options="tagOptions"
                    ></a-select>
                    <div class="form-hint">为站点添加分类标签，便于管理和搜索</div>
                </a-form-item>

                <!-- 域名 -->
                <a-form-item label="站点域名" name="siteDomain">
                    <a-input
                        v-model:value="formState.siteDomain"
                        placeholder="例如：wiki.example.com"
                        style="max-width: 400px"
                    />
                    <div class="form-hint">用于生成链接和通知邮件</div>
                </a-form-item>

                <!-- 默认语言 -->
                <a-form-item label="默认语言" name="defaultLanguage">
                    <a-select
                        v-model:value="formState.defaultLanguage"
                        style="max-width: 200px"
                        :options="languageOptions"
                    ></a-select>
                    <div class="form-hint">新用户的默认界面语言</div>
                </a-form-item>

                <!-- 默认首页 -->
                <a-form-item label="默认首页" name="defaultHomePage">
                    <a-select
                        v-model:value="formState.defaultHomePage"
                        placeholder="选择默认首页"
                        style="max-width: 400px"
                        :options="homePageOptions"
                        :field-names="{ label: 'name', value: 'id' }"
                        show-search
                        :filter-option="filterHomePage"
                    ></a-select>
                    <div class="form-hint">用户登录后默认显示的页面</div>
                </a-form-item>

                <!-- 时区设置 -->
                <a-form-item label="时区" name="timezone">
                    <a-select
                        v-model:value="formState.timezone"
                        show-search
                        placeholder="选择时区"
                        style="max-width: 400px"
                        :options="timezoneOptions"
                        :filter-option="filterTimezone"
                    ></a-select>
                    <div class="form-hint">用于显示日期和时间的时区</div>
                </a-form-item>

                <!-- 允许注册 -->
                <a-form-item label="允许注册" name="allowRegistration">
                    <a-switch
                        v-model:checked="formState.allowRegistration"
                        checked-children="开启"
                        un-checked-children="关闭"
                    />
                    <div class="form-hint">是否允许新用户自行注册账号</div>
                </a-form-item>

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
import { ref, onMounted, computed } from 'vue'
import { message } from 'ant-design-vue'
import { systemSettingApi, workspaceApi, pageApi } from '@/api'

const loading = ref(false)
const saving = ref(false)

// 表单数据
const formState = ref({
    siteName: '',
    siteDescription: '',
    siteTags: [],
    siteDomain: '',
    defaultLanguage: 'zh-CN',
    defaultHomePage: null,
    timezone: 'Asia/Shanghai',
    allowRegistration: true
})

// 标签选项
const tagOptions = ref([
    { label: '文档', value: 'documentation' },
    { label: '知识库', value: 'knowledge' },
    { label: '团队', value: 'team' },
    { label: '产品', value: 'product' },
    { label: '技术', value: 'technical' },
    { label: 'API', value: 'api' }
])

// 语言选项
const languageOptions = ref([
    { label: '简体中文', value: 'zh-CN' },
    { label: '繁體中文', value: 'zh-TW' },
    { label: 'English', value: 'en-US' },
    { label: '日本語', value: 'ja-JP' },
    { label: '한국어', value: 'ko-KR' }
])

// 首页选项（从工作空间和页面加载）
const homePageOptions = ref([])

// 时区选项
const timezoneOptions = ref([
    { label: '(GMT+08:00) 北京/上海/香港/台北', value: 'Asia/Shanghai' },
    { label: '(GMT+09:00) 东京/首尔', value: 'Asia/Tokyo' },
    { label: '(GMT+00:00) 伦敦/都柏林', value: 'Europe/London' },
    { label: '(GMT-05:00) 纽约/波士顿', value: 'America/New_York' },
    { label: '(GMT-08:00) 太平洋时间 (洛杉矶)', value: 'America/Los_Angeles' },
    { label: '(GMT+10:00) 悉尼/墨尔本', value: 'Australia/Sydney' }
])

// 加载配置
const loadConfig = async () => {
    loading.value = true
    try {
        const data = await systemSettingApi.getSiteConfig()
        if (data) {
            formState.value = {
                ...formState.value,
                ...data,
                siteTags: data.siteTags || []
            }
        }
    } catch (error) {
        console.error('加载配置失败:', error)
    } finally {
        loading.value = false
    }
}

// 加载首页选项
const loadHomePageOptions = async () => {
    try {
        const spaces = await workspaceApi.getMy()
        const options = []

        for (const space of spaces) {
            // 添加空间首页
            options.push({
                id: `space:${space.id}`,
                name: `📁 ${space.name} (${space.key})`,
                type: 'space'
            })

            // 加载空间的页面树
            try {
                const pages = await pageApi.getTree(space.id)
                if (pages && pages.length > 0) {
                    pages.forEach(page => {
                        options.push({
                            id: `page:${page.id}`,
                            name: `  📄 ${page.title}`,
                            type: 'page',
                            spaceKey: space.key
                        })
                    })
                }
            } catch (e) {
                console.warn(`加载空间 ${space.key} 的页面失败:`, e)
            }
        }

        homePageOptions.value = options
    } catch (error) {
        console.error('加载首页选项失败:', error)
    }
}

// 搜索首页
const filterHomePage = (input, option) => {
    return option.name.toLowerCase().includes(input.toLowerCase())
}

// 搜索时区
const filterTimezone = (input, option) => {
    return option.label.toLowerCase().includes(input.toLowerCase())
}

// 提交表单
const handleSubmit = async () => {
    saving.value = true
    try {
        await systemSettingApi.updateSiteConfig(formState.value)
        message.success('设置保存成功')
    } catch (error) {
        console.error('保存设置失败:', error)
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
    loadHomePageOptions()
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
