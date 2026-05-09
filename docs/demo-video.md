# Demo video

A 5–8 minute walkthrough of MediStock will be linked here after recording.

Planned shot list:

1. `dotnet run --project src/MediStock.Web` from a clean clone — show migrations
   running and seed data being applied in the console.
2. Browse to http://localhost:5000 and sign in as **Admin**. Show the dashboard
   with KPI cards, the daily-revenue chart, and the bell icon.
3. Sign out, sign in as **Pharmacist**. Add a new drug, then receive a new batch
   for it. Show the alert badges update.
4. Create a customer, then create a prescription with two line items.
5. Sign out, sign in as **Cashier**. Open the till.
6. Search for a drug, add to cart, take a cash payment, watch the PDF receipt
   open in a new tab. Show the FIFO behaviour by inspecting batch quantities
   before and after.
7. From the prescription detail page, click **Dispense** — the till loads with
   the prescription pre-filled. Complete the sale. Show the prescription status
   flip to **Dispensed**.
8. Sign in as **Admin**. Open the audit log; expand a row to show the JSON diff.
   Visit Settings, change the pharmacy name, save, and show the change reflected
   on the next receipt.
9. Run `dotnet test` to show all unit + integration tests pass.

Recording target: 5–8 minutes. Video link will go here:

> _video URL pending_
