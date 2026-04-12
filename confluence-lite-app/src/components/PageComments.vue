<template>
    <div class="comments-section">
        <div class="comments-header">
            <h3>
                <CommentIcon :size="16" />
                Comments ({{ comments.length }})
            </h3>
            <a-dropdown v-if="comments.length > 0">
                <a class="sort-link">
                    {{ sortText }}
                    <DownIcon :size="12" />
                </a>
                <template #overlay>
                    <a-menu @click="handleSortChange">
                        <a-menu-item key="newest">Newest first</a-menu-item>
                        <a-menu-item key="oldest">Oldest first</a-menu-item>
                    </a-menu>
                </template>
            </a-dropdown>
        </div>

        <!-- Add Comment Form -->
        <div class="add-comment">
            <a-avatar class="user-avatar">{{ userInitial }}</a-avatar>
            <div class="comment-input-wrapper">
                <a-textarea
                    v-model:value="newComment"
                    placeholder="Write a comment..."
                    :rows="2"
                    class="comment-input"
                    @keydown="handleKeyDown"
                />
                <div class="comment-actions">
                    <div class="spacer"></div>
                    <span class="hint">Press <kbd>Enter</kbd> to submit</span>
                    <a-button type="primary" size="small" @click="addComment" :disabled="!newComment.trim()">
                        Save
                    </a-button>
                </div>
            </div>
        </div>

        <!-- Comments List -->
        <a-spin v-if="loading" style="display:block;padding:2rem 0;text-align:center;" />
        <div v-else class="comments-list">
            <div v-for="comment in sortedComments" :key="comment.id" class="comment-item">
                <a-avatar class="comment-avatar" :style="{ backgroundColor: avatarColor(comment.user?.name || '') }">
                    {{ (comment.user?.name || 'U').charAt(0).toUpperCase() }}
                </a-avatar>
                <div class="comment-content-wrapper">
                    <div class="comment-meta">
                        <span class="comment-author">{{ comment.user?.name || 'Unknown' }}</span>
                        <a-dropdown>
                            <span class="comment-time">{{ formatTime(comment.createdAt) }}</span>
                            <template #overlay>
                                <a-menu>
                                    <a-menu-item @click="deleteComment(comment.id)" danger>Delete</a-menu-item>
                                </a-menu>
                            </template>
                        </a-dropdown>
                    </div>
                    <div class="comment-body" v-html="comment.content"></div>

                    <!-- Replies -->
                    <div class="comment-replies" v-if="comment.replies && comment.replies.length > 0">
                        <div v-for="reply in comment.replies" :key="reply.id" class="reply-item">
                            <a-avatar class="reply-avatar" :style="{ backgroundColor: avatarColor(reply.user?.name || ''), fontSize: '12px' }">
                                {{ (reply.user?.name || 'U').charAt(0).toUpperCase() }}
                            </a-avatar>
                            <div class="reply-content-wrapper">
                                <div class="reply-meta">
                                    <span class="reply-author">{{ reply.user?.name || 'Unknown' }}</span>
                                    <span class="reply-time">{{ formatTime(reply.createdAt) }}</span>
                                </div>
                                <div class="reply-body">{{ reply.content }}</div>
                            </div>
                        </div>
                    </div>

                    <!-- Reply Button -->
                    <a-button
                        v-if="!comment.showReply"
                        type="link"
                        size="small"
                        @click="toggleReply(comment.id)"
                        class="reply-btn"
                    >
                        Reply
                    </a-button>

                    <!-- Reply Input -->
                    <div v-if="comment.showReply" class="reply-input-wrapper">
                        <a-input
                            v-model:value="comment.replyText"
                            placeholder="Write a reply..."
                            size="small"
                            @keydown.enter="addReply(comment.id, $event)"
                        />
                        <div class="reply-actions">
                            <a-button size="small" @click="cancelReply(comment.id)">Cancel</a-button>
                            <a-button type="primary" size="small" @click="addReply(comment.id)" :disabled="!comment.replyText?.trim()">
                                Reply
                            </a-button>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Empty State -->
            <div v-if="comments.length === 0" class="empty-comments">
                <CommentIcon :size="32" class="empty-icon" />
                <p>No comments yet. Be the first to comment!</p>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue';
import { MessageSquare as CommentIcon, ChevronDown as DownIcon } from 'lucide-vue-next';
import { commentApi } from '../api';

const props = defineProps({
    userInitial: {
        type: String,
        default: 'U'
    },
    pageId: {
        type: [String, Number],
        required: true
    }
});

const loading = ref(false);
const newComment = ref('');
const sortOrder = ref('newest');
const comments = ref([]);

// Avatar colors
const avatarColors = ['#3b82f6', '#10b981', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4'];
function avatarColor(name) {
    let hash = 0;
    for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
    return avatarColors[Math.abs(hash) % avatarColors.length];
}

function formatTime(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const now = new Date();
    const diff = now - date;
    const minutes = Math.floor(diff / 60000);
    if (minutes < 1) return '刚刚';
    if (minutes < 60) return `${minutes} 分钟前`;
    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `${hours} 小时前`;
    const days = Math.floor(hours / 24);
    if (days < 30) return `${days} 天前`;
    return date.toLocaleDateString('zh-CN');
}

// Load comments from API
async function loadComments() {
    if (!props.pageId) return;
    loading.value = true;
    try {
        const data = await commentApi.getList(props.pageId);
        comments.value = (data || []).map(c => ({
            ...c,
            showReply: false,
            replyText: '',
        }));
    } catch (e) {
        console.error('加载评论失败:', e);
    } finally {
        loading.value = false;
    }
}

onMounted(loadComments);
watch(() => props.pageId, loadComments);

const sortText = computed(() => {
    return sortOrder.value === 'newest' ? 'Newest first' : 'Oldest first';
});

const sortedComments = computed(() => {
    return [...comments.value].sort((a, b) => {
        return sortOrder.value === 'newest'
            ? new Date(b.createdAt) - new Date(a.createdAt)
            : new Date(a.createdAt) - new Date(b.createdAt);
    });
});

const handleSortChange = ({ key }) => {
    sortOrder.value = key;
};

const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        addComment();
    }
};

const addComment = async () => {
    if (!newComment.value.trim()) return;
    try {
        await commentApi.create(props.pageId, {
            pageId: Number(props.pageId),
            content: newComment.value,
        });
        newComment.value = '';
        await loadComments();
    } catch (e) {
        console.error('发表评论失败:', e);
    }
};

const deleteComment = async (id) => {
    try {
        await commentApi.remove(id);
        await loadComments();
    } catch (e) {
        console.error('删除评论失败:', e);
    }
};

const toggleReply = (id) => {
    const comment = comments.value.find(c => c.id === id);
    if (comment) {
        comment.showReply = !comment.showReply;
    }
};

const cancelReply = (id) => {
    const comment = comments.value.find(c => c.id === id);
    if (comment) {
        comment.showReply = false;
        comment.replyText = '';
    }
};

const addReply = async (commentId, event) => {
    if (event && event.key === 'Enter' && event.shiftKey) return;
    const comment = comments.value.find(c => c.id === commentId);
    if (!comment || !comment.replyText?.trim()) return;

    try {
        await commentApi.create(props.pageId, {
            pageId: Number(props.pageId),
            content: comment.replyText,
            parentId: commentId,
        });
        comment.showReply = false;
        comment.replyText = '';
        await loadComments();
    } catch (e) {
        console.error('回复评论失败:', e);
    }
};
</script>

<style scoped>
.comments-section {
    max-width: 900px;
    margin:0;
    padding-top: 2rem;
    border-top: 1px solid #dfe1e6;
}

.comments-header {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-bottom: 1.5rem;
}

.comments-header h3 {
    font-size: 18px;
    font-weight: 500;
    color: #172b4d;
    margin: 0;
    display: flex;
    align-items: center;
    gap: 8px;
}

.sort-link {
    font-size: 13px;
    color: #42526e;
    display: flex;
    align-items: center;
    gap: 4px;
    cursor: pointer;
}

.sort-link:hover {
    color: #0052cc;
}

/* Add Comment Form */
.add-comment {
    display: flex;
    gap: 12px;
    margin-bottom: 2rem;
}

.user-avatar {
    width: 32px;
    height: 32px;
    background-color: #0052cc;
    color: white;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 14px;
}

.comment-input-wrapper {
    flex: 1;
}

.comment-input {
    border-radius: 3px !important;
    border: 1px solid #dfe1e6 !important;
    font-size: 14px !important;
}

.comment-input:focus {
    box-shadow: 0 0 0 2px rgba(0, 82, 204, 0.2) !important;
}

.comment-actions {
    display: flex;
    align-items: center;
    margin-top: 8px;
    gap: 4px;
}

.spacer {
    flex: 1;
}

.hint {
    font-size: 12px;
    color: #6b778c;
    margin-right: 12px;
}

.hint kbd {
    background-color: #f4f5f7;
    border: 1px solid #dfe1e6;
    border-radius: 3px;
    padding: 2px 6px;
    font-family: monospace;
    font-size: 11px;
}

/* Comments List */
.comments-list {
    display: flex;
    flex-direction: column;
    gap: 1.5rem;
}

.comment-item {
    display: flex;
    gap: 12px;
}

.comment-avatar {
    width: 32px;
    height: 32px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
    font-size: 14px;
}

.comment-content-wrapper {
    flex: 1;
}

.comment-meta {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 4px;
}

.comment-author {
    font-weight: 500;
    font-size: 14px;
    color: #172b4d;
}

.comment-time {
    font-size: 13px;
    color: #6b778c;
    cursor: pointer;
}

.comment-time:hover {
    color: #42526e;
}

.comment-body {
    font-size: 14px;
    line-height: 1.6;
    color: #172b4d;
}

/* Replies */
.comment-replies {
    margin-top: 1rem;
    display: flex;
    flex-direction: column;
    gap: 1rem;
    padding-left: 1rem;
    border-left: 2px solid #f4f5f7;
}

.reply-item {
    display: flex;
    gap: 8px;
}

.reply-avatar {
    width: 24px;
    height: 24px;
    flex-shrink: 0;
    display: flex;
    align-items: center;
    justify-content: center;
    color: white;
}

.reply-content-wrapper {
    flex: 1;
}

.reply-meta {
    display: flex;
    align-items: center;
    gap: 8px;
    margin-bottom: 2px;
}

.reply-author {
    font-weight: 500;
    font-size: 13px;
    color: #172b4d;
}

.reply-time {
    font-size: 12px;
    color: #6b778c;
}

.reply-body {
    font-size: 13px;
    line-height: 1.5;
    color: #172b4d;
}

.reply-btn {
    margin-top: 8px;
    padding: 0;
    font-size: 13px;
    color: #0052cc;
}

.reply-input-wrapper {
    margin-top: 12px;
}

.reply-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 8px;
}

/* Empty State */
.empty-comments {
    text-align: center;
    padding: 3rem 2rem;
    color: #6b778c;
}

.empty-icon {
    margin-bottom: 1rem;
    opacity: 0.5;
}

.empty-comments p {
    font-size: 14px;
    margin: 0;
}

/* Dropdown Menu Styles */
:deep(.ant-dropdown-menu) {
    border-radius: 3px;
    box-shadow: 0 4px 8px -2px rgba(9, 30, 66, 0.25);
}
</style>
