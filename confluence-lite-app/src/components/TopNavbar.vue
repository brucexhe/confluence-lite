<template>
    <header class="top-navbar">
        <div class="nav-left">
            <img v-if="siteLogo" class="logo" :src="siteLogo" alt="" @click="navigateTo('/')" style="cursor: pointer" />
            <div v-else class="logo" @click="navigateTo('/')" style="cursor: pointer"></div>
            <a class="nav-title" href="/" style="cursor: pointer; color: #fff">{{ siteName }}</a>
            <nav class="nav-links">
                <a-dropdown :trigger="['click']" placement="bottomLeft">
                    <a class="nav-link dropdown-link" @click.prevent>
                        Spaces
                        <svg
                            width="14"
                            height="14"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2"
                            stroke-linecap="round"
                            stroke-linejoin="round"
                            style="margin-left: 2px; margin-top: 1px"
                        >
                            <polyline points="6 9 12 15 18 9"></polyline>
                        </svg>
                    </a>
                    <template #overlay>
                        <a-menu
                            style="
                                width: 280px;
                                padding: 8px 0;
                                border-radius: 3px;
                                box-shadow:
                                    0 4px 8px -2px rgba(9, 30, 66, 0.25),
                                    0 0 1px rgba(9, 30, 66, 0.31);
                            "
                        >
                            <div
                                style="
                                    padding: 4px 16px 8px;
                                    font-size: 11px;
                                    font-weight: 700;
                                    color: #6b778c;
                                    text-transform: uppercase;
                                "
                            >
                                Recent Spaces
                            </div>
                            <a-menu-item
                                v-for="space in spaces"
                                :key="space.id"
                                style="padding: 8px 16px"
                                @click="navigateToSpace(space.key)"
                            >
                                <div style="display: flex; align-items: center; gap: 12px">
                                    <img
                                        v-if="space.icon && isImageUrl(space.icon)"
                                        :src="space.icon"
                                        :style="{
                                            width: '32px',
                                            height: '32px',
                                            borderRadius: '3px',
                                            objectFit: 'cover',
                                        }"
                                    />
                                    <div
                                        v-else
                                        :style="{
                                            width: '32px',
                                            height: '32px',
                                            background: space.icon || getSpaceColorById(space.id),
                                            borderRadius: '3px',
                                            display: 'flex',
                                            alignItems: 'center',
                                            justifyContent: 'center',
                                            color: '#fff',
                                            fontWeight: 600,
                                            fontSize: '14px',
                                        }"
                                    >
                                        {{ getSpaceInitial(space) }}
                                    </div>
                                    <div>
                                        <div
                                            style="font-weight: 500; color: #172b4d; font-size: 14px; line-height: 1.2"
                                        >
                                            {{ space.name || space.key }}
                                        </div>
                                        <div style="font-size: 12px; color: #6b778c">{{ space.key }}</div>
                                    </div>
                                </div>
                            </a-menu-item>
                            <a-menu-item v-if="spaces.length === 0" disabled style="padding: 8px 16px">
                                <span style="color: #6b778c; font-size: 13px">暂无空间</span>
                            </a-menu-item>
                            <a-menu-divider v-if="spaces.length > 0" />
                            <a-menu-item key="view-all" style="padding: 4px 16px" @click="navigateTo('/spaces')">
                                <span style="color: #0052cc; font-size: 14px; font-weight: 500">View all spaces</span>
                            </a-menu-item>
                        </a-menu>
                    </template>
                </a-dropdown>
                <a href="#" class="nav-link">Recent</a>
                <a href="#" class="nav-link">People</a>
            </nav>
        </div>
        <div class="nav-right">
            <div class="search-box">
                <a-auto-complete
                    v-model:value="searchText"
                    :options="suggestions"
                    style="width: 220px"
                    @search="handleSearchInput"
                    @select="handleSelect"
                >
                    <a-input-search 
                        placeholder="Search..." 
                        class="confluence-search" 
                        @search="onFullSearch" 
                    />
                    <template #option="item">
                        <div class="suggestion-item">
                            <span class="suggestion-icon">
                                <FileText v-if="item.type === 'page'" :size="14" />
                                <template v-else>
                                    <Image v-if="isImage(item.contentType)" :size="14" />
                                    <Paperclip v-else :size="14" />
                                </template>
                            </span>
                            <span class="suggestion-title">{{ item.title }}</span>
                            <span class="suggestion-space">{{ item.spaceKey }}</span>
                        </div>
                    </template>
                </a-auto-complete>
            </div>
            <button class="create-btn" @click="handleCreate">Create</button>
            <a-dropdown :trigger="['click']">
                <UserAvatar
                    :user="authStore.user"
                    :size="28"
                    style="cursor: pointer"
                    @click.prevent
                />
                <template #overlay>
                    <a-menu style="min-width: 120px; padding: 4px 0">
                        <a-menu-item @click="navigateTo('/spaces')">
                            <span style="font-size: 14px; color: #172b4d">空间列表</span>
                        </a-menu-item>
                        <a-menu-item @click="navigateTo('/recent')">
                            <span style="font-size: 14px; color: #172b4d">最近浏览</span>
                        </a-menu-item>
                        <a-menu-item @click="navigateTo('/profile')">
                            <span style="font-size: 14px; color: #172b4d">用户信息</span>
                        </a-menu-item>
                        <a-menu-item @click="navigateTo('/settings')">
                            <span style="font-size: 14px; color: #172b4d">系统设置</span>
                        </a-menu-item>
                        <a-menu-divider />
                        <a-menu-item @click="handleLogout">
                            <span style="font-size: 14px; color: #ef4444">退出登录</span>
                        </a-menu-item>
                    </a-menu>
                </template>
            </a-dropdown>
        </div>
    </header>
</template>

<script setup>
import { computed, ref } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../store/auth";
import { useSiteInfo } from "../store/site";
import { getSpaceColorById, getSpaceInitial } from "../utils/workspace";
import UserAvatar from "./UserAvatar.vue";
import { searchApi } from "../api";
import { FileText, Paperclip, Image } from "lucide-vue-next";

const searchText = ref("");
const suggestions = ref([]);

const handleSearchInput = async (val) => {
    if (!val) {
        suggestions.value = [];
        return;
    }
    try {
        const res = await searchApi.getSuggestions(val);
        if (res && res.length > 0) {
            suggestions.value = res.map(item => ({
                value: item.title,
                id: item.id,
                title: item.title,
                spaceKey: item.spaceKey,
                type: item.type,
                contentType: item.contentType
            }));
        } else {
            suggestions.value = [];
        }
    } catch (error) {
        console.error("Search suggestions error:", error);
    }
};

const handleSelect = (val, option) => {
    if (option.id) {
        router.push(`/${option.spaceKey}/page/${option.id}`);
        searchText.value = "";
    }
};

const onFullSearch = (val) => {
    if (val) {
        router.push({ path: '/search', query: { key: val } });
        searchText.value = "";
    }
};

const isImage = (contentType) => {
    return contentType?.startsWith("image/");
};

function isImageUrl(icon) {
    if (!icon) return false;
    return /^(https?:\/\/|data:image\/|\/)/.test(icon);
}

const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { siteName, siteLogo } = useSiteInfo();

// 空间列表（从 localStorage 读取）
const spaces = computed(() => {
    return JSON.parse(localStorage.getItem("auth_spaces") || "[]");
});

function navigateToSpace(key) {
    const upperKey = key.toUpperCase();
    const currentKey = route.params.spaceKey?.toUpperCase();

    // 如果是同一个空间，不处理
    if (upperKey === currentKey) return;

    // 导航到空间首页
    router.push(`/${upperKey}`);
}


const handleLogout = () => {
    authStore.logout();
};

const navigateTo = (path) => {
    router.push(path);
};

const handleCreate = () => {
    // 从路由获取当前空间 key
    const key = route.params.spaceKey;
    if (key) {
        const currentId = route.params.id;
        const query = currentId ? { parentId: currentId } : {};
        router.push({ path: `/${key}/page/new`, query });
    } else {
        // 如果没有空间 key，跳转到第一个空间
        const spacesList = spaces.value;
        if (spacesList.length > 0) {
            router.push({ path: `/${spacesList[0].key}/page/new` });
        }
    }
};
</script>

<style scoped>
.top-navbar {
    height: 40px;
    background-color: #0049b0;
    color: #ffffff;
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 0 1rem;
    z-index: 10;
    box-shadow: 0 1px 2px rgba(0, 0, 0, 0.1);
}

.nav-left {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.logo {
    width: 24px;
    height: 24px;
    background-color: transparent;
    border-radius: 3px;
    object-fit: contain;
}

.logo:hover {
    opacity: 0.8;
}

.nav-title {
    font-weight: 500;
    font-size: 14px;
}

.nav-title:hover {
    text-decoration: underline;
}

.nav-links {
    display: flex;
    gap: 0.2rem;
    margin-left: 0.5rem;
}

.nav-link {
    color: rgba(255, 255, 255, 0.9);
    font-size: 14px;
    font-weight: 500;
    padding: 0.25rem 0.5rem;
    border-radius: 3px;
    text-decoration: none;
    cursor: pointer;
}

.nav-link.dropdown-link {
    display: inline-flex;
    align-items: center;
}

.nav-link:hover {
    background-color: rgba(0, 0, 0, 0.1);
    color: #ffffff;
}

.nav-right {
    display: flex;
    align-items: center;
    gap: 0.75rem;
}

:deep(.confluence-search .ant-input) {
    border-radius: 3px 0 0 3px !important;
    height: 30px !important;
    font-size: 13px !important;
    border: none !important;
    background-color: rgba(255, 255, 255, 0.2) !important;
    color: #ffffff !important;
}

:deep(.confluence-search .ant-input::placeholder) {
    color: rgba(255, 255, 255, 0.7) !important;
}

:deep(.confluence-search .ant-btn) {
    border-radius: 0 3px 3px 0 !important;
    height: 30px !important;
    border: none !important;
    background-color: rgba(255, 255, 255, 0.2) !important;
    color: #ffffff !important;
}

.create-btn {
    background-color: #0052cc;
    color: #ffffff;
    border: none;
    border-radius: 3px;
    font-weight: 500;
    font-size: 14px;
    height: 30px;
    padding: 0 12px;
    cursor: pointer;
    display: inline-flex;
    align-items: center;
    transition: all 0.2s;
}

.create-btn:hover {
    background-color: #0065ff;
}

.suggestion-item {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 4px 0;
}

.suggestion-icon {
    font-size: 14px;
}

.suggestion-title {
    flex: 1;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    color: #172b4d;
}

.suggestion-space {
    font-size: 11px;
    color: #6b778c;
    background: #ebecf0;
    padding: 0 4px;
    border-radius: 3px;
}

:deep(.ant-select-selection-search-input) {
    color: white !important;
}

:deep(.ant-select-auto-complete.ant-select-single .ant-select-selector) {
    background-color: rgba(255, 255, 255, 0.2) !important;
    border: none !important;
    color: white !important;
}
</style>
