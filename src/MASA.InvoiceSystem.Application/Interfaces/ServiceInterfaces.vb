Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Interfaces
    Public Interface ICustomerService
        Function GetAllAsync(Optional searchQuery As String = Nothing) As Task(Of IReadOnlyList(Of CustomerDto))
        Function GetByIdAsync(id As Long) As Task(Of CustomerDto)
        Function SaveAsync(customer As Customer) As Task(Of CustomerDto)
        Function DeleteAsync(id As Long) As Task
        Function GetCustomerInvoicesAsync(customerId As Long) As Task(Of IReadOnlyList(Of InvoiceDto))
        Function GetCustomerPaymentsAsync(customerId As Long) As Task(Of IReadOnlyList(Of PaymentDto))
    End Interface

    Public Interface IProductService
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional onlyActive As Boolean = False, Optional typeFilter As ProductType? = Nothing) As Task(Of IReadOnlyList(Of ProductDto))
        Function GetByIdAsync(id As Long) As Task(Of ProductDto)
        Function SaveAsync(product As Product) As Task(Of ProductDto)
        Function DeleteAsync(id As Long) As Task
        Function GetLowStockProductsAsync() As Task(Of IReadOnlyList(Of ProductDto))
    End Interface

    Public Interface IInvoiceService
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional statusFilter As InvoiceStatus? = Nothing, Optional customerId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of InvoiceDto))
        Function GetByIdAsync(id As Long) As Task(Of InvoiceDto)
        Function CreateAsync(invoice As Invoice) As Task(Of InvoiceDto)
        Function UpdateAsync(invoice As Invoice) As Task(Of InvoiceDto)
        Function DeleteAsync(id As Long) As Task
        Function DuplicateAsync(id As Long) As Task(Of InvoiceDto)
        Function UpdateStatusAsync(id As Long, newStatus As InvoiceStatus) As Task
        Function GetNextInvoiceNumberAsync() As Task(Of String)
    End Interface

    Public Interface IPaymentService
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional invoiceId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of PaymentDto))
        Function GetByIdAsync(id As Long) As Task(Of PaymentDto)
        Function RecordPaymentAsync(payment As Payment) As Task(Of PaymentDto)
        Function DeletePaymentAsync(id As Long) As Task
    End Interface

    Public Interface ICompanySettingService
        Function GetSettingAsync() As Task(Of CompanySettingDto)
        Function SaveSettingAsync(setting As CompanySetting) As Task(Of CompanySettingDto)
        Function SaveLogoAsync(sourceFilePath As String) As Task(Of String)
    End Interface

    Public Interface ITaxRateService
        Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of IReadOnlyList(Of TaxRateDto))
        Function GetByIdAsync(id As Long) As Task(Of TaxRateDto)
        Function SaveAsync(taxRate As TaxRate) As Task(Of TaxRateDto)
        Function DeleteAsync(id As Long) As Task
        Function SetDefaultAsync(id As Long) As Task
    End Interface

    Public Interface IDashboardService
        Function GetDashboardSummaryAsync() As Task(Of DashboardSummaryDto)
    End Interface

    Public Interface IReportService
        Function GetSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of SalesReportItemDto))
        Function GetRevenueReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of MonthlySalesDto))
        Function GetOutstandingInvoicesReportAsync() As Task(Of IReadOnlyList(Of InvoiceDto))
        Function GetCustomerSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of CustomerSalesReportDto))
        Function GetProductSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of ProductSalesReportDto))
        Function GetPaymentsReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of PaymentDto))
    End Interface

    Public Interface IAuditLogService
        Function GetRecentLogsAsync(Optional limit As Integer = 200, Optional entityName As String = Nothing) As Task(Of IReadOnlyList(Of AuditLogDto))
        Function LogAsync(action As AuditAction, entityName As String, entityId As String, description As String) As Task
    End Interface

    Public Interface IBackupService
        Function BackupDatabaseAsync(destinationFilePath As String) As Task(Of String)
        Function RestoreDatabaseAsync(sourceBackupFilePath As String) As Task(Of Boolean)
        Function ValidateBackupFile(filePath As String) As Boolean
    End Interface

    Public Interface IInvoicePdfService
        Function GeneratePdfBytes(invoice As InvoiceDto, company As CompanySettingDto) As Byte()
        Function ExportPdfToFile(invoice As InvoiceDto, company As CompanySettingDto, destinationPath As String) As Task
    End Interface
End Namespace
