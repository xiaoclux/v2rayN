<script setup lang="ts">
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { ElMessage, type UploadRawFile } from 'element-plus'
import { MAX_UPLOAD_BYTES } from '../api/endpoints'
import { useProfilesStore } from '../stores/profiles'
import { reportError } from '../utils/notify'

defineProps<{ modelValue: boolean }>()
const emit = defineEmits<{ 'update:modelValue': [value: boolean] }>()

const { t } = useI18n()
const profiles = useProfilesStore()
const text = ref('')
const isBusy = ref(false)

async function run(action: () => Promise<number>, successKey: string): Promise<void> {
  isBusy.value = true
  try {
    const count = await action()
    ElMessage.success(t(successKey, [count]))
    text.value = ''
    emit('update:modelValue', false)
  } catch (err) {
    reportError(err)
  } finally {
    isBusy.value = false
  }
}

function handleImportText(): void {
  if (text.value.trim()) {
    void run(() => profiles.importText(text.value), 'SuccessfullyImportedServerViaClipboard')
  }
}

// Returning false stops el-upload's own request; the file goes through the API client instead.
function handleQrFile(file: UploadRawFile): boolean {
  if (file.size > MAX_UPLOAD_BYTES) {
    ElMessage.error(t('OperationFailed'))
    return false
  }
  void run(() => profiles.importQr(file), 'SuccessfullyImportedServerViaScan')
  return false
}
</script>

<template>
  <el-dialog :model-value="modelValue" :title="t('menuAddServerViaClipboard')" width="640px" @update:model-value="emit('update:modelValue', $event)">
    <el-input v-model="text" type="textarea" :rows="10" placeholder="vmess:// vless:// ss:// trojan:// hysteria2:// ..." />
    <template #footer>
      <el-upload :show-file-list="false" accept="image/*" :before-upload="handleQrFile" class="import__qr">
        <el-button :loading="isBusy">{{ t('menuAddServerViaImage') }}</el-button>
      </el-upload>
      <el-button @click="emit('update:modelValue', false)">{{ t('TbCancel') }}</el-button>
      <el-button type="primary" :loading="isBusy" :disabled="!text.trim()" @click="handleImportText">{{ t('TbConfirm') }}</el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.import__qr {
  display: inline-block;
  margin-right: 12px;
}
</style>
