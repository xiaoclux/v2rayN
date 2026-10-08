<script setup lang="ts">
import { onMounted } from 'vue'

const DARK_QUERY = '(prefers-color-scheme: dark)'
const DARK_CLASS = 'dark'

// Element Plus dark mode is class-based; follow the OS preference.
onMounted(() => {
  const media = window.matchMedia(DARK_QUERY)
  const apply = () => document.documentElement.classList.toggle(DARK_CLASS, media.matches)
  apply()
  media.addEventListener('change', apply)
})
</script>

<template>
  <RouterView />
</template>
