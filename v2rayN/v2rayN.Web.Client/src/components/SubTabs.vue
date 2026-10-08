<script setup lang="ts">
import { computed, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { SubItem } from '../api/types'
import { ALL_SUBS_ID, newSubItem, useSubsStore } from '../stores/subs'
import { useProfilesStore } from '../stores/profiles'
import { runAction } from '../utils/notify'
import SubEditDialog from './SubEditDialog.vue'

const { t } = useI18n()
const subs = useSubsStore()
const profiles = useProfilesStore()

const isEditorOpen = ref(false)
const editing = ref<SubItem | null>(null)
const currentSub = computed(() => subs.items.find((s) => s.id === subs.currentSubId) ?? null)

const activeTab = computed({
  get: () => subs.currentSubId,
  set: (id: string) =>
    runAction(async () => {
      await subs.selectSub(id)
      await profiles.load()
    }),
})

function openEditor(sub: SubItem): void {
  editing.value = sub
  isEditorOpen.value = true
}

function handleDelete(): void {
  const sub = currentSub.value
  if (!sub) {
    return
  }
  runAction(async () => {
    await ElMessageBox.confirm(`${t('menuSubDelete')}: ${sub.remarks}?`, { type: 'warning' })
    await subs.remove(sub.id)
    await profiles.load()
  })
}

function handleUpdate(isViaProxy: boolean, isAll: boolean): void {
  runAction(async () => {
    await subs.update(isAll ? ALL_SUBS_ID : subs.currentSubId, isViaProxy)
    ElMessage.info(t('MsgUpdateSubscriptionStart'))
  })
}
</script>

<template>
  <div class="sub-tabs">
    <el-tabs v-model="activeTab" class="sub-tabs__tabs">
      <el-tab-pane :label="t('menuAllServers')" :name="ALL_SUBS_ID" />
      <el-tab-pane v-for="s in subs.items" :key="s.id" :label="s.remarks" :name="s.id" />
    </el-tabs>
    <el-dropdown trigger="click">
      <el-button size="small">{{ t('menuSubscription') }}</el-button>
      <template #dropdown>
        <el-dropdown-menu>
          <el-dropdown-item @click="openEditor(newSubItem())">{{ t('menuSubAdd') }}</el-dropdown-item>
          <el-dropdown-item :disabled="!currentSub" @click="currentSub && openEditor(currentSub)">{{ t('menuSubEdit') }}</el-dropdown-item>
          <el-dropdown-item :disabled="!currentSub" @click="handleDelete">{{ t('menuSubDelete') }}</el-dropdown-item>
          <el-dropdown-item divided @click="handleUpdate(false, true)">{{ t('menuSubUpdate') }}</el-dropdown-item>
          <el-dropdown-item @click="handleUpdate(true, true)">{{ t('menuSubUpdateViaProxy') }}</el-dropdown-item>
          <el-dropdown-item :disabled="!currentSub" @click="handleUpdate(false, false)">{{ t('menuSubGroupUpdate') }}</el-dropdown-item>
          <el-dropdown-item :disabled="!currentSub" @click="handleUpdate(true, false)">{{ t('menuSubGroupUpdateViaProxy') }}</el-dropdown-item>
        </el-dropdown-menu>
      </template>
    </el-dropdown>
    <SubEditDialog v-model="isEditorOpen" :sub="editing" />
  </div>
</template>

<style scoped>
.sub-tabs {
  display: flex;
  align-items: center;
  gap: 8px;
}
.sub-tabs__tabs {
  flex: 1;
  min-width: 0;
}
.sub-tabs__tabs :deep(.el-tabs__header) {
  margin: 0;
}
</style>
