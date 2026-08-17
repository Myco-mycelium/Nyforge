#!/usr/bin/env python3
"""check_contracts_synced.py — verify that the generated C# contract
tables match the vendored Nyrqis API Registry.

This is a CI gate (run from .github/workflows/build.yml).  It
re-generates the C# files in memory and compares them byte-for-byte
with what's checked in.  Any drift — someone editing the .cs files by
hand instead of the registry — causes a build failure.

Exit codes:
    0  all files in sync
    1  drift detected (files need regeneration)
"""

from __future__ import annotations

import os
import sys

# Ensure tools/ is importable.
TOOLS_DIR = os.path.dirname(os.path.abspath(__file__))
if TOOLS_DIR not in sys.path:
    sys.path.insert(0, TOOLS_DIR)

from generate_contracts import (
    CONTRACTS_OUT,
    SYSTEM_ACTIONS_OUT,
    NYRQIS_UPSTREAM,
    generate_contracts,
    generate_system_actions,
    load_registry,
    check_upstream,
)


def _read(path: str) -> str:
    with open(path, "r", encoding="utf-8") as fh:
        return fh.read()


def main() -> None:
    components, system_actions = load_registry()
    expected_contracts = generate_contracts(components)
    expected_actions = generate_system_actions(system_actions)

    errors: list[str] = []

    for label, path, expected in [
        ("ComponentContracts.cs", CONTRACTS_OUT, expected_contracts),
        ("NuiSystemActions.cs", SYSTEM_ACTIONS_OUT, expected_actions),
    ]:
        if not os.path.isfile(path):
            errors.append(f"{label}: file missing — run generate_contracts.py")
            continue
        actual = _read(path)
        if actual != expected:
            errors.append(
                f"{label}: drift detected — the file differs from what "
                f"generate_contracts.py produces from the registry.  "
                f"Run:  python tools/generate_contracts.py"
            )

    # Warn (non-fatal) if the vendored copy doesn't match upstream Nyrqis
    # when the sibling checkout is available.
    check_upstream()

    if errors:
        for msg in errors:
            print(f"ERROR: {msg}", file=sys.stderr)
        sys.exit(1)

    print("all contracts in sync with registry")


if __name__ == "__main__":
    main()
