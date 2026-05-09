#!/usr/bin/env python3
"""Anonymise bank CSV data for testing.

Maintains a persistent alias table (JSON) so the same party always maps
to the same alias across runs. Interactive: prompts for new aliases,
prevents duplicates.

Preserves original file encoding (UTF-8/UTF-8 BOM/UTF-16/Latin-1)
and line endings (CRLF/LF).

Zero external dependencies — stdlib only (Python 3.10+).

Usage:
    # Barclays (default columns: Account;Memo)
    python anonymise_csv.py barclays.csv

    # Wise
    python anonymise_csv.py wise.csv -c "Source name;Target name;Created by"

    # Custom
    python anonymise_csv.py export.csv -c "Account;Memo;Created By"

Prompt shortcuts:
    alias       Alias the matched party, keep reference tail
    !           Keep original value unchanged
    !text       Override the entire cell with "text"
    ~           Delete the row (and all future rows matching this party)

Column default: Account;Memo (if -c not given)
"""

import argparse
import csv
import json
import re
import sys
from pathlib import Path


# ── Encoding detection ───────────────────────────────────────────────

def detect_file_format(path: Path) -> tuple[str, str]:
    """Detect encoding and line endings from raw bytes."""
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


# ── Alias table ──────────────────────────────────────────────────────

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

DELETE_SENTINEL = "\x00__DELETE__"
OVERRIDE_PREFIX = "\x00__OVERRIDE__"


def prompt_alias(real_value: str, label: str, used_aliases: set[str]) -> str | None:
    """Prompt for an alias. Returns None for 'keep original'."""
    print(f"\n  New {label}:")
    print(f"    \033[1m{real_value}\033[0m")
    while True:
        choice = input("  Alias (! = keep, !text = override cell, ~ = delete row): ").strip()
        if choice == "!":
            return None
        if choice == "~":
            return DELETE_SENTINEL
        if choice.startswith("!"):
            return OVERRIDE_PREFIX + choice[1:].strip()
        if not choice:
            print("    Cannot be empty. Type ! to keep, !text to override cell, ~ to delete row.")
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


# ── Cell parsing ─────────────────────────────────────────────────────

PARTY_SPLIT = re.compile(r"^(.+?)( {2,}|\t)(.*)$")
PARTY_PREFIX = re.compile(r"^(.+?)\*(.+)$")


def split_cell(value: str) -> tuple[str, str]:
    """Split a cell into (party, tail).

    Extracts the party name and keeps everything after it (separators
    included). Checks double-space/tab first, then '*' within the
    extracted party.

    Examples:
        "COMPANY  REF123"       → ("COMPANY", "  REF123")
        "AMZN*1234  ORDER REF"  → ("AMZN", "*1234  ORDER REF")
        "AMZN*1234"             → ("AMZN", "*1234")
        "John Smith"            → ("John Smith", "")
    """
    m = PARTY_SPLIT.match(value)
    if m:
        party, sep, ref = m.group(1), m.group(2), m.group(3)
    else:
        party, sep, ref = value, "", ""

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
            '  %(prog)s wise.csv -c "Source name;Target name;Created by"\n'
        ),
    )
    ap.add_argument("input", help="Input CSV file")
    ap.add_argument("-o", "--output", help="Output file (default: <input>_anon.csv)")
    ap.add_argument(
        "-c", "--columns",
        default=None,
        metavar="COLS",
        help='Semicolon-separated column names to anonymise (default: "Account;Memo")',
    )
    ap.add_argument(
        "--tables-dir",
        default=None,
        help="Directory for alias JSON file (default: next to this script)",
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

    user_cols = [c.strip() for c in (args.columns or "Account;Memo").split(";") if c.strip()]

    tables_dir = Path(args.tables_dir) if args.tables_dir else Path(__file__).parent
    aliases_path = tables_dir / "anon_parties.json"

    aliases = load_table(aliases_path)
    used_aliases: set[str] = set(aliases.values())

    # ── Detect original encoding & line endings ──────────────────
    enc, newline = detect_file_format(input_path)
    print(f"Detected: encoding={enc}  line-ending={repr(newline)}")

    # ── Read ─────────────────────────────────────────────────────
    with open(input_path, newline="", encoding=enc) as f:
        reader = csv.DictReader(f)
        if not reader.fieldnames:
            print("Error: CSV has no header row.", file=sys.stderr)
            sys.exit(1)
        fieldnames = list(reader.fieldnames)
        rows = list(reader)

    # ── Resolve columns (case-/quote-insensitive) ────────────────
    col_lookup: dict[str, str] = {}
    for fn in fieldnames:
        col_lookup[fn.strip('"').strip().lower()] = fn

    active_cols: list[str] = []
    missing: list[str] = []
    for uc in user_cols:
        actual = col_lookup.get(uc.strip('"').strip().lower())
        if actual:
            active_cols.append(actual)
        else:
            missing.append(uc)

    if missing:
        print(f"Warning: column(s) not found and will be skipped: {missing}", file=sys.stderr)
    if not active_cols:
        print("Error: none of the specified columns exist in the CSV.", file=sys.stderr)
        print(f"  CSV columns: {fieldnames}", file=sys.stderr)
        sys.exit(1)

    print(f"Processing {len(rows)} rows from {input_path} ...")
    print(f"  Columns: {active_cols}")

    # ── Process rows ─────────────────────────────────────────────
    kept_rows: list[dict[str, str]] = []
    deleted = 0

    for row in rows:
        delete_row = False

        for col in active_cols:
            raw = (row[col] or "").strip()
            if not raw:
                continue

            party, tail = split_cell(raw)
            hint = f"[{col}]  (full: {raw})" if tail else f"[{col}]"
            anon = resolve(party, aliases, used_aliases, hint)

            if anon == DELETE_SENTINEL:
                delete_row = True
                break
            if anon.startswith(OVERRIDE_PREFIX):
                row[col] = anon[len(OVERRIDE_PREFIX):]
            else:
                row[col] = anon + tail if tail else anon

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

    save_table(aliases, aliases_path)

    written = len(kept_rows)
    print(f"\nDone — {written} rows written to {output_path}")
    if deleted:
        print(f"  Deleted:  {deleted} row(s)")
    print(f"  Encoding: {enc}  Line endings: {repr(newline)}")
    print(f"  Aliases:  {len(aliases)} mapping(s) in {aliases_path}")


if __name__ == "__main__":
    main()
