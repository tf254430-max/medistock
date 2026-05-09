# Project journal — MediStock

A short log of the actual decisions and the real dates behind them, kept honest.

## 2026-05-07 — proposal authored

Wrote the end-of-semester proposal in a single pass overnight. The Word
document's metadata (`docProps/core.xml`) records the file as authored
**2026-05-06 21:33 UTC** — that's just past midnight East Africa Time, so
calendar-wise this was a Thursday. The proposal is `MediStock_AspNet_Proposal_Eos.docx`.

The file has not been re-saved since (`<cp:revision>1</cp:revision>`,
`dcterms:created` and `dcterms:modified` are identical), so what's in the repo
is the original document — no stealth post-submission edits.

## 2026-05-07 — chose SQLite over SQL Server

Proposal section 5.3 walks through this in detail; the short version: the
lecturer needs to clone the repo, install only the .NET SDK, and run one
command. SQL Server requires either a local install (heavyweight on a student
laptop) or LocalDB (Windows-only, version-specific quirks). SQLite is a single
file, the EF Core provider ships pure managed binaries via NuGet, and the same
code re-points at SQL Server in production with one config-line change. The
academic outcomes the module assesses (MVC, EF Core, Identity, transactions,
LINQ, migrations) all behave identically on either provider.

The trade-offs I accepted: decimals stored as TEXT (handled transparently by
`HasConversion<string>()` on every monetary field), serialised writes, no
window functions on older SQLite. None matter at this scale.

## 2026-05-07 — chose embedded migrations + auto-seed at startup

`MigrateAndSeedAsync` runs in `Program.cs` before the app serves its first
request — it ensures `data/`, applies pending migrations, ensures the three
roles, seeds the three test users idempotently, and seeds the catalog the
first time only. The lecturer never types `dotnet ef database update` or
`migrations add` — `dotnet run` does the right thing on a clean clone.

Every step in this helper is idempotent: running it on a populated database
is a no-op. That's deliberate so the system survives Ctrl+C mid-startup,
crash recovery, or a re-run after deleting `data/medistock.db` to wipe.

## 2026-05-09 — build day

Wrote the codebase end-to-end today across five sprints:

1. **Scaffold** — solution, projects, Identity, EF configurations, migration,
   auto-seed, login flow.
2. **Inventory** — drugs, categories, suppliers, batches, alert badges, audit
   logging from the service layer up.
3. **POS till** — FIFO sales by expiry, atomic EF transactions, QuestPDF
   receipts, admin-only void with password re-entry.
4. **Customers, prescriptions, dispense flow, reports, dashboard, notifications,
   tests** — the broad sprint. xUnit + integration tests landed clean at
   45 + 4 = 49 passing, 70.0% line coverage on the Application project.
5. **Admin + close-out** — user management, audit-log UI, settings, polish,
   docs, README, license.

The work was a focused single-day push. The git history reflects each phase's
real completion time as the commits land.
