# MediStock

A pharmacy inventory, prescription, and sales management web application for Ugandan pharmacies, built on **ASP.NET Core MVC + Entity Framework Core + SQLite**. The system runs entirely on the lecturer's laptop with one command. EF migrations and seed data apply automatically on first start.

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

# 4. Sign in with one of the pre-seeded accounts:
#    Admin       admin@medistock.local       Admin@123
#    Pharmacist  pharmacist@medistock.local  Pharma@123
#    Cashier     cashier@medistock.local     Cash@123

# To stop:        Ctrl+C
# To wipe data:   delete data/medistock.db, run again
```

## Pre-seeded test accounts

| Role        | Email                          | Password   |
|-------------|--------------------------------|------------|
| Admin       | admin@medistock.local          | Admin@123  |
| Pharmacist  | pharmacist@medistock.local     | Pharma@123 |
| Cashier     | cashier@medistock.local        | Cash@123   |

## Tools

Built with ASP.NET Core 10, EF Core 10, SQLite, ASP.NET Core Identity, QuestPDF, Serilog, Bootstrap 5, Chart.js, xUnit, Moq, FluentAssertions, FluentValidation.

## Author

```
Tinka Fahad
Registration No: 254430
BSc Data Science and AI — Year 2 Semester 1
Cavendish University Uganda — ASP.NET Core & C# Module
Academic Year 2025/2026
```
