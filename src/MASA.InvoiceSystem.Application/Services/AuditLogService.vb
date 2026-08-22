Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Interfaces

Namespace Services
    Public Class AuditLogService
        Implements IAuditLogService

        Private ReadOnly _auditRepo As IAuditLogRepository

        Public Sub New(auditRepo As IAuditLogRepository)
            _auditRepo = auditRepo
        End Sub

        Public Async Function GetRecentLogsAsync(Optional limit As Integer = 200, Optional entityName As String = Nothing) As Task(Of IReadOnlyList(Of AuditLogDto)) Implements IAuditLogService.GetRecentLogsAsync
            Dim logs = Await _auditRepo.GetRecentLogsAsync(limit, entityName)
            Return logs.Select(Function(l) New AuditLogDto With {
                .Id = l.Id,
                .Action = l.Action,
                .EntityName = l.EntityName,
                .EntityId = l.EntityId,
                .Description = l.Description,
                .CreatedAt = l.CreatedAt
            }).ToList()
        End Function

        Public Async Function LogAsync(action As AuditAction, entityName As String, entityId As String, description As String) As Task Implements IAuditLogService.LogAsync
            Dim log As New AuditLog With {
                .Action = action,
                .EntityName = entityName,
                .EntityId = entityId,
                .Description = description,
                .CreatedAt = DateTime.UtcNow
            }

            Await _auditRepo.InsertAsync(log)
        End Function
    End Class
End Namespace
