#!/usr/bin/env python3
"""Lee SUPABASE_DB_URL (URI o Npgsql) y escribe PG* en GITHUB_ENV para psql."""
from __future__ import annotations

import os
import re
import sys
import urllib.parse

KNOWN_NPGSQL_KEYS = (
    "host",
    "port",
    "database",
    "username",
    "password",
    "ssl mode",
    "trust server certificate",
    "sslmode",
)


def fail(msg: str) -> None:
    print(msg, file=sys.stderr)
    raise SystemExit(1)


def parse_npgsql(raw: str) -> tuple[str, str, str, str, str]:
    """Parsea Npgsql sin partir la password si contiene ';'."""
    parts: dict[str, str] = {}
    pattern = re.compile(
        r"(?i)(?:^|;)\s*("
        + "|".join(re.escape(k) for k in KNOWN_NPGSQL_KEYS)
        + r")\s*="
    )
    matches = list(pattern.finditer(raw))
    if not matches:
        fail("No se reconocen claves Npgsql (Host, Username, Password, …).")

    for i, match in enumerate(matches):
        key = match.group(1).strip().lower()
        value_start = match.end()
        value_end = matches[i + 1].start() if i + 1 < len(matches) else len(raw)
        value = raw[value_start:value_end].strip().rstrip(";").strip()
        parts[key] = value

    username = parts.get("username", "")
    password = parts.get("password", "")
    host = parts.get("host", "")
    missing = [k for k, v in (("host", host), ("username", username), ("password", password)) if not v]
    if missing:
        fail(f"Cadena Npgsql incompleta; faltan: {', '.join(missing)}")

    return (
        host,
        parts.get("port") or "5432",
        parts.get("database") or "postgres",
        username,
        password,
    )


def parse_secret(raw: str) -> tuple[str, str, str, str, str]:
    raw = raw.strip().strip('"').strip("'")
    if not raw:
        fail("Falta el secret SUPABASE_DB_URL.")

    if raw.startswith("postgres://") or raw.startswith("postgresql://"):
        u = urllib.parse.urlparse(raw)
        host = u.hostname or ""
        port = str(u.port or 5432)
        database = (u.path or "/postgres").lstrip("/") or "postgres"
        username = urllib.parse.unquote(u.username or "")
        password = urllib.parse.unquote(u.password or "")
        return host, port, database, username, password

    if ";" in raw and "=" in raw:
        return parse_npgsql(raw)

    fail(
        "Formato de SUPABASE_DB_URL no reconocido. Usa URI postgresql://… "
        "o Npgsql Host=…;Username=postgres.<ref>;Password=…"
    )


def main() -> None:
    host, port, database, username, password = parse_secret(os.environ.get("SUPABASE_DB_URL", ""))

    if not password or password in ("[YOUR-PASSWORD]", "YOUR-PASSWORD", "TU_PASSWORD", "TU_PASSWORD_AQUI", "AQUI_LA_PASSWORD"):
        fail("La password del secret parece un placeholder. Pon la password real de la base de datos.")

    if "pooler.supabase.com" in host and (username == "postgres" or "." not in username):
        fail(
            f"Usuario del pooler incorrecto: '{username}'. "
            "Debe ser postgres.<project-ref>, p.ej. postgres.gzcrrlazcodfhhjjntmn"
        )

    print(
        f"Parsed host={host} port={port} db={database} user={username} password_len={len(password)}",
        file=sys.stderr,
    )

    github_env = os.environ.get("GITHUB_ENV")
    if not github_env:
        fail("GITHUB_ENV no está definido (¿fuera de Actions?).")

    print(f"::add-mask::{password}")

    with open(github_env, "a", encoding="utf-8") as fh:
        fh.write(f"PGHOST={host}\n")
        fh.write(f"PGPORT={port}\n")
        fh.write(f"PGDATABASE={database}\n")
        fh.write(f"PGUSER={username}\n")
        fh.write("PGPASSWORD<<EOF\n")
        fh.write(f"{password}\n")
        fh.write("EOF\n")
        fh.write("PGSSLMODE=require\n")


if __name__ == "__main__":
    main()
