# MASA Invoice & Billing Management System

[![GitHub Repository](https://img.shields.io/badge/GitHub-Repository-181717?logo=github)](https://github.com/XREFS0/MASA-Invoice-Generator)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Language](https://img.shields.io/badge/Language-VB.NET-512BD4)](https://learn.microsoft.com/en-us/dotnet/visual-basic/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20Forms-0078D4)](https://learn.microsoft.com/en-us/dotnet/desktop/winforms/)
[![Database](https://img.shields.io/badge/Database-SQLite%203%20%2B%20Dapper-003B57?logo=sqlite)](https://www.sqlite.org/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

A standalone desktop invoice, billing, client management, and financial reporting system engineered in **VB.NET (.NET 8)** with **Windows Forms**, **SQLite (`Microsoft.Data.Sqlite`)**, **Dapper**, **Microsoft.Extensions.DependencyInjection**, and **QuestPDF**.

Repository: [https://github.com/XREFS0/MASA-Invoice-Generator](https://github.com/XREFS0/MASA-Invoice-Generator)

---

## Screenshots

### 1. Executive Dashboard & Overview
![Executive Dashboard](ScreenShot/01_Dashboard.png)

---

### 2. Invoices Management & Lifecycle
| Invoice Registry | Invoice Creator & Line Items Editor |
| :---: | :---: |
| ![Invoices List](ScreenShot/02_Invoices_List.png) | ![Invoice Editor](ScreenShot/03_Invoice_Editor_New.png) |

| Invoice Overview & PDF Export | Payment Recording Dialog |
| :---: | :---: |
| ![Invoice Details](ScreenShot/04_Invoice_Details_View.png) | ![Payment Dialog](ScreenShot/11_Payment_Dialog_Record.png) |

---

### 3. Customer Directory & Account Statements
| Customer Directory | Customer Account Statement Ledger |
| :---: | :---: |
| ![Customer Directory](ScreenShot/05_Customers_Directory.png) | ![Customer Statement](ScreenShot/07_Customer_Statement.png) |

| Add / Edit Customer Dialog |
| :---: |
| ![Customer Dialog](ScreenShot/06_Customer_Dialog_Edit.png) |

---

### 4. Products & Services Catalog
| Catalog & Inventory Tracking | Add / Edit Catalog Item |
| :---: | :---: |
| ![Products Catalog](ScreenShot/08_Products_Catalog.png) | ![Product Dialog](ScreenShot/09_Product_Dialog_Add.png) |

---

### 5. Payments & Business Intelligence Reports
| Payment Transactions Ledger | Financial & Sales Reports (CSV Export) |
| :---: | :---: |
| ![Payments Transactions](ScreenShot/10_Payments_Transactions.png) | ![Sales Report](ScreenShot/12_Reports_Sales.png) |

---

### 6. System Configuration & Audit Trail
| Company Profile & Numbering | Egyptian Tax Rates (VAT 14%) |
| :---: | :---: |
| ![Company Settings](ScreenShot/17_Settings_Company.png) | ![Tax Rates](ScreenShot/18_Settings_TaxRates.png) |

| SQLite Backup & Restore | System Activity & Audit Trail |
| :---: | :---: |
| ![Database Backup](ScreenShot/19_Settings_Backup.png) | ![Audit Trail Logs](ScreenShot/20_Audit_Trail_Logs.png) |

---

## Features

* **Executive Dashboard**: Real-time financial metrics, recent invoices & payments, and 6-month sales trend graphs.
* **Invoices Management**: Lifecycle tracking (`Draft`, `Sent`, `PartialPayment`, `Paid`, `Overdue`, `Cancelled`), customizable numbering (`MASA-EG-`), multi-item line calculations, and invoice duplication.
* **Customer Management**: Contact directory, account statements with transaction histories, and deletion protection for active accounts.
* **Products & Services Catalog**: Physical products with low stock alerts and hourly/fixed services with custom tax rates.
* **Payment Processing**: Flexible partial and full balance settlements with strict overpayment protection.
* **Professional PDF Generation**: QuestPDF vector invoice generation with letterhead, bill-to block, itemized tables, tax breakdown, and payment status badges.
* **Business Intelligence & Reporting**: 6 reports with date range filtering and one-click CSV export.
* **Database Maintenance**: SQLite automatic schema initialization, WAL mode, full backup creation, and binary-validated database restoration.
* **System Audit Trail**: Complete activity logs tracking creation, modifications, deletions, and payments.

---

## Architecture & Project Structure

```text
MASA.InvoiceSystem
│
├── src/
│   ├── MASA.InvoiceSystem.Domain/             # Business models, enums, exceptions & repository contracts
│   ├── MASA.InvoiceSystem.Application/        # Application services, DTOs, calculations & validation
│   ├── MASA.InvoiceSystem.Infrastructure/     # SQLite data access, Dapper repositories, Backup & QuestPDF
│   └── MASA.InvoiceSystem.WinForms/           # Windows Forms UI, views, dialogs, styling & themes
│
├── tests/
│   └── MASA.InvoiceSystem.Tests/              # Automated unit and integration tests (xUnit)
│
└── ScreenShot/                                # Application UI screenshots
```

---

## Getting Started

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* Windows 10 / Windows 11 / Windows Server

### Build the Solution

```bash
dotnet build MASA.InvoiceSystem.sln
```

### Run Tests

```bash
dotnet test
```

### Run the Application

```bash
dotnet run --project src/MASA.InvoiceSystem.WinForms/MASA.InvoiceSystem.WinForms.vbproj
```

---

## Configuration & Storage

* Database location: `%AppData%\MASA Invoice System\Data\masa_invoice.db`
* Database backups can be saved to any chosen directory or external drive.

---

## Copyright & License

**All rights reserved © 2026 XREFS0.**

Source Code Repository: [https://github.com/XREFS0/MASA-Invoice-Generator](https://github.com/XREFS0/MASA-Invoice-Generator)

This project is open source and available under the [MIT License](LICENSE).
