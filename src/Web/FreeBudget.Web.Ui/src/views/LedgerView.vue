<script setup lang="ts">
import { ref, watch, computed, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { MemberBalance, LedgerEntry, GroupMember, TransactionListItem } from '../api/types';

const me = useMeStore();

const selectedGroupId = ref<string>('');
const balances = ref<MemberBalance[]>([]);
const entries = ref<LedgerEntry[]>([]);
const sharedTransactions = ref<TransactionListItem[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

const selectedGroup = computed(() =>
  me.groups.find(g => g.id === selectedGroupId.value) ?? null
);

const membersById = computed<Record<string, GroupMember>>(() => {
  const map: Record<string, GroupMember> = {};
  if (!selectedGroup.value) return map;
  for (const m of selectedGroup.value.members) map[m.id] = m;
  return map;
});

function memberLabel(id: string): string {
  const m = membersById.value[id];
  if (!m) return id.slice(0, 8);
  if (m.owningUserId === me.user?.id) return `${m.label} (you)`;
  return m.label;
}

watch(
  () => me.groups,
  (groups) => {
    if (!selectedGroupId.value && groups.length) selectedGroupId.value = groups[0].id;
  },
  { immediate: true }
);

watch(selectedGroupId, async () => { await load(); }, { immediate: true });

async function load() {
  if (!selectedGroupId.value) return;
  loading.value = true;
  error.value = null;
  try {
    const [b, e, t] = await Promise.all([
      api.get<MemberBalance[]>('/ledger/balances', { params: { groupId: selectedGroupId.value } }),
      api.get<LedgerEntry[]>('/ledger/entries', { params: { groupId: selectedGroupId.value } }),
      api.get<TransactionListItem[]>(`/groups/${selectedGroupId.value}/transactions`),
    ]);
    balances.value = b.data;
    entries.value = e.data;
    sharedTransactions.value = t.data;
  } catch (err: unknown) {
    error.value = err instanceof Error ? err.message : 'Failed to load ledger';
  } finally {
    loading.value = false;
  }
}

async function deleteEntry(entry: LedgerEntry) {
  if (!confirm('Delete this ledger entry?')) return;
  try {
    await api.delete(`/ledger/${entry.id}`);
    await load();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Delete failed';
  }
}

interface SettleForm {
  debtorMemberId: string;
  creditorMemberId: string;
  amount: number;
  description: string;
  saving: boolean;
}
const settling = ref<SettleForm | null>(null);

function startSettle(balance: MemberBalance) {
  settling.value = {
    debtorMemberId: balance.memberId,
    creditorMemberId: balance.owesToMemberId,
    amount: balance.amount,
    description: `Settlement: ${memberLabel(balance.memberId)} → ${memberLabel(balance.owesToMemberId)}`,
    saving: false,
  };
}

async function saveSettlement() {
  if (!settling.value || !selectedGroup.value) return;
  const s = settling.value;
  if (s.amount <= 0) {
    error.value = 'Amount must be positive.';
    return;
  }
  s.saving = true;
  error.value = null;
  try {
    await api.post('/ledger/settlements', {
      groupId: selectedGroup.value.id,
      paidByMemberId: s.debtorMemberId,
      owedByMemberId: s.creditorMemberId,
      amount: s.amount,
      currencyCode: balances.value[0]?.currencyCode ?? 'GBP',
      description: s.description.trim() || 'Settlement',
      entryDate: new Date().toISOString(),
      transactionId: null,
    });
    settling.value = null;
    await load();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Settle failed');
    s.saving = false;
  }
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Ledger</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
      <label class="flex flex-col text-sm max-w-md">
        <span class="text-slate-600 mb-1">Group</span>
        <select v-model="selectedGroupId" class="border border-slate-300 rounded px-3 py-2">
          <option v-for="g in me.groups" :key="g.id" :value="g.id">{{ g.name }}</option>
        </select>
      </label>
      <p v-if="error" class="text-red-600 text-sm mt-2">{{ error }}</p>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <header class="px-4 py-3 border-b border-slate-200 font-medium">Balances</header>
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="balances.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Member</th>
            <th class="text-left px-4 py-2">Owes</th>
            <th class="text-right px-4 py-2">Amount</th>
            <th class="px-4 py-2 w-24"></th>
          </tr>
        </thead>
        <tbody>
          <template v-for="b in balances" :key="b.memberId + b.owesToMemberId">
            <tr class="border-t border-slate-100">
              <td class="px-4 py-2">{{ memberLabel(b.memberId) }}</td>
              <td class="px-4 py-2">{{ memberLabel(b.owesToMemberId) }}</td>
              <td class="px-4 py-2 text-right tabular-nums">{{ b.amount.toFixed(2) }} {{ b.currencyCode }}</td>
              <td class="px-4 py-2 text-right">
                <button
                  v-if="settling?.debtorMemberId !== b.memberId || settling?.creditorMemberId !== b.owesToMemberId"
                  @click="startSettle(b)"
                  class="text-blue-600 hover:underline text-xs"
                >Settle</button>
              </td>
            </tr>
            <tr v-if="settling && settling.debtorMemberId === b.memberId && settling.creditorMemberId === b.owesToMemberId"
                class="bg-emerald-50 border-t border-emerald-200">
              <td colspan="4" class="px-4 py-3">
                <div class="grid gap-3 md:grid-cols-[10rem_1fr_auto_auto] items-end">
                  <label class="flex flex-col text-sm">
                    <span class="text-slate-600 mb-1">Amount</span>
                    <input
                      v-model.number="settling.amount"
                      type="number"
                      step="0.01"
                      min="0"
                      :max="b.amount"
                      class="border border-slate-300 rounded px-3 py-1 text-right tabular-nums"
                    />
                  </label>
                  <label class="flex flex-col text-sm">
                    <span class="text-slate-600 mb-1">Description</span>
                    <input
                      v-model="settling.description"
                      type="text"
                      class="border border-slate-300 rounded px-3 py-1"
                    />
                  </label>
                  <button
                    @click="saveSettlement"
                    :disabled="settling.saving"
                    class="bg-emerald-600 text-white px-3 py-1 rounded disabled:bg-slate-300 text-sm"
                  >{{ settling.saving ? 'Saving...' : 'Record settlement' }}</button>
                  <button @click="settling = null" class="px-3 py-1 border border-slate-300 rounded text-sm">Cancel</button>
                </div>
                <p class="text-xs text-slate-600 mt-2">
                  Records that {{ memberLabel(b.memberId) }} paid {{ memberLabel(b.owesToMemberId) }} {{ settling.amount.toFixed(2) }} {{ b.currencyCode }}.
                </p>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No outstanding balances.</div>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <header class="px-4 py-3 border-b border-slate-200 font-medium">Entries</header>
      <table v-if="entries.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Date</th>
            <th class="text-left px-4 py-2">Description</th>
            <th class="text-left px-4 py-2">Type</th>
            <th class="text-left px-4 py-2">Paid by</th>
            <th class="text-left px-4 py-2">Owed by</th>
            <th class="text-right px-4 py-2">Amount</th>
            <th class="px-4 py-2 w-16"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="e in entries" :key="e.id" class="border-t border-slate-100">
            <td class="px-4 py-2 whitespace-nowrap">{{ new Date(e.entryDate).toLocaleDateString() }}</td>
            <td class="px-4 py-2">{{ e.description }}</td>
            <td class="px-4 py-2 text-slate-600">{{ e.entryType }}</td>
            <td class="px-4 py-2">{{ memberLabel(e.paidByMemberId) }}</td>
            <td class="px-4 py-2">{{ memberLabel(e.owedByMemberId) }}</td>
            <td class="px-4 py-2 text-right tabular-nums">{{ e.amount.toFixed(2) }} {{ e.currencyCode }}</td>
            <td class="px-4 py-2 text-right">
              <button @click="deleteEntry(e)" class="text-red-600 hover:underline text-xs">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No ledger entries yet.</div>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <header class="px-4 py-3 border-b border-slate-200 font-medium">
        Shared transactions
        <span class="text-xs text-slate-500 font-normal ml-2">
          Underlying transactions any member has shared into this group
        </span>
      </header>
      <table v-if="sharedTransactions.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Date</th>
            <th class="text-left px-4 py-2">Description</th>
            <th class="text-left px-4 py-2">Category</th>
            <th class="text-right px-4 py-2">Amount</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in sharedTransactions" :key="t.id" class="border-t border-slate-100">
            <td class="px-4 py-2 whitespace-nowrap">{{ new Date(t.transactionDate).toLocaleDateString() }}</td>
            <td class="px-4 py-2">
              {{ t.description }}
              <span v-if="t.matchedTransactionId" class="ml-2 text-xs px-1.5 py-0.5 rounded bg-amber-100 text-amber-800">↔ transfer</span>
            </td>
            <td class="px-4 py-2 text-slate-600">{{ t.category ?? '—' }}</td>
            <td class="px-4 py-2 text-right tabular-nums" :class="t.direction === 'Debit' ? 'text-red-700' : 'text-green-700'">
              {{ (t.direction === 'Debit' ? -t.amount : t.amount).toFixed(2) }} {{ t.currencyCode }}
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No transactions shared into this group yet.</div>
    </section>
  </div>
</template>
