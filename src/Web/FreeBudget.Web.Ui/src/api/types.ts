export interface User {
  id: string;
  email: string;
  displayName: string;
}

export interface GroupMember {
  id: string;
  groupId: string;
  label: string;
  owningUserId?: string | null;
  role: string;
}

export interface Group {
  id: string;
  name: string;
  createdByUserId: string;
  members: GroupMember[];
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

export type RuleMatchType = 'Contains' | 'Exact' | 'StartsWith' | 'EndsWith';

export interface CategorizationRule {
  id: string;
  pattern: string;
  matchType: RuleMatchType;
  category: string;
  priority: number;
}

export type LedgerEntryKind = 'Expense' | 'Settlement';

export interface SharingRule {
  id: string;
  pattern: string;
  matchType: RuleMatchType;
  entryType: LedgerEntryKind;
  priority: number;
  groupId: string;
  paidByMemberId: string;
  participantMemberIds: string[];
}

export interface SplitParticipantInput {
  memberId: string;
  amount: number;
}

export interface SplitTransactionInput {
  groupId: string;
  paidByMemberId: string;
  transactionId: string;
  currencyCode: string;
  description: string;
  entryDate: string;
  participants: SplitParticipantInput[];
}

export interface MemberBalance {
  memberId: string;
  owesToMemberId: string;
  amount: number;
  currencyCode: string;
}

export interface LedgerEntry {
  id: string;
  groupId: string;
  paidByMemberId: string;
  owedByMemberId: string;
  amount: number;
  currencyCode: string;
  description: string;
  entryType: 'Expense' | 'Settlement';
  transactionId?: string | null;
  entryDate: string;
}
