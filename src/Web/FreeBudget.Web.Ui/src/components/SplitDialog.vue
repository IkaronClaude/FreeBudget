<script setup lang="ts">
import { ref, reactive, computed, watch } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { TransactionListItem, Group, GroupMember } from '../api/types';

const props = defineProps<{ transaction: TransactionListItem }>();
const emit = defineEmits<{ (e: 'close'): void; (e: 'created'): void }>();

const me = useMeStore();

const selectedGroupId = ref<string>('');
const selectedPayerId = ref<string>('');
const participants = reactive<Record<string, { included: boolean; amount: number }>>({});
const saving = ref(false);
const error = ref<string | null>(null);

const selectedGroup = computed<Group | null>(() =>
  me.groups.find(g => g.id === selectedGroupId.value) ?? null
);

const includedCount = computed(() => {
  if (!selectedGroup.value) return 0;
  return selectedGroup.value.members.filter(m => participants[m.id]?.included).length;
});

const totalAssigned = computed(() => {
  if (!selectedGroup.value) return 0;
  return selectedGroup.value.members
    .filter(m => participants[m.id]?.included && m.id !== selectedPayerId.value)
    .reduce((sum, m) => sum + (participants[m.id]?.amount ?? 0), 0);
});

watch(
  () => me.groups,
  (groups) => {
    if (!selectedGroupId.value && groups.length) selectedGroupId.value = groups[0].id;
  },
  { immediate: true }
);

watch(selectedGroupId, () => {
  const g = selectedGroup.value;
  if (!g) return;
  // Default payer: a member linked to current user, else group creator's member, else first
  const meMember = g.members.find(m => m.owningUserId === me.user?.id);
  selectedPayerId.value = meMember?.id ?? g.members[0].id;
  // Reset participants: all included
  Object.keys(participants).forEach(k => delete participants[k]);
  for (const m of g.members) {
    participants[m.id] = { included: true, amount: 0 };
  }
  splitEqually();
}, { immediate: true });

watch(selectedPayerId, () => splitEqually());

function splitEqually() {
  const g = selectedGroup.value;
  if (!g) return;
  const included = g.members.filter(m => participants[m.id]?.included);
  if (!included.length) return;
  const perHead = props.transaction.amount / included.length;
  for (const m of g.members) {
    if (m.id === selectedPayerId.value) {
      participants[m.id].amount = 0;
    } else if (participants[m.id]?.included) {
      participants[m.id].amount = parseFloat(perHead.toFixed(2));
    } else {
      participants[m.id].amount = 0;
    }
  }
}

function toggleMember(memberId: string) {
  if (!participants[memberId]) return;
  participants[memberId].included = !participants[memberId].included;
  splitEqually();
}

async function submit() {
  if (!selectedGroup.value || !selectedPayerId.value) return;
  const owers = selectedGroup.value.members
    .filter(m => m.id !== selectedPayerId.value && participants[m.id]?.included && participants[m.id].amount > 0)
    .map(m => ({ memberId: m.id, amount: participants[m.id].amount }));

  if (!owers.length) {
    error.value = 'At least one non-payer participant with a positive amount is required.';
    return;
  }

  saving.value = true;
  error.value = null;
  try {
    await api.post('/ledger/splits', {
      groupId: selectedGroup.value.id,
      paidByMemberId: selectedPayerId.value,
      transactionId: props.transaction.id,
      currencyCode: props.transaction.currencyCode,
      description: props.transaction.description,
      entryDate: props.transaction.transactionDate,
      participants: owers,
    });
    emit('created');
    emit('close');
  } catch (e: any) {
    error.value = e?.response?.data?.error ?? (e instanceof Error ? e.message : 'Split failed');
  } finally {
    saving.value = false;
  }
}

function memberLabel(m: GroupMember): string {
  if (m.owningUserId === me.user?.id) return `${m.label} (you)`;
  return m.label;
}
</script>

<template>
  <div class="bg-amber-50 border-t border-amber-200 px-4 py-3 space-y-3">
    <div class="flex items-center justify-between">
      <div class="font-medium text-amber-900">
        Split {{ transaction.amount.toFixed(2) }} {{ transaction.currencyCode }} — {{ transaction.description }}
      </div>
      <button @click="emit('close')" class="text-slate-600 hover:underline text-sm">Cancel</button>
    </div>

    <div v-if="!me.groups.length" class="text-sm text-slate-600">
      No groups yet. Create one on the Groups page first.
    </div>
    <template v-else>
      <div class="grid gap-3 md:grid-cols-2">
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Group</span>
          <select v-model="selectedGroupId" class="border border-slate-300 rounded px-3 py-1">
            <option v-for="g in me.groups" :key="g.id" :value="g.id">{{ g.name }}</option>
          </select>
        </label>
        <label class="flex flex-col text-sm">
          <span class="text-slate-600 mb-1">Paid by</span>
          <select v-model="selectedPayerId" class="border border-slate-300 rounded px-3 py-1">
            <option v-for="m in selectedGroup?.members ?? []" :key="m.id" :value="m.id">{{ memberLabel(m) }}</option>
          </select>
        </label>
      </div>

      <div v-if="selectedGroup" class="border border-amber-200 rounded">
        <div class="bg-amber-100 px-3 py-2 flex items-center justify-between text-xs font-medium text-amber-900">
          <span>Split between {{ includedCount }} member(s)</span>
          <button @click="splitEqually" class="text-blue-700 hover:underline">Split equally</button>
        </div>
        <table class="w-full text-sm">
          <tbody>
            <tr v-for="m in selectedGroup.members" :key="m.id" class="border-t border-amber-100">
              <td class="px-3 py-2">
                <label class="flex items-center gap-2">
                  <input
                    type="checkbox"
                    :checked="participants[m.id]?.included"
                    @change="toggleMember(m.id)"
                  />
                  <span>{{ memberLabel(m) }}</span>
                  <span v-if="m.id === selectedPayerId" class="text-xs text-amber-700">(payer)</span>
                </label>
              </td>
              <td class="px-3 py-2 text-right w-40">
                <input
                  v-if="m.id !== selectedPayerId && participants[m.id]?.included"
                  v-model.number="participants[m.id].amount"
                  type="number"
                  step="0.01"
                  min="0"
                  class="border border-slate-300 rounded px-2 py-1 w-28 text-right tabular-nums"
                />
                <span v-else class="text-slate-400 text-xs">—</span>
              </td>
            </tr>
          </tbody>
          <tfoot class="bg-amber-100">
            <tr>
              <td class="px-3 py-2 text-right text-xs text-amber-900">
                Assigned: {{ totalAssigned.toFixed(2) }} of {{ transaction.amount.toFixed(2) }}
                <span v-if="Math.abs(totalAssigned - transaction.amount) > 0.01" class="ml-2 text-red-700">
                  (off by {{ (totalAssigned - transaction.amount).toFixed(2) }})
                </span>
              </td>
              <td class="px-3 py-2 text-right">
                <button
                  @click="submit"
                  :disabled="saving || !selectedPayerId"
                  class="bg-blue-600 text-white px-3 py-1 rounded disabled:bg-slate-300 text-sm"
                >
                  {{ saving ? 'Saving...' : 'Save split' }}
                </button>
              </td>
            </tr>
          </tfoot>
        </table>
      </div>

      <p v-if="error" class="text-red-600 text-sm">{{ error }}</p>
    </template>
  </div>
</template>
