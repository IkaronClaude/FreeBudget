<script setup lang="ts">
import { computed, ref, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { BankAccount } from '../api/types';

const me = useMeStore();
const error = ref<string | null>(null);

type BankTypeName = 'Barclays' | 'Wise' | 'NatWest' | 'Manual';

const newAccount = reactive({
  bankType: 'Barclays' as BankTypeName,
  nickname: '',
  currencies: '',
  saving: false,
});

const renaming = ref<{ id: string; nickname: string } | null>(null);
const addingChildTo = ref<{ parentId: string; currency: string } | null>(null);

const bankTypes: BankTypeName[] = ['Barclays', 'Wise', 'NatWest', 'Manual'];

const parsedCurrencies = computed<string[]>(() => {
  const seen = new Set<string>();
  for (const raw of newAccount.currencies.split(/[\s,]+/)) {
    const c = raw.trim().toUpperCase();
    if (c.length === 3) seen.add(c);
  }
  return [...seen];
});

async function refresh() {
  await me.refresh();
}

async function createAccount() {
  if (!newAccount.nickname.trim()) return;
  newAccount.saving = true;
  error.value = null;
  try {
    if (parsedCurrencies.value.length > 1) {
      await api.post('/bank-accounts/parent', {
        bankType: newAccount.bankType,
        nickname: newAccount.nickname.trim(),
        currencyCodes: parsedCurrencies.value,
      });
    } else {
      await api.post('/bank-accounts', {
        bankType: newAccount.bankType,
        nickname: newAccount.nickname.trim(),
        currencyCode: parsedCurrencies.value[0] ?? null,
      });
    }
    newAccount.nickname = '';
    newAccount.currencies = '';
    await refresh();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Create failed');
  } finally {
    newAccount.saving = false;
  }
}

function startRename(account: BankAccount) {
  renaming.value = { id: account.id, nickname: account.nickname ?? '' };
}

function cancelRename() {
  renaming.value = null;
}

async function saveRename() {
  if (!renaming.value || !renaming.value.nickname.trim()) return;
  try {
    await api.put(`/bank-accounts/${renaming.value.id}`, {
      nickname: renaming.value.nickname.trim(),
    });
    renaming.value = null;
    await refresh();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Rename failed');
  }
}

async function deleteAccount(account: BankAccount) {
  const label = me.accountLabel(account);
  if (!confirm(`Delete "${label}"? Existing transactions stay imported but the account link is gone.`)) return;
  try {
    await api.delete(`/bank-accounts/${account.id}`);
    await refresh();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Delete failed');
  }
}

function startAddChild(parentId: string) {
  addingChildTo.value = { parentId, currency: '' };
}

function cancelAddChild() {
  addingChildTo.value = null;
}

async function saveAddChild() {
  if (!addingChildTo.value) return;
  const currency = addingChildTo.value.currency.trim().toUpperCase();
  if (currency.length !== 3) {
    error.value = 'Enter a 3-letter currency code.';
    return;
  }
  try {
    await api.post(`/bank-accounts/${addingChildTo.value.parentId}/children`, {
      currencyCode: currency,
    });
    addingChildTo.value = null;
    await refresh();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Add currency failed');
  }
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Bank accounts</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
      <h3 class="font-medium mb-3">Add account</h3>
      <div class="grid gap-3 md:grid-cols-[8rem_1fr_12rem_auto] items-end">
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Bank</span>
          <select v-model="newAccount.bankType" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="b in bankTypes" :key="b" :value="b">{{ b }}</option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Nickname</span>
          <input
            v-model="newAccount.nickname"
            type="text"
            placeholder="Wise - Personal"
            class="border border-slate-300 rounded px-3 py-2"
            @keydown.enter="createAccount"
          />
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Currencies (optional)</span>
          <input
            v-model="newAccount.currencies"
            type="text"
            placeholder="GBP, EUR, USD"
            class="border border-slate-300 rounded px-3 py-2 uppercase"
            @keydown.enter="createAccount"
          />
        </label>
        <button
          @click="createAccount"
          :disabled="!newAccount.nickname.trim() || newAccount.saving"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >
          {{ newAccount.saving ? 'Adding...' : 'Add' }}
        </button>
      </div>
      <p class="mt-2 text-xs text-slate-500">
        Enter one currency for a single-currency account, or comma-separated codes (e.g. <code>GBP, EUR, USD</code>) for a multi-currency parent like Wise.
      </p>
      <p v-if="error" class="mt-3 text-red-600 text-sm">{{ error }}</p>
    </section>

    <section v-if="me.loading" class="bg-white rounded border border-slate-200 p-4 text-slate-500">Loading...</section>

    <section v-else-if="!me.bankAccounts.length" class="bg-white rounded border border-slate-200 p-4 text-slate-500">
      No bank accounts yet. Add one above.
    </section>

    <template v-else>
      <section v-if="me.standaloneAccounts.length" class="bg-white rounded border border-slate-200 overflow-hidden">
        <header class="px-4 py-2 bg-slate-50 text-slate-600 text-sm font-medium">Standalone accounts</header>
        <table class="w-full text-sm">
          <tbody>
            <tr v-for="a in me.standaloneAccounts" :key="a.id" class="border-t border-slate-100">
              <td class="px-4 py-2 w-32 text-slate-600">{{ a.bankType }}</td>
              <td class="px-4 py-2">
                <template v-if="renaming?.id === a.id">
                  <div class="flex gap-2">
                    <input v-model="renaming.nickname" type="text" class="border border-slate-300 rounded px-3 py-1 flex-1" @keydown.enter="saveRename" @keydown.escape="cancelRename" />
                    <button @click="saveRename" class="text-blue-600 hover:underline text-sm">Save</button>
                    <button @click="cancelRename" class="text-slate-600 hover:underline text-sm">Cancel</button>
                  </div>
                </template>
                <template v-else>{{ me.accountLabel(a) }}</template>
              </td>
              <td class="px-4 py-2 text-right space-x-2 w-40">
                <button v-if="!renaming || renaming.id !== a.id" @click="startRename(a)" class="text-blue-600 hover:underline">Rename</button>
                <button v-if="!renaming || renaming.id !== a.id" @click="deleteAccount(a)" class="text-red-600 hover:underline">Delete</button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>

      <section v-for="group in me.accountGroups" :key="group.parent.id" class="bg-white rounded border border-slate-200 overflow-hidden">
        <header class="px-4 py-3 bg-slate-50 flex items-center justify-between gap-3">
          <div class="flex items-center gap-3">
            <span class="text-slate-600 text-xs uppercase tracking-wide">{{ group.parent.bankType }}</span>
            <template v-if="renaming?.id === group.parent.id">
              <input v-model="renaming.nickname" type="text" class="border border-slate-300 rounded px-3 py-1" @keydown.enter="saveRename" @keydown.escape="cancelRename" />
              <button @click="saveRename" class="text-blue-600 hover:underline text-sm">Save</button>
              <button @click="cancelRename" class="text-slate-600 hover:underline text-sm">Cancel</button>
            </template>
            <span v-else class="font-medium">{{ group.parent.nickname }}</span>
          </div>
          <div class="space-x-2 text-sm" v-if="!renaming || renaming.id !== group.parent.id">
            <button @click="startRename(group.parent)" class="text-blue-600 hover:underline">Rename</button>
            <button @click="deleteAccount(group.parent)" class="text-red-600 hover:underline">Delete parent</button>
          </div>
        </header>
        <table class="w-full text-sm">
          <tbody>
            <tr v-for="child in group.children" :key="child.id" class="border-t border-slate-100">
              <td class="px-4 py-2 w-32 text-slate-500 pl-8">↳ {{ child.currencyCode }}</td>
              <td class="px-4 py-2 text-slate-600">{{ me.accountLabel(child) }}</td>
              <td class="px-4 py-2 text-right w-40">
                <button @click="deleteAccount(child)" class="text-red-600 hover:underline">Delete</button>
              </td>
            </tr>
            <tr v-if="addingChildTo?.parentId === group.parent.id" class="border-t border-slate-100 bg-slate-50">
              <td colspan="3" class="px-4 py-2">
                <div class="flex gap-2 items-center text-sm">
                  <span class="text-slate-600">Add currency:</span>
                  <input
                    v-model="addingChildTo.currency"
                    type="text"
                    maxlength="3"
                    placeholder="JPY"
                    class="border border-slate-300 rounded px-3 py-1 w-24 uppercase"
                    @keydown.enter="saveAddChild"
                    @keydown.escape="cancelAddChild"
                  />
                  <button @click="saveAddChild" class="text-blue-600 hover:underline">Save</button>
                  <button @click="cancelAddChild" class="text-slate-600 hover:underline">Cancel</button>
                </div>
              </td>
            </tr>
            <tr v-else class="border-t border-slate-100">
              <td colspan="3" class="px-4 py-2">
                <button @click="startAddChild(group.parent.id)" class="text-blue-600 hover:underline text-sm">+ Add currency</button>
              </td>
            </tr>
          </tbody>
        </table>
      </section>
    </template>
  </div>
</template>
