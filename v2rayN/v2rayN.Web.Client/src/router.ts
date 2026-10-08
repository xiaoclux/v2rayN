import { createRouter, createWebHistory } from 'vue-router'
import { ROUTES } from './api/endpoints'
import { useSessionStore } from './stores/session'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: ROUTES.login, name: 'login', component: () => import('./views/LoginView.vue'), meta: { isPublic: true } },
    { path: ROUTES.home, name: 'home', component: () => import('./views/MainView.vue') },
    { path: '/:pathMatch(.*)*', redirect: ROUTES.home },
  ],
})

router.beforeEach(async (to) => {
  const session = useSessionStore()
  const isAuthenticated = session.isAuthenticated || (await session.refresh())
  if (to.meta.isPublic) {
    return isAuthenticated && to.path === ROUTES.login ? ROUTES.home : true
  }
  return isAuthenticated ? true : { path: ROUTES.login, query: { redirect: to.fullPath } }
})
