<script setup lang="ts">
import { ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import type { SubItem } from '../api/types'
import { useSubsStore } from '../stores/subs'
import { reportError } from '../utils/notify'

const props = defineProps<{ modelValue: boolean; sub: SubItem | null }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()

const { t } = useI18n()
const subs = useSubsStore()
const form = ref<SubItem | null>(null)
const isSaving = ref(false)

// Edit a copy so cancelling leaves the list untouched.
watch(
  () => [props.modelValue, props.sub] as const,
  ([isOpen, sub]) => {
    if (isOpen && sub) {
      form.value = { ...sub }
    }
  },
  { immediate: true },
)

function close(): void {
  emit('update:modelValue', false)
}

async function handleSave(): Promise<void> {
  if (!form.value) {
    return
  }
  isSaving.value = true
  try {
    const res = await subs.save(form.value)
    ElMessage.success(res.message ?? t('OperationSuccess'))
    close()
  } catch (err) {
    reportError(err)
  } finally {
    isSaving.value = false
  }
}
</script>

<template>
  <el-dialog :model-value="modelValue" :title="t('menuSubSetting')" width="640px" @update:model-value="emit('update:modelValue', $event)">
    <el-form v-if="form" label-width="160px" label-position="right">
      <el-form-item :label="t('LvRemarks')" required>
        <el-input v-model="form.remarks" maxlength="256" />
      </el-form-item>
      <el-form-item :label="t('LvUrl')">
        <el-input v-model="form.url" type="textarea" :rows="2" :placeholder="t('SubUrlTips')" />
      </el-form-item>
      <el-form-item :label="t('LvMoreUrl')">
        <el-input v-model="form.moreUrl" type="textarea" :rows="2" />
      </el-form-item>
      <el-form-item :label="t('LvEnabled')">
        <el-switch v-model="form.enabled" />
      </el-form-item>
      <el-form-item :label="t('LvAutoUpdateInterval')">
        <el-input-number v-model="form.autoUpdateInterval" :min="0" :max="525600" />
      </el-form-item>
      <el-form-item :label="t('LvUserAgent')">
        <el-input v-model="form.userAgent" />
      </el-form-item>
      <el-form-item :label="t('LvRequestHeaders')">
        <el-input v-model="form.requestHeaders" type="textarea" :rows="2" :placeholder="t('SubRequestHeadersTips')" />
      </el-form-item>
      <el-form-item :label="t('LvFilter')">
        <el-input v-model="form.filter" />
      </el-form-item>
      <el-form-item :label="t('LvConvertTarget')">
        <el-input v-model="form.convertTarget" :placeholder="t('LvConvertTargetTip')" />
      </el-form-item>
      <el-form-item :label="t('LvSort')">
        <el-input-number v-model="form.sort" :min="0" />
      </el-form-item>
      <el-form-item :label="t('TbPreSocksPort4Sub')">
        <el-input-number v-model="form.preSocksPort" :min="1" :max="65535" :value-on-clear="null" :placeholder="t('TipPreSocksPort')" />
      </el-form-item>
      <el-form-item :label="t('LvMemo')">
        <el-input v-model="form.memo" type="textarea" :rows="2" />
      </el-form-item>
    </el-form>
    <template #footer>
      <el-button @click="close">{{ t('TbCancel') }}</el-button>
      <el-button type="primary" :loading="isSaving" @click="handleSave">{{ t('TbConfirm') }}</el-button>
    </template>
  </el-dialog>
</template>
