<script setup lang="ts">
import NotFound from "../components/NotFound.vue";
</script>

<template>
  <div class="layout-wrapper" :class="{ 'is-resizing': isResizing }">
    <!-- Top Navigation -->
    <TopNavbar />

    <NotFound />
  </div>
</template>
<script setup>
import { ref } from "vue";
import viteLogo from "../assets/vite.svg";
import heroImg from "../assets/hero.png";
import vueLogo from "../assets/vue.svg";

const count = ref(0);
</script>