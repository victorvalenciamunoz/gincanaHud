#!/usr/bin/env python3
"""Genera actividad de BD suficiente para el free tier de Supabase.

Un solo SELECT/UPDATE no basta: la doc pide varias peticiones de usuario
al día. Este script hace varias rondas SQL y, si hay secrets, varias llamadas REST.
"""
from __future__ import annotations

import os
import re
import subprocess
import sys
import time
import urllib.error
import urllib.parse
import urllib.request

# Cuántas rondas por run (cada una = varias queries).
SQL_ROUNDS = 8
REST_ROUNDS = 8

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
        return (
            u.hostname or "",
            str(u.port or 5432),
            (u.path or "/postgres").lstrip("/") or "postgres",
            urllib.parse.unquote(u.username or ""),
            urllib.parse.unquote(u.password or ""),
        )

    if ";" in raw and "=" in raw:
        return parse_npgsql(raw)

    fail(
        "Formato de SUPABASE_DB_URL no reconocido. Usa URI postgresql://… "
        "o Npgsql Host=…;Username=postgres.<ref>;Password=…"
    )


def ensure_keepalive_table(env: dict[str, str]) -> None:
    sql = """
create table if not exists public.keepalive_ping (
  id smallint primary key default 1,
  last_ping timestamptz not null default now(),
  hit_count bigint not null default 0
);
alter table public.keepalive_ping add column if not exists hit_count bigint not null default 0;
insert into public.keepalive_ping (id) values (1) on conflict (id) do nothing;
alter table public.keepalive_ping enable row level security;
do $$ begin
  create policy keepalive_ping_anon_select
    on public.keepalive_ping for select to anon using (true);
exception when duplicate_object then null;
end $$;
grant usage on schema public to anon, authenticated;
grant select on public.keepalive_ping to anon, authenticated;
"""
    result = subprocess.run(
        ["psql", "-v", "ON_ERROR_STOP=1", "-c", sql],
        env=env,
        check=False,
    )
    if result.returncode != 0:
        fail("No se pudo preparar la tabla keepalive_ping.")


def sql_activity_burst(env: dict[str, str], rounds: int) -> None:
    # Varias conexiones/rondas: más señales de "user database activity".
    for i in range(1, rounds + 1):
        sql = f"""
update public.keepalive_ping
  set last_ping = now(), hit_count = hit_count + 1
  where id = 1;
select id, last_ping, hit_count from public.keepalive_ping where id = 1;
select count(*) as keepalive_rows from public.keepalive_ping;
select now() as wall_clock_{i};
"""
        result = subprocess.run(
            ["psql", "-v", "ON_ERROR_STOP=1", "-c", sql],
            env=env,
            check=False,
        )
        if result.returncode != 0:
            fail(f"Fallo en ronda SQL {i}/{rounds}.")
        print(f"SQL round {i}/{rounds} OK", file=sys.stderr)
        time.sleep(0.4)


def rest_activity_burst(rounds: int) -> None:
    base = os.environ.get("SUPABASE_URL", "").strip().rstrip("/")
    key = os.environ.get("SUPABASE_ANON_KEY", "").strip()
    if not base or not key:
        print(
            "AVISO: sin SUPABASE_URL + SUPABASE_ANON_KEY el ping REST no corre. "
            "Supabase valora más las peticiones vía API; conviene añadir esos secrets.",
            file=sys.stderr,
        )
        return

    url = f"{base}/rest/v1/keepalive_ping?select=id,last_ping,hit_count&limit=1"
    ok = 0
    for i in range(1, rounds + 1):
        req = urllib.request.Request(
            url,
            headers={
                "apikey": key,
                "Authorization": f"Bearer {key}",
                "Accept": "application/json",
            },
            method="GET",
        )
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                _ = resp.read()
                ok += 1
                print(f"REST round {i}/{rounds} OK status={resp.status}", file=sys.stderr)
        except urllib.error.HTTPError as ex:
            body = ex.read().decode("utf-8", errors="replace")[:300]
            fail(f"REST round {i} HTTP {ex.code}: {body}")
        except Exception as ex:
            fail(f"REST round {i} falló: {ex}")
        time.sleep(0.3)

    print(f"REST burst complete ({ok}/{rounds})", file=sys.stderr)


def main() -> None:
    host, port, database, username, password = parse_secret(os.environ.get("SUPABASE_DB_URL", ""))

    if not password or password in (
        "[YOUR-PASSWORD]",
        "YOUR-PASSWORD",
        "TU_PASSWORD",
        "TU_PASSWORD_AQUI",
        "AQUI_LA_PASSWORD",
    ):
        fail("La password del secret parece un placeholder.")

    if "pooler.supabase.com" in host and (username == "postgres" or "." not in username):
        fail(
            f"Usuario del pooler incorrecto: '{username}'. "
            "Debe ser postgres.<project-ref>, p.ej. postgres.gzcrrlazcodfhhjjntmn"
        )

    print(f"::add-mask::{password}")
    print(
        f"Burst host={host} port={port} db={database} user={username} "
        f"sql_rounds={SQL_ROUNDS} rest_rounds={REST_ROUNDS}",
        file=sys.stderr,
    )

    env = os.environ.copy()
    env.update(
        {
            "PGHOST": host,
            "PGPORT": port,
            "PGDATABASE": database,
            "PGUSER": username,
            "PGPASSWORD": password,
            "PGSSLMODE": "require",
        }
    )

    ensure_keepalive_table(env)
    sql_activity_burst(env, SQL_ROUNDS)
    rest_activity_burst(REST_ROUNDS)
    print("Keepalive burst finished.", file=sys.stderr)


if __name__ == "__main__":
    main()
