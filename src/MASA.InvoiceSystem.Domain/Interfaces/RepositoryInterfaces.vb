Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Interfaces
    Public Interface ICustomerRepository
        Function GetAllAsync(Optional searchQuery As String = Nothing) As Task(Of IReadOnlyList(Of Customer))
        Function GetByIdAsync(id As Long) As Task(Of Customer)
        Function InsertAsync(customer As Customer) As Task(Of Long)
        Function UpdateAsync(customer As Customer) As Task
        Function DeleteAsync(id As Long) As Task
        Function HasInvoicesAsync(customerId As Long) As Task(Of Boolean)
    End Interface

    Public Interface IProductRepository
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional onlyActive As Boolean = False, Optional typeFilter As ProductType? = Nothing) As Task(Of IReadOnlyList(Of Product))
        Function GetByIdAsync(id As Long) As Task(Of Product)
        Function GetByCodeAsync(code As String) As Task(Of Product)
        Function InsertAsync(product As Product) As Task(Of Long)
        Function UpdateAsync(product As Product) As Task
        Function DeleteAsync(id As Long) As Task
        Function GetLowStockAsync() As Task(Of IReadOnlyList(Of Product))
        Function AdjustStockAsync(productId As Long, quantityDelta As Decimal) As Task
    End Interface

    Public Interface IInvoiceRepository
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional statusFilter As InvoiceStatus? = Nothing, Optional customerId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of Invoice))
        Function GetByIdAsync(id As Long) As Task(Of Invoice)
        Function GetByInvoiceNumberAsync(invoiceNumber As String) As Task(Of Invoice)
        Function InsertWithItemsAsync(invoice As Invoice) As Task(Of Long)
        Function UpdateWithItemsAsync(invoice As Invoice) As Task
        Function DeleteAsync(id As Long) As Task
        Function UpdateStatusAsync(id As Long, status As InvoiceStatus) As Task
        Function RecalculatePaidAmountAsync(invoiceId As Long) As Task
    End Interface

    Public Interface IPaymentRepository
        Function GetAllAsync(Optional searchQuery As String = Nothing, Optional invoiceId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of Payment))
        Function GetByIdAsync(id As Long) As Task(Of Payment)
        Function InsertAsync(payment As Payment) As Task(Of Long)
        Function DeleteAsync(id As Long) As Task
        Function GetTotalPaidForInvoiceAsync(invoiceId As Long) As Task(Of Decimal)
    End Interface

    Public Interface ICompanySettingRepository
        Function GetSettingAsync() As Task(Of CompanySetting)
        Function UpdateSettingAsync(setting As CompanySetting) As Task
        Function GetAndIncrementNextInvoiceNumberAsync(prefix As String) As Task(Of String)
    End Interface

    Public Interface ITaxRateRepository
        Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of IReadOnlyList(Of TaxRate))
        Function GetByIdAsync(id As Long) As Task(Of TaxRate)
        Function InsertAsync(taxRate As TaxRate) As Task(Of Long)
        Function UpdateAsync(taxRate As TaxRate) As Task
        Function DeleteAsync(id As Long) As Task
        Function SetDefaultAsync(id As Long) As Task
    End Interface

    Public Interface IAuditLogRepository
        Function GetRecentLogsAsync(Optional limit As Integer = 200, Optional entityName As String = Nothing) As Task(Of IReadOnlyList(Of AuditLog))
        Function InsertAsync(log As AuditLog) As Task(Of Long)
    End Interface
End Namespace
