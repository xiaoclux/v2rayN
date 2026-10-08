<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import { SPEED_ACTIONS, type SpeedAction } from '../api/endpoints'
import type { ProfileRow } from '../api/types'
import { useProfilesStore } from '../stores/profiles'
import { runAction } from '../utils/notify'
import ImportDialog from './ImportDialog.vue'
import ShareDialog from './ShareDialog.vue'

const FILTER_DEBOUNCE_MS = 300

const { t } = useI18n()
const profiles = useProfilesStore()

const selected = ref<ProfileRow[]>([])
const isImportOpen = ref(false)
const isShareOpen = ref(false)
const share = ref<{ indexId: string | null; uri: string }>({ indexId: null, uri: '' })
const selectedIds = computed(() => selected.value.map((row) => row.indexId))
const hasSelection = computed(() => selected.value.length > 0)
let filterTimer: ReturnType<typeof setTimeout> | undefined

function handleFilterInput(): void {
  clearTimeout(filterTimer)
  filterTimer = setTimeout(() => runAction(profiles.load), FILTER_DEBOUNCE_MS)
}

function handleActivate(row: ProfileRow): void {
  runAction(() => profiles.activate(row.indexId))
}

function handleRemove(): void {
  const ids = selectedIds.value
  runAction(async () => {
    await ElMessageBox.confirm(t('RemoveServer'), { type: 'warning' })
    await profiles.remove(ids)
  })
}

function handleShare(): void {
  const row = selected.value[0]
  if (!row) {
    return
  }
  runAction(async () => {
    share.value = { indexId: row.indexId, uri: await profiles.share(row.indexId) }
    isShareOpen.value = true
  })
}

function handleSpeedtest(action: SpeedAction): void {
  const isWholeList = action === SPEED_ACTIONS.mixedtest || action === SPEED_ACTIONS.fastRealping
  if (!isWholeList && !hasSelection.value) {
    ElMessage.warning(t('PleaseSelectServer'))
    return
  }
  runAction(() => profiles.speedtest(action, selectedIds.value))
}

function rowClassName({ row }: { row: ProfileRow }): string {
  return row.isActive ? 'profile-table__row--active' : ''
}
</script>

<template>
  <div class="profile-table">
    <div class="profile-table__toolbar">
      <el-input v-model="profiles.filter" :placeholder="t('MsgServerTitle')" clearable size="small" class="profile-table__filter" @input="handleFilterInput" @clear="handleFilterInput" />
      <el-button size="small" @click="isImportOpen = true">{{ t('menuAddServerViaClipboard') }}</el-button>
      <el-button size="small" :disabled="selected.length !== 1" @click="selected[0] && handleActivate(selected[0])">{{ t('menuSetDefaultServer') }}</el-button>
      <el-button size="small" :disabled="!hasSelection" @click="handleRemove">{{ t('menuRemoveServer') }}</el-button>
      <el-button size="small" :disabled="selected.length !== 1" @click="handleShare">{{ t('menuShareServer') }}</el-button>
      <el-dropdown trigger="click" size="small">
        <el-button size="small">{{ t('web.speedtest.menu') }}</el-button>
        <template #dropdown>
          <el-dropdown-menu>
            <el-dropdown-item @click="handleSpeedtest(SPEED_ACTIONS.fastRealping)">{{ t('menuFastRealPing') }}</el-dropdown-item>
            <el-dropdown-item @click="handleSpeedtest(SPEED_ACTIONS.mixedtest)">{{ t('menuMixedTestServer') }}</el-dropdown-item>
            <el-dropdown-item divided @click="handleSpeedtest(SPEED_ACTIONS.tcping)">{{ t('menuTcpingServer') }}</el-dropdown-item>
            <el-dropdown-item @click="handleSpeedtest(SPEED_ACTIONS.realping)">{{ t('menuRealPingServer') }}</el-dropdown-item>
            <el-dropdown-item @click="handleSpeedtest(SPEED_ACTIONS.udpTest)">{{ t('menuUdpTestServer') }}</el-dropdown-item>
            <el-dropdown-item @click="handleSpeedtest(SPEED_ACTIONS.speedtest)">{{ t('menuSpeedServer') }}</el-dropdown-item>
            <el-dropdown-item divided @click="runAction(profiles.stopSpeedtest)">{{ t('TbCancel') }}</el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>
    </div>
    <el-table
      v-loading="profiles.isLoading"
      :data="profiles.rows"
      row-key="indexId"
      height="100%"
      size="small"
      border
      :row-class-name="rowClassName"
      class="profile-table__grid"
      @selection-change="selected = $event"
      @row-dblclick="handleActivate"
    >
      <el-table-column type="selection" width="40" />
      <el-table-column prop="configType" :label="t('LvServiceType')" width="110" />
      <el-table-column prop="remarks" :label="t('LvRemarks')" min-width="200" show-overflow-tooltip />
      <el-table-column prop="address" :label="t('LvAddress')" min-width="160" show-overflow-tooltip />
      <el-table-column prop="port" :label="t('LvPort')" width="70" />
      <el-table-column prop="network" :label="t('LvTransportProtocol')" width="90" />
      <el-table-column prop="streamSecurity" :label="t('LvTLS')" width="80" />
      <el-table-column prop="subRemarks" :label="t('LvSubscription')" width="120" show-overflow-tooltip />
      <el-table-column prop="delayVal" :label="t('LvTestDelay')" width="90" sortable :sort-by="'delay'" />
      <el-table-column prop="speedVal" :label="t('LvTestSpeed')" width="100" show-overflow-tooltip />
      <el-table-column prop="ipInfo" :label="t('LvTestIpInfo')" width="140" show-overflow-tooltip />
      <el-table-column prop="todayUp" :label="t('LvTodayUploadDataAmount')" width="100" />
      <el-table-column prop="todayDown" :label="t('LvTodayDownloadDataAmount')" width="100" />
      <el-table-column prop="totalUp" :label="t('LvTotalUploadDataAmount')" width="100" />
      <el-table-column prop="totalDown" :label="t('LvTotalDownloadDataAmount')" width="100" />
    </el-table>
    <ImportDialog v-model="isImportOpen" />
    <ShareDialog v-model="isShareOpen" :index-id="share.indexId" :uri="share.uri" />
  </div>
</template>

<style scoped>
.profile-table {
  display: flex;
  flex-direction: column;
  height: 100%;
  min-height: 0;
}
.profile-table__toolbar {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  padding: 6px 0;
}
.profile-table__filter {
  width: 220px;
}
.profile-table__grid {
  flex: 1;
}
.profile-table :deep(.profile-table__row--active) {
  font-weight: 600;
  color: var(--el-color-primary);
}
</style>
