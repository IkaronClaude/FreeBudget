<script setup lang="ts">
import { useMeStore } from '../stores/me';

const me = useMeStore();
</script>

<template>
  <div class="space-y-6">
    <h2 class="text-2xl font-semibold">Dashboard</h2>
    <div v-if="me.loading" class="text-slate-500">Loading...</div>
    <div v-else-if="me.error" class="text-red-600">{{ me.error }}</div>
    <div v-else class="grid gap-6 md:grid-cols-3">
      <section class="bg-white rounded border border-slate-200 p-4">
        <h3 class="text-sm font-medium text-slate-500 uppercase tracking-wide mb-2">You</h3>
        <p class="font-medium">{{ me.user?.displayName }}</p>
        <p class="text-sm text-slate-600">{{ me.user?.email }}</p>
      </section>
      <section class="bg-white rounded border border-slate-200 p-4">
        <h3 class="text-sm font-medium text-slate-500 uppercase tracking-wide mb-2">Groups</h3>
        <ul v-if="me.groups.length" class="space-y-1">
          <li v-for="g in me.groups" :key="g.id" class="flex justify-between">
            <span>{{ g.name }}</span>
            <span class="text-xs text-slate-500">{{ g.members.length }} members</span>
          </li>
        </ul>
        <p v-else class="text-sm text-slate-500">No groups yet.</p>
      </section>
      <section class="bg-white rounded border border-slate-200 p-4">
        <h3 class="text-sm font-medium text-slate-500 uppercase tracking-wide mb-2">Bank accounts</h3>
        <ul v-if="me.bankAccounts.length" class="space-y-1">
          <li v-for="a in me.bankAccounts" :key="a.id" class="flex justify-between">
            <span>{{ a.nickname }}</span>
            <span class="text-xs text-slate-500">{{ a.bankType }}</span>
          </li>
        </ul>
        <p v-else class="text-sm text-slate-500">No accounts yet.</p>
      </section>
    </div>
  </div>
</template>
