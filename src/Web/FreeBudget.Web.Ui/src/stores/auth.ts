import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { api } from '../api/client';
import type { User } from '../api/types';

const TOKEN_KEY = 'freebudget.auth.token';
const EXPIRES_KEY = 'freebudget.auth.expires';

interface AuthResponse {
  accessToken: string;
  expiresAt: string;
  user: User;
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem(TOKEN_KEY));
  const expiresAt = ref<Date | null>(parseExpires(localStorage.getItem(EXPIRES_KEY)));
  const submitting = ref(false);
  const error = ref<string | null>(null);

  const isAuthenticated = computed(() => {
    if (!token.value) return false;
    if (expiresAt.value && expiresAt.value.getTime() <= Date.now()) return false;
    return true;
  });

  function setSession(t: string, exp: string) {
    token.value = t;
    expiresAt.value = new Date(exp);
    localStorage.setItem(TOKEN_KEY, t);
    localStorage.setItem(EXPIRES_KEY, exp);
  }

  function clearSession() {
    token.value = null;
    expiresAt.value = null;
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(EXPIRES_KEY);
  }

  async function login(email: string, password: string): Promise<boolean> {
    submitting.value = true;
    error.value = null;
    try {
      const { data } = await api.post<AuthResponse>('/auth/login', { email, password });
      setSession(data.accessToken, data.expiresAt);
      return true;
    } catch (e: unknown) {
      error.value = extractError(e, 'Invalid email or password.');
      return false;
    } finally {
      submitting.value = false;
    }
  }

  async function register(email: string, displayName: string, password: string): Promise<boolean> {
    submitting.value = true;
    error.value = null;
    try {
      const { data } = await api.post<AuthResponse>('/auth/register', { email, displayName, password });
      setSession(data.accessToken, data.expiresAt);
      return true;
    } catch (e: unknown) {
      error.value = extractError(e, 'Registration failed.');
      return false;
    } finally {
      submitting.value = false;
    }
  }

  function logout() {
    clearSession();
  }

  return {
    token,
    expiresAt,
    submitting,
    error,
    isAuthenticated,
    login,
    register,
    logout,
    clearSession,
  };
});

function parseExpires(raw: string | null): Date | null {
  if (!raw) return null;
  const d = new Date(raw);
  return isNaN(d.getTime()) ? null : d;
}

function extractError(e: unknown, fallback: string): string {
  if (typeof e === 'object' && e !== null && 'response' in e) {
    const resp = (e as { response?: { data?: { error?: string } } }).response;
    if (resp?.data?.error) return resp.data.error;
  }
  return fallback;
}
