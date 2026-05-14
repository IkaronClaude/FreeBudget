<script setup lang="ts">
import { ref, reactive, onMounted, computed, watch } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { SharingRule, RuleMatchType, LedgerEntryKind, Group, GroupMember } from '../api/types';

const me = useMeStore();
const rules = ref<SharingRule[]>([]);
const loading = ref(false);
const error = ref<string | null>(null);

interface RuleForm {
  pattern: string;
  matchType: RuleMatchType;
  entryType: LedgerEntryKind;
  priority: number;
  groupId: string;
  paidByMemberId: string;
  participantMemberIds: string[];
  settlementRecipientId: string;
}

const form = reactive<RuleForm>({
  pattern: '',
  matchType: 'Contains',
  entryType: 'Expense',
  priority: 0,
  groupId: '',
  paidByMemberId: '',
  participantMemberIds: [],
  settlementRecipientId: '',
});
const editingId = ref<string | null>(null);
const matchTypes: RuleMatchType[] = ['Contains', 'Exact', 'StartsWith', 'EndsWith', 'Any'];

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

const formGroup = computed<Group | null>(() =>
  me.groups.find(g => g.id === form.groupId) ?? null
);

function memberLabel(m: GroupMember): string {
  if (m.owningUserId === me.user?.id) return `${m.label} (you)`;
  return m.label;
}

function findMember(groupId: string, memberId: string): GroupMember | null {
  const g = me.groups.find(gr => gr.id === groupId);
  return g?.members.find(m => m.id === memberId) ?? null;
}

watch(() => form.groupId, () => {
  if (!formGroup.value) return;
  const meMember = formGroup.value.members.find(m => m.owningUserId === me.user?.id);
  form.paidByMemberId = meMember?.id ?? formGroup.value.members[0]?.id ?? '';
  form.participantMemberIds = formGroup.value.members.map(m => m.id);
  form.settlementRecipientId = formGroup.value.members.find(m => m.id !== form.paidByMemberId)?.id ?? '';
});

// When the user flips an Exclude rule back to Expense/Settlement, repopulate
// payer + participants from the current group so the form is valid again.
watch(() => form.entryType, (next, prev) => {
  if (prev === 'Exclude' && next !== 'Exclude' && formGroup.value) {
    const meMember = formGroup.value.members.find(m => m.owningUserId === me.user?.id);
    form.paidByMemberId = meMember?.id ?? formGroup.value.members[0]?.id ?? '';
    form.participantMemberIds = formGroup.value.members.map(m => m.id);
    form.settlementRecipientId = formGroup.value.members.find(m => m.id !== form.paidByMemberId)?.id ?? '';
  }
});

async function load() {
  loading.value = true;
  error.value = null;
  try {
    const { data } = await api.get<SharingRule[]>('/sharing-rules');
    rules.value = data;
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Failed to load';
  } finally {
    loading.value = false;
  }
}

function resetForm() {
  editingId.value = null;
  form.pattern = '';
  form.matchType = 'Contains';
  form.entryType = 'Expense';
  form.priority = 0;
  form.groupId = me.groups[0]?.id ?? '';
}

function editRule(rule: SharingRule) {
  editingId.value = rule.id;
  form.pattern = rule.pattern;
  form.matchType = rule.matchType;
  form.entryType = rule.entryType;
  form.priority = rule.priority;
  form.groupId = rule.groupId;
  form.paidByMemberId = rule.paidByMemberId;
  if (rule.entryType === 'Settlement') {
    form.settlementRecipientId = rule.participantMemberIds[0] ?? '';
    form.participantMemberIds = [...rule.participantMemberIds];
  } else {
    form.participantMemberIds = [...rule.participantMemberIds];
  }
}

function effectiveParticipants(): string[] {
  if (form.entryType === 'Exclude') return [];
  return form.entryType === 'Settlement'
    ? (form.settlementRecipientId ? [form.settlementRecipientId] : [])
    : form.participantMemberIds;
}

function effectivePaidByMemberId(): string {
  return form.entryType === 'Exclude' ? EMPTY_GUID : form.paidByMemberId;
}

const canSave = computed(() => {
  if (!form.groupId) return false;
  if (form.matchType !== 'Any' && !form.pattern.trim()) return false;
  if (form.entryType === 'Exclude') return true;
  if (!form.paidByMemberId) return false;
  const p = effectiveParticipants();
  if (!p.length) return false;
  if (form.entryType === 'Settlement' && p[0] === form.paidByMemberId) return false;
  return true;
});

async function saveRule() {
  if (!canSave.value) return;
  error.value = null;
  try {
    const payload = {
      pattern: form.matchType === 'Any' ? '' : form.pattern.trim(),
      matchType: form.matchType,
      entryType: form.entryType,
      priority: form.priority,
      groupId: form.groupId,
      paidByMemberId: effectivePaidByMemberId(),
      participantMemberIds: effectiveParticipants(),
    };
    if (editingId.value) {
      await api.put(`/sharing-rules/${editingId.value}`, payload);
    } else {
      await api.post('/sharing-rules', payload);
    }
    resetForm();
    await load();
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Save failed');
  }
}

async function deleteRule(id: string) {
  if (!confirm('Delete this sharing rule?')) return;
  try {
    await api.delete(`/sharing-rules/${id}`);
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
    const { data } = await api.post<{ examined: number; matched: number; split: number; skipped: number; excluded: number; transfersPaired: number }>(
      '/sharing-rules/apply'
    );
    applyMessage.value = `Examined ${data.examined}, matched ${data.matched}, applied ${data.split}, excluded ${data.excluded}, skipped ${data.skipped}. Transfer pairs found: ${data.transfersPaired}.`;
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Apply failed');
  } finally {
    applying.value = false;
  }
}

function summary(rule: SharingRule): string {
  const g = me.groups.find(gr => gr.id === rule.groupId);
  const groupName = g?.name ?? '?';
  if (rule.entryType === 'Exclude') {
    return `Exclude: don't auto-share with ${groupName}`;
  }
  const payer = findMember(rule.groupId, rule.paidByMemberId);
  const payerName = payer ? memberLabel(payer) : '?';
  const others = rule.participantMemberIds
    .map(id => findMember(rule.groupId, id))
    .filter((m): m is GroupMember => !!m)
    .map(m => memberLabel(m));
  if (rule.entryType === 'Settlement') {
    return `Settle: ${payerName} → ${others[0] ?? '?'} (clears debt in ${groupName})`;
  }
  return `Split: ${groupName} • paid by ${payerName} • between ${others.join(', ')}`;
}

function toggleParticipant(memberId: string) {
  const i = form.participantMemberIds.indexOf(memberId);
  if (i >= 0) form.participantMemberIds.splice(i, 1);
  else form.participantMemberIds.push(memberId);
}

onMounted(async () => {
  await load();
  if (!form.groupId && me.groups.length) form.groupId = me.groups[0].id;
});

watch(() => me.groups, () => {
  if (!form.groupId && me.groups.length) form.groupId = me.groups[0].id;
});
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-start justify-between gap-4">
      <div>
        <h2 class="text-2xl font-semibold">Sharing rules</h2>
        <p class="text-sm text-slate-600 mt-1">
          When a transaction matches the pattern, it's automatically turned into either an expense split or a debt settlement
          (highest priority wins). Rules only fire on the "Apply" button below.
        </p>
      </div>
      <button
        @click="applyToExisting"
        :disabled="applying || !rules.length"
        class="bg-slate-800 text-white px-4 py-2 rounded disabled:bg-slate-300 whitespace-nowrap"
      >{{ applying ? 'Applying...' : 'Apply to existing' }}</button>
    </div>
    <p v-if="applyMessage" class="text-green-700 text-sm">{{ applyMessage }}</p>

    <section class="bg-white rounded border border-slate-200 p-4 space-y-3">
      <h3 class="font-medium">{{ editingId ? 'Edit rule' : 'New rule' }}</h3>
      <div class="flex flex-wrap gap-4 text-sm">
        <label class="flex items-center gap-2">
          <input v-model="form.entryType" type="radio" value="Expense" />
          <span>Expense (split debt across members)</span>
        </label>
        <label class="flex items-center gap-2">
          <input v-model="form.entryType" type="radio" value="Settlement" />
          <span>Settlement (clear existing debt)</span>
        </label>
        <label class="flex items-center gap-2">
          <input v-model="form.entryType" type="radio" value="Exclude" />
          <span>Don't share (skip matched transactions)</span>
        </label>
      </div>
      <div class="grid gap-3 md:grid-cols-4">
        <label v-if="form.matchType !== 'Any'" class="flex flex-col text-sm md:col-span-2">
          <span class="text-slate-600 mb-1">Pattern</span>
          <input v-model="form.pattern" type="text" placeholder="Transfer to Joint" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <div v-else class="md:col-span-2 self-end pb-2 text-xs text-slate-500">
          Pattern is ignored — this rule matches every transaction. Useful as a
          low-priority catch-all "exclude everything else".
        </div>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Match</span>
          <select v-model="form.matchType" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="m in matchTypes" :key="m" :value="m">{{ m }}</option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Priority</span>
          <input v-model.number="form.priority" type="number" class="border border-slate-300 rounded px-3 py-2" />
        </label>
        <label class="flex flex-col text-sm md:col-span-2">
          <span class="text-slate-600 mb-1">Group</span>
          <select v-model="form.groupId" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="g in me.groups" :key="g.id" :value="g.id">{{ g.name }}</option>
          </select>
        </label>
        <label v-if="formGroup && form.entryType !== 'Exclude'" class="flex flex-col text-sm md:col-span-2">
          <span class="text-slate-600 mb-1">{{ form.entryType === 'Settlement' ? 'Sender (your side)' : 'Paid by' }}</span>
          <select v-model="form.paidByMemberId" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="m in formGroup.members" :key="m.id" :value="m.id">{{ memberLabel(m) }}</option>
          </select>
        </label>
      </div>

      <div v-if="formGroup && form.entryType === 'Expense'">
        <div class="text-sm text-slate-600 mb-2">Split equally between (the payer's share is excluded from owers):</div>
        <div class="flex flex-wrap gap-3">
          <label v-for="m in formGroup.members" :key="m.id" class="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              :checked="form.participantMemberIds.includes(m.id)"
              @change="toggleParticipant(m.id)"
            />
            <span>{{ memberLabel(m) }}</span>
          </label>
        </div>
      </div>

      <div v-if="formGroup && form.entryType === 'Settlement'">
        <label class="flex flex-col text-sm max-w-md">
          <span class="text-slate-600 mb-1">Recipient (whose debt is being cleared)</span>
          <select v-model="form.settlementRecipientId" class="border border-slate-300 rounded px-3 py-2">
            <option v-for="m in formGroup.members.filter(x => x.id !== form.paidByMemberId)" :key="m.id" :value="m.id">
              {{ memberLabel(m) }}
            </option>
          </select>
        </label>
        <p class="text-xs text-slate-500 mt-1">
          Matched transactions become a settlement: {{ formGroup.members.find(m => m.id === form.paidByMemberId) ? memberLabel(formGroup.members.find(m => m.id === form.paidByMemberId)!) : '?' }}
          paid {{ formGroup.members.find(m => m.id === form.settlementRecipientId) ? memberLabel(formGroup.members.find(m => m.id === form.settlementRecipientId)!) : '?' }} the full transaction amount.
        </p>
      </div>

      <div class="flex gap-2">
        <button
          @click="saveRule"
          :disabled="!canSave"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >{{ editingId ? 'Save' : 'Add' }}</button>
        <button v-if="editingId" @click="resetForm" class="px-4 py-2 border border-slate-300 rounded">Cancel</button>
      </div>
      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    </section>

    <section class="bg-white rounded border border-slate-200 overflow-hidden">
      <div v-if="loading" class="p-4 text-slate-500">Loading...</div>
      <table v-else-if="rules.length" class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-right px-4 py-2 w-20">Priority</th>
            <th class="text-left px-4 py-2">Pattern</th>
            <th class="text-left px-4 py-2 w-24">Match</th>
            <th class="text-left px-4 py-2 w-28">Type</th>
            <th class="text-left px-4 py-2">Effect</th>
            <th class="text-right px-4 py-2 w-32"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="r in rules" :key="r.id" class="border-t border-slate-100">
            <td class="px-4 py-2 text-right tabular-nums">{{ r.priority }}</td>
            <td class="px-4 py-2 font-mono">{{ r.pattern }}</td>
            <td class="px-4 py-2 text-slate-600">{{ r.matchType }}</td>
            <td class="px-4 py-2 text-slate-600">{{ r.entryType }}</td>
            <td class="px-4 py-2 text-slate-600">{{ summary(r) }}</td>
            <td class="px-4 py-2 text-right space-x-2">
              <button @click="editRule(r)" class="text-blue-600 hover:underline">Edit</button>
              <button @click="deleteRule(r.id)" class="text-red-600 hover:underline">Delete</button>
            </td>
          </tr>
        </tbody>
      </table>
      <div v-else class="p-4 text-slate-500">No sharing rules yet.</div>
    </section>
  </div>
</template>
