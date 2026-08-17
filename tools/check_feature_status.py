#!/usr/bin/env python3
"""Validate README.md and engineering/ROADMAP.md against engineering/FEATURE_STATUS.json.

FEATURE_STATUS.json is the single machine-readable source of truth for
Nyforge feature status (added 2026-08-17 after the multi-select claim in
the README drifted from the actual implementation). Rules:

  * status "implemented"  -> the feature's readmePhrase MUST appear in the
                             README "What's built so far" section, and its
                             roadmapPhrase MUST be an unchecked-to-checked
                             ("[x]") roadmap line.
  * status "partial"      -> readmePhrase must not overclaim (same rule as
                             not-started), roadmap line must be "[ ]".
  * status "not-started"  -> the feature's readmePhrase MUST NOT appear in
                             the README "What's built so far" section, and
                             its roadmapPhrase must be "[ ]" (or a plain
                             bullet, treated as unchecked).

Run from anywhere: `python3 tools/check_feature_status.py`. Exits nonzero
on any violation so CI (build.yml) fails when docs drift.
"""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
STATUS_FILE = ROOT / "engineering" / "FEATURE_STATUS.json"
README = ROOT / "README.md"
ROADMAP = ROOT / "engineering" / "ROADMAP.md"

VALID_STATUSES = {"implemented", "partial", "not-started"}


def readme_claims_section(text: str) -> str:
    """The 'What's built so far' section — where claims live. The 'What's
    still not there yet' section is explicitly honest and must NOT count
    as a claim (that is the whole point of it)."""
    start = text.find("## What's built so far")
    end = text.find("## What's still not there yet")
    if start == -1 or end == -1 or end <= start:
        return text  # section markers missing: fall back to whole file
    return text[start:end]


def find_roadmap_line(lines, phrase: str):
    """All roadmap lines containing the phrase (case-insensitive)."""
    return [
        i for i, line in enumerate(lines)
        if phrase.lower() in line.lower()
    ]


def line_is_checked(line: str) -> bool:
    stripped = line.strip()
    if stripped.startswith("- [x]") or stripped.startswith("- [X]"):
        return True
    return False


def main() -> int:
    data = json.loads(STATUS_FILE.read_text(encoding="utf-8"))
    if data.get("schemaVersion") != "1.0":
        print(f"FAIL: unexpected schemaVersion {data.get('schemaVersion')!r}")
        return 1

    features = data.get("features", [])
    if not isinstance(features, list) or not features:
        print("FAIL: 'features' must be a non-empty list")
        return 1

    readme_text = README.read_text(encoding="utf-8")
    claims = readme_claims_section(readme_text)
    roadmap_lines = ROADMAP.read_text(encoding="utf-8").splitlines()

    violations = []
    seen_ids = set()

    for f in features:
        fid = f.get("id")
        if not fid or fid in seen_ids:
            violations.append(f"feature without unique 'id': {f!r}")
            continue
        seen_ids.add(fid)

        status = f.get("status")
        if status not in VALID_STATUSES:
            violations.append(f"{fid}: invalid status {status!r} "
                              f"(expected one of {sorted(VALID_STATUSES)})")
            continue

        rp = f.get("readmePhrase")
        if rp:
            present = rp.lower() in claims.lower()
            if status == "implemented" and not present:
                violations.append(
                    f"{fid}: status is 'implemented' but README 'What's built "
                    f"so far' does not mention {rp!r} — update README or the "
                    f"status entry")
            elif status in ("partial", "not-started") and present:
                violations.append(
                    f"{fid}: status is {status!r} but README 'What's built "
                    f"so far' claims {rp!r} — update README or the status entry")

        mp = f.get("roadmapPhrase")
        if mp:
            matches = find_roadmap_line(roadmap_lines, mp)
            if not matches:
                violations.append(
                    f"{fid}: roadmapPhrase {mp!r} not found in "
                    f"engineering/ROADMAP.md — add or fix the roadmap item")
            elif len(matches) > 1:
                violations.append(
                    f"{fid}: roadmapPhrase {mp!r} matches {len(matches)} "
                    f"lines ({[roadmap_lines[i].strip()[:60] for i in matches]}) "
                    f"— make the phrase unambiguous")
            else:
                line = roadmap_lines[matches[0]]
                checked = line_is_checked(line)
                if status == "implemented" and not checked:
                    violations.append(
                        f"{fid}: status is 'implemented' but ROADMAP line is "
                        f"unchecked: {line.strip()}")
                elif status in ("partial", "not-started") and checked:
                    violations.append(
                        f"{fid}: status is {status!r} but ROADMAP line is "
                        f"checked: {line.strip()}")

    if "FEATURE_STATUS" not in readme_text:
        violations.append(
            "README.md must reference FEATURE_STATUS (the machine-readable "
            "feature-status source) so the source of truth is discoverable")

    for f in features:
        fid = f.get("id", "?")
        status = f.get("status", "?")
        introduced = f.get("introduced") or "—"
        print(f"  [{status:>10}] {fid:<24} introduced {introduced}")

    if violations:
        print(f"\n{len(violations)} violation(s):")
        for v in violations:
            print(f"  FAIL: {v}")
        return 1

    print(f"\nOK: {len(features)} features, README + ROADMAP consistent with "
          f"FEATURE_STATUS.json")
    return 0


if __name__ == "__main__":
    sys.exit(main())
