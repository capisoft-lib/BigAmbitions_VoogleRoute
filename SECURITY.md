# Security & privacy

This repository is **public**. Do not commit:

| Category | Examples |
|----------|----------|
| Credentials | API keys, tokens, `.env`, Steam Web API keys |
| Personal paths | `C:\Users\…`, machine-specific `local.paths.json` |
| Game dumps | `il2cpp_dump/`, raw waypoint dumps not intended for release |
| Build artifacts | `*.dll`, `Output/`, Unity `Library/` |
| Player data | `line_color.txt`, `WaypointDumps/` |

## Shipped data

Route graph CSV is **intentional mod data** (synced from the [PathFinding](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute.PathFinding) submodule into `Data/`). It is not a secret, but it is tied to a specific game map version — regenerate in PathFinding after major city updates.

## Reporting

Open a [GitHub issue](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/issues) if you find credentials or personal data in the repo history.
