<script setup lang="ts">
import { ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useMeStore } from '../stores/me';

const router = useRouter();
const route = useRoute();
const auth = useAuthStore();
const me = useMeStore();

const email = ref('');
const password = ref('');

async function submit() {
  const ok = await auth.login(email.value, password.value);
  if (!ok) return;
  await me.refresh();
  const redirect = typeof route.query.redirect === 'string' ? route.query.redirect : '/';
  router.replace(redirect);
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 px-4">
    <form @submit.prevent="submit" class="w-full max-w-sm bg-white border border-slate-200 rounded p-6 space-y-4">
      <h1 class="text-xl font-semibold">Sign in to FreeBudget</h1>
      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="email">Email</label>
        <input
          id="email"
          v-model="email"
          type="email"
          autocomplete="email"
          required
          class="w-full px-3 py-2 border border-slate-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="password">Password</label>
        <input
          id="password"
          v-model="password"
          type="password"
          autocomplete="current-password"
          required
          class="w-full px-3 py-2 border border-slate-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <p v-if="auth.error" class="text-sm text-red-600">{{ auth.error }}</p>
      <button
        type="submit"
        :disabled="auth.submitting"
        class="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-60"
      >
        {{ auth.submitting ? 'Signing in...' : 'Sign in' }}
      </button>
      <p class="text-sm text-slate-600 text-center">
        No account?
        <RouterLink to="/register" class="text-blue-600 hover:underline">Register</RouterLink>
      </p>
    </form>
  </div>
</template>
