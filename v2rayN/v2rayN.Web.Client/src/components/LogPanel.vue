<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useLogsStore } from '../stores/logs'

const { t } = useI18n()
const logs = useLogsStore()
const filter = ref('')
const container = ref<HTMLElement | null>(null)

// Plain substring filter; avoids compiling user-supplied regular expressions (ReDoS).
const visibleLines = computed(() => {
  const needle = filter.value.trim().toLowerCase()
  return needle ? logs.lines.filter((line) => line.toLowerCase().includes(needle)) : logs.lines
})

watch(
  () => logs.lines.length,
  async () => {
    await nextTick()
    const el = container.value
    if (el) {
      el.scrollTop = el.scrollHeight
    }
  },
)
</script>

<template>
  <div class="log-panel">
    <div class="log-panel__toolbar">
      <el-input v-model="filter" :placeholder="t('web.logs.filter')" clearable size="small" class="log-panel__filter" />
      <el-button size="small" @click="logs.isPaused = !logs.isPaused">
        {{ logs.isPaused ? t('web.logs.resume') : t('web.logs.pause') }}
      </el-button>
      <el-button size="small" @click="logs.clear">{{ t('menuMsgViewClear') }}</el-button>
    </div>
    <pre ref="container" class="log-panel__body">{{ visibleLines.join('\n') }}</pre>
  </div>
</template>

<style scoped>
.log-panel {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}
.log-panel__toolbar {
  display: flex;
  gap: 8px;
  padding: 4px 0;
}
.log-panel__filter {
  max-width: 260px;
}
.log-panel__body {
  flex: 1;
  margin: 0;
  overflow: auto;
  padding: 8px;
  font-size: 12px;
  line-height: 1.5;
  background: var(--el-fill-color-lighter);
  border-radius: 4px;
  white-space: pre-wrap;
  word-break: break-all;
}
</style>
