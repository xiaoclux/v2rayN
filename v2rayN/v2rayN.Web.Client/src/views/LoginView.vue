<script setup lang="ts">
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useI18n } from 'vue-i18n'
import { ElMessage } from 'element-plus'
import { ROUTES } from '../api/endpoints'
import { useSessionStore } from '../stores/session'

const { t } = useI18n()
const route = useRoute()
const router = useRouter()
const session = useSessionStore()

const credential = ref('')
const isSubmitting = ref(false)

async function handleSubmit(): Promise<void> {
  if (!credential.value || isSubmitting.value) {
    return
  }
  isSubmitting.value = true
  try {
    await session.login(credential.value)
    credential.value = ''
    const redirect = typeof route.query.redirect === 'string' && route.query.redirect.startsWith('/') ? route.query.redirect : ROUTES.home
    await router.replace(redirect)
  } catch (err) {
    ElMessage.error(`${t('web.login.failed')}: ${err instanceof Error ? err.message : String(err)}`)
  } finally {
    isSubmitting.value = false
  }
}
</script>

<template>
  <div class="login">
    <el-card class="login__card">
      <template #header>{{ t('web.login.title') }}</template>
      <el-form @submit.prevent="handleSubmit">
        <el-form-item :label="t('web.login.credential')">
          <el-input v-model="credential" type="password" show-password autocomplete="current-password" autofocus />
        </el-form-item>
        <el-button type="primary" native-type="submit" :loading="isSubmitting" class="login__submit">
          {{ t('web.login.submit') }}
        </el-button>
      </el-form>
    </el-card>
  </div>
</template>

<style scoped>
.login {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: 16px;
  box-sizing: border-box;
}
.login__card {
  width: 100%;
  max-width: 380px;
}
.login__submit {
  width: 100%;
}
</style>
