import { defineStore } from 'pinia';
import { computed, ref } from 'vue';
import { api } from '../api/client';
import type { MeResponse, User, Group, BankAccount } from '../api/types';

export interface AccountGroup {
  parent: BankAccount;
  children: BankAccount[];
}

export const useMeStore = defineStore('me', () => {
  const user = ref<User | null>(null);
  const groups = ref<Group[]>([]);
  const bankAccounts = ref<BankAccount[]>([]);
  const loading = ref(false);
  const error = ref<string | null>(null);

  async function load(force = false) {
    if (loading.value) return;
    if (!force && user.value) return;
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

  const refresh = () => load(true);

  function accountById(id: string | null | undefined): BankAccount | undefined {
    if (!id) return undefined;
    return bankAccounts.value.find(a => a.id === id);
  }

  function accountLabel(account: BankAccount | null | undefined): string {
    if (!account) return '';
    if (account.parentBankAccountId) {
      const parent = accountById(account.parentBankAccountId);
      const stem = parent?.nickname ?? account.bankType;
      return account.currencyCode ? `${stem} (${account.currencyCode})` : stem;
    }
    return account.currencyCode ? `${account.nickname ?? account.bankType} (${account.currencyCode})` : (account.nickname ?? account.bankType);
  }

  const standaloneAccounts = computed<BankAccount[]>(() =>
    bankAccounts.value.filter(a => !a.parentBankAccountId && !hasChildren(a.id)));

  const accountGroups = computed<AccountGroup[]>(() => {
    const parents = bankAccounts.value.filter(a => !a.parentBankAccountId && hasChildren(a.id));
    return parents.map(parent => ({
      parent,
      children: bankAccounts.value.filter(a => a.parentBankAccountId === parent.id),
    }));
  });

  function hasChildren(parentId: string): boolean {
    return bankAccounts.value.some(a => a.parentBankAccountId === parentId);
  }

  return {
    user,
    groups,
    bankAccounts,
    loading,
    error,
    load,
    refresh,
    accountById,
    accountLabel,
    standaloneAccounts,
    accountGroups,
  };
});
