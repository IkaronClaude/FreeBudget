<script setup lang="ts">
import { ref, watch } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { CategoryBreakdownItem, PeriodBreakdownItem } from '../api/types';

const me = useMeStore();

const selectedAccountId = ref<string>('');
const tab = ref<'category' | 'period'>('category');
const granularity = ref<'Day' | 'Week' | 'Month'>('Month');

const today = new Date();
const thirtyDaysAgo = new Date(today.getTime() - 30 * 24 * 60 * 60 * 1000);
const fromDate = ref(thirtyDaysAgo.toISOString().slice(0, 10));
const toDate = ref(today.toISOString().slice(0, 10));

const categoryItems = ref<CategoryBreakdownItem[]>([]);
const periodItems = ref<PeriodBreakdownItem[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

watch(
  () => me.bankAccounts,
  (accs) => {
    if (!selectedAccountId.value && accs.length) selectedAccountId.value = accs[0].id;
  },
  { immediate: true }
);

watch([selectedAccountId, tab, granularity, fromDate, toDate], () => loadReport());

async function loadReport() {
  if (!selectedAccountId.value) return;
  loading.value = true;
  error.value = null;
  try {
    const params = {
      bankAccountId: selectedAccountId.value,
      from: new Date(fromDate.value).toISOString(),
      to: new Date(toDate.value).toISOString()
    };
    if (tab.value === 'category') {
      const { data } = await api.get<CategoryBreakdownItem[]>('/reports/category-breakdown', { params });
      categoryItems.value = data;
    } else {
      const { data } = await api.get<PeriodBreakdownItem[]>('/reports/period-breakdown', {
        params: { ...params, granularity: granularity.value }
      });
      periodItems.value = data;
    }
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load report';
  } finally {
    loading.value = false;
  }
}

const fmt = (n: number) => n.toFixed(2);
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Reports</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
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
          <span class="text-slate-600 mb-1">From</span>
          <input v-model="fromDate" type="date" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">To</span>
          <input v-model="toDate" type="date" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <label v-if="tab === 'period'" class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Granularity</span>
          <select v-model="granularity" class="border border-slate-300 rounded px-3 py-2">
            <option value="Day">Day</option>
            <option value="Week">Week</option>
            <option value="Month">Month</option>
          </select>
        </label>
      </div>
    </section>

    <div class="flex gap-2 border-b border-slate-200">
      <button
        @click="tab = 'category'"
        :class="tab === 'category' ? 'border-b-2 border-blue-600 text-blue-600' : 'text-slate-600'"
        class="px-3 py-2 text-sm"
      >
        Category
      </button>
      <button
        @click="tab = 'period'"
        :class="tab === 'period' ? 'border-b-2 border-blue-600 text-blue-600' : 'text-slate-600'"
        class="px-3 py-2 text-sm"
      >
        Period
      </button>
    </div>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <p v-else-if="error" class="p-4 text-red-600">{{ error }}</p>

      <table v-else-if="tab === 'category' && categoryItems.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Category</th>
            <th class="text-right px-4 py-2">Credit</th>
            <th class="text-right px-4 py-2">Debit</th>
            <th class="text-right px-4 py-2">Net</th>
            <th class="text-right px-4 py-2"># txns</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in categoryItems" :key="c.category" class="border-t border-slate-100">
            <td class="px-4 py-2">{{ c.category }}</td>
            <td class="px-4 py-2 text-right tabular-nums text-green-700">{{ fmt(c.totalCredit) }}</td>
            <td class="px-4 py-2 text-right tabular-nums text-red-700">{{ fmt(c.totalDebit) }}</td>
            <td class="px-4 py-2 text-right tabular-nums">{{ fmt(c.net) }}</td>
            <td class="px-4 py-2 text-right tabular-nums">{{ c.transactionCount }}</td>
          </tr>
        </tbody>
      </table>

      <table v-else-if="tab === 'period' && periodItems.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Period</th>
            <th class="text-right px-4 py-2">Credit</th>
            <th class="text-right px-4 py-2">Debit</th>
            <th class="text-right px-4 py-2">Net</th>
            <th class="text-right px-4 py-2"># txns</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="p in periodItems" :key="p.periodLabel" class="border-t border-slate-100">
            <td class="px-4 py-2">{{ p.periodLabel }}</td>
            <td class="px-4 py-2 text-right tabular-nums text-green-700">{{ fmt(p.totalCredit) }}</td>
            <td class="px-4 py-2 text-right tabular-nums text-red-700">{{ fmt(p.totalDebit) }}</td>
            <td class="px-4 py-2 text-right tabular-nums">{{ fmt(p.net) }}</td>
            <td class="px-4 py-2 text-right tabular-nums">{{ p.transactionCount }}</td>
          </tr>
        </tbody>
      </table>

      <div v-else class="p-4 text-slate-500">No data for this range.</div>
    </section>
  </div>
</template>
