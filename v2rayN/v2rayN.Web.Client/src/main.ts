import { createApp } from 'vue'
import { createPinia } from 'pinia'
import ElementPlus, { ElMessage } from 'element-plus'
import 'element-plus/dist/index.css'
import 'element-plus/theme-chalk/dark/css-vars.css'
import App from './App.vue'
import { router } from './router'
import { i18n } from './i18n'
import { setUnauthorizedHandler } from './api/client'
import { ROUTES } from './api/endpoints'
import { useSessionStore } from './stores/session'
import { reportError } from './utils/notify'
import './style.css'

const app = createApp(App)
app.use(createPinia())
app.use(router)
app.use(i18n)
app.use(ElementPlus)

app.config.errorHandler = (err) => reportError(err)
window.addEventListener('unhandledrejection', (evt) => reportError(evt.reason))

setUnauthorizedHandler(() => {
  useSessionStore().markSignedOut()
  if (router.currentRoute.value.path !== ROUTES.login) {
    router.push(ROUTES.login).catch(reportError)
    ElMessage.closeAll()
  }
})

app.mount('#app')
