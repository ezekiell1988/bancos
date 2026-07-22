"""
04_diff_envs.py
Compara conteos y diferencias entre prod y dev local.
No modifica ninguna base de datos. Solo lectura.

Dependencias: pip install pyodbc
Uso:
    python examples/04_diff_envs.py             # resumen de conteos por tabla
    python examples/04_diff_envs.py --detail    # incluir PKs de filas que difieren
"""

import json
import argparse
import sys
from pathlib import Path
import pyodbc

REPO_ROOT    = Path(__file__).resolve().parents[4]
SECRETS_DEV  = REPO_ROOT / ".local-secrets" / "db.json"
SECRETS_PROD = REPO_ROOT / ".local-secrets" / "dbProd.json"

ALL_TABLES = [
    "Owners", "Currencies", "ExchangeRates", "Accounts", "AccountAuxiliaries",
    "Categories", "ClassificationTags", "ClassificationRules",
    "ImportTemplatePatterns", "Imports", "ImportFingerprints", "ImportProgress",
    "Transactions", "CreditFinancings", "LoanStatements", "LoanPayments",
    "JournalEntries", "JournalLines", "Reconciliations", "ReconciliationTransactions",
    "ReportPeriods", "ForeignExchangeClosings", "ForeignExchangeClosingLines", "AuditLogs",
]


def build_conn_str(path: Path) -> str:
    s = json.loads(path.read_text())
    return (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={s['Server']};"
        f"DATABASE={s['Database']};"
        f"UID={s['User']};"
        f"PWD={s['Password']};"
        "TrustServerCertificate=yes;"
        "Connection Timeout=30;"
    )


def count_table(cursor, table: str) -> int | None:
    try:
        cursor.execute(f"SELECT COUNT(*) FROM [{table}]")
        return cursor.fetchone()[0]
    except Exception:
        return None


def get_pk_values(cursor, table: str, pk_cols: list[str]) -> set[tuple]:
    col_list = ", ".join(f"[{c}]" for c in pk_cols)
    try:
        cursor.execute(f"SELECT {col_list} FROM [{table}]")
        return {tuple(row) for row in cursor.fetchall()}
    except Exception:
        return set()


def get_pk_columns(cursor, table: str) -> list[str]:
    cursor.execute(
        """
        SELECT c.COLUMN_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE c
          ON tc.CONSTRAINT_NAME = c.CONSTRAINT_NAME
        WHERE tc.TABLE_NAME = ? AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
        ORDER BY c.COLUMN_NAME
        """,
        table,
    )
    return [row[0] for row in cursor.fetchall()]


def main():
    parser = argparse.ArgumentParser(description="Comparar prod vs dev (solo lectura)")
    parser.add_argument("--detail", action="store_true", help="Mostrar PKs de filas que difieren")
    args = parser.parse_args()

    for f in [SECRETS_PROD, SECRETS_DEV]:
        if not f.exists():
            sys.exit(f"Error: no se encontró {f}")

    prod_conn = pyodbc.connect(build_conn_str(SECRETS_PROD), autocommit=True, readonly=True)
    dev_conn  = pyodbc.connect(build_conn_str(SECRETS_DEV),  autocommit=True, readonly=True)
    pc = prod_conn.cursor()
    dc = dev_conn.cursor()

    print(f"\n{'Tabla':<40} {'PROD':>8} {'DEV':>8} {'Diff':>8}")
    print("-" * 70)

    only_in_prod_total = 0
    only_in_dev_total  = 0

    for table in ALL_TABLES:
        prod_count = count_table(pc, table)
        dev_count  = count_table(dc, table)

        if prod_count is None and dev_count is None:
            print(f"  {table:<38} {'N/A':>8} {'N/A':>8}")
            continue

        p = prod_count or 0
        d = dev_count  or 0
        diff = d - p
        diff_str = f"+{diff}" if diff > 0 else str(diff)
        flag = " <<" if abs(diff) > 0 else ""
        print(f"  {table:<38} {p:>8} {d:>8} {diff_str:>8}{flag}")

        if args.detail and abs(diff) > 0:
            pk_cols = get_pk_columns(pc, table)
            if pk_cols:
                prod_pks = get_pk_values(pc, table, pk_cols)
                dev_pks  = get_pk_values(dc, table, pk_cols)
                only_prod = prod_pks - dev_pks
                only_dev  = dev_pks - prod_pks
                if only_prod:
                    print(f"    Solo en PROD ({len(only_prod)}): {list(only_prod)[:5]}{'...' if len(only_prod)>5 else ''}")
                if only_dev:
                    print(f"    Solo en DEV  ({len(only_dev)}):  {list(only_dev)[:5]}{'...' if len(only_dev)>5 else ''}")
                only_in_prod_total += len(only_prod)
                only_in_dev_total  += len(only_dev)

    print("-" * 70)
    if args.detail:
        print(f"\nResumen: {only_in_prod_total} filas solo en PROD | {only_in_dev_total} filas solo en DEV")
        if only_in_prod_total > 0:
            print("  → Ejecutar 03_prod_to_dev.py para traer datos de prod a dev")
    print()

    prod_conn.close()
    dev_conn.close()


if __name__ == "__main__":
    main()
