# MediStock — Architecture

MediStock is a layered ASP.NET Core MVC application. Each layer has a single responsibility and depends only on the layers below it. Cross-cutting concerns (logging, auth, audit) are wired in `Program.cs`.

```
+------------------------------------------------------------------+
|                          MediStock.Web                           |
|  Controllers · Razor views · view models · middleware · DI root  |
|  (HTTP, antiforgery, Identity UI, JSON serialization, Serilog)   |
+--------------------------------+---------------------------------+
                                 |
                                 v
+------------------------------------------------------------------+
|                       MediStock.Application                      |
|  Service interfaces · service implementations · DTOs · validators|
|  (FluentValidation, business rules, transactional sale flow,    |
|   QuestPDF receipt rendering, notification cache + bg refresh)   |
+--------------------------------+---------------------------------+
                                 |
                                 v
+------------------------------------------------------------------+
|                     MediStock.Infrastructure                     |
|  AppDbContext · EF Core configurations · migrations · seed data  |
|  StartupExtensions (MigrateAndSeedAsync runs on app start)       |
+--------------------------------+---------------------------------+
                                 |
                                 v
+------------------------------------------------------------------+
|                         MediStock.Domain                         |
|  Entity classes (14) · enums · domain exceptions                 |
|  Pure C# types — no framework or persistence dependencies *      |
+------------------------------------------------------------------+

* AppUser extends IdentityUser, so the Domain assembly carries a thin
  reference to Microsoft.AspNetCore.Identity.EntityFrameworkCore so
  Identity-managed identity properties (UserName, Email, etc.) live
  alongside the rest of the domain model — pragmatic compromise for an
  academic project.
```

## Project references

```
MediStock.Web         -> Application, Infrastructure, Domain
MediStock.Application -> Infrastructure, Domain (+ ASP.NET Core framework reference for IHttpContextAccessor and Identity)
MediStock.Infrastructure -> Domain (+ ASP.NET Core framework reference for WebApplication startup helpers)
MediStock.Domain      -> (no project deps; only Microsoft.AspNetCore.Identity.EntityFrameworkCore)

tests/MediStock.UnitTests        -> Application, Domain, Infrastructure
tests/MediStock.IntegrationTests -> Web, Application, Domain, Infrastructure
```

> Note on direction: the proposal's §9.3 puts `SaleService.cs` in
> `MediStock.Application/Services/` and uses `_db.Database.BeginTransactionAsync(...)`.
> To keep that code verbatim, Application references Infrastructure (rather than
> the other way around). Web is then the composition root that ties everything together.

## Request lifecycle (cashier completes a sale)

1. Browser POSTs `/Sales/Checkout` with a JSON cart payload and an
   `RequestVerificationToken` header.
2. ASP.NET Core routing matches `SalesController.Checkout`. The global
   `AutoValidateAntiforgeryTokenAttribute` rejects requests without a valid
   token; cookie auth verifies the user is in `Admin` or `Cashier`.
3. `AuditEnricherMiddleware` stashes `userId` and `IpAddress` into
   `HttpContext.Items` so the audit service can read them later.
4. `JsonStringEnumConverter` deserializes `"Cash"` / `"MtnMoMo"` /
   `"AirtelMoney"` into the `PaymentMethod` enum.
5. `SaleService.ValidateCartAsync` runs first — checks each line for stock,
   active flag, and Rx-with-prescription requirement. Returns a structured
   error list without throwing.
6. `SaleService.CompleteSaleAsync` opens an EF Core transaction, draws batches
   FIFO by `ExpiryDate`, builds `SaleItem` + `StockMovement` rows, writes the
   `Sale`, calls `IReceiptPdfService.GenerateAsync` to emit a QuestPDF receipt
   to `data/receipts/{ReceiptNumber}.pdf`, writes one `AuditLog` row, and
   commits. If the prescription was loaded, its `Status` flips to `Dispensed`
   inside the same transaction.
7. The controller returns `{ ok: true, saleId, receiptNumber, receiptUrl }`.
   The till JS opens the PDF in a new tab and resets for the next sale.

## Key cross-cutting concerns

| Concern        | Where it lives                                                 |
|----------------|----------------------------------------------------------------|
| Auth           | ASP.NET Core Identity + `[Authorize(Roles=...)]` on controllers |
| CSRF           | Global `AutoValidateAntiforgeryTokenAttribute`; JSON POSTs use  the `RequestVerificationToken` header |
| Logging        | Serilog (console + rolling file in `logs/`)                    |
| Audit          | `IAuditService` writes `AuditLog` rows from any service that  changes state |
| Notifications  | `INotificationCache` (singleton `ConcurrentDictionary`); `NotificationRefreshService` `BackgroundService` runs on startup + every 6 h |
| PDF rendering  | QuestPDF (Community licence) — receipts and report exports     |
| Validation     | FluentValidation (server-side + jQuery client adapters)        |

## Data layer

- SQLite, single file at `data/medistock.db`, created automatically on first run.
- `Program.PinWorkingDirectoryToRepoRoot()` walks up from `AppContext.BaseDirectory`
  looking for `MediStock.sln` / `MediStock.slnx` and pins the working directory there,
  so relative paths (`data/medistock.db`, `data/receipts/`, `logs/`) always resolve to
  the same place regardless of where `dotnet run` was invoked from.
- All decimal fields use `HasConversion<string>()` to preserve precision in SQLite
  per Section 5.6 of the proposal.
- Migrations are committed under `src/MediStock.Infrastructure/Data/Migrations/`
  and applied on startup by `StartupExtensions.MigrateAndSeedAsync`.
