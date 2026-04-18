<template>
  <div class="not-found-container">
    <div class="not-found-content">
      <div class="error-icon">404</div>
      <h2>{{ title }}</h2>
      <p>{{ message }}</p>
      <button class="back-btn" @click="goBack">{{ buttonText }}</button>
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'

const router = useRouter()
const route = useRoute()

// 根据路由参数动态设置标题和消息
const title = computed(() => route.meta.notFoundTitle || '页面不存在')
const message = computed(() => route.meta.notFoundMessage || '您访问的页面不存在')
const buttonText = computed(() => route.meta.notFoundButton || '返回首页')

function goBack() {
  // 如果有指定的返回路径，使用它；否则返回首页
  const backPath = route.meta.notFoundBackPath
  if (backPath) {
    router.push(backPath)
  } else {
    router.push('/')
  }
}
</script>

<style scoped>
.not-found-container {
  min-height: calc(100vh - 64px);
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f4f5f7;
}

.not-found-content {
  text-align: center;
  padding: 48px 60px;
  background: white;
  border-radius: 12px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
  width: 500px;
}

.error-icon {
  font-size: 72px;
  font-weight: bold;
  color: #0049b0;
  margin-bottom: 16px;
}

h2 {
  color: #172b4d;
  margin: 0 0 8px;
  font-size: 20px;
  font-weight: 600;
}

p {
  color: #6b778c;
  font-size: 14px;
  margin: 0 0 24px;
}

.back-btn {
  background: #0049b0;
  color: white;
  border: none;
  padding: 10px 24px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 14px;
  transition: background 0.2s;
}

.back-btn:hover {
  background: #0747a6;
}
</style>
