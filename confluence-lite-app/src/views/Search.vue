<template>
    <div class="search-page">
        <div class="search-header">
            <h1>Search Results</h1>
            <div class="search-info" v-if="!loading">Found {{ results.length }} results for "{{ searchKey }}"</div>
        </div>

        <div class="search-results-container">
            <div v-if="loading" class="loading-state">
                <a-spin size="large" />
            </div>

            <div v-else-if="results.length > 0" class="results-list">
                <div v-for="item in results" :key="item.type + item.id" class="result-item">
                    <div class="result-icon">
                        <FileText v-if="item.type === 'page'" :size="18" />
                        <template v-else>
                            <Image v-if="isImage(item.contentType)" :size="18" />
                            <Paperclip v-else :size="18" />
                        </template>
                    </div>
                    <div class="result-content">
                        <h3 class="result-title">
                            <router-link :to="getResultLink(item)" v-html="item.title">
                            </router-link>
                        </h3>
                        <div class="result-snippet" v-html="item.content"></div>
                        <div class="result-meta">
                            <span class="space-tag">{{ item.spaceName }}</span>
                            <span class="meta-divider">·</span>
                            <span class="updated-at">Updated {{ formatDate(item.updatedAt) }}</span>
                            <span class="meta-divider">·</span>
                            <span class="creator">By {{ item.creatorName }}</span>
                        </div>
                    </div>
                </div>
            </div>

            <div v-else class="no-results">
                <div class="no-results-icon">🔍</div>
                <h2>No results found</h2>
                <p>Try different keywords or check your spelling.</p>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, watch, computed } from "vue";
import { useRoute } from "vue-router";
import { searchApi } from "../api";
import dayjs from "dayjs";
import relativeTime from "dayjs/plugin/relativeTime";
import { FileText, Paperclip, Image } from "lucide-vue-next";

dayjs.extend(relativeTime);

const route = useRoute();
const loading = ref(true);
const results = ref([]);
const searchKey = computed(() => route.query.key || "");

const fetchResults = async () => {
    if (!searchKey.value) {
        results.value = [];
        loading.value = false;
        return;
    }

    loading.value = true;
    try {
        const data = await searchApi.searchAll(searchKey.value);
        results.value = data || [];
    } catch (error) {
        console.error("Failed to fetch search results:", error);
    } finally {
        loading.value = false;
    }
};

const getResultLink = (item) => {
    if (item.type === "page") {
        return `/${item.spaceKey}/page/${item.id}`;
    } else {
        // 附件链接，通常跳转到所属页面或直接下载
        // 假设跳到页面
        return `/${item.spaceKey}/page/${item.pageId || item.id}`;
    }
};

const formatDate = (date) => {
    return dayjs(date).fromNow();
};

const isImage = (contentType) => {
    return contentType?.startsWith("image/");
};

onMounted(fetchResults);

watch(() => route.query.key, fetchResults);
</script>

<style scoped>
.search-page {
    margin: 0px auto;
    padding: 40px 2rem;
}

.search-header {
    margin-bottom: 2rem;
    border-bottom: 1px solid var(--color-border);
    padding-bottom: 1rem;
}

.search-header h1 {
    font-size: 1.5rem;
    font-weight: 600;
    color: var(--color-text-primary);
    margin-bottom: 0.5rem;
}

.search-info {
    color: var(--color-text-secondary);
    font-size: 0.9rem;
}

.search-results-container {
    min-height: 400px;
}

.loading-state {
    display: flex;
    justify-content: center;
    align-items: center;
    height: 200px;
}

.results-list {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.result-item {
    display: flex;
    gap: 1rem;
}

.result-icon {
    flex-shrink: 0;
    padding-top: 4px;
    color: var(--color-text-secondary);
}

.result-title {
    font-size: 14px;
    font-weight: 500;
    margin-bottom: 0.25rem;
}

.result-title a {
    color: #0052cc;
    text-decoration: none;
}

.result-title a:hover {
    text-decoration: underline;
}

.result-snippet {
    font-size: 14px;
    color: var(--color-text-secondary);
    line-height: 1.5;
    margin-bottom: 0.5rem;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

:deep(mark) {
    background-color: #fffae6;
    color: #172b4d;
    font-weight: 600;
    padding: 0 2px;
    border-radius: 2px;
}

.result-meta {
    display: flex;
    align-items: center;
    font-size: 0.8rem;
    color: var(--color-text-muted);
    gap: 0.5rem;
}

.space-tag {
    background: var(--color-bg-secondary);
    padding: 2px 6px;
    border-radius: 3px;
    font-weight: 500;
}

.no-results {
    text-align: center;
    padding: 4rem 0;
}

.no-results-icon {
    font-size: 4rem;
    margin-bottom: 1rem;
}

.no-results h2 {
    font-size: 1.25rem;
    margin-bottom: 0.5rem;
}

.no-results p {
    color: var(--color-text-secondary);
}
</style>
