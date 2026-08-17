# tools/

- `check_feature_status.py` — validates `README.md` and
  `engineering/ROADMAP.md` against `engineering/FEATURE_STATUS.json`,
  the machine-readable feature-status source of truth. Run in CI on
  every push (`python3 tools/check_feature_status.py`); exits nonzero on
  any drift so documentation can't claim a feature that isn't
  implemented (or forget one that is). Stdlib-only, no dependencies.

`dotnet build`/`dotnet test` against `Nyforge.sln` covers the rest of the
v0.x verification. This directory also reserves the place a future NUI
schema validator, `.nstudio` migrator, or CLI exporter would go (see
`engineering/ROADMAP.md` and `engineering/NFS-006`).
