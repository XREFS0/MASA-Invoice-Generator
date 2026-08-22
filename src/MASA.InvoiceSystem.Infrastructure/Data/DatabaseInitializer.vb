Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports Dapper
Imports Microsoft.Data.Sqlite
Imports Microsoft.Extensions.Logging

Namespace Data
    Public Interface IDatabaseInitializer
        Function InitializeAsync() As Task
    End Interface

    Public Class DatabaseInitializer
        Implements IDatabaseInitializer

        Private ReadOnly _connectionFactory As ISqliteConnectionFactory
        Private ReadOnly _logger As ILogger(Of DatabaseInitializer)

        Public Sub New(connectionFactory As ISqliteConnectionFactory, logger As ILogger(Of DatabaseInitializer))
            _connectionFactory = connectionFactory
            _logger = logger
        End Sub

        Public Async Function InitializeAsync() As Task Implements IDatabaseInitializer.InitializeAsync
            Try
                Dim dbPath = _connectionFactory.DatabaseConfig.DatabaseFilePath
                Dim directoryPath = Path.GetDirectoryName(dbPath)

                If Not String.IsNullOrEmpty(directoryPath) AndAlso Not Directory.Exists(directoryPath) Then
                    Directory.CreateDirectory(directoryPath)
                    _logger.LogInformation("Created application data directory at '{Directory}'", directoryPath)
                End If

                Using conn = Await _connectionFactory.CreateOpenConnectionAsync()
                    Using transaction = conn.BeginTransaction()
                        Dim customersTableSql = "
                            CREATE TABLE IF NOT EXISTS Customers (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                FullName TEXT NOT NULL,
                                CompanyName TEXT NULL,
                                Email TEXT NULL,
                                Phone TEXT NULL,
                                Address TEXT NULL,
                                City TEXT NULL,
                                Country TEXT NULL,
                                PostalCode TEXT NULL,
                                TaxNumber TEXT NULL,
                                Notes TEXT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL
                            );"
                        Await conn.ExecuteAsync(customersTableSql, transaction:=transaction)

                        Dim taxRatesTableSql = "
                            CREATE TABLE IF NOT EXISTS TaxRates (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Name TEXT NOT NULL,
                                Percentage NUMERIC NOT NULL DEFAULT 0,
                                IsDefault INTEGER NOT NULL DEFAULT 0,
                                IsActive INTEGER NOT NULL DEFAULT 1,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL
                            );"
                        Await conn.ExecuteAsync(taxRatesTableSql, transaction:=transaction)

                        Dim productsTableSql = "
                            CREATE TABLE IF NOT EXISTS Products (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                ProductCode TEXT NOT NULL UNIQUE,
                                Name TEXT NOT NULL,
                                Description TEXT NULL,
                                Type INTEGER NOT NULL DEFAULT 0,
                                UnitPrice NUMERIC NOT NULL DEFAULT 0,
                                CostPrice NUMERIC NOT NULL DEFAULT 0,
                                TaxRateId INTEGER NULL,
                                StockQuantity NUMERIC NOT NULL DEFAULT 0,
                                MinimumStock NUMERIC NOT NULL DEFAULT 0,
                                IsActive INTEGER NOT NULL DEFAULT 1,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL,
                                FOREIGN KEY (TaxRateId) REFERENCES TaxRates(Id) ON DELETE SET NULL
                            );"
                        Await conn.ExecuteAsync(productsTableSql, transaction:=transaction)

                        Dim invoicesTableSql = "
                            CREATE TABLE IF NOT EXISTS Invoices (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                InvoiceNumber TEXT NOT NULL UNIQUE,
                                CustomerId INTEGER NOT NULL,
                                IssueDate TEXT NOT NULL,
                                DueDate TEXT NOT NULL,
                                Status INTEGER NOT NULL DEFAULT 0,
                                Currency TEXT NOT NULL DEFAULT 'EGP',
                                Subtotal NUMERIC NOT NULL DEFAULT 0,
                                DiscountAmount NUMERIC NOT NULL DEFAULT 0,
                                TaxAmount NUMERIC NOT NULL DEFAULT 0,
                                TotalAmount NUMERIC NOT NULL DEFAULT 0,
                                PaidAmount NUMERIC NOT NULL DEFAULT 0,
                                RemainingAmount NUMERIC NOT NULL DEFAULT 0,
                                Notes TEXT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL,
                                FOREIGN KEY (CustomerId) REFERENCES Customers(Id) ON DELETE RESTRICT
                            );"
                        Await conn.ExecuteAsync(invoicesTableSql, transaction:=transaction)

                        Dim invoiceItemsTableSql = "
                            CREATE TABLE IF NOT EXISTS InvoiceItems (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                InvoiceId INTEGER NOT NULL,
                                ProductId INTEGER NULL,
                                Description TEXT NOT NULL,
                                Quantity NUMERIC NOT NULL DEFAULT 1,
                                UnitPrice NUMERIC NOT NULL DEFAULT 0,
                                DiscountAmount NUMERIC NOT NULL DEFAULT 0,
                                TaxRate NUMERIC NOT NULL DEFAULT 0,
                                TotalAmount NUMERIC NOT NULL DEFAULT 0,
                                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE,
                                FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL
                            );"
                        Await conn.ExecuteAsync(invoiceItemsTableSql, transaction:=transaction)

                        Dim paymentsTableSql = "
                            CREATE TABLE IF NOT EXISTS Payments (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                InvoiceId INTEGER NOT NULL,
                                Amount NUMERIC NOT NULL DEFAULT 0,
                                PaymentDate TEXT NOT NULL,
                                PaymentMethod INTEGER NOT NULL DEFAULT 0,
                                ReferenceNumber TEXT NULL,
                                Notes TEXT NULL,
                                CreatedAt TEXT NOT NULL,
                                UpdatedAt TEXT NOT NULL,
                                FOREIGN KEY (InvoiceId) REFERENCES Invoices(Id) ON DELETE CASCADE
                            );"
                        Await conn.ExecuteAsync(paymentsTableSql, transaction:=transaction)

                        Dim companySettingsTableSql = "
                            CREATE TABLE IF NOT EXISTS CompanySettings (
                                Id INTEGER PRIMARY KEY,
                                CompanyName TEXT NOT NULL,
                                LogoPath TEXT NULL,
                                Email TEXT NULL,
                                Phone TEXT NULL,
                                Website TEXT NULL,
                                Address TEXT NULL,
                                City TEXT NULL,
                                Country TEXT NULL,
                                PostalCode TEXT NULL,
                                TaxNumber TEXT NULL,
                                DefaultCurrency TEXT NOT NULL DEFAULT 'EGP',
                                DefaultTaxRateId INTEGER NULL,
                                InvoicePrefix TEXT NOT NULL DEFAULT 'MASA-EG-',
                                NextInvoiceNumber INTEGER NOT NULL DEFAULT 1001,
                                UpdatedAt TEXT NOT NULL,
                                FOREIGN KEY (DefaultTaxRateId) REFERENCES TaxRates(Id) ON DELETE SET NULL
                            );"
                        Await conn.ExecuteAsync(companySettingsTableSql, transaction:=transaction)

                        Dim auditLogsTableSql = "
                            CREATE TABLE IF NOT EXISTS AuditLogs (
                                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                Action INTEGER NOT NULL,
                                EntityName TEXT NOT NULL,
                                EntityId TEXT NOT NULL,
                                Description TEXT NOT NULL,
                                CreatedAt TEXT NOT NULL
                            );"
                        Await conn.ExecuteAsync(auditLogsTableSql, transaction:=transaction)

                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_customers_name ON Customers(FullName);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_customers_email ON Customers(Email);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_products_code ON Products(ProductCode);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_products_name ON Products(Name);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_invoices_number ON Invoices(InvoiceNumber);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_invoices_customer ON Invoices(CustomerId);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_invoices_status ON Invoices(Status);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_invoices_dates ON Invoices(IssueDate, DueDate);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_items_invoice ON InvoiceItems(InvoiceId);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_payments_invoice ON Payments(InvoiceId);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_payments_date ON Payments(PaymentDate);", transaction:=transaction)
                        Await conn.ExecuteAsync("CREATE INDEX IF NOT EXISTS idx_audit_created ON AuditLogs(CreatedAt);", transaction:=transaction)

                        Dim nowIso = DateTime.UtcNow.ToString("o")

                        Dim taxCount = Await conn.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM TaxRates;", transaction:=transaction)
                        If taxCount = 0 Then
                            Await conn.ExecuteAsync("
                                INSERT INTO TaxRates (Name, Percentage, IsDefault, IsActive, CreatedAt, UpdatedAt)
                                VALUES 
                                    ('Value Added Tax (VAT 14%)', 14.0, 1, 1, @Now, @Now),
                                    ('Withholding Tax (1%)', 1.0, 0, 1, @Now, @Now),
                                    ('Zero Tax / Exempt (0%)', 0.0, 0, 1, @Now, @Now);", New With {.Now = nowIso}, transaction:=transaction)
                        End If

                        Dim settingCount = Await conn.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM CompanySettings WHERE Id = 1;", transaction:=transaction)
                        If settingCount = 0 Then
                            Await conn.ExecuteAsync("
                                INSERT INTO CompanySettings (Id, CompanyName, Email, Phone, Website, Address, City, Country, PostalCode, TaxNumber, DefaultCurrency, DefaultTaxRateId, InvoicePrefix, NextInvoiceNumber, UpdatedAt)
                                VALUES (1, 'MASA Solutions Egypt S.A.E', 'invoicing@masa-egypt.com', '+20 2 2736 8490', 'https://www.masa-egypt.com', '90th Street North, Sector 1, Fifth Settlement', 'New Cairo', 'Egypt', '11835', 'EG-584-920-311', 'EGP', 1, 'MASA-EG-', 1001, @Now);", New With {.Now = nowIso}, transaction:=transaction)
                        End If

                        Dim custCount = Await conn.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM Customers;", transaction:=transaction)
                        If custCount = 0 Then
                            Await conn.ExecuteAsync("
                                INSERT INTO Customers (FullName, CompanyName, Email, Phone, Address, City, Country, PostalCode, TaxNumber, Notes, CreatedAt, UpdatedAt)
                                VALUES 
                                    ('Ahmed Mahmoud El-Ghamry', 'El-Ghamry Contracting & Engineering', 'a.elghamry@elghamry-group.com', '+20 10 0123 4567', 'Street 15, Maadi', 'Cairo', 'Egypt', '11728', 'EG-392-104-582', 'Commercial client - Payment terms 30 days', @Now, @Now),
                                    ('Tarek Mohamed El-Sayed', 'Cairo Tech Solutions S.A.E', 'tarek.elsayed@cairotech.eg', '+20 11 2345 6789', 'Building 4B, Smart Village', 'Giza', 'Egypt', '12577', 'EG-618-492-731', 'Technology partner - Annual software contract', @Now, @Now),
                                    ('Nour El-Din Mostafa', 'Alexandria Maritime & Logistics', 'nour.mostafa@alexmaritime.com', '+20 12 3456 7890', '24 El-Horreya Avenue', 'Alexandria', 'Egypt', '21519', 'EG-829-371-654', 'Maritime logistics billing', @Now, @Now),
                                    ('Kareem Abdel-Aziz', 'Al-Ahram Trading & Distribution', 'kareem.aziz@alahram-trading.eg', '+20 15 5678 9012', 'Nasr Road, Heliopolis', 'Cairo', 'Egypt', '11361', 'EG-741-852-963', 'Wholesale hardware purchases', @Now, @Now);", New With {.Now = nowIso}, transaction:=transaction)
                        End If

                        Dim prodCount = Await conn.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM Products;", transaction:=transaction)
                        If prodCount = 0 Then
                            Await conn.ExecuteAsync("
                                INSERT INTO Products (ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt)
                                VALUES 
                                    ('DEV-WEB', 'Enterprise Web Application Development', 'Custom business management web portal and database system', 1, 28500.0, 12000.0, 1, 0, 0, 1, @Now, @Now),
                                    ('CONS-ERP', 'ERP & Accounting Financial Consultation', 'Comprehensive financial workflow setup and ledger configuration', 1, 16000.0, 7000.0, 1, 0, 0, 1, @Now, @Now),
                                    ('HW-POS-01', 'Touch POS Terminal System 15"" - Core i5', 'Electronic billing POS machine with integrated thermal printer', 0, 18500.0, 13200.0, 1, 20, 5, 1, @Now, @Now),
                                    ('HW-SCAN-2D', 'Wireless Barcode & QR Code Scanner', 'High-speed wireless 2D handheld barcode and QR reader', 0, 2400.0, 1600.0, 1, 45, 10, 1, @Now, @Now),
                                    ('SRV-MAINT', 'Annual IT Support & Maintenance Package', '24/7 IT system support, data backup and emergency maintenance', 1, 9500.0, 3500.0, 1, 0, 0, 1, @Now, @Now);", New With {.Now = nowIso}, transaction:=transaction)
                        End If

                        transaction.Commit()
                    End Using
                End Using

                _logger.LogInformation("Database initialization completed successfully.")
            Catch ex As Exception
                _logger.LogError(ex, "Failed to initialize SQLite database.")
                Throw
            End Try
        End Function
    End Class
End Namespace
