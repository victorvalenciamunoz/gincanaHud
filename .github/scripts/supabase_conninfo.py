#!/usr/bin/env python3
"""Normaliza SUPABASE_DB_URL (URI o Npgsql) a conninfo de libpq para psql."""
from __future__ import annotations

import os
import sys
import urllib.parse


def fail(msg: str) -> None:
    print(msg, file=sys.stderr)
    raise SystemExit(1)


def validate_pooler_user(host: str, username: str) -> None:
    if "pooler.supabase.com" not in host:
        return
    if username == "postgres" or "." not in username:
        fail(
            "En el pooler el usuario debe ser postgres.<project-ref>, "
            f"pero llegó '{username}'.\n"
            "Ejemplo Npgsql:\n"
            "Host=aws-1-eu-west-3.pooler.supabase.com;Port=5432;Database=postgres;"
            "Username=postgres.gzcrrlazcodfhhjjntmn;Password=...;"
            "SSL Mode=Require;Trust Server Certificate=true"
        )


def quote_libpq(value: str) -> str:
    # Siempre entre comillas: passwords con @ # ; espacios, etc.
    escaped = value.replace("\\", "\\\\").replace("'", "\\'")
    return f"'{escaped}'"


def to_libpq(host: str, port: str, database: str, username: str, password: str) -> str:
    validate_pooler_user(host, username)
    if not password:
        fail("Falta la password en el secret.")
    parts = {
        "host": host,
        "port": port,
        "dbname": database,
        "user": username,
        "password": password,
        "sslmode": "require",
    }
    return " ".join(f"{k}={quote_libpq(v)}" for k, v in parts.items())


def from_uri(raw: str) -> str:
    u = urllib.parse.urlparse(raw)
    host = u.hostname or ""
    port = str(u.port or 5432)
    database = (u.path or "/postgres").lstrip("/") or "postgres"
    username = urllib.parse.unquote(u.username or "")
    password = urllib.parse.unquote(u.password or "")
    return to_libpq(host, port, database, username, password)


def from_npgsql(raw: str) -> str:
    parts: dict[str, str] = {}
    for segment in raw.split(";"):
        segment = segment.strip()
        if not segment or "=" not in segment:
            continue
        key, value = segment.split("=", 1)
        parts[key.strip().lower()] = value.strip()

    missing = [k for k in ("host", "username", "password") if not parts.get(k)]
    if missing:
        fail(f"Cadena Npgsql incompleta; faltan: {', '.join(missing)}")

    return to_libpq(
        parts["host"],
        parts.get("port") or "5432",
        parts.get("database") or "postgres",
        parts["username"],
        parts["password"],
    )


def main() -> None:
    raw = os.environ.get("SUPABASE_DB_URL", "").strip().strip('"').strip("'")
    if not raw:
        fail("Falta el secret SUPABASE_DB_URL.")

    if raw.startswith("postgres://") or raw.startswith("postgresql://"):
        conninfo = from_uri(raw)
    elif ";" in raw and "=" in raw:
        conninfo = from_npgsql(raw)
    else:
        fail(
            "Formato de SUPABASE_DB_URL no reconocido. Usa URI postgresql://… "
            "o cadena Npgsql Host=…;Username=postgres.<ref>;Password=…"
        )

    # Diagnóstico sin password
    probe = dict(part.split("=", 1) for part in conninfo.split(" ") if "=" in part and not part.startswith("password="))
    print(
        f"OK: host={probe.get('host')} port={probe.get('port')} "
        f"user={probe.get('user')} db={probe.get('dbname')}",
        file=sys.stderr,
    )

    out = os.environ.get("GITHUB_ENV")
    if out:
        with open(out, "a", encoding="utf-8") as fh:
            fh.write(f"PG_CONNINFO<<EOF\n{conninfo}\nEOF\n")
    else:
        print(conninfo)


if __name__ == "__main__":
    main()
