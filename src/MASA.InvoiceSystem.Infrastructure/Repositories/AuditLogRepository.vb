Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data

Namespace Repositories
    Public Class AuditLogRepository
        Implements IAuditLogRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetRecentLogsAsync(Optional limit As Integer = 200, Optional entityName As String = Nothing) As Task(Of IReadOnlyList(Of AuditLog)) Implements IAuditLogRepository.GetRecentLogsAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, Action, EntityName, EntityId, Description, CreatedAt FROM AuditLogs"
                Dim params As New DynamicParameters()

                If Not String.IsNullOrWhiteSpace(entityName) Then
                    sql &= " WHERE EntityName = @EntityName"
                    params.Add("@EntityName", entityName)
                End If

                sql &= " ORDER BY Id DESC LIMIT @Limit;"
                params.Add("@Limit", Math.Max(1, limit))

                Dim rows = Await conn.QueryAsync(Of AuditLogEntityRow)(sql, params)
                Return rows.Select(Function(r) r.ToAuditLog()).ToList()
            End Using
        End Function

        Public Async Function InsertAsync(log As AuditLog) As Task(Of Long) Implements IAuditLogRepository.InsertAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO AuditLogs (Action, EntityName, EntityId, Description, CreatedAt)
                    VALUES (@Action, @EntityName, @EntityId, @Description, @CreatedAt);
                    SELECT last_insert_rowid();"

                Dim id = Await conn.ExecuteScalarAsync(Of Long)(sql, New With {
                    .Action = CInt(log.Action),
                    .EntityName = log.EntityName,
                    .EntityId = log.EntityId,
                    .Description = log.Description,
                    .CreatedAt = log.CreatedAt.ToString("o")
                })

                Return id
            End Using
        End Function

        Private Class AuditLogEntityRow
            Public Property Id As Long
            Public Property Action As Integer
            Public Property EntityName As String
            Public Property EntityId As String
            Public Property Description As String
            Public Property CreatedAt As String

            Public Function ToAuditLog() As AuditLog
                Dim created As DateTime
                DateTime.TryParse(CreatedAt, created)

                Return New AuditLog With {
                    .Id = Id,
                    .Action = CType(Action, AuditAction),
                    .EntityName = If(EntityName, String.Empty),
                    .EntityId = If(EntityId, String.Empty),
                    .Description = If(Description, String.Empty),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created)
                }
            End Function
        End Class
    End Class
End Namespace
