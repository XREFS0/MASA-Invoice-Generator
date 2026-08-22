Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Entities
    Public Class Invoice
        Public Property Id As Long
        Public Property InvoiceNumber As String = String.Empty
        Public Property CustomerId As Long
        Public Property IssueDate As DateTime = DateTime.Today
        Public Property DueDate As DateTime = DateTime.Today.AddDays(30)
        Public Property Status As InvoiceStatus = InvoiceStatus.Draft
        Public Property Currency As String = "EGP"
        Public Property Subtotal As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxAmount As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
        Public Property PaidAmount As Decimal = 0D
        Public Property RemainingAmount As Decimal = 0D
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow

        Public Property CustomerName As String = String.Empty
        Public Property CustomerEmail As String = String.Empty

        Public Property Items As List(Of InvoiceItem) = New List(Of InvoiceItem)()
    End Class
End Namespace
