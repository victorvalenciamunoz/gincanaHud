#!/usr/bin/env python3
"""Lee SUPABASE_DB_URL y hace ping de actividad (DDL mínimo + UPDATE)."""
from __future__ import annotations

import os
import re
import subprocess
import sys
import urllib.parse
import urllib.request

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


def ping_via_psql(host: str, port: str, database: str, username: str, password: str) -> int:
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

    # Solo keepalive_ping: no toca tablas de la app (evita fallos a medias / "BD rara").
    sql = """
create table if not exists public.keepalive_ping (
  id smallint primary key default 1,
  last_ping timestamptz not null default now()
);
insert into public.keepalive_ping (id) values (1) on conflict (id) do nothing;
update public.keepalive_ping set last_ping = now() where id = 1;
alter table public.keepalive_ping enable row level security;
do $$ begin
  create policy keepalive_ping_anon_select
    on public.keepalive_ping for select to anon using (true);
exception when duplicate_object then null;
end $$;
grant usage on schema public to anon, authenticated;
grant select on public.keepalive_ping to anon, authenticated;
select id, last_ping from public.keepalive_ping where id = 1;
"""
    return subprocess.run(
        ["psql", "-v", "ON_ERROR_STOP=1", "-c", sql],
        env=env,
        check=False,
    ).returncode


def ping_via_rest() -> None:
    """Actividad 'de usuario' vía PostgREST (opcional; secrets SUPABASE_URL + SUPABASE_ANON_KEY)."""
    base = os.environ.get("SUPABASE_URL", "").strip().rstrip("/")
    key = os.environ.get("SUPABASE_ANON_KEY", "").strip()
    if not base or not key:
        print("REST opcional omitido (sin SUPABASE_URL / SUPABASE_ANON_KEY).", file=sys.stderr)
        return

    url = f"{base}/rest/v1/keepalive_ping?select=id,last_ping&limit=1"
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
            body = resp.read().decode("utf-8", errors="replace")
            print(f"REST OK status={resp.status} body={body[:200]}", file=sys.stderr)
    except Exception as ex:
        # No tumba el job: el ping SQL ya corrió. Avisa para configurar RLS/anon.
        print(f"REST ping falló (¿RLS/anon?): {ex}", file=sys.stderr)


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
        f"Ping host={host} port={port} db={database} user={username} password_len={len(password)}",
        file=sys.stderr,
    )

    code = ping_via_psql(host, port, database, username, password)
    if code != 0:
        raise SystemExit(code)

    ping_via_rest()
    raise SystemExit(0)


if __name__ == "__main__":
    main()
