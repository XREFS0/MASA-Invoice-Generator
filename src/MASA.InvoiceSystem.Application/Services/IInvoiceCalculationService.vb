Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Services
    Public Interface IInvoiceCalculationService
        Function CalculateItemTotals(quantity As Decimal, unitPrice As Decimal, discountAmount As Decimal, taxRate As Decimal) As InvoiceItemCalculationResult
        Function CalculateInvoiceTotals(items As IEnumerable(Of InvoiceItem), currentPaidAmount As Decimal, dueDate As DateTime, currentStatus As InvoiceStatus) As InvoiceCalculationResult
        Function DetermineInvoiceStatus(totalAmount As Decimal, paidAmount As Decimal, dueDate As DateTime, currentStatus As InvoiceStatus) As InvoiceStatus
    End Interface

    Public Class InvoiceItemCalculationResult
        Public Property GrossAmount As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxableAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property TotalAmount As Decimal
    End Class

    Public Class InvoiceCalculationResult
        Public Property Subtotal As Decimal
        Public Property DiscountAmount As Decimal
        Public Property TaxAmount As Decimal
        Public Property TotalAmount As Decimal
        Public Property PaidAmount As Decimal
        Public Property RemainingAmount As Decimal
        Public Property Status As InvoiceStatus
    End Class
End Namespace
