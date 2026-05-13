<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useMeStore } from '../stores/me';
import { api } from '../api/client';
import type { Group, GroupMember, User } from '../api/types';

const me = useMeStore();
const error = ref<string | null>(null);

const newGroup = reactive({ name: '', saving: false });
const renamingGroup = ref<{ id: string; name: string } | null>(null);
const newMember = reactive<Record<string, { mode: 'placeholder' | 'invite'; label: string; email: string; busy: boolean }>>({});
const renamingMember = ref<{ groupId: string; memberId: string; label: string } | null>(null);
const linkingMember = ref<{ groupId: string; memberId: string; email: string; busy: boolean } | null>(null);

function rowState(groupId: string) {
  if (!newMember[groupId]) {
    newMember[groupId] = { mode: 'placeholder', label: '', email: '', busy: false };
  }
  return newMember[groupId];
}

async function refresh() {
  await me.refresh();
}

async function lookupUserByEmail(email: string): Promise<User | null> {
  try {
    const res = await api.get<User>('/users/lookup', { params: { email } });
    return res.data;
  } catch (e: unknown) {
    type StatusErr = { response?: { status?: number } };
    const status = (e as StatusErr)?.response?.status;
    if (status === 404) return null;
    throw e;
  }
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

async function addPlaceholder(group: Group) {
  const state = rowState(group.id);
  const label = state.label.trim();
  if (!label) return;
  state.busy = true;
  try {
    await api.post(`/groups/${group.id}/members`, { label, owningUserId: null });
    state.label = '';
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Add member failed';
  } finally {
    state.busy = false;
  }
}

async function inviteByEmail(group: Group) {
  const state = rowState(group.id);
  const email = state.email.trim();
  if (!email) return;
  state.busy = true;
  error.value = null;
  try {
    const user = await lookupUserByEmail(email);
    if (!user) {
      error.value = `No user with email "${email}". Ask them to sign up first, or add a placeholder you can link later.`;
      return;
    }
    const label = state.label.trim() || user.displayName;
    await api.post(`/groups/${group.id}/members`, { label, owningUserId: user.id });
    state.email = '';
    state.label = '';
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Invite failed';
  } finally {
    state.busy = false;
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

async function linkPlaceholderToMe(group: Group, member: GroupMember) {
  if (!me.user) return;
  try {
    await api.post(`/groups/${group.id}/members/${member.id}/link`, {
      owningUserId: me.user.id,
    });
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Link failed';
  }
}

function startLinkByEmail(group: Group, member: GroupMember) {
  linkingMember.value = { groupId: group.id, memberId: member.id, email: '', busy: false };
}

async function saveLinkByEmail() {
  if (!linkingMember.value) return;
  const { groupId, memberId, email } = linkingMember.value;
  const trimmed = email.trim();
  if (!trimmed) return;
  linkingMember.value.busy = true;
  error.value = null;
  try {
    const user = await lookupUserByEmail(trimmed);
    if (!user) {
      error.value = `No user with email "${trimmed}".`;
      return;
    }
    await api.post(`/groups/${groupId}/members/${memberId}/link`, { owningUserId: user.id });
    linkingMember.value = null;
    await refresh();
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Link failed';
  } finally {
    if (linkingMember.value) linkingMember.value.busy = false;
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
            <th class="text-right px-4 py-2 w-72"></th>
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
              <template v-if="linkingMember?.memberId === m.id">
                <div class="flex gap-2 items-center justify-end">
                  <input
                    v-model="linkingMember.email"
                    type="email"
                    placeholder="user@example.com"
                    class="border border-slate-300 rounded px-3 py-1 text-sm w-56"
                    :disabled="linkingMember.busy"
                    @keydown.enter="saveLinkByEmail"
                    @keydown.escape="linkingMember = null"
                  />
                  <button @click="saveLinkByEmail" :disabled="linkingMember.busy" class="text-blue-600 hover:underline disabled:text-slate-400">Link</button>
                  <button @click="linkingMember = null" class="text-slate-600 hover:underline">Cancel</button>
                </div>
              </template>
              <template v-else>
                <button
                  v-if="!m.owningUserId"
                  @click="linkPlaceholderToMe(g, m)"
                  class="text-blue-600 hover:underline"
                >Link to me</button>
                <button
                  v-if="!m.owningUserId"
                  @click="startLinkByEmail(g, m)"
                  class="text-blue-600 hover:underline"
                >Link by email…</button>
                <button v-if="renamingMember?.memberId !== m.id" @click="startRenameMember(g, m)" class="text-blue-600 hover:underline">Rename</button>
                <button v-if="renamingMember?.memberId !== m.id" @click="deleteMember(g, m)" class="text-red-600 hover:underline">Remove</button>
              </template>
            </td>
          </tr>
          <tr class="border-t border-slate-100 bg-slate-50">
            <td colspan="4" class="px-4 py-2 space-y-2">
              <div class="flex gap-3 text-xs text-slate-600">
                <label class="flex items-center gap-1">
                  <input type="radio" :value="'placeholder'" v-model="rowState(g.id).mode" /> Placeholder
                </label>
                <label class="flex items-center gap-1">
                  <input type="radio" :value="'invite'" v-model="rowState(g.id).mode" /> Invite by email
                </label>
              </div>
              <div v-if="rowState(g.id).mode === 'placeholder'" class="flex gap-2 items-center">
                <input
                  v-model="rowState(g.id).label"
                  type="text"
                  placeholder="Add member label (e.g. partner, joint)"
                  class="border border-slate-300 rounded px-3 py-1 flex-1 max-w-md"
                  :disabled="rowState(g.id).busy"
                  @keydown.enter="addPlaceholder(g)"
                />
                <button
                  @click="addPlaceholder(g)"
                  :disabled="!rowState(g.id).label.trim() || rowState(g.id).busy"
                  class="bg-blue-600 text-white px-3 py-1 rounded disabled:bg-slate-300 text-sm"
                >Add</button>
              </div>
              <div v-else class="flex gap-2 items-center flex-wrap">
                <input
                  v-model="rowState(g.id).email"
                  type="email"
                  placeholder="user@example.com"
                  class="border border-slate-300 rounded px-3 py-1 w-64"
                  :disabled="rowState(g.id).busy"
                  @keydown.enter="inviteByEmail(g)"
                />
                <input
                  v-model="rowState(g.id).label"
                  type="text"
                  placeholder="Label (optional, defaults to display name)"
                  class="border border-slate-300 rounded px-3 py-1 flex-1 min-w-[12rem]"
                  :disabled="rowState(g.id).busy"
                  @keydown.enter="inviteByEmail(g)"
                />
                <button
                  @click="inviteByEmail(g)"
                  :disabled="!rowState(g.id).email.trim() || rowState(g.id).busy"
                  class="bg-blue-600 text-white px-3 py-1 rounded disabled:bg-slate-300 text-sm"
                >{{ rowState(g.id).busy ? 'Inviting…' : 'Invite' }}</button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>
