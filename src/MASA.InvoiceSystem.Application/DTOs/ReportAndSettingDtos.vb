Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Domain.Enums

Namespace DTOs
    Public Class CompanySettingDto
        Public Property Id As Long = 1
        Public Property CompanyName As String = String.Empty
        Public Property LogoPath As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Website As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property City As String = String.Empty
        Public Property Country As String = String.Empty
        Public Property PostalCode As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property DefaultCurrency As String = "USD"
        Public Property DefaultTaxRateId As Long?
        Public Property InvoicePrefix As String = "INV-"
        Public Property NextInvoiceNumber As Long = 1001
        Public Property UpdatedAt As DateTime
    End Class

    Public Class TaxRateDto
        Public Property Id As Long
        Public Property Name As String = String.Empty
        Public Property Percentage As Decimal
        Public Property IsDefault As Boolean
        Public Property IsActive As Boolean
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime
    End Class

    Public Class AuditLogDto
        Public Property Id As Long
        Public Property Action As AuditAction
        Public Property EntityName As String = String.Empty
        Public Property EntityId As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property CreatedAt As DateTime
    End Class

    Public Class DashboardSummaryDto
        Public Property TotalInvoicesCount As Integer
        Public Property PaidInvoicesCount As Integer
        Public Property UnpaidInvoicesCount As Integer
        Public Property OverdueInvoicesCount As Integer
        Public Property TotalRevenue As Decimal
        Public Property OutstandingBalance As Decimal
        Public Property TotalCustomersCount As Integer
        Public Property LowStockItemsCount As Integer
        Public Property RecentInvoices As List(Of InvoiceDto) = New List(Of InvoiceDto)()
        Public Property RecentPayments As List(Of PaymentDto) = New List(Of PaymentDto)()
        Public Property MonthlySales As List(Of MonthlySalesDto) = New List(Of MonthlySalesDto)()
    End Class

    Public Class MonthlySalesDto
        Public Property MonthLabel As String = String.Empty
        Public Property Year As Integer
        Public Property Month As Integer
        Public Property TotalAmount As Decimal
        Public Property PaidAmount As Decimal
        Public Property InvoiceCount As Integer
    End Class

    Public Class SalesReportItemDto
        Public Property InvoiceNumber As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property IssueDate As DateTime
        Public Property DueDate As DateTime
        Public Property Status As String = String.Empty
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property PaidAmount As Decimal
        Public Property RemainingAmount As Decimal
    End Class

    Public Class CustomerSalesReportDto
        Public Property CustomerId As Long
        Public Property CustomerName As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property InvoiceCount As Integer
        Public Property TotalBilled As Decimal
        Public Property TotalPaid As Decimal
        Public Property OutstandingBalance As Decimal
    End Class

    Public Class ProductSalesReportDto
        Public Property ProductId As Long
        Public Property ProductCode As String = String.Empty
        Public Property ProductName As String = String.Empty
        Public Property ProductType As String = String.Empty
        Public Property QuantitySold As Decimal
        Public Property TotalRevenue As Decimal
    End Class
End Namespace
