<script setup lang="ts">
import { ref, watch } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { TransactionListItem, ImportCsvResponse } from '../api/types';

const me = useMeStore();

const selectedAccountId = ref<string>('');
const layout = ref<'barclays' | 'wise'>('barclays');
const file = ref<File | null>(null);
const transactions = ref<TransactionListItem[]>([]);
const loading = ref(false);
const importing = ref(false);
const message = ref<string | null>(null);
const error = ref<string | null>(null);

watch(
  () => me.bankAccounts,
  (accs) => {
    if (!selectedAccountId.value && accs.length) selectedAccountId.value = accs[0].id;
  },
  { immediate: true }
);

watch(selectedAccountId, async (id) => {
  if (!id) return;
  await loadTransactions();
});

async function loadTransactions() {
  loading.value = true;
  error.value = null;
  try {
    const { data } = await api.get<TransactionListItem[]>('/transactions', {
      params: { bankAccountId: selectedAccountId.value }
    });
    transactions.value = data;
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load transactions';
  } finally {
    loading.value = false;
  }
}

function onFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  file.value = target.files?.[0] ?? null;
}

async function importCsv() {
  if (!file.value || !selectedAccountId.value) return;
  importing.value = true;
  message.value = null;
  error.value = null;
  try {
    const form = new FormData();
    form.append('file', file.value);
    const { data } = await api.post<ImportCsvResponse>('/transactions/import', form, {
      params: { bankAccountId: selectedAccountId.value, layout: layout.value },
      headers: { 'Content-Type': 'multipart/form-data' }
    });
    message.value = `Imported ${data.transactionCount} transactions, skipped ${data.skippedDuplicates} duplicates.`;
    await loadTransactions();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Import failed';
  } finally {
    importing.value = false;
  }
}

const signedAmount = (t: TransactionListItem) =>
  (t.direction === 'Debit' ? -t.amount : t.amount).toFixed(2);
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Transactions</h2>

    <section class="bg-white rounded border border-slate-200 p-4 space-y-4">
      <div class="flex flex-wrap gap-3 items-end">
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Bank account</span>
          <select v-model="selectedAccountId" class="border border-slate-300 rounded px-3 py-2 min-w-[14rem]">
            <option v-for="a in me.bankAccounts" :key="a.id" :value="a.id">
              {{ a.nickname }} ({{ a.bankType }})
            </option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">CSV layout</span>
          <select v-model="layout" class="border border-slate-300 rounded px-3 py-2">
            <option value="barclays">Barclays</option>
            <option value="wise">Wise</option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">CSV file</span>
          <input type="file" accept=".csv" @change="onFileChange" class="text-sm" />
        </label>
        <button
          @click="importCsv"
          :disabled="!file || !selectedAccountId || importing"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >
          {{ importing ? 'Importing...' : 'Import CSV' }}
        </button>
      </div>
      <p v-if="message" class="text-green-700 text-sm">{{ message }}</p>
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="transactions.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Date</th>
            <th class="text-left px-4 py-2">Description</th>
            <th class="text-left px-4 py-2">Category</th>
            <th class="text-right px-4 py-2">Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in transactions" :key="t.id" class="border-t border-slate-100">
            <td class="px-4 py-2 whitespace-nowrap">{{ new Date(t.transactionDate).toLocaleDateString() }}</td>
            <td class="px-4 py-2">{{ t.description }}</td>
            <td class="px-4 py-2 text-slate-600">{{ t.category ?? '—' }}</td>
            <td class="px-4 py-2 text-right tabular-nums" :class="t.direction === 'Debit' ? 'text-red-700' : 'text-green-700'">
              {{ signedAmount(t) }} {{ t.currencyCode }}
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No transactions for this account.</div>
    </section>
  </div>
</template>
