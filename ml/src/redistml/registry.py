"""Registro de modelos em disco: versões imutáveis + ponteiro 'current' (rollback = apontar para outra versão)."""
from __future__ import annotations

import json
import os
from datetime import datetime, timezone
from pathlib import Path


class ModelRegistry:
    def __init__(self, root: str | Path):
        self.root = Path(root)

    def version_dir(self, version: str) -> Path:
        return self.root / "versions" / version

    def new_version(self, version: str) -> Path:
        d = self.version_dir(version)
        d.mkdir(parents=True, exist_ok=False)     # versões são imutáveis: nunca sobrescreve
        return d

    def set_current(self, version: str) -> None:
        if not self.version_dir(version).exists():
            raise FileNotFoundError(f"Versão inexistente: {version}")
        self.root.mkdir(parents=True, exist_ok=True)
        tmp = self.root / "current.json.tmp"
        tmp.write_text(json.dumps({"version": version, "set_at": datetime.now(timezone.utc).isoformat()}))
        os.replace(tmp, self.root / "current.json")  # troca atômica: leitores nunca veem arquivo pela metade

    def current_version(self) -> str | None:
        p = self.root / "current.json"
        return json.loads(p.read_text())["version"] if p.exists() else None

    def list_versions(self) -> list[str]:
        d = self.root / "versions"
        return sorted(x.name for x in d.iterdir()) if d.exists() else []
