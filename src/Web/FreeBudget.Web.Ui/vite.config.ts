import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

const proxyTarget = process.env.VITE_PROXY_TARGET ?? 'http://localhost:5500';

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    host: true,
    watch: { usePolling: true },
    proxy: {
      '/api': {
        target: proxyTarget,
        changeOrigin: true
      }
    }
  }
});
