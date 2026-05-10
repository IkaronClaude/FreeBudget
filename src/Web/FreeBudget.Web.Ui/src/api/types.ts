export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface Group {
  id: string;
  name: string;
  role: string;
}

export interface BankAccount {
  id: string;
  ownerUserId: string;
  bankType: string;
  nickname: string;
  externalAccountId?: string | null;
  hasApiCredentials: boolean;
}

export interface MeResponse {
  user: User;
  groups: Group[];
  bankAccounts: BankAccount[];
}

export interface TransactionListItem {
  id: string;
  bankAccountId: string;
  transactionDate: string;
  description: string;
  amount: number;
  currencyCode: string;
  direction: 'Credit' | 'Debit';
  category?: string | null;
  externalTransactionId?: string | null;
}

export interface CategoryBreakdownItem {
  category: string;
  totalCredit: number;
  totalDebit: number;
  net: number;
  transactionCount: number;
}

export interface PeriodBreakdownItem {
  periodLabel: string;
  periodStart: string;
  totalCredit: number;
  totalDebit: number;
  net: number;
  transactionCount: number;
}

export interface ImportCsvResponse {
  importBatchId: string;
  transactionCount: number;
  skippedDuplicates: number;
}
