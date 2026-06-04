<template>
  <a-config-provider :locale="antdLocale">
    <router-view />
  </a-config-provider>
</template>

<script setup>
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import zhCN from 'ant-design-vue/es/locale/zh_CN'
import enUS from 'ant-design-vue/es/locale/en_US'

const { locale } = useI18n()
const antdLocale = computed(() => locale.value === 'zh-CN' ? zhCN : enUS)
</script>

<style>
/* Global styles handled in main.css */
</style>
