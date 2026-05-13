<script setup lang="ts">
import { ref, watch, computed } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { TransactionListItem, RuleMatchType } from '../api/types';
import ImportBuilder from '../components/ImportBuilder.vue';
import SplitDialog from '../components/SplitDialog.vue';

const me = useMeStore();

const selectedAccountId = ref<string>('');
const transactions = ref<TransactionListItem[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const selectedAccount = computed(() =>
  me.bankAccounts.find(a => a.id === selectedAccountId.value) ?? null
);

interface EditState {
  txnId: string;
  category: string;
  createRule: boolean;
  pattern: string;
  matchType: RuleMatchType;
  saving: boolean;
}
const editing = ref<EditState | null>(null);
const sharingTxnId = ref<string | null>(null);

const onlyUncategorized = ref(false);
const search = ref('');
const categoryFilter = ref<string>(''); // '' = all, '__uncategorized__' = no category, otherwise specific category
const directionFilter = ref<'all' | 'Credit' | 'Debit'>('all');
const showTransfers = ref(true);
const fromDate = ref<string>('');
const toDate = ref<string>('');

const distinctCategories = computed<string[]>(() => {
  const set = new Set<string>();
  for (const t of transactions.value) {
    if (t.category) set.add(t.category);
  }
  return [...set].sort((a, b) => a.localeCompare(b));
});

const visibleTransactions = computed(() => {
  const q = search.value.trim().toLowerCase();
  const fromMs = fromDate.value ? new Date(fromDate.value).getTime() : null;
  // toDate is inclusive end-of-day
  const toMs = toDate.value ? new Date(toDate.value).getTime() + 24 * 60 * 60 * 1000 - 1 : null;
  return transactions.value.filter(t => {
    if (onlyUncategorized.value && t.category) return false;
    if (!showTransfers.value && t.matchedTransactionId) return false;
    if (q && !t.description.toLowerCase().includes(q)) return false;
    if (categoryFilter.value === '__uncategorized__' && t.category) return false;
    if (categoryFilter.value && categoryFilter.value !== '__uncategorized__' && t.category !== categoryFilter.value) return false;
    if (directionFilter.value !== 'all' && t.direction !== directionFilter.value) return false;
    if (fromMs !== null || toMs !== null) {
      const txnMs = new Date(t.transactionDate).getTime();
      if (fromMs !== null && txnMs < fromMs) return false;
      if (toMs !== null && txnMs > toMs) return false;
    }
    return true;
  });
});

const visibleTotals = computed(() => {
  let credit = 0;
  let debit = 0;
  const currencies = new Set<string>();
  for (const t of visibleTransactions.value) {
    currencies.add(t.currencyCode);
    if (t.direction === 'Credit') credit += t.amount;
    else debit += t.amount;
  }
  return {
    credit,
    debit,
    net: credit - debit,
    currency: currencies.size === 1 ? [...currencies][0] : '',
  };
});

const uncategorizedCount = computed(() =>
  transactions.value.filter(t => !t.category).length
);

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
}, { immediate: true });

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

const matching = ref(false);
const matchMessage = ref<string | null>(null);
const matchScope = ref<string>(''); // '' = my accounts; otherwise a groupId

async function matchTransfers() {
  matching.value = true;
  matchMessage.value = null;
  error.value = null;
  try {
    const params: Record<string, string> = {};
    if (matchScope.value) params.groupId = matchScope.value;
    const { data } = await api.post<{ examined: number; matched: number; ambiguousSkipped: number }>(
      '/transactions/match-transfers', null, { params }
    );
    const scopeLabel = matchScope.value
      ? `within group "${me.groups.find(g => g.id === matchScope.value)?.name ?? '?'}"`
      : 'across your accounts';
    matchMessage.value = `Examined ${data.examined} ${scopeLabel}, paired ${data.matched}, skipped ${data.ambiguousSkipped} ambiguous.`;
    await loadTransactions();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Match failed');
  } finally {
    matching.value = false;
  }
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Transactions</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
      <label class="flex flex-col text-sm max-w-md">
        <span class="text-slate-600 mb-1">Bank account</span>
        <select v-model="selectedAccountId" class="border border-slate-300 rounded px-3 py-2">
          <option v-for="a in me.bankAccounts" :key="a.id" :value="a.id">
            {{ a.nickname }} ({{ a.bankType }})
          </option>
        </select>
      </label>
      <p v-if="error" class="text-red-600 text-sm mt-2">{{ error }}</p>
    </section>

    <ImportBuilder
      v-if="selectedAccount"
      :key="selectedAccount.id"
      :bank-account-id="selectedAccount.id"
      :bank-type="selectedAccount.bankType"
      @imported="loadTransactions"
    />

    <section class="bg-white rounded border border-slate-200 p-3 space-y-2 text-sm">
      <div class="flex flex-wrap gap-3 items-end">
        <label class="flex flex-col flex-1 min-w-[12rem]">
          <span class="text-slate-600 mb-1">Search description</span>
          <input v-model="search" type="text" placeholder="e.g. TESCO" class="border border-slate-300 rounded px-3 py-1" />
        </label>
        <label class="flex flex-col">
          <span class="text-slate-600 mb-1">Category</span>
          <select v-model="categoryFilter" class="border border-slate-300 rounded px-3 py-1 min-w-[10rem]">
            <option value="">All</option>
            <option value="__uncategorized__">— Uncategorized —</option>
            <option v-for="c in distinctCategories" :key="c" :value="c">{{ c }}</option>
          </select>
        </label>
        <label class="flex flex-col">
          <span class="text-slate-600 mb-1">Direction</span>
          <select v-model="directionFilter" class="border border-slate-300 rounded px-3 py-1">
            <option value="all">All</option>
            <option value="Debit">Debit</option>
            <option value="Credit">Credit</option>
          </select>
        </label>
        <label class="flex flex-col">
          <span class="text-slate-600 mb-1">From</span>
          <input v-model="fromDate" type="date" class="border border-slate-300 rounded px-3 py-1" />
        </label>
        <label class="flex flex-col">
          <span class="text-slate-600 mb-1">To</span>
          <input v-model="toDate" type="date" class="border border-slate-300 rounded px-3 py-1" />
        </label>
        <label class="flex items-center gap-2 pb-1">
          <input v-model="showTransfers" type="checkbox" />
          <span>Show transfers</span>
        </label>
        <label class="flex items-center gap-2 pb-1">
          <input v-model="onlyUncategorized" type="checkbox" />
          <span>Uncategorized only</span>
        </label>
      </div>
    </section>

    <div class="flex items-center justify-between gap-3 text-sm">
      <span class="text-slate-500">
        Showing {{ visibleTransactions.length }} of {{ transactions.length }} • {{ uncategorizedCount }} uncategorized
      </span>
      <div class="flex items-center gap-3">
        <select v-model="matchScope" class="border border-slate-300 rounded px-2 py-1 text-sm">
          <option value="">My accounts</option>
          <option v-for="g in me.groups" :key="g.id" :value="g.id">In group: {{ g.name }}</option>
        </select>
        <button
          @click="matchTransfers"
          :disabled="matching"
          class="border border-slate-300 px-3 py-1 rounded disabled:text-slate-400"
          title="Pair transactions that look like the same transfer"
        >{{ matching ? 'Matching...' : 'Match transfers' }}</button>
      </div>
    </div>
    <p v-if="matchMessage" class="text-green-700 text-sm">{{ matchMessage }}</p>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="visibleTransactions.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Date</th>
            <th class="text-left px-4 py-2">Description</th>
            <th class="text-left px-4 py-2">Category</th>
            <th class="text-right px-4 py-2">Amount</th>
            <th class="px-4 py-2 w-20"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="t in visibleTransactions" :key="t.id">
            <tr class="border-t border-slate-100">
              <td class="px-4 py-2 whitespace-nowrap">{{ new Date(t.transactionDate).toLocaleDateString() }}</td>
              <td class="px-4 py-2">
                {{ t.description }}
                <span v-if="t.matchedTransactionId" class="ml-2 text-xs px-1.5 py-0.5 rounded bg-amber-100 text-amber-800" title="Matched to a transaction on another account">↔ transfer</span>
              </td>
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
              <td class="px-4 py-2 text-right">
                <button
                  v-if="sharingTxnId !== t.id"
                  @click="sharingTxnId = t.id"
                  class="text-blue-600 hover:underline text-xs"
                >Share</button>
              </td>
            </tr>
            <tr v-if="sharingTxnId === t.id">
              <td colspan="5" class="p-0">
                <SplitDialog
                  :transaction="t"
                  @close="sharingTxnId = null"
                  @created="sharingTxnId = null"
                />
              </td>
            </tr>
            <tr v-if="editing?.txnId === t.id" class="bg-blue-50 border-t border-blue-200">
              <td colspan="5" class="px-4 py-3">
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
        <tfoot v-if="visibleTransactions.length" class="bg-slate-50 text-sm font-medium">
          <tr class="border-t-2 border-slate-200">
            <td class="px-4 py-2" colspan="2">Totals ({{ visibleTransactions.length }} txns)</td>
            <td class="px-4 py-2 text-right tabular-nums text-slate-600">
              <span class="text-green-700">+{{ visibleTotals.credit.toFixed(2) }}</span>
              <span class="text-slate-400 mx-1">/</span>
              <span class="text-red-700">−{{ visibleTotals.debit.toFixed(2) }}</span>
            </td>
            <td class="px-4 py-2 text-right tabular-nums" :class="visibleTotals.net >= 0 ? 'text-green-700' : 'text-red-700'">
              {{ visibleTotals.net >= 0 ? '+' : '' }}{{ visibleTotals.net.toFixed(2) }} {{ visibleTotals.currency || '(mixed)' }}
            </td>
            <td></td>
          </tr>
        </tfoot>
      </table>
      <div v-else class="p-4 text-slate-500">
        {{ onlyUncategorized && transactions.length ? 'Nothing uncategorized here.' : 'No transactions match the current filters.' }}
      </div>
    </section>
  </div>
</template>
