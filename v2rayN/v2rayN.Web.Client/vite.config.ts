import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// Dev: `npm run dev` proxies the API to a locally running v2rayN.Web (default port 8080).
const apiTarget = process.env.V2RAYN_API_TARGET ?? 'http://127.0.0.1:8080'

export default defineConfig({
  plugins: [vue()],
  build: {
    outDir: '../v2rayN.Web/wwwroot',
    emptyOutDir: true,
    chunkSizeWarningLimit: 1500,
  },
  server: {
    proxy: {
      '/api': { target: apiTarget, changeOrigin: false },
      '/healthz': { target: apiTarget },
    },
  },
})
