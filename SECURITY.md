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

`VoogleRoute/Data/big_ambitions_enhanced_routes.csv` is **intentional mod data** (derived traffic graph for routing). It is not a secret, but it is tied to a specific game map version — regenerate after major city updates.

## Reporting

Open a [GitHub issue](https://github.com/capisoft-lib/BigAmbitions_VoogleRoute/issues) if you find credentials or personal data in the repo history.
