#!/usr/bin/env python3
"""Exporta SUPABASE_DB_URL como variables PG* para los pasos de psql de una GitHub Action.

Acepta los mismos formatos que el keepalive (URI postgresql://… o cadena Npgsql) y oculta la contraseña en el log.
"""
from __future__ import annotations

import os

from supabase_conninfo import parse_secret

host, port, database, username, password = parse_secret(os.environ.get("SUPABASE_DB_URL", ""))
print(f"::add-mask::{password}")

with open(os.environ["GITHUB_ENV"], "a", encoding="utf-8") as env:
    env.write(f"PGHOST={host}\n")
    env.write(f"PGPORT={port}\n")
    env.write(f"PGDATABASE={database}\n")
    env.write(f"PGUSER={username}\n")
    env.write(f"PGPASSWORD={password}\n")
    env.write("PGSSLMODE=require\n")
