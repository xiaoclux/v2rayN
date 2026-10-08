<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { API } from '../api/endpoints'

const props = defineProps<{ modelValue: boolean; indexId: string | null; uri: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()

const { t } = useI18n()
const qrUrl = computed(() => (props.indexId ? API.profileQr(props.indexId) : ''))

async function handleCopy(): Promise<void> {
  // The async clipboard API only exists on HTTPS/localhost; otherwise the user copies from the text box.
  if (!navigator.clipboard) {
    ElMessage.warning(t('OperationFailed'))
    return
  }
  try {
    await navigator.clipboard.writeText(props.uri)
    ElMessage.success(t('OperationSuccess'))
  } catch (err) {
    ElMessage.warning(`${t('OperationFailed')}: ${err instanceof Error ? err.message : String(err)}`)
  }
}
</script>

<template>
  <el-dialog :model-value="modelValue" :title="t('menuShareServer')" width="420px" @update:model-value="emit('update:modelValue', $event)">
    <div class="share">
      <img v-if="qrUrl" :src="qrUrl" alt="QR" class="share__qr" />
      <el-input :model-value="uri" type="textarea" :rows="4" readonly />
    </div>
    <template #footer>
      <el-button type="primary" @click="handleCopy">{{ t('menuMsgViewCopy') }}</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.share {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}
.share__qr {
  width: 260px;
  height: 260px;
  background: #fff;
}
</style>
