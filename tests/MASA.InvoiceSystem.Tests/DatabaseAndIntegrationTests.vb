Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data
Imports MASA.InvoiceSystem.Infrastructure.Repositories
Imports MASA.InvoiceSystem.Infrastructure.Services
Imports Microsoft.Extensions.Logging.Abstractions
Imports Xunit

Namespace Tests
    Public Class DatabaseAndIntegrationTests
        Implements IDisposable

        Private ReadOnly _tempDbFile As String
        Private ReadOnly _dbConfig As DatabaseConfig
        Private ReadOnly _connFactory As ISqliteConnectionFactory
        Private ReadOnly _dbInitializer As IDatabaseInitializer

        Private ReadOnly _custRepo As ICustomerRepository
        Private ReadOnly _prodRepo As IProductRepository
        Private ReadOnly _taxRepo As ITaxRateRepository
        Private ReadOnly _companyRepo As ICompanySettingRepository
        Private ReadOnly _invRepo As IInvoiceRepository
        Private ReadOnly _payRepo As IPaymentRepository
        Private ReadOnly _auditRepo As IAuditLogRepository

        Private ReadOnly _auditService As IAuditLogService
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _custService As ICustomerService
        Private ReadOnly _prodService As IProductService
        Private ReadOnly _invService As IInvoiceService
        Private ReadOnly _payService As IPaymentService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _taxService As ITaxRateService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _backupService As IBackupService
        Private ReadOnly _dashboardService As IDashboardService
        Private ReadOnly _reportService As IReportService

        Public Sub New()
            _tempDbFile = Path.Combine(Path.GetTempPath(), $"masa_test_{Guid.NewGuid():N}.db")
            _dbConfig = New DatabaseConfig(_tempDbFile)
            _connFactory = New SqliteConnectionFactory(_dbConfig)
            _dbInitializer = New DatabaseInitializer(_connFactory, NullLogger(Of DatabaseInitializer).Instance)

            ' Repositories
            _custRepo = New CustomerRepository(_connFactory)
            _prodRepo = New ProductRepository(_connFactory)
            _taxRepo = New TaxRateRepository(_connFactory)
            _companyRepo = New CompanySettingRepository(_connFactory)
            _invRepo = New InvoiceRepository(_connFactory)
            _payRepo = New PaymentRepository(_connFactory)
            _auditRepo = New AuditLogRepository(_connFactory)

            ' Services
            _auditService = New AuditLogService(_auditRepo)
            _calcService = New InvoiceCalculationService()
            _custService = New CustomerService(_custRepo, _invRepo, _payRepo, _auditService, NullLogger(Of CustomerService).Instance)
            _prodService = New ProductService(_prodRepo, _taxRepo, _auditService, NullLogger(Of ProductService).Instance)
            _companyService = New CompanySettingService(_companyRepo, _auditService, NullLogger(Of CompanySettingService).Instance)
            _taxService = New TaxRateService(_taxRepo, _auditService, NullLogger(Of TaxRateService).Instance)
            _invService = New InvoiceService(_invRepo, _custRepo, _prodRepo, _companyRepo, _calcService, _auditService, NullLogger(Of InvoiceService).Instance)
            _payService = New PaymentService(_payRepo, _invRepo, _calcService, _auditService, NullLogger(Of PaymentService).Instance)
            _pdfService = New QuestPdfInvoiceGenerator()
            _backupService = New BackupService(_connFactory, _auditService, NullLogger(Of BackupService).Instance)
            _dashboardService = New DashboardService(_invRepo, _payRepo, _custRepo, _prodRepo, NullLogger(Of DashboardService).Instance)
            _reportService = New ReportService(_invRepo, _payRepo, _custRepo, _prodRepo)

            ' Initialize DB
            _dbInitializer.InitializeAsync().GetAwaiter().GetResult()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools()
            If File.Exists(_tempDbFile) Then
                Try
                    File.Delete(_tempDbFile)
                Catch
                End Try
            End If
        End Sub

        <Fact>
        Public Async Function EndToEnd_CreateCustomerProductInvoicePayment_WorkflowSucceeds() As Task
            ' 1. Create Customer
            Dim cust = Await _custService.SaveAsync(New Customer With {
                .FullName = "Acme Corp",
                .CompanyName = "Acme Enterprises Inc.",
                .Email = "contact@acme.com",
                .City = "Metropolis"
            })
            Assert.True(cust.Id > 0)

            ' 2. Create Product
            Dim prod = Await _prodService.SaveAsync(New Product With {
                .ProductCode = "SRV-CONSULT",
                .Name = "Architecture Consulting",
                .Type = ProductType.Service,
                .UnitPrice = 150D,
                .IsActive = True
            })
            Assert.True(prod.Id > 0)

            ' 3. Create Invoice
            Dim invoice = Await _invService.CreateAsync(New Invoice With {
                .CustomerId = cust.Id,
                .IssueDate = DateTime.Today,
                .DueDate = DateTime.Today.AddDays(14),
                .Status = InvoiceStatus.Sent,
                .Items = New List(Of InvoiceItem) From {
                    New InvoiceItem With {
                        .ProductId = prod.Id,
                        .Description = "5 hours Consulting",
                        .Quantity = 5D,
                        .UnitPrice = 150D,
                        .DiscountAmount = 50D,
                        .TaxRate = 10D
                    }
                }
            })

            ' 5 * 150 = 750 Gross
            ' Taxable = 750 - 50 = 700
            ' Tax = 70.00
            ' Total = 770.00
            Assert.Equal(770.00D, invoice.TotalAmount)
            Assert.Equal(770.00D, invoice.RemainingAmount)
            Assert.Equal(0.00D, invoice.PaidAmount)
            Assert.Equal(InvoiceStatus.Sent, invoice.Status)

            ' 4. Record Partial Payment
            Dim p1 = Await _payService.RecordPaymentAsync(New Payment With {
                .InvoiceId = invoice.Id,
                .Amount = 300D,
                .PaymentMethod = PaymentMethod.BankTransfer,
                .ReferenceNumber = "TXN-001"
            })

            Dim updatedInv = Await _invService.GetByIdAsync(invoice.Id)
            Assert.Equal(300.00D, updatedInv.PaidAmount)
            Assert.Equal(470.00D, updatedInv.RemainingAmount)
            Assert.Equal(InvoiceStatus.PartialPayment, updatedInv.Status)

            ' 5. Record Remaining Payment
            Dim p2 = Await _payService.RecordPaymentAsync(New Payment With {
                .InvoiceId = invoice.Id,
                .Amount = 470D,
                .PaymentMethod = PaymentMethod.CreditCard,
                .ReferenceNumber = "TXN-002"
            })

            Dim finalInv = Await _invService.GetByIdAsync(invoice.Id)
            Assert.Equal(770.00D, finalInv.PaidAmount)
            Assert.Equal(0.00D, finalInv.RemainingAmount)
            Assert.Equal(InvoiceStatus.Paid, finalInv.Status)

            ' 6. Verify QuestPDF Generation
            Dim company = Await _companyService.GetSettingAsync()
            Dim pdfBytes = _pdfService.GeneratePdfBytes(finalInv, company)
            Assert.NotNull(pdfBytes)
            Assert.True(pdfBytes.Length > 1000)

            ' 7. Verify Dashboard KPIs
            Dim dashboard = Await _dashboardService.GetDashboardSummaryAsync()
            Assert.Equal(1, dashboard.TotalInvoicesCount)
            Assert.Equal(1, dashboard.PaidInvoicesCount)
            Assert.Equal(0, dashboard.UnpaidInvoicesCount)
            Assert.Equal(770.00D, dashboard.TotalRevenue)
            Assert.Equal(0.00D, dashboard.OutstandingBalance)

            ' 8. Verify Reports
            Dim salesReport = Await _reportService.GetSalesReportAsync(DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1))
            Assert.Single(salesReport)
            Assert.Equal(770.00D, salesReport(0).TotalAmount)

            ' 9. Verify Backup & Validation
            Dim backupPath = Path.Combine(Path.GetTempPath(), $"backup_test_{Guid.NewGuid():N}.db")
            Await _backupService.BackupDatabaseAsync(backupPath)
            Assert.True(_backupService.ValidateBackupFile(backupPath))

            If File.Exists(backupPath) Then File.Delete(backupPath)
        End Function

        <Fact>
        Public Async Function CustomerWithInvoices_CannotBeDeleted_ThrowsBusinessRuleException() As Task
            Dim cust = Await _custService.SaveAsync(New Customer With {
                .FullName = "Test Client",
                .Email = "client@test.com"
            })

            Dim inv = Await _invService.CreateAsync(New Invoice With {
                .CustomerId = cust.Id,
                .Items = New List(Of InvoiceItem) From {
                    New InvoiceItem With {.Description = "Dev Work", .Quantity = 1D, .UnitPrice = 100D}
                }
            })

            Await Assert.ThrowsAsync(Of BusinessRuleException)(Function() _custService.DeleteAsync(cust.Id))
        End Function

        <Fact>
        Public Async Function DuplicateInvoice_CreatesNewDraftWithDistinctNumber() As Task
            Dim cust = Await _custService.SaveAsync(New Customer With {.FullName = "Company A"})
            Dim original = Await _invService.CreateAsync(New Invoice With {
                .CustomerId = cust.Id,
                .Items = New List(Of InvoiceItem) From {
                    New InvoiceItem With {.Description = "Hosting", .Quantity = 1D, .UnitPrice = 50D}
                }
            })

            Dim duplicated = Await _invService.DuplicateAsync(original.Id)
            Assert.NotEqual(original.InvoiceNumber, duplicated.InvoiceNumber)
            Assert.Equal(InvoiceStatus.Draft, duplicated.Status)
            Assert.Equal(50.00D, duplicated.TotalAmount)
        End Function
    End Class
End Namespace
