Imports System
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Entities
    Public Class AuditLog
        Public Property Id As Long
        Public Property Action As AuditAction = AuditAction.Created
        Public Property EntityName As String = String.Empty
        Public Property EntityId As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
