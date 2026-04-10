<template>
  <div class="workspace-home">
    <div class="welcome-header">
      <a-typography-title :level="2">Welcome to Engineering Space</a-typography-title>
      <a-typography-paragraph class="placeholder-text">
        This is your workspace home page. Get started by viewing recent updates or creating a new page.
      </a-typography-paragraph>
    </div>

    <a-row :gutter="24" style="margin-top: 2rem;">
      <a-col :span="16">
        <a-card title="Recent Activity" :bordered="false" class="shadow-sm">
          <a-list item-layout="horizontal" :data-source="recentActivity">
            <template #renderItem="{ item }">
              <a-list-item>
                <a-list-item-meta :description="item.time">
                  <template #title>
                    <a href="#">{{ item.title }}</a>
                  </template>
                  <template #avatar>
                    <a-avatar style="background-color: #3b82f6; color: white;">{{ item.user.charAt(0) }}</a-avatar>
                  </template>
                </a-list-item-meta>
              </a-list-item>
            </template>
          </a-list>
        </a-card>
      </a-col>
      <a-col :span="8">
        <a-card title="Quick Links" :bordered="false" class="shadow-sm">
          <ul class="quick-links">
            <li><a href="#">Space Settings</a></li>
            <li><a href="#">Members</a></li>
            <li><a href="#">Templates</a></li>
          </ul>
        </a-card>
      </a-col>
    </a-row>
  </div>
</template>

<script setup>
import { ref } from 'vue'

const recentActivity = ref([
  { title: 'Updated Architecture Diagram', user: 'Admin', time: '2 hours ago' },
  { title: 'Created Frontend Guidelines', user: 'User 1', time: '5 hours ago' },
  { title: 'Commented on Release Notes', user: 'Admin', time: '1 day ago' },
])
</script>

<style scoped>
.workspace-home {
  max-width: 1200px;
  padding: 20px 2rem 0;
}
.welcome-header {
  margin-bottom: 2rem;
}
.placeholder-text {
  font-size: 1.1rem;
  color: var(--color-text-secondary);
}
.shadow-sm {
  box-shadow: 0 1px 2px 0 rgba(0,0,0,0.05);
  border-radius: var(--radius-md);
}
.quick-links {
  list-style: none;
  padding: 0;
  margin: 0;
}
.quick-links li {
  margin-bottom: 0.75rem;
}
.quick-links a {
  color: var(--color-primary-accent);
  text-decoration: none;
  font-weight: 500;
}
.quick-links a:hover {
  text-decoration: underline;
}
</style>
