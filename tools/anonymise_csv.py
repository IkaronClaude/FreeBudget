#!/usr/bin/env python3
"""Anonymise bank CSV data for testing.

Maintains persistent alias tables (JSON) so the same account/party
always maps to the same alias across runs. Interactive: prompts for
new aliases, prevents duplicates, supports '!' to keep original.

Preserves original file encoding (UTF-8/UTF-8 BOM/UTF-16/Latin-1)
and line endings (CRLF/LF).

Zero external dependencies — stdlib only (Python 3.10+).

Usage:
    # Barclays (default columns: Account + Memo)
    python anonymise_csv.py barclays.csv

    # Wise
    python anonymise_csv.py wise.csv \\
        --name-col "Source name" \\
        --name-col "Target name" \\
        --name-col "Created By"

    # Mix of column types
    python anonymise_csv.py export.csv \\
        --account-col Account \\
        --memo-col Memo \\
        --name-col "Created By"

Column types:
    --account-col   Account identifiers (e.g. "12-34-56 12345678").
                    Aliased from the accounts table.
    --memo-col      Memo/description fields with embedded party names.
                    Splits on double-space or tab to extract the party,
                    aliases the party, keeps the reference portion.
    --name-col      Plain name fields (e.g. "Source name"). The entire
                    cell value is aliased from the parties table.

If no column flags are given, defaults to: --account-col Account --memo-col Memo
"""

import argparse
import csv
import json
import re
import sys
from pathlib import Path


# ── Encoding detection ───────────────────────────────────────────────

def detect_file_format(path: Path) -> tuple[str, str]:
    """Detect encoding and line endings from raw bytes.

    Returns (encoding, newline) where encoding is suitable for open().
    Writing with the same encoding preserves BOM where applicable:
      - 'utf-8-sig' re-emits the UTF-8 BOM on write
      - 'utf-16'    re-emits the UTF-16 BOM on write
    """
    raw = path.read_bytes()

    if raw[:3] == b"\xef\xbb\xbf":
        enc = "utf-8-sig"
    elif raw[:2] in (b"\xff\xfe", b"\xfe\xff"):
        enc = "utf-16"
    else:
        try:
            raw.decode("utf-8")
            enc = "utf-8"
        except UnicodeDecodeError:
            enc = "latin-1"

    text = raw.decode(enc)
    if "\r\n" in text:
        newline = "\r\n"
    elif "\r" in text:
        newline = "\r"
    else:
        newline = "\n"

    return enc, newline


# ── Alias tables ─────────────────────────────────────────────────────

def load_table(path: Path) -> dict[str, str]:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {}


def save_table(table: dict[str, str], path: Path) -> None:
    path.write_text(
        json.dumps(table, indent=2, ensure_ascii=False) + "\n",
        encoding="utf-8",
    )


# ── Interactive prompting ────────────────────────────────────────────

def prompt_alias(real_value: str, label: str, used_aliases: set[str]) -> str | None:
    """Prompt for an alias. Returns None for 'keep original', DELETE_SENTINEL for delete."""
    print(f"\n  New {label}:")
    print(f"    \033[1m{real_value}\033[0m")
    while True:
        choice = input("  Alias (! = keep, ~ = delete row): ").strip()
        if choice == "!":
            return None
        if choice == "~":
            return DELETE_SENTINEL
        if not choice:
            print("    Cannot be empty. Type ! to keep, ~ to delete row.")
            continue
        if choice in used_aliases:
            print(f"    '{choice}' is already taken. Pick another.")
            continue
        return choice


def resolve(
    real: str,
    table: dict[str, str],
    used: set[str],
    label: str,
) -> str:
    """Return alias for *real*, prompting interactively if unseen."""
    if real in table:
        used.add(table[real])
        return table[real]

    alias = prompt_alias(real, label, used)
    if alias is None:
        table[real] = real
        used.add(real)
        return real

    table[real] = alias
    used.add(alias)
    return alias


# ── Memo parsing ─────────────────────────────────────────────────────

PARTY_SPLIT = re.compile(r"^(.+?)( {2,}|\t)(.*)$")
PARTY_PREFIX = re.compile(r"^(.+?)\*(.+)$")

DELETE_SENTINEL = "\x00__DELETE__"


def split_memo(memo: str) -> tuple[str, str]:
    """Split memo into (party, tail).

    Returns the party name and everything after it (separators included).
    Checks double-space/tab first, then '*' within the extracted party.

    Examples:
        "COMPANY  REF123"       → ("COMPANY", "  REF123")
        "AMZN*1234  ORDER REF"  → ("AMZN", "*1234  ORDER REF")
        "AMZN*1234"             → ("AMZN", "*1234")
        "PLAIN TEXT"            → ("PLAIN TEXT", "")
    """
    m = PARTY_SPLIT.match(memo)
    if m:
        party, sep, ref = m.group(1), m.group(2), m.group(3)
    else:
        party, sep, ref = memo, "", ""

    m2 = PARTY_PREFIX.match(party)
    if m2:
        return m2.group(1), "*" + m2.group(2) + sep + ref

    if sep:
        return party, sep + ref
    return party, ""


# ── Main ─────────────────────────────────────────────────────────────

def main() -> None:
    ap = argparse.ArgumentParser(
        description="Anonymise bank CSV data for testing",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=(
            "examples:\n"
            "  %(prog)s barclays.csv\n"
            '  %(prog)s wise.csv --name-col "Source name" '
            '--name-col "Target name" --name-col "Created By"\n'
        ),
    )
    ap.add_argument("input", help="Input CSV file")
    ap.add_argument("-o", "--output", help="Output file (default: <input>_anon.csv)")
    ap.add_argument(
        "--account-col",
        action="append",
        default=None,
        metavar="COL",
        help="Account column to anonymise (repeatable)",
    )
    ap.add_argument(
        "--memo-col",
        action="append",
        default=None,
        metavar="COL",
        help="Memo column — extracts party via double-space split (repeatable)",
    )
    ap.add_argument(
        "--name-col",
        action="append",
        default=None,
        metavar="COL",
        help="Name column — aliases whole cell value (repeatable)",
    )
    ap.add_argument(
        "--tables-dir",
        default=None,
        help="Directory for alias JSON files (default: next to this script)",
    )
    args = ap.parse_args()

    input_path = Path(args.input)
    if not input_path.exists():
        print(f"Error: {input_path} not found.", file=sys.stderr)
        sys.exit(1)

    output_path = (
        Path(args.output)
        if args.output
        else input_path.with_stem(input_path.stem + "_anon")
    )

    account_cols: list[str] = args.account_col or []
    memo_cols: list[str] = args.memo_col or []
    name_cols: list[str] = args.name_col or []

    if not account_cols and not memo_cols and not name_cols:
        account_cols = ["Account"]
        memo_cols = ["Memo"]

    tables_dir = Path(args.tables_dir) if args.tables_dir else Path(__file__).parent
    accounts_path = tables_dir / "anon_accounts.json"
    parties_path = tables_dir / "anon_parties.json"

    accounts = load_table(accounts_path)
    parties = load_table(parties_path)
    used_accounts: set[str] = set(accounts.values())
    used_parties: set[str] = set(parties.values())

    # ── Detect original encoding & line endings ──────────────────
    enc, newline = detect_file_format(input_path)
    print(f"Detected: encoding={enc}  line-ending={repr(newline)}")

    # ── Read ─────────────────────────────────────────────────────
    with open(input_path, newline="", encoding=enc) as f:
        reader = csv.DictReader(f)
        if not reader.fieldnames:
            print("Error: CSV has no header row.", file=sys.stderr)
            sys.exit(1)
        raw_fieldnames = list(reader.fieldnames)
        rows_raw = list(reader)

    # Some CSV dialects leave quotes around header names — strip them
    fieldnames = [fn.strip('"').strip() for fn in raw_fieldnames]
    if fieldnames != raw_fieldnames:
        remap = dict(zip(raw_fieldnames, fieldnames))
        rows = [{remap.get(k, k): v for k, v in row.items()} for row in rows_raw]
    else:
        rows = rows_raw

    # ── Validate column names ────────────────────────────────────
    all_cols = account_cols + memo_cols + name_cols
    missing = [c for c in all_cols if c not in fieldnames]
    present = [c for c in all_cols if c in fieldnames]

    if missing:
        print(
            f"Warning: column(s) not found and will be skipped: {missing}",
            file=sys.stderr,
        )
    if not present:
        print("Error: none of the specified columns exist in the CSV.", file=sys.stderr)
        print(f"  CSV columns: {fieldnames}", file=sys.stderr)
        sys.exit(1)

    active_account = [c for c in account_cols if c in fieldnames]
    active_memo = [c for c in memo_cols if c in fieldnames]
    active_name = [c for c in name_cols if c in fieldnames]

    print(f"Processing {len(rows)} rows from {input_path} ...")
    if active_account:
        print(f"  Account columns: {active_account}")
    if active_memo:
        print(f"  Memo columns:    {active_memo}")
    if active_name:
        print(f"  Name columns:    {active_name}")

    # ── Process rows ─────────────────────────────────────────────
    kept_rows: list[dict[str, str]] = []
    deleted = 0

    for row in rows:
        delete_row = False

        for col in active_account:
            raw = (row[col] or "").strip()
            if raw:
                alias = resolve(raw, accounts, used_accounts, f"account [{col}]")
                if alias == DELETE_SENTINEL:
                    delete_row = True
                    break
                row[col] = alias

        if delete_row:
            deleted += 1
            continue

        for col in active_memo:
            raw = (row[col] or "").strip()
            if raw:
                party, tail = split_memo(raw)
                if party:
                    hint = f"party [{col}]  (full: {raw})" if tail else f"party [{col}]"
                    anon = resolve(party, parties, used_parties, hint)
                    if anon == DELETE_SENTINEL:
                        delete_row = True
                        break
                    row[col] = anon + tail if tail else anon

        if delete_row:
            deleted += 1
            continue

        for col in active_name:
            raw = (row[col] or "").strip()
            if raw:
                alias = resolve(raw, parties, used_parties, f"name [{col}]")
                if alias == DELETE_SENTINEL:
                    delete_row = True
                    break
                row[col] = alias

        if delete_row:
            deleted += 1
            continue

        kept_rows.append(row)

    # ── Write with original encoding & line endings ──────────────
    with open(output_path, "w", newline="", encoding=enc) as f:
        writer = csv.DictWriter(
            f,
            fieldnames=fieldnames,
            lineterminator=newline,
            quoting=csv.QUOTE_MINIMAL,
        )
        writer.writeheader()
        writer.writerows(kept_rows)

    save_table(accounts, accounts_path)
    save_table(parties, parties_path)

    written = len(kept_rows)
    print(f"\nDone — {written} rows written to {output_path}")
    if deleted:
        print(f"  Deleted:  {deleted} row(s)")
    print(f"  Encoding: {enc}  Line endings: {repr(newline)}")
    print(f"  Accounts: {len(accounts)} mapping(s) in {accounts_path}")
    print(f"  Parties:  {len(parties)} mapping(s) in {parties_path}")


if __name__ == "__main__":
    main()
