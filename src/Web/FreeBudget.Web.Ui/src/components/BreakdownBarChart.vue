<script setup lang="ts">
import { computed } from 'vue';

interface BarRow {
  label: string;
  credit: number;
  debit: number;
}

const props = defineProps<{
  rows: BarRow[];
  /** Max bars to render — others summarised. */
  maxRows?: number;
  /** Show the net difference column on the right. */
  showNet?: boolean;
}>();

const maxRows = computed(() => props.maxRows ?? 12);

const visibleRows = computed(() => {
  if (props.rows.length <= maxRows.value) return props.rows;
  const head = props.rows.slice(0, maxRows.value - 1);
  const rest = props.rows.slice(maxRows.value - 1);
  const credit = rest.reduce((a, r) => a + r.credit, 0);
  const debit = rest.reduce((a, r) => a + r.debit, 0);
  return [...head, { label: `+${rest.length} more`, credit, debit }];
});

const scaleMax = computed(() => {
  let max = 0;
  for (const r of visibleRows.value) {
    if (r.credit > max) max = r.credit;
    if (r.debit > max) max = r.debit;
  }
  return max || 1;
});

const fmt = (n: number) => n.toFixed(2);
function widthPct(value: number) {
  return `${Math.max(0, (value / scaleMax.value) * 100)}%`;
}
</script>

<template>
  <div class="space-y-1.5">
    <div v-for="r in visibleRows" :key="r.label" class="grid grid-cols-[10rem_1fr_6rem] gap-3 items-center text-xs">
      <div class="truncate text-slate-700" :title="r.label">{{ r.label }}</div>
      <div class="relative h-5 bg-slate-50 rounded overflow-hidden border border-slate-100">
        <div class="absolute inset-y-0 left-1/2 w-px bg-slate-300"></div>
        <div
          class="absolute top-0 bottom-0 right-1/2 bg-red-400/80"
          :style="{ width: `calc(${widthPct(r.debit)} / 2)` }"
          :title="`Debit ${fmt(r.debit)}`"
        ></div>
        <div
          class="absolute top-0 bottom-0 left-1/2 bg-emerald-500/80"
          :style="{ width: `calc(${widthPct(r.credit)} / 2)` }"
          :title="`Credit ${fmt(r.credit)}`"
        ></div>
      </div>
      <div v-if="showNet" class="text-right tabular-nums" :class="(r.credit - r.debit) >= 0 ? 'text-emerald-700' : 'text-red-700'">
        {{ (r.credit - r.debit) >= 0 ? '+' : '' }}{{ fmt(r.credit - r.debit) }}
      </div>
      <div v-else class="text-right tabular-nums text-slate-500">
        <span class="text-red-700">−{{ fmt(r.debit) }}</span>
      </div>
    </div>
    <div class="flex gap-4 text-xs text-slate-500 pt-1">
      <span class="flex items-center gap-1"><span class="inline-block w-3 h-3 bg-red-400/80 rounded-sm"></span>Debit</span>
      <span class="flex items-center gap-1"><span class="inline-block w-3 h-3 bg-emerald-500/80 rounded-sm"></span>Credit</span>
    </div>
  </div>
</template>
