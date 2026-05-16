import { createApp } from 'vue';
import { createPinia } from 'pinia';
import App from './App.vue';
import { router } from './router';
import { api } from './api/client';
import { useAuthStore } from './stores/auth';
import { useMeStore } from './stores/me';
import './style.css';

const app = createApp(App);
const pinia = createPinia();
app.use(pinia);
app.use(router);

const auth = useAuthStore();
const me = useMeStore();

api.interceptors.request.use(config => {
  if (auth.token) {
    config.headers.set('Authorization', `Bearer ${auth.token}`);
  }
  return config;
});

api.interceptors.response.use(
  response => response,
  error => {
    if (error?.response?.status === 401) {
      auth.clearSession();
      me.reset();
      if (router.currentRoute.value.name !== 'login' && router.currentRoute.value.name !== 'register') {
        router.push({ name: 'login', query: { redirect: router.currentRoute.value.fullPath } });
      }
    }
    return Promise.reject(error);
  }
);

router.beforeEach(to => {
  const publicRoute = to.name === 'login' || to.name === 'register';
  if (!publicRoute && !auth.isAuthenticated) {
    return { name: 'login', query: { redirect: to.fullPath } };
  }
  if (publicRoute && auth.isAuthenticated) {
    return { name: 'dashboard' };
  }
  return true;
});

app.mount('#app');
