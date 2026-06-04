<template>
    <div v-if="visible" class="image-preview-overlay" @click.self="close" @wheel.prevent="handleWheel">
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
                        @click.stop="zoomOut"
                        :title="$t('imagePreview.zoomOut')"
                        :disabled="scale <= 0.1"
                    >
                        <template #icon><ZoomOutIcon :size="16" /></template>
                    </a-button>
                    <span class="scale-label-small">{{ Math.round(scale * 100) }}%</span>
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="zoomIn"
                        :title="$t('imagePreview.zoomIn')"
                        :disabled="scale >= 3"
                    >
                        <template #icon><ZoomInIcon :size="16" /></template>
                    </a-button>
                    <a-divider type="vertical" style="background: rgba(255,255,255,0.2); margin: 0 4px;" />
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="rotateLeft"
                        :title="$t('imagePreview.rotateLeft')"
                    >
                        <template #icon><RotateCcwIcon :size="16" /></template>
                    </a-button>
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="rotateRight"
                        :title="$t('imagePreview.rotateRight')"
                    >
                        <template #icon><RotateCwIcon :size="16" /></template>
                    </a-button>
                    <a-button
                        type="text"
                        size="small"
                        class="toolbar-btn"
                        @click.stop="close"
                        :title="$t('imagePreview.close')"
                    >
                        <template #icon><XIcon :size="16" /></template>
                    </a-button>
                </a-space>
            </div>
        </div>

        <!-- 左箭头 -->
        <div
            v-if="images.length > 1 && currentIndex > 0"
            class="nav-arrow nav-arrow-left"
            @click.stop="prevImage"
        >
            <ChevronLeftIcon :size="40" />
        </div>

        <!-- 图片容器 -->
        <div class="preview-image-container" @click="close">
            <img
                :src="currentSrc"
                :style="imageStyle"
                class="preview-image"
                draggable="false"
                @click.stop
            />
        </div>

        <!-- 右箭头 -->
        <div
            v-if="images.length > 1 && currentIndex < images.length - 1"
            class="nav-arrow nav-arrow-right"
            @click.stop="nextImage"
        >
            <ChevronRightIcon :size="40" />
        </div>

        <!-- 底部信息 -->
        <div v-if="images.length > 1" class="bottom-info">
            {{ currentIndex + 1 }} / {{ images.length }}
        </div>
    </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import { ChevronLeftIcon, ChevronRightIcon, XIcon, RotateCwIcon, RotateCcwIcon, ZoomInIcon, ZoomOutIcon } from 'lucide-vue-next';

const props = defineProps({
    open: Boolean,
    src: String,
    images: {
        type: Array,
        default: () => []
    },
    currentIndex: {
        type: Number,
        default: 0
    }
});

const emit = defineEmits(['update:open', 'update:currentIndex']);

const visible = ref(false);
const scale = ref(1);
const rotation = ref(0);
const currentIndex = ref(0);

const currentSrc = computed(() => {
    if (props.images.length > 0 && props.currentIndex < props.images.length) {
        return props.images[props.currentIndex];
    }
    return props.src || '';
});

const fileName = computed(() => {
    if (!currentSrc.value) return '';
    try {
        const url = new URL(currentSrc.value);
        const pathname = url.pathname;
        const parts = pathname.split('/');
        const name = parts[parts.length - 1];
        return name || 'Image';
    } catch {
        const parts = currentSrc.value.split('/');
        return parts[parts.length - 1] || 'Image';
    }
});

const imageStyle = computed(() => ({
    transform: `scale(${scale.value}) rotate(${rotation.value}deg)`,
    transition: 'transform 0.3s ease',
    maxHeight: 'calc(100vh - 100px)',
    maxWidth: 'calc(100vw - 100px)'
}));

watch(() => props.open, (val) => {
    visible.value = val;
    if (val) {
        resetTransform();
    }
});

watch(visible, (val) => {
    emit('update:open', val);
});

watch(() => props.currentIndex, (val) => {
    currentIndex.value = val;
});

watch(currentIndex, (val) => {
    emit('update:currentIndex', val);
});

const close = () => {
    visible.value = false;
};

const zoomIn = () => {
    scale.value = Math.min(scale.value + 0.1, 3);
};

const zoomOut = () => {
    scale.value = Math.max(scale.value - 0.1, 0.1);
};

const rotateLeft = () => {
    rotation.value -= 90;
};

const rotateRight = () => {
    rotation.value += 90;
};

const resetTransform = () => {
    scale.value = 1;
    rotation.value = 0;
};

const prevImage = () => {
    if (currentIndex.value > 0) {
        currentIndex.value--;
        resetTransform();
    }
};

const nextImage = () => {
    if (currentIndex.value < props.images.length - 1) {
        currentIndex.value++;
        resetTransform();
    }
};

const handleWheel = (e) => {
    if (e.deltaY < 0) {
        zoomIn();
    } else {
        zoomOut();
    }
};

const handleKeydown = (e) => {
    if (!visible.value) return;

    switch (e.key) {
        case 'Escape':
            close();
            break;
        case 'ArrowLeft':
            prevImage();
            break;
        case 'ArrowRight':
            nextImage();
            break;
        case '+':
        case '=':
            zoomIn();
            break;
        case '-':
        case '_':
            zoomOut();
            break;
        case '0':
            resetTransform();
            break;
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
.image-preview-overlay {
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

.toolbar-btn:hover:not(:disabled) {
    color: #fff;
    background: rgba(255, 255, 255, 0.1);
}

.toolbar-btn:disabled {
    opacity: 0.3;
    cursor: not-allowed;
}

.scale-label-small {
    color: rgba(255, 255, 255, 0.9);
    font-size: 12px;
    min-width: 40px;
    text-align: center;
    display: inline-block;
}

.preview-image-container {
    display: flex;
    align-items: center;
    justify-content: center;
    flex: 1;
    overflow: hidden;
    position: relative;
}

.preview-image {
    max-width: 100%;
    max-height: 100%;
    object-fit: contain;
    user-select: none;
    cursor: pointer;
}

.nav-arrow {
    position: absolute;
    top: 50%;
    transform: translateY(-50%);
    width: 48px;
    height: 48px;
    display: flex;
    align-items: center;
    justify-content: center;
    background: rgba(255, 255, 255, 0.1);
    border-radius: 50%;
    cursor: pointer;
    color: rgba(255, 255, 255, 0.8);
    transition: all 0.2s ease;
    z-index: 10;
}

.nav-arrow:hover {
    background: rgba(255, 255, 255, 0.2);
    color: #fff;
}

.nav-arrow-left {
    left: 20px;
}

.nav-arrow-right {
    right: 20px;
}

.bottom-info {
    position: absolute;
    bottom: 16px;
    left: 50%;
    transform: translateX(-50%);
    color: rgba(255, 255, 255, 0.8);
    font-size: 13px;
    background: rgba(0, 0, 0, 0.5);
    padding: 6px 12px;
    border-radius: 16px;
    z-index: 10;
}
</style>
