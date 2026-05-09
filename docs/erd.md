# MediStock — Database ERD

The full schema as 14 logical tables (Identity manages 6 of these — `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`, `AspNetUserClaims`, `AspNetUserLogins`, `AspNetUserTokens` — only the application tables are shown below). The diagram renders directly on GitHub via Mermaid.

```mermaid
erDiagram
    AspNetUsers ||--o{ Prescriptions : "creates"
    AspNetUsers ||--o{ Sales : "cashier"
    AspNetUsers ||--o{ StockMovements : "performs"
    AspNetUsers ||--o{ AuditLogs : "actor"

    Categories ||--o{ Drugs : "has"
    Drugs ||--o{ Batches : "stocked as"
    Suppliers ||--o{ Batches : "supplies"
    Batches ||--o{ StockMovements : "tracked by"
    Batches ||--o{ SaleItems : "drawn from"
    Drugs ||--o{ SaleItems : "is"
    Drugs ||--o{ PrescriptionItems : "ordered as"

    Customers ||--o{ Prescriptions : "for"
    Customers ||--o{ Sales : "buyer (optional)"
    Prescriptions ||--o{ PrescriptionItems : "lists"
    Prescriptions ||--o{ Sales : "dispensed by"
    Sales ||--o{ SaleItems : "contains"

    AspNetUsers {
      string Id PK
      string FullName
      string Email
      string PhoneNumber
      bool IsActive
      datetime CreatedAt
    }
    Categories {
      int Id PK
      string Name UK
      string Description
    }
    Drugs {
      int Id PK
      string Name
      string GenericName
      string NdaNumber
      int CategoryId FK
      enum Form
      string Strength
      bool RequiresPrescription
      string Barcode
      int ReorderLevel
      bool IsActive
    }
    Suppliers {
      int Id PK
      string Name
      string ContactPerson
      string Phone
      string Email
      string Address
      string Tin
      bool IsActive
    }
    Batches {
      int Id PK
      int DrugId FK
      string BatchNumber
      int SupplierId FK
      int QuantityIn
      int QuantityRemaining
      decimal CostPrice
      decimal SellPrice
      datetime ExpiryDate
      datetime ReceivedAt
      bool IsActive
    }
    Customers {
      int Id PK
      string FullName
      string Phone
      string NIN
      bool IsRecurring
      datetime CreatedAt
    }
    Prescriptions {
      int Id PK
      int CustomerId FK
      string DoctorName
      datetime IssuedDate
      string Notes
      enum Status
      string CreatedById FK
      datetime CreatedAt
    }
    PrescriptionItems {
      int Id PK
      int PrescriptionId FK
      int DrugId FK
      int Quantity
      string Dosage
    }
    Sales {
      int Id PK
      string ReceiptNumber UK
      int CustomerId FK
      int PrescriptionId FK
      string CashierId FK
      decimal SubTotal
      decimal Discount
      decimal VatAmount
      decimal Total
      enum PaymentMethod
      string MobileMoneyRef
      datetime CompletedAt
      bool IsVoided
      datetime VoidedAt
      string VoidedById FK
      string VoidReason
    }
    SaleItems {
      int Id PK
      int SaleId FK
      int DrugId FK
      int BatchId FK
      int Quantity
      decimal UnitPrice
      decimal LineTotal
    }
    StockMovements {
      int Id PK
      int BatchId FK
      enum MovementType
      int Quantity
      string Reference
      string PerformedById FK
      datetime PerformedAt
      string Notes
    }
    AuditLogs {
      int Id PK
      string UserId FK
      string EntityName
      string EntityId
      string Action
      string OldValuesJson
      string NewValuesJson
      string IpAddress
      datetime OccurredAt
    }
    Settings {
      int Id PK
      string Key UK
      string Value
    }
```

## Notes

- **All decimal fields** are stored as `TEXT` in SQLite (via EF Core
  `HasConversion<string>()`) to preserve precision for monetary values.
- **`Sales.ReceiptNumber`** is unique. Format: `R-{yyyy}-{NNNNNN}` produced by
  the singleton `SqliteReceiptNumberGenerator`.
- **`Settings.Key`** is unique; values are arbitrary strings parsed at read
  time. Current keys: `Pharmacy.Name`, `Pharmacy.Address`, `Pharmacy.Phone`,
  `Pharmacy.Currency`, `Tax.VatRate`.
- **FIFO dispense:** when a sale is made, the `Batches` query orders by
  `ExpiryDate` ascending, so the earliest-expiry stock drains first.
- **Audit log** captures every state-changing action. `OldValuesJson` is
  `null` for `Create`; `NewValuesJson` is the post-state for every action.
