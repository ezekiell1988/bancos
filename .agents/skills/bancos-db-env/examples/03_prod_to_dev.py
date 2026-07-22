"""
03_prod_to_dev.py
Copia registros de referencia desde prod hacia dev local para pruebas.
Usa MERGE (upsert) — no duplica, no borra datos existentes en dev.

Dependencias: pip install pyodbc
Uso:
    python examples/03_prod_to_dev.py                  # tablas de referencia
    python examples/03_prod_to_dev.py --transactions   # incluir transacciones recientes
    python examples/03_prod_to_dev.py --days 30        # transacciones de los últimos N días
"""

import json
import argparse
import sys
from pathlib import Path
import pyodbc

REPO_ROOT = Path(__file__).resolve().parents[4]
SECRETS_DEV  = REPO_ROOT / ".local-secrets" / "db.json"
SECRETS_PROD = REPO_ROOT / ".local-secrets" / "dbProd.json"

# Tablas copiadas siempre (referencia / catálogo)
REFERENCE_TABLES = [
    "Owners",
    "Currencies",
    "ExchangeRates",
    "Accounts",
    "AccountAuxiliaries",
    "Categories",
    "ClassificationTags",
    "ClassificationRules",
    "ImportTemplatePatterns",
]

# Tablas de datos financieros (solo con --transactions)
TRANSACTION_TABLES = [
    "Imports",
    "ImportFingerprints",
    "ImportProgress",
    "Transactions",
    "CreditFinancings",
    "LoanStatements",
    "LoanPayments",
    "JournalEntries",
    "JournalLines",
    "Reconciliations",
    "ReconciliationTransactions",
    "ReportPeriods",
    "ForeignExchangeClosings",
    "ForeignExchangeClosingLines",
]


def build_conn_str(secrets_path: Path) -> str:
    s = json.loads(secrets_path.read_text())
    return (
        f"DRIVER={{ODBC Driver 18 for SQL Server}};"
        f"SERVER={s['Server']};"
        f"DATABASE={s['Database']};"
        f"UID={s['User']};"
        f"PWD={s['Password']};"
        "TrustServerCertificate=yes;"
        "Connection Timeout=30;"
    )


def get_columns(cursor, table: str) -> list[str]:
    cursor.execute(
        "SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS "
        "WHERE TABLE_NAME = ? ORDER BY ORDINAL_POSITION",
        table,
    )
    return [row[0] for row in cursor.fetchall()]


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


def merge_table(
    prod_cursor,
    dev_conn,
    table: str,
    days_filter: int | None = None,
) -> int:
    all_cols = get_columns(prod_cursor, table)
    pk_cols  = get_pk_columns(prod_cursor, table)
    if not all_cols or not pk_cols:
        print(f"  [skip] {table}: no se pudo leer esquema")
        return 0

    # Construir SELECT desde prod
    date_filter = ""
    if days_filter and "CreatedAt" in all_cols:
        date_filter = f"WHERE CreatedAt >= DATEADD(day, -{days_filter}, GETUTCDATE())"

    prod_cursor.execute(f"SELECT {', '.join(all_cols)} FROM [{table}] {date_filter}")
    rows = prod_cursor.fetchall()
    if not rows:
        print(f"  [skip] {table}: sin filas en prod")
        return 0

    # MERGE en dev usando columnas PK como clave de coincidencia
    non_pk = [c for c in all_cols if c not in pk_cols]
    placeholders = ", ".join("?" * len(all_cols))
    col_list = ", ".join(f"[{c}]" for c in all_cols)
    pk_on    = " AND ".join(f"target.[{c}] = source.[{c}]" for c in pk_cols)
    update_set = ", ".join(f"target.[{c}] = source.[{c}]" for c in non_pk) if non_pk else "target.[{pk_cols[0]}] = target.[{pk_cols[0]}]"
    source_cols = ", ".join(f"source.[{c}]" for c in all_cols)

    merge_sql = f"""
    MERGE [{table}] AS target
    USING (VALUES ({placeholders})) AS source ({col_list})
    ON ({pk_on})
    WHEN MATCHED THEN UPDATE SET {update_set}
    WHEN NOT MATCHED THEN INSERT ({col_list}) VALUES ({source_cols});
    """

    dev_cursor = dev_conn.cursor()
    dev_cursor.fast_executemany = True
    try:
        dev_cursor.executemany(merge_sql, [list(r) for r in rows])
        dev_conn.commit()
    except Exception as e:
        dev_conn.rollback()
        print(f"  [error] {table}: {e}")
        return 0
    finally:
        dev_cursor.close()

    print(f"  [ok]   {table}: {len(rows)} filas mergeadas")
    return len(rows)


def main():
    parser = argparse.ArgumentParser(description="Sincronizar prod → dev (merge)")
    parser.add_argument("--transactions", action="store_true", help="Incluir tablas de transacciones")
    parser.add_argument("--days", type=int, default=90, help="Días hacia atrás para filtrar transacciones (default: 90)")
    args = parser.parse_args()

    if not SECRETS_PROD.exists():
        sys.exit(f"Error: no se encontró {SECRETS_PROD}")
    if not SECRETS_DEV.exists():
        sys.exit(f"Error: no se encontró {SECRETS_DEV}")

    print(f"Conectando a PROD y DEV...")
    prod_conn = pyodbc.connect(build_conn_str(SECRETS_PROD), autocommit=False)
    dev_conn  = pyodbc.connect(build_conn_str(SECRETS_DEV),  autocommit=False)
    prod_cursor = prod_conn.cursor()

    tables = REFERENCE_TABLES[:]
    if args.transactions:
        tables += TRANSACTION_TABLES
        print(f"Modo: referencia + transacciones (últimos {args.days} días)")
    else:
        print("Modo: solo tablas de referencia (usa --transactions para incluir datos financieros)")

    total = 0
    for table in tables:
        days = args.days if args.transactions and table in TRANSACTION_TABLES else None
        total += merge_table(prod_cursor, dev_conn, table, days)

    prod_conn.close()
    dev_conn.close()
    print(f"\nSync prod→dev completado: {total} filas totales procesadas.")


if __name__ == "__main__":
    main()
