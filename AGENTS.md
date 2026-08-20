# MulaiDariKecil ("Tjoean") — Agent Workflow & Branch Model

Educational investment-simulation game. Blazor WASM + ASP.NET Core 8 + SignalR, in-memory storage. **UI text is Bahasa Indonesia; code and comments are English.**

## Branch model (STRICT — read before doing anything)

| Branch | Purpose | Rules |
|---|---|---|
| `master` | **PRODUCTION — human-approved truth.** | NEVER commit or push directly. Protected: requires PR + review. |
| `hermes-staging` | **AI integration/staging.** All AI work lands here first. | Merges only via PR from `hermes/*`. No force-push/delete. |
| `hermes/<task-slug>` | One branch per AI task. | Branch from `hermes-staging` (not master). |

## Rules for every agent

1. **Never** commit to or push `master`. If a task "just needs a quick fix on master", stop — it goes through `hermes-staging`.
2. Create `hermes/<task-slug>` from `hermes-staging` for your task (e.g. `hermes/52-fix-doc-drift`).
3. One branch per task. Open a PR targeting `hermes-staging` and reference the issue number.
4. Commit style: `type: subject` (`feat:`/`fix:`/`refactor:`/`docs:`/`chore:`).
5. Don't touch other agents' branches or the user's existing `<n>-<slug>` branches.

## Build & run

- Requires .NET 8 SDK.
- Build (from repo root): `dotnet build`
- Run: `cd Server && dotnet run` → http://localhost:5000
- Test: Playwright against a running instance.

## Conventions

- **Localization** — single mechanism only: `Lang.T("KEY")` backed by `Client/wwwroot/localization/*.json` (4 locales). Do NOT add `GetText(...)`, inline ternaries, or hardcoded UI strings.
- **Design tokens** — `Client/wwwroot/css/app.css`: use CSS custom properties (tokens); no new magic hex/px values; no `!important`.
- **Accessibility** — every interactive element is a real `<button>` (or `role` + `tabindex` + keydown). Label all inputs. Use `aria-*`; modals use `role="dialog"` + focus trap.
- **Taxes** — simulation taxes are configurable via appsettings/env (see `TaxEnabled` flag). Keep the on/off switch, don't hardcode.
