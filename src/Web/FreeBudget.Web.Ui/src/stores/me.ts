import { defineStore } from 'pinia';
import { ref } from 'vue';
import { api } from '../api/client';
import type { MeResponse, User, Group, BankAccount } from '../api/types';

export const useMeStore = defineStore('me', () => {
  const user = ref<User | null>(null);
  const groups = ref<Group[]>([]);
  const bankAccounts = ref<BankAccount[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function load() {
    if (loading.value || user.value) return;
    loading.value = true;
    error.value = null;
    try {
      const { data } = await api.get<MeResponse>('/me');
      user.value = data.user;
      groups.value = data.groups;
      bankAccounts.value = data.bankAccounts;
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : 'Failed to load';
      error.value = msg;
    } finally {
      loading.value = false;
    }
  }

  return { user, groups, bankAccounts, loading, error, load };
});
