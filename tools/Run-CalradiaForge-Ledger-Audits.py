"""Run the documentation integrity gates used by GitHub Actions."""

from __future__ import annotations

import sys
from pathlib import Path


def main() -> int:
    repository_root = str(Path(__file__).resolve().parent.parent)
    if repository_root not in sys.path:
        sys.path.insert(0, repository_root)

    from agents.tools import (
        audit_code_smells,
        audit_documentation_parity,
        audit_ledger_integrity,
        audit_section_playbooks,
    )

    checks = (
        ("Ledger integrity", audit_ledger_integrity(raw=True), "PASSED"),
        ("Section playbooks", audit_section_playbooks(raw=True), "PASSED"),
        ("Code smells (static source checks)", audit_code_smells(raw=True), "PASSED"),
        (
            "Documentation parity",
            audit_documentation_parity(raw=True),
            "All English technical documents have synchronized Spanish counterparts!",
        ),
    )
    failed = False
    for name, report, required_marker in checks:
        print(f"=== {name} ===")
        print(report)
        if required_marker is not None and required_marker not in report:
            failed = True
            print(f"ERROR: {name} did not report its required success marker: {required_marker}")
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
