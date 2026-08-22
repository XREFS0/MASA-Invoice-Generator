Imports System

Namespace Entities
    Public Class InvoiceItem
        Public Property Id As Long
        Public Property InvoiceId As Long
        Public Property ProductId As Long?
        Public Property Description As String = String.Empty
        Public Property Quantity As Decimal = 1D
        Public Property UnitPrice As Decimal = 0D
        Public Property DiscountAmount As Decimal = 0D
        Public Property TaxRate As Decimal = 0D
        Public Property TotalAmount As Decimal = 0D
    End Class
End Namespace
