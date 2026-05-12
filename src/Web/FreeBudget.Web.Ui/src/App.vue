<script setup lang="ts">
import { onMounted } from 'vue';
import { RouterLink, RouterView } from 'vue-router';
import { useMeStore } from './stores/me';

const me = useMeStore();
onMounted(() => me.load());
</script>

<template>
  <div class="min-h-full flex flex-col bg-slate-50 text-slate-900">
    <header class="bg-white border-b border-slate-200">
      <div class="max-w-6xl mx-auto px-6 py-4 flex items-center gap-6">
        <h1 class="text-xl font-semibold tracking-tight">FreeBudget</h1>
        <nav class="flex gap-4 text-sm">
          <RouterLink to="/" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Dashboard</RouterLink>
          <RouterLink to="/transactions" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Transactions</RouterLink>
          <RouterLink to="/reports" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Reports</RouterLink>
          <RouterLink to="/rules" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Rules</RouterLink>
          <RouterLink to="/accounts" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Accounts</RouterLink>
          <RouterLink to="/groups" class="hover:text-blue-600" active-class="text-blue-600 font-medium">Groups</RouterLink>
        </nav>
        <div class="ml-auto text-sm text-slate-500">
          <span v-if="me.user">{{ me.user.displayName }}</span>
          <span v-else-if="me.error" class="text-red-600">{{ me.error }}</span>
          <span v-else>Loading...</span>
        </div>
      </div>
    </header>
    <main class="flex-1">
      <div class="max-w-6xl mx-auto px-6 py-6">
        <RouterView />
      </div>
    </main>
  </div>
</template>
