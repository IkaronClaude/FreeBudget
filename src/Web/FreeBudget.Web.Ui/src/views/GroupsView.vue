<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { Group, GroupMember } from '../api/types';

const me = useMeStore();
const error = ref<string | null>(null);

const newGroup = reactive({ name: '', saving: false });
const renamingGroup = ref<{ id: string; name: string } | null>(null);
const newMember = reactive<Record<string, string>>({});
const renamingMember = ref<{ groupId: string; memberId: string; label: string } | null>(null);

async function refresh() {
  await me.refresh();
}

async function createGroup() {
  if (!newGroup.name.trim()) return;
  newGroup.saving = true;
  error.value = null;
  try {
    await api.post('/groups', { name: newGroup.name.trim(), creatorLabel: 'me' });
    newGroup.name = '';
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Create failed';
  } finally {
    newGroup.saving = false;
  }
}

function startRenameGroup(group: Group) {
  renamingGroup.value = { id: group.id, name: group.name };
}

async function saveRenameGroup() {
  if (!renamingGroup.value || !renamingGroup.value.name.trim()) return;
  try {
    await api.put(`/groups/${renamingGroup.value.id}`, { name: renamingGroup.value.name.trim() });
    renamingGroup.value = null;
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Rename failed';
  }
}

async function deleteGroup(group: Group) {
  if (!confirm(`Delete "${group.name}"? Members are dropped; ledger entries referencing the group will be orphaned.`)) return;
  try {
    await api.delete(`/groups/${group.id}`);
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Delete failed';
  }
}

async function addMember(group: Group) {
  const label = (newMember[group.id] ?? '').trim();
  if (!label) return;
  try {
    await api.post(`/groups/${group.id}/members`, { label, owningUserId: null });
    newMember[group.id] = '';
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Add member failed';
  }
}

function startRenameMember(group: Group, member: GroupMember) {
  renamingMember.value = { groupId: group.id, memberId: member.id, label: member.label };
}

async function saveRenameMember() {
  if (!renamingMember.value || !renamingMember.value.label.trim()) return;
  const { groupId, memberId, label } = renamingMember.value;
  try {
    await api.put(`/groups/${groupId}/members/${memberId}`, { label: label.trim() });
    renamingMember.value = null;
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Rename failed';
  }
}

async function deleteMember(group: Group, member: GroupMember) {
  if (!confirm(`Remove member "${member.label}" from "${group.name}"?`)) return;
  try {
    await api.delete(`/groups/${group.id}/members/${member.id}`);
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Remove failed';
  }
}

async function linkMemberToMe(group: Group, member: GroupMember) {
  if (!me.user) return;
  try {
    await api.post(`/groups/${group.id}/members`, {
      label: member.label,
      owningUserId: me.user.id,
    });
    await api.delete(`/groups/${group.id}/members/${member.id}`);
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Link failed';
  }
}
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Groups</h2>

    <section class="bg-white rounded border border-slate-200 p-4">
      <h3 class="font-medium mb-3">New group</h3>
      <div class="flex gap-3 items-end">
        <label class="flex flex-col text-sm flex-1 max-w-md">
          <span class="text-slate-600 mb-1">Name</span>
          <input
            v-model="newGroup.name"
            type="text"
            placeholder="Household"
            class="border border-slate-300 rounded px-3 py-2"
            @keydown.enter="createGroup"
          />
        </label>
        <button
          @click="createGroup"
          :disabled="!newGroup.name.trim() || newGroup.saving"
          class="bg-blue-600 text-white px-4 py-2 rounded disabled:bg-slate-300"
        >
          {{ newGroup.saving ? 'Creating...' : 'Create' }}
        </button>
      </div>
      <p class="mt-2 text-xs text-slate-500">You'll be added as a member labelled "me".</p>
      <p v-if="error" class="mt-3 text-red-600 text-sm">{{ error }}</p>
    </section>

    <div v-if="me.loading" class="text-slate-500">Loading...</div>
    <div v-else-if="!me.groups.length" class="bg-white rounded border border-slate-200 p-4 text-slate-500">
      No groups yet.
    </div>

    <section v-for="g in me.groups" :key="g.id" class="bg-white rounded border border-slate-200 overflow-hidden">
      <header class="px-4 py-3 border-b border-slate-200 flex items-center gap-3">
        <template v-if="renamingGroup?.id === g.id">
          <input
            v-model="renamingGroup.name"
            type="text"
            class="border border-slate-300 rounded px-3 py-1 flex-1 max-w-md"
            @keydown.enter="saveRenameGroup"
            @keydown.escape="renamingGroup = null"
          />
          <button @click="saveRenameGroup" class="text-blue-600 hover:underline text-sm">Save</button>
          <button @click="renamingGroup = null" class="text-slate-600 hover:underline text-sm">Cancel</button>
        </template>
        <template v-else>
          <h3 class="font-semibold flex-1">{{ g.name }}</h3>
          <button @click="startRenameGroup(g)" class="text-blue-600 hover:underline text-sm">Rename</button>
          <button @click="deleteGroup(g)" class="text-red-600 hover:underline text-sm">Delete</button>
        </template>
      </header>

      <table class="w-full text-sm">
        <thead class="bg-slate-50 text-slate-600">
          <tr>
            <th class="text-left px-4 py-2">Label</th>
            <th class="text-left px-4 py-2 w-32">Role</th>
            <th class="text-left px-4 py-2 w-48">Linked to</th>
            <th class="text-right px-4 py-2 w-56"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="m in g.members" :key="m.id" class="border-t border-slate-100">
            <td class="px-4 py-2">
              <template v-if="renamingMember?.memberId === m.id">
                <div class="flex gap-2">
                  <input
                    v-model="renamingMember.label"
                    type="text"
                    class="border border-slate-300 rounded px-3 py-1 flex-1"
                    @keydown.enter="saveRenameMember"
                    @keydown.escape="renamingMember = null"
                  />
                  <button @click="saveRenameMember" class="text-blue-600 hover:underline text-sm">Save</button>
                  <button @click="renamingMember = null" class="text-slate-600 hover:underline text-sm">Cancel</button>
                </div>
              </template>
              <template v-else>{{ m.label }}</template>
            </td>
            <td class="px-4 py-2 text-slate-600">{{ m.role }}</td>
            <td class="px-4 py-2 text-slate-600">
              <span v-if="m.owningUserId === me.user?.id" class="text-blue-700">you</span>
              <span v-else-if="m.owningUserId" class="font-mono text-xs">{{ m.owningUserId.slice(0, 8) }}…</span>
              <span v-else class="italic text-slate-400">placeholder</span>
            </td>
            <td class="px-4 py-2 text-right space-x-3 whitespace-nowrap">
              <button
                v-if="!m.owningUserId"
                @click="linkMemberToMe(g, m)"
                class="text-blue-600 hover:underline"
              >Link to me</button>
              <button v-if="renamingMember?.memberId !== m.id" @click="startRenameMember(g, m)" class="text-blue-600 hover:underline">Rename</button>
              <button v-if="renamingMember?.memberId !== m.id" @click="deleteMember(g, m)" class="text-red-600 hover:underline">Remove</button>
            </td>
          </tr>
          <tr class="border-t border-slate-100 bg-slate-50">
            <td colspan="4" class="px-4 py-2">
              <div class="flex gap-2 items-center">
                <input
                  v-model="newMember[g.id]"
                  type="text"
                  placeholder="Add member label (e.g. partner, joint)"
                  class="border border-slate-300 rounded px-3 py-1 flex-1 max-w-md"
                  @keydown.enter="addMember(g)"
                />
                <button
                  @click="addMember(g)"
                  :disabled="!(newMember[g.id] ?? '').trim()"
                  class="bg-blue-600 text-white px-3 py-1 rounded disabled:bg-slate-300 text-sm"
                >Add</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>
