import { createRouter, createWebHistory } from 'vue-router';
import DashboardView from '../views/DashboardView.vue';
import TransactionsView from '../views/TransactionsView.vue';
import ReportsView from '../views/ReportsView.vue';
import RulesView from '../views/RulesView.vue';
import AccountsView from '../views/AccountsView.vue';
import GroupsView from '../views/GroupsView.vue';
import LedgerView from '../views/LedgerView.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', name: 'dashboard', component: DashboardView },
    { path: '/transactions', name: 'transactions', component: TransactionsView },
    { path: '/reports', name: 'reports', component: ReportsView },
    { path: '/rules', name: 'rules', component: RulesView },
    { path: '/accounts', name: 'accounts', component: AccountsView },
    { path: '/groups', name: 'groups', component: GroupsView },
    { path: '/ledger', name: 'ledger', component: LedgerView }
  ]
});
