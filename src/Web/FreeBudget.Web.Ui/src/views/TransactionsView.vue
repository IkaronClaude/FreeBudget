<script setup lang="ts">
import { ref, watch, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { TransactionListItem, ImportCsvResponse, RuleMatchType } from '../api/types';

const me = useMeStore();

const selectedAccountId = ref<string>('');
const layout = ref<'barclays' | 'wise'>('barclays');
const file = ref<File | null>(null);
const transactions = ref<TransactionListItem[]>([]);
const loading = ref(false);
const importing = ref(false);
const message = ref<string | null>(null);
const error = ref<string | null>(null);

interface EditState {
  txnId: string;
  category: string;
  createRule: boolean;
  pattern: string;
  matchType: RuleMatchType;
  saving: boolean;
}
const editing = ref<EditState | null>(null);

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

function suggestPattern(description: string): string {
  const firstWord = description.trim().split(/\s+/)[0] ?? '';
  return firstWord.replace(/[^\w]+$/, '');
}

function startEdit(txn: TransactionListItem) {
  editing.value = {
    txnId: txn.id,
    category: txn.category ?? '',
    createRule: !txn.category,
    pattern: suggestPattern(txn.description),
    matchType: 'Contains',
    saving: false,
  };
}

function cancelEdit() {
  editing.value = null;
}

async function saveEdit() {
  if (!editing.value) return;
  const state = editing.value;
  state.saving = true;
  error.value = null;
  try {
    await api.patch(`/transactions/${state.txnId}/category`, {
      category: state.category.trim() || null,
    });

    if (state.createRule && state.pattern.trim() && state.category.trim()) {
      await api.post('/categorization-rules', {
        pattern: state.pattern.trim(),
        matchType: state.matchType,
        category: state.category.trim(),
        priority: 0,
      });
    }

    editing.value = null;
    await loadTransactions();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Save failed';
    state.saving = false;
  }
}

const matchTypes: RuleMatchType[] = ['Contains', 'Exact', 'StartsWith', 'EndsWith'];
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
          <template v-for="t in transactions" :key="t.id">
            <tr class="border-t border-slate-100">
              <td class="px-4 py-2 whitespace-nowrap">{{ new Date(t.transactionDate).toLocaleDateString() }}</td>
              <td class="px-4 py-2">{{ t.description }}</td>
              <td class="px-4 py-2">
                <button
                  v-if="editing?.txnId !== t.id"
                  @click="startEdit(t)"
                  class="text-left rounded px-2 py-1 -mx-2 -my-1 hover:bg-slate-100 w-full"
                  :class="t.category ? 'text-slate-800' : 'text-slate-400 italic'"
                >
                  {{ t.category ?? 'set category' }}
                </button>
              </td>
              <td class="px-4 py-2 text-right tabular-nums" :class="t.direction === 'Debit' ? 'text-red-700' : 'text-green-700'">
                {{ signedAmount(t) }} {{ t.currencyCode }}
              </td>
            </tr>
            <tr v-if="editing?.txnId === t.id" class="bg-blue-50 border-t border-blue-200">
              <td colspan="4" class="px-4 py-3">
                <div class="grid gap-3 md:grid-cols-[1fr_auto_auto] items-end">
                  <label class="flex flex-col text-sm">
                    <span class="text-slate-600 mb-1">Category</span>
                    <input
                      v-model="editing.category"
                      type="text"
                      class="border border-slate-300 rounded px-3 py-2"
                      placeholder="e.g. Groceries"
                      @keydown.enter="saveEdit"
                      @keydown.escape="cancelEdit"
                    />
                  </label>
                  <button
                    @click="saveEdit"
                    :disabled="editing.saving"
                    class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
                  >
                    {{ editing.saving ? 'Saving...' : 'Save' }}
                  </button>
                  <button @click="cancelEdit" class="px-4 py-2 border border-slate-300 rounded">
                    Cancel
                  </button>
                </div>
                <div class="mt-3 pt-3 border-t border-blue-200">
                  <label class="flex items-center gap-2 text-sm mb-2">
                    <input v-model="editing.createRule" type="checkbox" />
                    <span>Also create a rule for similar transactions</span>
                  </label>
                  <div v-if="editing.createRule" class="grid gap-3 md:grid-cols-[1fr_auto] items-end pl-6">
                    <label class="flex flex-col text-sm">
                      <span class="text-slate-600 mb-1">Pattern</span>
                      <input
                        v-model="editing.pattern"
                        type="text"
                        class="border border-slate-300 rounded px-3 py-2"
                        placeholder="TESCO"
                      />
                    </label>
                    <label class="flex flex-col text-sm">
                      <span class="text-slate-600 mb-1">Match</span>
                      <select v-model="editing.matchType" class="border border-slate-300 rounded px-3 py-2">
                        <option v-for="m in matchTypes" :key="m" :value="m">{{ m }}</option>
                      </select>
                    </label>
                  </div>
                </div>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No transactions for this account.</div>
    </section>
  </div>
</template>
