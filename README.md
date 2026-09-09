# RetailFlow

**RetailFlow** (branded in-app as **MiniMart Manager**) is a Windows desktop application for managing a small retail store's products, sales, and stock. It lets a store owner or cashier maintain a product catalog, process sales at a point-of-sale style screen, keep stock counts accurate automatically, review past transactions, and get a live analytics dashboard of the business — all offline, backed by a local SQLite database.

## Features

- **Product management** — add, edit, and deactivate products; search and filter the catalog by name, code, or category.
- **Sales processing** — a point-of-sale style screen: search the catalog, build a cart, apply a discount, and complete a sale.
- **Stock management** — stock is deducted automatically and atomically when a sale completes, so counts are never updated by hand.
- **Transaction history** — browse past sales by date range, with a full line-item receipt view for each one.
- **Low-stock alerts** — any product at or below its reorder level is flagged consistently across the Dashboard, the Products screen, and the MiniMart Assistant.

Also included: an interactive **Dashboard** (KPI cards and five charts, filterable by date/category/product) and the **MiniMart Assistant**, a rule-based chat assistant for quick stock/sales questions and navigation.

## Technologies

- C#
- .NET 8
- WPF (Windows Presentation Foundation)
- SQLite
- Entity Framework Core

## Requirements

- Windows PC
- .NET 8 SDK

## Setup

1. **Extract the project.** Unzip (or clone) the project folder to a location of your choice.
2. **Open the project.** Open `RetailFlow.csproj` in Visual Studio / Rider, or just open the project folder in a terminal — no IDE is required for the steps below.
3. **Restore packages.**
   ```
   dotnet restore
   ```
   This pulls down the only dependency, `Microsoft.EntityFrameworkCore.Sqlite`.
4. **Build.**
   ```
   dotnet build
   ```
5. **Run.**
   ```
   dotnet run
   ```
   Or run the built executable directly from `bin/Debug/net8.0-windows/RetailFlow.exe`, or press **Start** (F5) in Visual Studio / Rider.

No database setup is needed — `retailflow.db` is created automatically next to the executable the first time the app runs, and seeded with a realistic demo catalog (28 products across 7 categories) and 12 sample sales, so every screen has real data to show immediately.

## Demo

The recommended way to demonstrate RetailFlow is to walk through one connected scenario rather than clicking through screens in isolation — it shows every feature actually affecting the others, not just existing side by side.

1. **Dashboard** — start here. Point out the KPI cards (Sales, Active Products, Low Stock, Best Selling Product) and the five charts, and try the Date Range / Category / Product filters to show them update live.
2. **Product Management** — open a product (e.g. one with stock close to its reorder level) to show the Edit form, and note its current stock quantity.
3. **New Sale** — search for that same product, add a few units to the cart (enough to push its stock at or below its reorder level), optionally apply a discount, and complete the sale.
4. **Completed Sale** — point out the confirmation dialog with the generated invoice number and total.
5. **Stock update** — go back to Product Management and show the product's stock has already dropped, with no manual entry.
6. **Low Stock Alert** — check "Low stock only" and show the product now appears there, having crossed its reorder level as a direct result of the sale just made.
7. **Transaction History** — show the new sale at the top of the list (most recent first), then click it to show the full receipt detail.
8. **Dashboard, again** — refresh (or navigate back) and show the KPIs, the Sales Trend, and the Stock vs. Sales chart all reflecting that same sale.
9. **MiniMart Assistant** *(optional)* — ask "how much did we sell today" or "show low stock products" to show the same figures are consistent everywhere they appear.

This sequence takes under two minutes and demonstrates product management, sales processing, automatic stock management, low-stock alerting, transaction history, and the dashboard as one coherent system rather than five disconnected screens.
