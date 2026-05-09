# MediStock

A pharmacy inventory, prescription, and sales management web application for Ugandan pharmacies, built on **ASP.NET Core MVC + Entity Framework Core + SQLite**. The system runs entirely on the marker's laptop with one command. EF migrations and seed data apply automatically on first start, leaving a fully working UI with three pre-seeded test accounts ready to sign in.

> **Framework note:** This codebase is built on **.NET 10 LTS**. The original
> proposal specified .NET 8 LTS — both are LTS releases and the codebase is
> identical on either. The substitution was made because the build environment
> had only the .NET 10 SDK installed at scaffold time.

---

## Quick start (lecturer-facing)

```bash
# Prerequisites: .NET 10 SDK installed (https://dotnet.microsoft.com/download)
# Nothing else.

# 1. Clone
git clone https://github.com/tinkafahad/medistock.git
cd medistock

# 2. Run (downloads packages on first run; ~60 seconds)
dotnet run --project src/MediStock.Web

# 3. Open the application
#    http://localhost:5000

# 4. Sign in with one of the pre-seeded accounts (table below).

# To stop:        Ctrl+C
# To wipe data:   delete data/medistock.db, run again

# OPTIONAL (no SDK install required):
# Download MediStock-win-x64.exe from the GitHub Releases page,
# double-click, and the app starts. Browser opens to localhost:5000
# automatically.
```

## Pre-seeded test accounts

| Role        | Email                          | Password   |
|-------------|--------------------------------|------------|
| Admin       | admin@medistock.local          | Admin@123  |
| Pharmacist  | pharmacist@medistock.local     | Pharma@123 |
| Cashier     | cashier@medistock.local        | Cash@123   |

Three users, three categories of permissions:

- **Admin** sees everything — dashboard, till, full inventory, prescriptions,
  customers, reports, user management, audit log, settings.
- **Pharmacist** manages drugs / batches / categories / suppliers / customers /
  prescriptions but cannot run sales or admin functions.
- **Cashier** opens the till, takes payments, and views their own receipts.

## What's inside

- **Dashboard** with role-aware KPI cards, 30-day revenue line chart (Chart.js),
  top-5 drugs today, and recent sales feed.
- **Inventory** — drugs, categories, suppliers, batches with FIFO-by-expiry stock
  tracking, low-stock + 30-day-expiry alerts.
- **Point-of-sale till** — autocomplete search, cart, Cash / MTN MoMo / Airtel
  Money, F2/Enter/F4 keyboard shortcuts, atomic EF transaction, **PDF receipts
  via QuestPDF**.
- **Prescriptions** — Draft → Issued → Dispensed/Cancelled lifecycle. "Dispense"
  pre-fills the till cart and the prescription flips to **Dispensed** as part
  of the sale transaction.
- **Reports** — daily revenue, top drugs, profit margin, supplier spend, stock
  valuation, sales report with **CSV and PDF export**.
- **Notifications** — singleton `ConcurrentDictionary` cache populated by a
  6-hour `BackgroundService`; bell-icon dropdown shows the live count.
- **Audit log** — every state-changing action captured with user, IP, and
  before/after JSON; admin-only filterable UI with row-level diffs.
- **Admin** — user management with admin-password re-entry on sensitive ops,
  pharmacy settings (name / VAT rate / currency).
- **Tests** — 45 unit tests + 4 integration tests, **>70% line coverage** on
  `MediStock.Application`.

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full layer diagram and
request lifecycle. ER diagram in Mermaid: [docs/erd.md](docs/erd.md).

## Tools

Built with ASP.NET Core 10, EF Core 10, SQLite, ASP.NET Core Identity, QuestPDF, Serilog, Bootstrap 5, Chart.js, xUnit, Moq, FluentAssertions, FluentValidation.

## Demo video

See [docs/demo-video.md](docs/demo-video.md) (link added after recording).

## License

[MIT](LICENSE) — Tinka Fahad, 2026.

## Author

```
Tinka Fahad
Registration No: 254430
BSc Data Science and AI — Year 2 Semester 1
Cavendish University Uganda — ASP.NET Core & C# Module
Academic Year 2025/2026
```
