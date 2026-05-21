<template>
  <a-avatar
    v-bind="$attrs"
    :src="user?.avatarUrl"
    :style="avatarStyle"
    class="user-avatar-comp"
  >
    <template v-if="!user?.avatarUrl">
      {{ initial }}
    </template>
  </a-avatar>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  user: {
    type: Object,
    required: false,
    default: () => ({})
  }
});

const initial = computed(() => {
  const name = props.user?.displayName || props.user?.name || props.user?.username || 'U';
  return name.charAt(0).toUpperCase();
});

const avatarStyle = computed(() => {
  if (props.user?.avatarUrl) return { verticalAlign: 'middle' };
  
  const name = props.user?.displayName || props.user?.name || props.user?.username || 'U';
  return {
    backgroundColor: getAvatarColor(name),
    verticalAlign: 'middle'
  };
});

function getAvatarColor(name) {
  const colors = ['#3b82f6', '#10b981', '#8b5cf6', '#f59e0b', '#ef4444', '#06b6d4', '#0052cc', '#00875a', '#253858'];
  let hash = 0;
  for (let i = 0; i < name.length; i++) {
    hash = name.charCodeAt(i) + ((hash << 5) - hash);
  }
  return colors[Math.abs(hash) % colors.length];
}
</script>

<style scoped>
.user-avatar-comp {
  flex-shrink: 0;
  user-select: none;
  display: inline-flex;
  align-items: center;
  justify-content: center;
}
</style>
