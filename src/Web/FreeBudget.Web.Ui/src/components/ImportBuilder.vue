<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import {
  type ImportLayout,
  blankLayout,
  getSavedLayout,
  getPresets,
  saveLayout,
  parseCsv,
} from '../api/importLayouts';
import type { ImportCsvResponse } from '../api/types';

const props = defineProps<{
  bankAccountId: string;
  bankType: string;
}>();

const emit = defineEmits<{ (e: 'imported'): void }>();

const me = useMeStore();
const file = ref<File | null>(null);
const rawText = ref<string>('');
const rawRows = computed(() => rawText.value ? parseCsv(rawText.value, layout.delimiter || ',') : []);
const headers = computed<string[]>(() => layout.hasHeaderRow && rawRows.value.length ? rawRows.value[0] : []);
const dataRows = computed<string[][]>(() => layout.hasHeaderRow ? rawRows.value.slice(1) : rawRows.value);
const previewRows = computed(() => dataRows.value.slice(0, 5));

const layout = reactive<ImportLayout>(blankLayout(props.bankAccountId));
const directionMappingRows = ref<Array<{ from: string; to: string }>>([]);

const message = ref<string | null>(null);
const error = ref<string | null>(null);
const importing = ref(false);
const savingLayout = ref(false);
const expanded = ref(false);

async function loadInitialLayout() {
  message.value = null;
  error.value = null;
  try {
    const saved = await getSavedLayout(props.bankAccountId);
    if (saved) {
      Object.assign(layout, saved);
      hydrateMappings();
      return;
    }
    if (props.bankType === 'Barclays' || props.bankType === 'Wise') {
      const presets = await getPresets();
      const preset = presets.find(p => p.name === props.bankType);
      if (preset) {
        Object.assign(layout, preset, { bankAccountId: props.bankAccountId });
        hydrateMappings();
        return;
      }
    }
    Object.assign(layout, blankLayout(props.bankAccountId));
    directionMappingRows.value = [];
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load layout';
  }
}

function hydrateMappings() {
  directionMappingRows.value = layout.directionMappings
    ? Object.entries(layout.directionMappings).map(([from, to]) => ({ from, to }))
    : [];
}

function syncMappings() {
  if (!directionMappingRows.value.length) {
    layout.directionMappings = null;
    return;
  }
  const dict: Record<string, string> = {};
  for (const row of directionMappingRows.value) {
    if (row.from.trim() && row.to.trim()) dict[row.from.trim()] = row.to.trim();
  }
  layout.directionMappings = Object.keys(dict).length ? dict : null;
}

watch(directionMappingRows, syncMappings, { deep: true });

watch(() => props.bankAccountId, async (id) => {
  if (!id) return;
  Object.assign(layout, blankLayout(id));
  await loadInitialLayout();
  file.value = null;
  rawText.value = '';
}, { immediate: true });

async function onFileChange(event: Event) {
  const target = event.target as HTMLInputElement;
  const f = target.files?.[0] ?? null;
  file.value = f;
  rawText.value = f ? await f.text() : '';
  expanded.value = !!f;
}

function getColumnValue(row: string[], col: string | null | undefined): string | null {
  if (!col) return null;
  const idx = headers.value.indexOf(col);
  if (idx < 0) return null;
  return row[idx] ?? null;
}

function previewRow(row: string[]) {
  const dateRaw = getColumnValue(row, layout.dateColumn) ?? '';
  const description = getColumnValue(row, layout.descriptionColumn) ?? '';
  const amountRaw = getColumnValue(row, layout.amountColumn) ?? '';
  const amount = parseFloat(amountRaw.replace(/,/g, ''));
  const currency = getColumnValue(row, layout.currencyColumn) ?? layout.defaultCurrencyCode;
  let direction = 'Debit';
  if (layout.directionColumn) {
    const raw = getColumnValue(row, layout.directionColumn) ?? '';
    direction = layout.directionMappings?.[raw] ?? raw;
  } else if (!isNaN(amount)) {
    direction = amount < 0 ? 'Debit' : 'Credit';
  }
  const category = getColumnValue(row, layout.categoryColumn);
  return {
    date: dateRaw,
    description: description.trim(),
    amount: isNaN(amount) ? amountRaw : Math.abs(amount).toFixed(2),
    currency,
    direction,
    category,
  };
}

async function persistLayout(): Promise<boolean> {
  if (!layout.dateColumn || !layout.descriptionColumn || !layout.amountColumn) {
    error.value = 'Date, Description, and Amount columns are required.';
    return false;
  }
  savingLayout.value = true;
  error.value = null;
  try {
    syncMappings();
    await saveLayout(props.bankAccountId, layout);
    return true;
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Save failed';
    return false;
  } finally {
    savingLayout.value = false;
  }
}

async function saveOnly() {
  const ok = await persistLayout();
  if (ok) message.value = 'Layout saved.';
}

async function saveAndImport() {
  if (!file.value) return;
  const ok = await persistLayout();
  if (!ok) return;

  importing.value = true;
  message.value = null;
  try {
    const form = new FormData();
    form.append('file', file.value);
    const params: Record<string, string> = {
      bankAccountId: props.bankAccountId,
      layout: 'saved',
    };
    // Only send the map if we actually need to route across multiple accounts
    if (needsRouting.value) {
      params.currencyMap = JSON.stringify(currencyToAccount);
    }
    const { data } = await api.post<ImportCsvResponse>('/transactions/import', form, {
      params,
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    message.value = `Imported ${data.transactionCount} transactions, skipped ${data.skippedDuplicates} duplicates.`;
    emit('imported');
    file.value = null;
    rawText.value = '';
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Import failed');
  } finally {
    importing.value = false;
  }
}

function addMappingRow() {
  directionMappingRows.value.push({ from: '', to: 'Debit' });
}

function removeMappingRow(index: number) {
  directionMappingRows.value.splice(index, 1);
}

// Multi-currency routing: detect distinct currencies (source + target) in the CSV
// and let the user route each to a specific bank account.
const detectedCurrencies = computed<string[]>(() => {
  const set = new Set<string>();
  for (const row of dataRows.value) {
    const c1 = getColumnValue(row, layout.currencyColumn);
    if (c1) set.add(c1.trim().toUpperCase());
    const c2 = getColumnValue(row, layout.targetCurrencyColumn);
    if (c2) set.add(c2.trim().toUpperCase());
  }
  return [...set];
});

const currencyToAccount = reactive<Record<string, string>>({});

watch(detectedCurrencies, (currencies) => {
  // Default each currency to the current account; user can change.
  for (const c of currencies) {
    if (!currencyToAccount[c]) currencyToAccount[c] = props.bankAccountId;
  }
});

const needsRouting = computed(() => detectedCurrencies.value.length > 1);
</script>

<template>
  <section class="bg-white rounded border border-slate-200 p-4 space-y-4">
    <div class="flex flex-wrap items-end gap-3">
      <label class="flex flex-col text-sm">
        <span class="text-slate-600 mb-1">CSV file</span>
        <input type="file" accept=".csv,.txt" @change="onFileChange" class="text-sm" />
      </label>
      <span v-if="!file" class="text-xs text-slate-500 self-center">
        Pick a file to preview and configure the import.
      </span>
    </div>

    <div v-if="file" class="space-y-4">
      <div class="grid gap-4 lg:grid-cols-2">
        <div>
          <h3 class="text-sm font-medium text-slate-700 mb-2">Raw rows (first 5)</h3>
          <div class="border border-slate-200 rounded overflow-auto max-h-64">
            <table class="text-xs">
              <thead v-if="layout.hasHeaderRow" class="bg-slate-50">
                <tr>
                  <th v-for="(h, i) in headers" :key="i" class="text-left px-2 py-1 font-medium whitespace-nowrap">{{ h }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(row, ri) in previewRows" :key="ri" class="border-t border-slate-100">
                  <td v-for="(cell, ci) in row" :key="ci" class="px-2 py-1 whitespace-nowrap">{{ cell }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
        <div>
          <h3 class="text-sm font-medium text-slate-700 mb-2">Will be imported as</h3>
          <div class="border border-slate-200 rounded overflow-auto max-h-64">
            <table class="text-xs w-full">
              <thead class="bg-slate-50">
                <tr>
                  <th class="text-left px-2 py-1">Date</th>
                  <th class="text-left px-2 py-1">Description</th>
                  <th class="text-right px-2 py-1">Amount</th>
                  <th class="text-left px-2 py-1">Direction</th>
                  <th class="text-left px-2 py-1">Category</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(row, i) in previewRows" :key="i" class="border-t border-slate-100">
                  <template v-for="(c, j) in [previewRow(row)]" :key="j">
                    <td class="px-2 py-1 whitespace-nowrap">{{ c.date }}</td>
                    <td class="px-2 py-1">{{ c.description }}</td>
                    <td class="px-2 py-1 text-right tabular-nums" :class="c.direction === 'Debit' ? 'text-red-700' : 'text-green-700'">
                      {{ c.amount }} {{ c.currency }}
                    </td>
                    <td class="px-2 py-1">{{ c.direction }}</td>
                    <td class="px-2 py-1 text-slate-600">{{ c.category ?? '—' }}</td>
                  </template>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <details :open="expanded" class="rounded border border-slate-200">
        <summary class="px-4 py-2 bg-slate-50 cursor-pointer text-sm font-medium">Column mapping</summary>
        <div class="p-4 grid gap-3 md:grid-cols-3">
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Date column *</span>
            <select v-model="layout.dateColumn" class="border border-slate-300 rounded px-2 py-1">
              <option value="">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Description column *</span>
            <select v-model="layout.descriptionColumn" class="border border-slate-300 rounded px-2 py-1">
              <option value="">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Amount column *</span>
            <select v-model="layout.amountColumn" class="border border-slate-300 rounded px-2 py-1">
              <option value="">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Direction column (optional)</span>
            <select v-model="layout.directionColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Currency column (optional)</span>
            <select v-model="layout.currencyColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Category column (optional)</span>
            <select v-model="layout.categoryColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Target amount column (for FX conversions)</span>
            <select v-model="layout.targetAmountColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Target currency column (for FX conversions)</span>
            <select v-model="layout.targetCurrencyColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">External ID column (optional)</span>
            <select v-model="layout.externalIdColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Running balance column (optional)</span>
            <select v-model="layout.runningBalanceColumn" class="border border-slate-300 rounded px-2 py-1">
              <option :value="null">—</option>
              <option v-for="h in headers" :key="h" :value="h">{{ h }}</option>
            </select>
          </label>
        </div>
        <div class="px-4 pb-4 grid gap-3 md:grid-cols-4">
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Date format</span>
            <input v-model="layout.dateFormat" type="text" class="border border-slate-300 rounded px-2 py-1" placeholder="dd/MM/yyyy" />
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Delimiter</span>
            <input v-model="layout.delimiter" type="text" maxlength="1" class="border border-slate-300 rounded px-2 py-1" />
          </label>
          <label class="flex flex-col text-sm">
            <span class="text-slate-600 mb-1">Default currency</span>
            <input v-model="layout.defaultCurrencyCode" type="text" maxlength="3" class="border border-slate-300 rounded px-2 py-1 uppercase" />
          </label>
          <label class="flex items-center text-sm gap-2 mt-6">
            <input v-model="layout.hasHeaderRow" type="checkbox" />
            <span>CSV has header row</span>
          </label>
        </div>
        <div v-if="layout.directionColumn" class="px-4 pb-4 border-t border-slate-200 pt-3">
          <div class="text-sm font-medium mb-2">Direction value mapping</div>
          <p class="text-xs text-slate-500 mb-2">Map raw values from the direction column to "Credit" or "Debit".</p>
          <div class="space-y-2">
            <div v-for="(m, i) in directionMappingRows" :key="i" class="flex gap-2 items-center text-sm">
              <input v-model="m.from" type="text" placeholder="IN" class="border border-slate-300 rounded px-2 py-1 flex-1 max-w-xs" />
              <span>→</span>
              <select v-model="m.to" class="border border-slate-300 rounded px-2 py-1">
                <option value="Credit">Credit</option>
                <option value="Debit">Debit</option>
              </select>
              <button @click="removeMappingRow(i)" class="text-red-600 hover:underline text-xs">Remove</button>
            </div>
            <button @click="addMappingRow" class="text-blue-600 hover:underline text-sm">+ Add mapping</button>
          </div>
        </div>
      </details>

      <details v-if="detectedCurrencies.length > 1" open class="rounded border border-amber-200 bg-amber-50">
        <summary class="px-4 py-2 cursor-pointer text-sm font-medium text-amber-900">
          Multiple currencies detected — route each to a bank account
        </summary>
        <div class="p-4 space-y-2 text-sm">
          <p class="text-xs text-amber-800 mb-2">
            Each row's currency is sent to the matching account. NEUTRAL/FX-conversion rows produce two transactions
            (a debit on the source-currency account and a credit on the target-currency account).
          </p>
          <div v-for="c in detectedCurrencies" :key="c" class="flex items-center gap-2">
            <span class="font-mono w-12">{{ c }}</span>
            <span>→</span>
            <select v-model="currencyToAccount[c]" class="border border-slate-300 rounded px-2 py-1 flex-1 max-w-md">
              <option v-for="a in me.bankAccounts" :key="a.id" :value="a.id">
                {{ a.nickname }} ({{ a.bankType }})
              </option>
            </select>
          </div>
        </div>
      </details>

      <div class="flex flex-wrap items-center gap-3">
        <button
          @click="saveAndImport"
          :disabled="importing || savingLayout || !file"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >
          {{ importing ? 'Importing...' : 'Save & Import' }}
        </button>
        <button
          @click="saveOnly"
          :disabled="savingLayout || importing"
          class="border border-slate-300 px-4 py-2 rounded"
        >
          {{ savingLayout ? 'Saving...' : 'Save layout only' }}
        </button>
      </div>
    </div>

    <p v-if="message" class="text-green-700 text-sm">{{ message }}</p>
    <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
  </section>
</template>
