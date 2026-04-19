"""
Минимальный ACP-клиент: поднимает локальный echo_agent.py и шлёт один prompt.

Запуск из каталога AcpSmoke:
  pip install -r requirements.txt
  python smoke_client.py
"""

from __future__ import annotations

import asyncio
import sys
from pathlib import Path
from typing import Any

from acp import spawn_agent_process, text_block
from acp.interfaces import Client


class SmokeClient(Client):
    """Заглушка прав доступа; печатает поток session_update от агента."""

    async def request_permission(self, options, session_id, tool_call, **kwargs: Any):
        return {"outcome": {"outcome": "cancelled"}}

    async def session_update(self, session_id, update, **kwargs):
        print("session_update:", session_id, update)


async def main() -> None:
    root = Path(__file__).resolve().parent
    agent_script = root / "echo_agent.py"
    if not agent_script.is_file():
        print("Не найден echo_agent.py рядом со smoke_client.py", file=sys.stderr)
        sys.exit(1)

    async with spawn_agent_process(SmokeClient(), sys.executable, str(agent_script)) as (conn, _proc):
        await conn.initialize(protocol_version=1)
        session = await conn.new_session(cwd=str(root), mcp_servers=[])
        await conn.prompt(
            session_id=session.session_id,
            prompt=[text_block("Hello ACP from Cascade AcpSmoke")],
        )
    print("ACP smoke OK")


if __name__ == "__main__":
    asyncio.run(main())
