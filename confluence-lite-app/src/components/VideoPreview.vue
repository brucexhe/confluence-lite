<template>
    <div v-if="visible" class="video-preview-overlay" @click.self="close" @keydown.esc="close">
        <!-- 顶部工具栏 -->
        <div class="top-toolbar-bar">
            <div class="toolbar-left">
                <span class="file-name">{{ fileName }}</span>
            </div>
            <div class="toolbar-right">
                <a-space :size="4">
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="handleDownload"
                        :title="$t('videoPreview.download')"
                    >
                        <template #icon><DownloadIcon :size="16" /></template>
                    </a-button>
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="close"
                        :title="$t('videoPreview.close')"
                    >
                        <template #icon><XIcon :size="16" /></template>
                    </a-button>
                </a-space>
            </div>
        </div>

        <!-- 内容区域 -->
        <div class="preview-content">
            <div class="video-container" @click.stop>
                <video
                    ref="videoRef"
                    :src="src"
                    controls
                    autoplay
                    class="preview-video"
                >
                    {{ $t('videoPreview.browserNotSupported') }}
                </video>
            </div>
        </div>
    </div>
</template>

<script setup>
import { ref, watch, onMounted, onUnmounted } from 'vue';
import { XIcon, DownloadIcon } from 'lucide-vue-next';

const props = defineProps({
    open: Boolean,
    src: String,
    fileName: String
});

const emit = defineEmits(['update:open']);

const visible = ref(false);
const videoRef = ref(null);

const close = () => {
    if (videoRef.value) {
        videoRef.value.pause();
    }
    visible.value = false;
};

const handleDownload = () => {
    if (props.src) {
        window.open(props.src, '_blank');
    }
};

watch(() => props.open, (val) => {
    visible.value = val;
});

watch(visible, (val) => {
    emit('update:open', val);
});

const handleKeydown = (e) => {
    if (!visible.value) return;
    if (e.key === 'Escape') {
        close();
    }
};

onMounted(() => {
    window.addEventListener('keydown', handleKeydown);
});

onUnmounted(() => {
    window.removeEventListener('keydown', handleKeydown);
});
</script>

<style scoped>
.video-preview-overlay {
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background: rgba(0, 0, 0, 0.85);
    z-index: 1000;
    display: flex;
    flex-direction: column;
}

.top-toolbar-bar {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 8px 16px;
    background: rgba(0, 0, 0, 0.6);
    border-bottom: 1px solid rgba(255, 255, 255, 0.1);
    flex-shrink: 0;
}

.toolbar-left {
    flex: 1;
    overflow: hidden;
}

.file-name {
    color: rgba(255, 255, 255, 0.9);
    font-size: 13px;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.toolbar-right {
    flex-shrink: 0;
}

.toolbar-btn {
    color: rgba(255, 255, 255, 0.8);
    background: transparent;
    border: none;
    width: 32px;
    height: 32px;
    padding: 0;
    display: flex;
    align-items: center;
    justify-content: center;
}

.toolbar-btn:hover {
    color: #fff;
    background: rgba(255, 255, 255, 0.1);
}

.preview-content {
    flex: 1;
    overflow: hidden;
    display: flex;
    align-items: center;
    justify-content: center;
    padding: 40px;
}

.video-container {
    max-width: 100%;
    max-height: 100%;
    display: flex;
    align-items: center;
    justify-content: center;
}

.preview-video {
    max-width: 100%;
    max-height: 100%;
    box-shadow: 0 4px 12px rgba(0, 0, 0, 0.5);
}
</style>
