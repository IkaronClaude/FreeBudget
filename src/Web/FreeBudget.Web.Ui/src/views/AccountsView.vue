<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { BankAccount } from '../api/types';

const me = useMeStore();
const error = ref<string | null>(null);

type BankTypeName = 'Barclays' | 'Wise' | 'NatWest' | 'Manual';

const newAccount = reactive({
  bankType: 'Barclays' as BankTypeName,
  nickname: '',
  saving: false,
});

const renaming = ref<{ id: string; nickname: string } | null>(null);

const bankTypes: BankTypeName[] = ['Barclays', 'Wise', 'NatWest', 'Manual'];

async function refresh() {
  await me.refresh();
}

async function createAccount() {
  if (!newAccount.nickname.trim()) return;
  newAccount.saving = true;
  error.value = null;
  try {
    await api.post('/bank-accounts', {
      bankType: newAccount.bankType,
      nickname: newAccount.nickname.trim(),
    });
    newAccount.nickname = '';
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Create failed';
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
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Rename failed';
  }
}

async function deleteAccount(account: BankAccount) {
  if (!confirm(`Delete "${account.nickname}"? Existing transactions stay imported but the account link is gone.`)) return;
  try {
    await api.delete(`/bank-accounts/${account.id}`);
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Delete failed';
  }
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Bank accounts</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
      <h3 class="font-medium mb-3">Add account</h3>
      <div class="grid gap-3 md:grid-cols-[10rem_1fr_auto] items-end">
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
        <button
          @click="createAccount"
          :disabled="!newAccount.nickname.trim() || newAccount.saving"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >
          {{ newAccount.saving ? 'Adding...' : 'Add' }}
        </button>
      </div>
      <p v-if="error" class="mt-3 text-red-600 text-sm">{{ error }}</p>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="me.loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="me.bankAccounts.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2 w-32">Bank</th>
            <th class="text-left px-4 py-2">Nickname</th>
            <th class="text-right px-4 py-2 w-40"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="a in me.bankAccounts" :key="a.id" class="border-t border-slate-100">
            <td class="px-4 py-2">{{ a.bankType }}</td>
            <td class="px-4 py-2">
              <template v-if="renaming?.id === a.id">
                <div class="flex gap-2">
                  <input
                    v-model="renaming.nickname"
                    type="text"
                    class="border border-slate-300 rounded px-3 py-1 flex-1"
                    @keydown.enter="saveRename"
                    @keydown.escape="cancelRename"
                  />
                  <button @click="saveRename" class="text-blue-600 hover:underline text-sm">Save</button>
                  <button @click="cancelRename" class="text-slate-600 hover:underline text-sm">Cancel</button>
                </div>
              </template>
              <template v-else>{{ a.nickname }}</template>
            </td>
            <td class="px-4 py-2 text-right space-x-2">
              <button v-if="!renaming || renaming.id !== a.id" @click="startRename(a)" class="text-blue-600 hover:underline">Rename</button>
              <button v-if="!renaming || renaming.id !== a.id" @click="deleteAccount(a)" class="text-red-600 hover:underline">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No bank accounts yet. Add one above.</div>
    </section>
  </div>
</template>
