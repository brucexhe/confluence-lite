<template>
    <div class="tasks-container">
        <div class="header-actions">
            <div>
                <h2>{{ $t('tasks.title') }}</h2>
                <div class="sub-title" v-if="spaceName">{{ spaceName }}</div>
            </div>
        </div>

        <a-spin :spinning="loading">
            <div v-if="groups.length === 0 && !loading" class="empty-state">
                <ClipboardList :size="40" />
                <p>{{ $t('tasks.empty') }}</p>
            </div>

            <div v-for="group in groups" :key="group.pageId" class="task-group">
                <div class="group-header">
                    <FileText :size="16" class="page-icon" />
                    <a class="page-title" @click="goPage(group.pageId)">{{ group.pageTitle }}</a>
                    <span class="progress">{{ progressText(group) }}</span>
                </div>
                <div class="group-body">
                    <div
                        v-for="task in group.tasks"
                        :key="task.taskUid"
                        class="task-row"
                        :class="{ 'task-done': task.isCompleted }"
                    >
                        <input
                            type="checkbox"
                            class="task-checkbox"
                            :checked="task.isCompleted"
                            @change="toggleTask(task, $event)"
                        />
                        <span class="task-content">{{ task.content }}</span>
                    </div>
                </div>
            </div>
        </a-spin>
    </div>
</template>

<script setup>
import { ref, computed, onMounted } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { message } from "ant-design-vue";
import { workspaceApi, pageApi } from "../../api";
import { FileText, ClipboardList } from "lucide-vue-next";

const route = useRoute();
const router = useRouter();
const { t } = useI18n();

const loading = ref(false);
const groups = ref([]);

const spaceName = computed(() => {
    const spaces = JSON.parse(localStorage.getItem("auth_spaces") || "[]");
    const key = route.params.spaceKey;
    const space = spaces.find((s) => s.key === key || s.key?.toUpperCase() === key?.toUpperCase());
    return space?.name || key || "";
});

const loadTasks = async () => {
    loading.value = true;
    try {
        const data = await workspaceApi.getTasks(route.params.spaceKey);
        groups.value = data || [];
    } catch (error) {
        groups.value = [];
    } finally {
        loading.value = false;
    }
};

const progressText = (group) => {
    const done = group.tasks.filter((task) => task.isCompleted).length;
    return t("tasks.progress", { done, total: group.tasks.length });
};

const goPage = (pageId) => {
    router.push(`/${route.params.spaceKey}/page/${pageId}`);
};

// 勾选即保存（与查看页一致，不产生版本快照）；无编辑权限时接口报错并回滚
const toggleTask = async (task, e) => {
    const checked = e.target.checked;
    try {
        await pageApi.updateTask(task.pageId, task.taskUid, checked);
        task.isCompleted = checked;
    } catch (error) {
        e.target.checked = !checked;
        message.error(t("tasks.toggleFailed"));
    }
};

onMounted(loadTasks);
</script>

<style scoped>
.tasks-container {
    max-width: 1100px;
    margin: 0 auto;
    padding: 20px 2rem 0;
    animation: fadeIn 0.3s ease-in-out;
}

.header-actions {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    margin-bottom: 20px;
}

.header-actions h2 {
    margin: 0;
    color: var(--color-text-primary, #172b4d);
}

.sub-title {
    color: var(--color-text-secondary, #6b778c);
    font-size: 14px;
    margin-top: 4px;
}

.empty-state {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 12px;
    padding: 64px 0;
    color: var(--color-text-secondary, #6b778c);
}

.task-group {
    background: #fff;
    border: 1px solid var(--color-border, #ebecf0);
    border-radius: 6px;
    margin-bottom: 16px;
    overflow: hidden;
}

.group-header {
    display: flex;
    align-items: center;
    gap: 8px;
    padding: 12px 16px;
    border-bottom: 1px solid var(--color-border, #ebecf0);
    background: var(--color-bg-tertiary, #fafbfc);
}

.page-icon {
    color: var(--color-text-secondary, #6b778c);
    flex-shrink: 0;
}

.page-title {
    font-weight: 600;
    font-size: 15px;
    color: var(--color-text-primary, #172b4d);
    cursor: pointer;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.page-title:hover {
    color: #0052cc;
    text-decoration: underline;
}

.progress {
    margin-left: auto;
    flex-shrink: 0;
    font-size: 12px;
    color: var(--color-text-secondary, #6b778c);
    background: var(--color-bg-secondary, #ebecf0);
    padding: 2px 8px;
    border-radius: 3px;
}

.group-body {
    padding: 8px 16px;
}

.task-row {
    display: flex;
    align-items: flex-start;
    gap: 10px;
    padding: 6px 0;
}

.task-checkbox {
    width: 16px;
    height: 16px;
    margin-top: 3px;
    accent-color: #0052cc;
    cursor: pointer;
    flex-shrink: 0;
}

.task-content {
    font-size: 14px;
    line-height: 22px;
    color: var(--color-text-primary, #172b4d);
    word-break: break-word;
}

.task-row.task-done .task-content {
    color: var(--color-text-secondary, #6b778c);
    text-decoration: line-through;
}

@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(8px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

@media (max-width: 768px) {
    .tasks-container {
        padding: 12px 1rem 0;
    }

    .header-actions h2 {
        font-size: 20px;
    }
}
</style>
