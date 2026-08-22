Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Domain.Enums

Namespace DTOs
    Public Class CustomerDto
        Public Property Id As Long
        Public Property FullName As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property City As String = String.Empty
        Public Property Country As String = String.Empty
        Public Property PostalCode As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime
        Public Property TotalInvoiced As Decimal
        Public Property TotalPaid As Decimal
        Public Property OutstandingBalance As Decimal
        Public Property InvoiceCount As Integer

        Public ReadOnly Property DisplayName As String
            Get
                If Not String.IsNullOrWhiteSpace(CompanyName) AndAlso Not String.IsNullOrWhiteSpace(FullName) Then
                    Return $"{FullName} ({CompanyName})"
                ElseIf Not String.IsNullOrWhiteSpace(CompanyName) Then
                    Return CompanyName
                Else
                    Return FullName
                End If
            End Get
        End Property
    End Class

    Public Class ProductDto
        Public Property Id As Long
        Public Property ProductCode As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Type As ProductType
        Public Property UnitPrice As Decimal
        Public Property CostPrice As Decimal
        Public Property TaxRateId As Long?
        Public Property TaxRatePercentage As Decimal
        Public Property StockQuantity As Decimal
        Public Property MinimumStock As Decimal
        Public Property IsActive As Boolean
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime

        Public ReadOnly Property TypeDisplay As String
            Get
                Return If(Type = ProductType.Product, "Physical Product", "Service")
            End Get
        End Property

        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return Type = ProductType.Product AndAlso StockQuantity <= MinimumStock
            End Get
        End Property
    End Class

    Public Class InvoiceItemDto
        Public Property Id As Long
        Public Property InvoiceId As Long
        Public Property ProductId As Long?
        Public Property ProductName As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal = 1D
        Public Property UnitPrice As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxRate As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
    End Class

    Public Class InvoiceDto
        Public Property Id As Long
        Public Property InvoiceNumber As String = String.Empty
        Public Property CustomerId As Long
        Public Property CustomerName As String = String.Empty
        Public Property CustomerCompanyName As String = String.Empty
        Public Property CustomerEmail As String = String.Empty
        Public Property CustomerPhone As String = String.Empty
        Public Property CustomerAddress As String = String.Empty
        Public Property CustomerCity As String = String.Empty
        Public Property CustomerCountry As String = String.Empty
        Public Property CustomerTaxNumber As String = String.Empty
        Public Property IssueDate As DateTime
        Public Property DueDate As DateTime
        Public Property Status As InvoiceStatus
        Public Property Currency As String = "USD"
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property PaidAmount As Decimal
        Public Property RemainingAmount As Decimal
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime
        Public Property UpdatedAt As DateTime
        Public Property Items As List(Of InvoiceItemDto) = New List(Of InvoiceItemDto)()

        Public ReadOnly Property IsOverdue As Boolean
            Get
                Return (Status <> InvoiceStatus.Paid AndAlso Status <> InvoiceStatus.Cancelled) AndAlso DueDate.Date < DateTime.Today
            End Get
        End Property
    End Class

    Public Class PaymentDto
        Public Property Id As Long
        Public Property InvoiceId As Long
        Public Property InvoiceNumber As String = String.Empty
        Public Property CustomerName As String = String.Empty
        Public Property CustomerId As Long
        Public Property Amount As Decimal
        Public Property PaymentDate As DateTime
        Public Property PaymentMethod As PaymentMethod
        Public Property ReferenceNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime
    End Class
End Namespace
