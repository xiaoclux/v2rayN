<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { useStatusStore } from '../stores/status'
import { useEventsStore } from '../stores/events'
import { formatRate } from '../utils/format'
import { runAction } from '../utils/notify'

const { t } = useI18n()
const statusStore = useStatusStore()
const events = useEventsStore()

const running = computed(() => statusStore.status?.running ?? null)
const currentRoutingId = computed({
  get: () => statusStore.status?.currentRoutingId ?? '',
  set: (id: string) => runAction(() => statusStore.setRouting(id)),
})
const speed = computed(() => statusStore.speed)

function handleReload(): void {
  runAction(statusStore.reload)
}
</script>

<template>
  <div class="status-bar">
    <el-tag v-if="!events.isConnected" type="warning">{{ t('web.events.disconnected') }}</el-tag>
    <span class="status-bar__item">
      <template v-if="running?.reloading">{{ t('web.status.reloading') }}</template>
      <template v-else-if="running?.profileRemarks">
        <el-tag type="success">{{ running.coreType }}</el-tag>
        {{ running.profileRemarks }}
        <template v-if="running.delayMs">· {{ t('web.status.delay') }} {{ running.delayMs }} ms</template>
        <template v-if="running.outboundIp">· {{ t('web.status.ip') }} {{ running.outboundIp }}</template>
      </template>
      <template v-else>{{ t('web.status.noProfile') }}</template>
    </span>
    <span v-if="speed" class="status-bar__item">
      {{ t('web.speed.proxy') }} ↑{{ formatRate(speed.proxyUp) }} ↓{{ formatRate(speed.proxyDown) }}
      · {{ t('web.speed.direct') }} ↑{{ formatRate(speed.directUp) }} ↓{{ formatRate(speed.directDown) }}
    </span>
    <span class="status-bar__spacer" />
    <span v-if="statusStore.status" class="status-bar__item">
      {{ t('web.status.inbound') }} :{{ statusStore.status.inboundPort }}
      <el-tag v-if="statusStore.status.allowLan" size="small">{{ t('web.status.lan') }}</el-tag>
    </span>
    <el-select v-if="statusStore.status" v-model="currentRoutingId" :placeholder="t('menuRouting')" class="status-bar__routing">
      <el-option v-for="r in statusStore.status.routings" :key="r.id" :label="r.remarks" :value="r.id" />
    </el-select>
    <el-button :loading="running?.reloading" @click="handleReload">{{ t('menuReload') }}</el-button>
  </div>
</template>

<style scoped>
.status-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 8px 12px;
  padding: 8px 16px;
  border-top: 1px solid var(--el-border-color);
  background: var(--el-bg-color);
  font-size: 13px;
}
.status-bar__spacer {
  flex: 1;
}
.status-bar__routing {
  width: 220px;
}
</style>
