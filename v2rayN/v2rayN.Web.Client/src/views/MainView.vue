<script setup lang="ts">
import { onBeforeUnmount, onMounted } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { ROUTES, SERVER_EVENTS } from '../api/endpoints'
import { AVAILABLE_LOCALES, initLocale, setLocale, storeLocale } from '../i18n'
import { useSessionStore } from '../stores/session'
import { useEventsStore } from '../stores/events'
import { useStatusStore } from '../stores/status'
import { useLogsStore } from '../stores/logs'
import { useSubsStore } from '../stores/subs'
import { useProfilesStore } from '../stores/profiles'
import { reportError, runAction } from '../utils/notify'
import StatusBar from '../components/StatusBar.vue'
import LogPanel from '../components/LogPanel.vue'
import SubTabs from '../components/SubTabs.vue'
import ProfileTable from '../components/ProfileTable.vue'

const { t, locale } = useI18n()
const router = useRouter()
const session = useSessionStore()
const events = useEventsStore()
const statusStore = useStatusStore()
const logs = useLogsStore()
const subs = useSubsStore()
const profiles = useProfilesStore()
const cleanups: Array<() => void> = []

onMounted(async () => {
  try {
    const meta = await session.loadMeta()
    await initLocale(meta.language)
    statusStore.bindEvents()
    logs.bindEvents()
    subs.bindEvents()
    profiles.bindEvents()
    cleanups.push(events.on<{ message: string }>(SERVER_EVENTS.toast, (evt) => ElMessage.info({ message: evt.message, grouping: true })))
    cleanups.push(events.on(SERVER_EVENTS.appStopping, () => ElMessage.warning(t('web.app.stopping'))))
    events.connect()
    await Promise.all([statusStore.load(), logs.loadRecent(), subs.load()])
    await profiles.load()
  } catch (err) {
    reportError(err)
  }
})

onBeforeUnmount(() => {
  cleanups.forEach((cleanup) => cleanup())
  events.disconnect()
})

function handleLocaleChange(next: string): void {
  storeLocale(next)
  runAction(() => setLocale(next))
}

function handleLogout(): void {
  runAction(async () => {
    events.disconnect()
    await session.logout()
    await router.replace(ROUTES.login)
  })
}
</script>

<template>
  <div class="main">
    <header class="main__header">
      <strong>v2rayN</strong>
      <span class="main__version">{{ session.meta?.version }} · {{ session.meta?.runtimeIdentifier }}</span>
      <span class="main__spacer" />
      <el-select :model-value="locale" size="small" class="main__locale" :aria-label="t('TbSettingsLanguage')" @update:model-value="handleLocaleChange">
        <el-option v-for="l in AVAILABLE_LOCALES" :key="l" :label="l" :value="l" />
      </el-select>
      <el-button size="small" @click="handleLogout">{{ t('web.logout') }}</el-button>
    </header>
    <main class="main__content">
      <SubTabs />
      <section class="main__profiles">
        <ProfileTable />
      </section>
      <section class="main__logs">
        <LogPanel />
      </section>
    </main>
    <StatusBar />
  </div>
</template>

<style scoped>
.main {
  display: flex;
  flex-direction: column;
  height: 100%;
}
.main__header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 8px 16px;
  border-bottom: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
}
.main__version {
  color: var(--el-text-color-secondary);
  font-size: 12px;
}
.main__spacer {
  flex: 1;
}
.main__locale {
  width: 110px;
}
.main__content {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  padding: 8px 16px;
}
.main__profiles {
  flex: 3;
  min-height: 200px;
}
.main__logs {
  flex: 1;
  min-height: 140px;
}
</style>
