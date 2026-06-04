<template>
  <div class="recent-page">
    <div class="header">
      <h1>{{ $t('recent.title') }}</h1>
      <p class="subtitle">{{ $t('recent.description') }}</p>
    </div>

    <div class="content">
      <div :bordered="false" class="recent-card">
        <a-list
          :loading="loading"
          item-layout="horizontal"
          :data-source="recentPages"
        >
          <template #renderItem="{ item }">
            <a-list-item class="recent-item" @click="navigateToPage(item)">
              <a-list-item-meta>
                <template #title>
                  <div class="item-title">
                    <span class="page-title">{{ item.title }}</span>
                    <span class="space-badge">{{ item.spaceName }}</span>
                  </div>
                </template>
                <template #description>
                  <div class="item-meta">
                    <span class="author">{{ $t('recent.createdBy', { name: item.creatorName }) }}</span>
                    <span class="divider">•</span>
                    <span class="visit-time">{{ $t('recent.lastVisited', { time: formatTime(item.visitedAt) }) }}</span>
                  </div>
                </template>
                <template #avatar>
                   <div class="file-icon">
                     <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                       <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"></path>
                       <polyline points="14 2 14 8 20 8"></polyline>
                     </svg>
                   </div>
                </template>
              </a-list-item-meta>
            </a-list-item>
          </template>
          <template #empty>
            <div class="empty-state">
              <a-empty :description="$t('recent.empty')" />
            </div>
          </template>
        </a-list>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { recentApi } from '../api';
import { message } from 'ant-design-vue';
import { useI18n } from 'vue-i18n';

const router = useRouter();
const recentPages = ref([]);
const loading = ref(true);
const { t, locale } = useI18n();

const fetchRecents = async () => {
  try {
    loading.value = true;
    const data = await recentApi.getList();
    recentPages.value = data || [];
  } catch (err) {
    console.error('获取最近访问失败:', err);
    message.error(t('recent.loadFailed'));
  } finally {
    loading.value = false;
  }
};

const navigateToPage = (item) => {
  router.push(`/${item.spaceKey}/page/${item.pageId}`);
};

const formatTime = (dateStr) => {
  if (!dateStr) return '';
  const date = new Date(dateStr);
  const now = new Date();
  const diff = now - date;
  
  const minutes = Math.floor(diff / 60000);
  if (minutes < 1) return t('common.justNow');
  if (minutes < 60) return t('common.minutesAgo', { n: minutes });
  
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return t('common.hoursAgo', { n: hours });
  
  const days = Math.floor(hours / 24);
  if (days < 7) return t('common.daysAgo', { n: days });
  
  return date.toLocaleDateString(locale.value, {
    month: 'long', 
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

onMounted(() => {
  fetchRecents();
});
</script>

<style scoped>
.recent-page {
  padding: 40px 2rem; 
  margin: 0 auto;
}

.header {
  margin-bottom: 32px;
}

.header h1 {
  font-size: 28px;
  font-weight: 600;
  color: #172b4d;
  margin-bottom: 8px;
}

.subtitle {
  color: #6b778c;
  font-size: 14px;
}

.recent-card {
  background: #fff; 
}

.recent-item {
  font-size: 14px;
  padding: 10px 5px !important;
  cursor: pointer;
  transition: background-color 0.2s;
  border-bottom: 1px solid #f4f5f7 !important;
}

.recent-item:hover {
  background-color: #f4f5f7;
}

.item-title {
  display: flex;
  align-items: center;
  gap: 12px;
}

.page-title {
  font-size: 14px;
  font-weight: 500;
  color: #0052cc;
}

.space-badge {
  font-size: 11px;
  background: #ebecf0;
  color: #42526e;
  padding: 2px 6px;
  border-radius: 3px;
  text-transform: uppercase;
  font-weight: 600;
}

.item-meta {
  display: flex;
  align-items: center;
  font-size: 12px;
  color: #6b778c;
  margin-top: 4px;
}

.divider {
  margin: 0 8px;
  color: #dfe1e6;
}

.file-icon {
  width: 40px;
  height: 40px;
  background: #deebff;
  color: #0052cc;
  border-radius: 4px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.empty-state {
  padding: 64px 0;
}

:deep(.ant-list-item-meta-avatar) {
  margin-right: 20px;
}

/* ==================== Mobile Responsive ==================== */
@media (max-width: 768px) {
  .recent-page {
    padding: 20px 1rem;
  }

  .header h1 {
    font-size: 22px;
  }

  .item-title {
    flex-wrap: wrap;
    gap: 6px;
  }

  .item-meta {
    flex-wrap: wrap;
  }

  :deep(.ant-list-item-meta-avatar) {
    margin-right: 12px;
  }
}
</style>
