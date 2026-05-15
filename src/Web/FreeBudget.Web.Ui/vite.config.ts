import { defineConfig } from 'vite';
import vue from '@vitejs/plugin-vue';

const proxyTarget = process.env.VITE_PROXY_TARGET ?? 'http://localhost:5500';

// Vite blocks unknown Host headers by default (SSRF mitigation). When
// running behind a reverse proxy / non-localhost domain, set
// VITE_ALLOWED_HOSTS to a comma-separated list (leading-dot wildcards
// supported, e.g. ".example.com").
const allowedHosts = process.env.VITE_ALLOWED_HOSTS
  ? process.env.VITE_ALLOWED_HOSTS.split(',').map(s => s.trim())
  : undefined;

export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5173,
    host: true,
    allowedHosts,
    watch: { usePolling: true },
    proxy: {
      '/api': {
        target: proxyTarget,
        changeOrigin: true
      }
    }
  }
});
