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
                    <a-button size="small" class="toolbar-btn">
                        <AtIcon :size="14" />
                    </a-button>
                    <a-button size="small" class="toolbar-btn">
                        <EmojiIcon :size="14" />
                    </a-button>
                    <a-button size="small" class="toolbar-btn">
                        <LinkIcon :size="14" />
                    </a-button>
                    <div class="spacer"></div>
                    <span class="hint">Press <kbd>Enter</kbd> to submit</span>
                    <a-button type="primary" size="small" @click="addComment" :disabled="!newComment.trim()">
                        Save
                    </a-button>
                </div>
            </div>
        </div>

        <!-- Comments List -->
        <div class="comments-list">
            <div v-for="comment in sortedComments" :key="comment.id" class="comment-item">
                <a-avatar class="comment-avatar" :style="{ backgroundColor: comment.color }">
                    {{ comment.author.charAt(0) }}
                </a-avatar>
                <div class="comment-content-wrapper">
                    <div class="comment-meta">
                        <span class="comment-author">{{ comment.author }}</span>
                        <a-dropdown>
                            <span class="comment-time">{{ comment.time }}</span>
                            <template #overlay>
                                <a-menu>
                                    <a-menu-item @click="editComment(comment.id)">Edit</a-menu-item>
                                    <a-menu-item @click="deleteComment(comment.id)" danger>Delete</a-menu-item>
                                </a-menu>
                            </template>
                        </a-dropdown>
                    </div>
                    <div class="comment-body" v-html="comment.content"></div>

                    <!-- Reply Section -->
                    <div class="comment-replies" v-if="comment.replies && comment.replies.length > 0">
                        <div v-for="reply in comment.replies" :key="reply.id" class="reply-item">
                            <a-avatar class="reply-avatar" :style="{ backgroundColor: reply.color, fontSize: '12px' }">
                                {{ reply.author.charAt(0) }}
                            </a-avatar>
                            <div class="reply-content-wrapper">
                                <div class="reply-meta">
                                    <span class="reply-author">{{ reply.author }}</span>
                                    <span class="reply-time">{{ reply.time }}</span>
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
import { ref, computed } from 'vue';
import { MessageSquare as CommentIcon, ChevronDown as DownIcon, AtSign as AtIcon, Smile as EmojiIcon, Link as LinkIcon } from 'lucide-vue-next';

// Props
const props = defineProps({
    userInitial: {
        type: String,
        default: 'U'
    }
});

// State
const newComment = ref('');
const sortOrder = ref('newest');

const comments = ref([
    {
        id: 1,
        author: 'Alice Chen',
        time: '2 hours ago',
        content: 'This page looks great! I especially like the <strong>architecture overview</strong> section. Maybe we could add more details about the API endpoints?',
        color: '#3b82f6',
        showReply: false,
        replyText: '',
        replies: [
            {
                id: 101,
                author: 'Admin',
                time: '1 hour ago',
                content: 'Good point! I\'ll add the API documentation section this week.',
                color: '#10b981'
            }
        ]
    },
    {
        id: 2,
        author: 'Bob Smith',
        time: '5 hours ago',
        content: 'Could you update the diagram? The current version is missing the new microservice component.',
        color: '#8b5cf6',
        showReply: false,
        replyText: '',
        replies: []
    },
    {
        id: 3,
        author: 'Carol Wang',
        time: '1 day ago',
        content: 'Thanks for documenting this! It really helps the new team members get on board quickly.',
        color: '#f59e0b',
        showReply: false,
        replyText: '',
        replies: []
    }
]);

// Computed
const sortText = computed(() => {
    return sortOrder.value === 'newest' ? 'Newest first' : 'Oldest first';
});

const sortedComments = computed(() => {
    return [...comments.value].sort((a, b) => {
        return sortOrder.value === 'newest' ? b.id - a.id : a.id - b.id;
    });
});

// Methods
const handleSortChange = ({ key }) => {
    sortOrder.value = key;
};

const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault();
        addComment();
    }
};

const addComment = () => {
    if (!newComment.value.trim()) return;

    const comment = {
        id: Date.now(),
        author: 'Current User',
        time: 'Just now',
        content: formatComment(newComment.value),
        color: '#0052cc',
        showReply: false,
        replyText: '',
        replies: []
    };

    comments.value.unshift(comment);
    newComment.value = '';
};

const formatComment = (text) => {
    // Simple @mention highlighting
    return text.replace(/@(\w+)/g, '<span class="mention">@$1</span>');
};

const editComment = (id) => {
    console.log('Edit comment:', id);
    // TODO: Implement edit functionality
};

const deleteComment = (id) => {
    const index = comments.value.findIndex(c => c.id === id);
    if (index !== -1) {
        comments.value.splice(index, 1);
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

const addReply = (commentId, event) => {
    if (event && event.key === 'Enter' && event.shiftKey) return;

    const comment = comments.value.find(c => c.id === commentId);
    if (!comment || !comment.replyText?.trim()) return;

    if (!comment.replies) {
        comment.replies = [];
    }

    comment.replies.push({
        id: Date.now(),
        author: 'Current User',
        time: 'Just now',
        content: comment.replyText,
        color: '#0052cc'
    });

    comment.showReply = false;
    comment.replyText = '';
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

.toolbar-btn {
    border: none !important;
    background: transparent !important;
    color: #6b778c !important;
    padding: 4px 8px !important;
}

.toolbar-btn:hover {
    background-color: rgba(9, 30, 66, 0.08) !important;
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

.comment-body :deep(.mention) {
    background-color: #deebff;
    color: #0052cc;
    border-radius: 3px;
    padding: 0 4px;
    font-weight: 500;
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
