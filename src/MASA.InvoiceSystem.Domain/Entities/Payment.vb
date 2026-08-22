Imports System
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Entities
    Public Class Payment
        Public Property Id As Long
        Public Property InvoiceId As Long
        Public Property Amount As Decimal = 0D
        Public Property PaymentDate As DateTime = DateTime.Today
        Public Property PaymentMethod As PaymentMethod = PaymentMethod.BankTransfer
        Public Property ReferenceNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow

        Public Property InvoiceNumber As String = String.Empty
        Public Property CustomerName As String = String.Empty
    End Class
End Namespace
