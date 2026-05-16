<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/auth';
import { useMeStore } from '../stores/me';

const router = useRouter();
const auth = useAuthStore();
const me = useMeStore();

const email = ref('');
const displayName = ref('');
const password = ref('');
const confirm = ref('');
const localError = ref<string | null>(null);

async function submit() {
  localError.value = null;
  if (password.value !== confirm.value) {
    localError.value = 'Passwords do not match.';
    return;
  }
  if (password.value.length < 8) {
    localError.value = 'Password must be at least 8 characters.';
    return;
  }
  const ok = await auth.register(email.value, displayName.value, password.value);
  if (!ok) return;
  await me.refresh();
  router.replace('/');
}
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-slate-50 px-4">
    <form @submit.prevent="submit" class="w-full max-w-sm bg-white border border-slate-200 rounded p-6 space-y-4">
      <h1 class="text-xl font-semibold">Create your FreeBudget account</h1>
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
        <label class="block text-sm font-medium text-slate-700 mb-1" for="displayName">Display name</label>
        <input
          id="displayName"
          v-model="displayName"
          type="text"
          autocomplete="name"
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
          autocomplete="new-password"
          required
          minlength="8"
          class="w-full px-3 py-2 border border-slate-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div>
        <label class="block text-sm font-medium text-slate-700 mb-1" for="confirm">Confirm password</label>
        <input
          id="confirm"
          v-model="confirm"
          type="password"
          autocomplete="new-password"
          required
          class="w-full px-3 py-2 border border-slate-300 rounded focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <p v-if="localError || auth.error" class="text-sm text-red-600">{{ localError ?? auth.error }}</p>
      <button
        type="submit"
        :disabled="auth.submitting"
        class="w-full bg-blue-600 text-white py-2 rounded hover:bg-blue-700 disabled:opacity-60"
      >
        {{ auth.submitting ? 'Creating account...' : 'Create account' }}
      </button>
      <p class="text-sm text-slate-600 text-center">
        Already have an account?
        <RouterLink to="/login" class="text-blue-600 hover:underline">Sign in</RouterLink>
      </p>
    </form>
  </div>
</template>
