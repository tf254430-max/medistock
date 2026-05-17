# MediStock

A pharmacy inventory, prescription, and sales management web application for Ugandan pharmacies, built on **ASP.NET Core MVC + Entity Framework Core + SQLite**. The system runs on a single laptop with one command. EF migrations and seed data apply automatically on first start, so a fresh clone boots straight into a working UI with three pre-seeded test accounts ready to sign in.

---

## Getting started

```bash
# Prerequisites: .NET SDK installed (https://dotnet.microsoft.com/download)

# 1. Clone
git clone https://github.com/tf254430-max/medistock.git
cd medistock

# 2. Run (restores packages on first run; ~60 seconds)
dotnet run --project src/MediStock.Web

# 3. Open the application
#    http://localhost:5000

# 4. Sign in with one of the pre-seeded accounts (table below).

# Stop:        Ctrl+C
# Reset data:  delete data/medistock.db, run again
```

## Pre-seeded test accounts

| Role        | Email                          | Password   |
|-------------|--------------------------------|------------|
| Admin       | admin@medistock.local          | Admin@123  |
| Pharmacist  | pharmacist@medistock.local     | Pharma@123 |
| Cashier     | cashier@medistock.local        | Cash@123   |

Three users, three categories of permissions:

- **Admin** sees everything — dashboard, till, full inventory, prescriptions, customers, reports, user management, audit log, settings.
- **Pharmacist** manages drugs / batches / categories / suppliers / customers / prescriptions but cannot run sales or admin functions.
- **Cashier** opens the till, takes payments, and views their own receipts.

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full layer diagram and request lifecycle. ER diagram in Mermaid: [docs/erd.md](docs/erd.md). Decision log in [docs/PROJECT_JOURNAL.md](docs/PROJECT_JOURNAL.md).

## Tech stack

ASP.NET Core, EF Core, SQLite, ASP.NET Core Identity, QuestPDF, Serilog, Bootstrap 5, Chart.js, xUnit, Moq, FluentAssertions, FluentValidation.

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
