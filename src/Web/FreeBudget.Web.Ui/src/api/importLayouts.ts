import { api } from './client';

export interface ImportLayout {
  id?: string | null;
  bankAccountId: string;
  name: string;
  dateColumn: string;
  descriptionColumn: string;
  amountColumn: string;
  currencyColumn?: string | null;
  directionColumn?: string | null;
  directionMappings?: Record<string, string> | null;
  externalIdColumn?: string | null;
  runningBalanceColumn?: string | null;
  categoryColumn?: string | null;
  targetAmountColumn?: string | null;
  targetCurrencyColumn?: string | null;
  dateFormat: string;
  hasHeaderRow: boolean;
  delimiter: string;
  defaultCurrencyCode: string;
}

export function blankLayout(bankAccountId: string): ImportLayout {
  return {
    bankAccountId,
    name: 'Custom',
    dateColumn: '',
    descriptionColumn: '',
    amountColumn: '',
    currencyColumn: null,
    directionColumn: null,
    directionMappings: null,
    externalIdColumn: null,
    runningBalanceColumn: null,
    categoryColumn: null,
    targetAmountColumn: null,
    targetCurrencyColumn: null,
    dateFormat: 'dd/MM/yyyy',
    hasHeaderRow: true,
    delimiter: ',',
    defaultCurrencyCode: 'GBP',
  };
}

export async function getSavedLayout(bankAccountId: string): Promise<ImportLayout | null> {
  try {
    const { data } = await api.get<ImportLayout>(`/bank-accounts/${bankAccountId}/import-layout`);
    return data;
  } catch (e: any) {
    if (e?.response?.status === 404) return null;
    throw e;
  }
}

export async function getPresets(): Promise<ImportLayout[]> {
  const { data } = await api.get<ImportLayout[]>('/import-layouts/presets');
  return data;
}

export async function saveLayout(bankAccountId: string, layout: ImportLayout): Promise<void> {
  await api.put(`/bank-accounts/${bankAccountId}/import-layout`, {
    name: layout.name,
    dateColumn: layout.dateColumn,
    descriptionColumn: layout.descriptionColumn,
    amountColumn: layout.amountColumn,
    currencyColumn: layout.currencyColumn,
    directionColumn: layout.directionColumn,
    directionMappings: layout.directionMappings,
    externalIdColumn: layout.externalIdColumn,
    runningBalanceColumn: layout.runningBalanceColumn,
    categoryColumn: layout.categoryColumn,
    targetAmountColumn: layout.targetAmountColumn,
    targetCurrencyColumn: layout.targetCurrencyColumn,
    dateFormat: layout.dateFormat,
    hasHeaderRow: layout.hasHeaderRow,
    delimiter: layout.delimiter,
    defaultCurrencyCode: layout.defaultCurrencyCode,
  });
}

export function parseCsv(text: string, delimiter: string): string[][] {
  const rows: string[][] = [];
  let current = '';
  let row: string[] = [];
  let inQuotes = false;

  for (let i = 0; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      if (inQuotes && text[i + 1] === '"') {
        current += '"';
        i++;
      } else {
        inQuotes = !inQuotes;
      }
    } else if (c === delimiter && !inQuotes) {
      row.push(current);
      current = '';
    } else if ((c === '\n' || c === '\r') && !inQuotes) {
      if (c === '\r' && text[i + 1] === '\n') i++;
      row.push(current);
      current = '';
      if (row.some(f => f.length > 0)) rows.push(row);
      row = [];
    } else {
      current += c;
    }
  }
  if (current.length > 0 || row.length > 0) {
    row.push(current);
    if (row.some(f => f.length > 0)) rows.push(row);
  }
  return rows;
}
