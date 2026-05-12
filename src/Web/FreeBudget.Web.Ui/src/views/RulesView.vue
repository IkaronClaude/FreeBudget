<script setup lang="ts">
import { ref, onMounted, reactive } from 'vue';
import { api } from '../api/client';
import type { CategorizationRule, RuleMatchType } from '../api/types';

const rules = ref<CategorizationRule[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

interface RuleForm {
  pattern: string;
  matchType: RuleMatchType;
  category: string;
  priority: number;
}

const form = reactive<RuleForm>({
  pattern: '',
  matchType: 'Contains',
  category: '',
  priority: 0,
});

const editingId = ref<string | null>(null);
const matchTypes: RuleMatchType[] = ['Contains', 'Exact', 'StartsWith', 'EndsWith'];

async function load() {
  loading.value = true;
  error.value = null;
  try {
    const { data } = await api.get<CategorizationRule[]>('/categorization-rules');
    rules.value = data.sort((a, b) => b.priority - a.priority);
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load rules';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  editingId.value = null;
  form.pattern = '';
  form.matchType = 'Contains';
  form.category = '';
  form.priority = 0;
}

function editRule(rule: CategorizationRule) {
  editingId.value = rule.id;
  form.pattern = rule.pattern;
  form.matchType = rule.matchType;
  form.category = rule.category;
  form.priority = rule.priority;
}

async function saveRule() {
  if (!form.pattern.trim() || !form.category.trim()) return;
  try {
    if (editingId.value) {
      await api.put(`/categorization-rules/${editingId.value}`, form);
    } else {
      await api.post('/categorization-rules', form);
    }
    resetForm();
    await load();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Save failed';
  }
}

async function deleteRule(id: string) {
  if (!confirm('Delete this rule?')) return;
  try {
    await api.delete(`/categorization-rules/${id}`);
    await load();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Delete failed';
  }
}

const applying = ref(false);
const applyMessage = ref<string | null>(null);

async function applyToExisting() {
  applying.value = true;
  applyMessage.value = null;
  error.value = null;
  try {
    const { data } = await api.post<{ examined: number; updated: number }>('/categorization-rules/apply');
    applyMessage.value = `Examined ${data.examined} uncategorized transactions, categorized ${data.updated}.`;
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Apply failed';
  } finally {
    applying.value = false;
  }
}

onMounted(load);
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h2 class="text-2xl font-semibold">Categorization rules</h2>
        <p class="text-sm text-slate-600 mt-1">
          Rules run during CSV import (highest priority first) and set a category on any transaction whose description matches the pattern.
        </p>
      </div>
      <button
        @click="applyToExisting"
        :disabled="applying || !rules.length"
        class="bg-slate-800 text-white px-4 py-2 rounded disabled:bg-slate-300 whitespace-nowrap"
        title="Run all rules against existing uncategorized transactions"
      >
        {{ applying ? 'Applying...' : 'Apply to uncategorized' }}
      </button>
    </div>
    <p v-if="applyMessage" class="text-green-700 text-sm">{{ applyMessage }}</p>

    <section class="bg-white rounded border border-slate-200 p-4">
      <h3 class="font-medium mb-3">{{ editingId ? 'Edit rule' : 'New rule' }}</h3>
      <div class="grid gap-3 md:grid-cols-5">
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Pattern</span>
          <input v-model="form.pattern" type="text" placeholder="TESCO" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Match type</span>
          <select v-model="form.matchType" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="m in matchTypes" :key="m" :value="m">{{ m }}</option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Category</span>
          <input v-model="form.category" type="text" placeholder="Groceries" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Priority</span>
          <input v-model.number="form.priority" type="number" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <div class="flex gap-2 items-end">
          <button @click="saveRule" :disabled="!form.pattern || !form.category"
                  class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300">
            {{ editingId ? 'Save' : 'Add' }}
          </button>
          <button v-if="editingId" @click="resetForm" class="px-4 py-2 border border-slate-300 rounded">
            Cancel
          </button>
        </div>
      </div>
      <p v-if="error" class="mt-3 text-red-600 text-sm">{{ error }}</p>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="rules.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-right px-4 py-2 w-20">Priority</th>
            <th class="text-left px-4 py-2">Pattern</th>
            <th class="text-left px-4 py-2">Match</th>
            <th class="text-left px-4 py-2">Category</th>
            <th class="text-right px-4 py-2 w-32"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rules" :key="r.id" class="border-t border-slate-100">
            <td class="px-4 py-2 text-right tabular-nums">{{ r.priority }}</td>
            <td class="px-4 py-2 font-mono">{{ r.pattern }}</td>
            <td class="px-4 py-2 text-slate-600">{{ r.matchType }}</td>
            <td class="px-4 py-2">{{ r.category }}</td>
            <td class="px-4 py-2 text-right space-x-2">
              <button @click="editRule(r)" class="text-blue-600 hover:underline">Edit</button>
              <button @click="deleteRule(r.id)" class="text-red-600 hover:underline">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No rules yet. Add one above.</div>
    </section>
  </div>
</template>
