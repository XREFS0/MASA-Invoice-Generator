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
    Public Class PaymentRepository
        Implements IPaymentRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional invoiceId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of Payment)) Implements IPaymentRepository.GetAllAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    SELECT p.Id, p.InvoiceId, p.Amount, p.PaymentDate, p.PaymentMethod, p.ReferenceNumber, p.Notes, p.CreatedAt, p.UpdatedAt,
                           i.InvoiceNumber, c.FullName as CustomerName
                    FROM Payments p
                    INNER JOIN Invoices i ON p.InvoiceId = i.Id
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE 1=1"

                Dim params As New DynamicParameters()

                If invoiceId.HasValue Then
                    sql &= " AND p.InvoiceId = @InvoiceId"
                    params.Add("@InvoiceId", invoiceId.Value)
                End If

                If fromDate.HasValue Then
                    sql &= " AND p.PaymentDate >= @FromDate"
                    params.Add("@FromDate", fromDate.Value.ToString("yyyy-MM-dd"))
                End If

                If toDate.HasValue Then
                    sql &= " AND p.PaymentDate <= @ToDate"
                    params.Add("@ToDate", toDate.Value.ToString("yyyy-MM-dd"))
                End If

                If Not String.IsNullOrWhiteSpace(searchQuery) Then
                    sql &= " AND (i.InvoiceNumber LIKE @Query OR c.FullName LIKE @Query OR p.ReferenceNumber LIKE @Query)"
                    params.Add("@Query", $"%{searchQuery.Trim()}%")
                End If

                sql &= " ORDER BY p.PaymentDate DESC, p.Id DESC;"

                Dim rows = Await conn.QueryAsync(Of PaymentEntityRow)(sql, params)
                Return rows.Select(Function(r) r.ToPayment()).ToList()
            End Using
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of Payment) Implements IPaymentRepository.GetByIdAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    SELECT p.Id, p.InvoiceId, p.Amount, p.PaymentDate, p.PaymentMethod, p.ReferenceNumber, p.Notes, p.CreatedAt, p.UpdatedAt,
                           i.InvoiceNumber, c.FullName as CustomerName
                    FROM Payments p
                    INNER JOIN Invoices i ON p.InvoiceId = i.Id
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE p.Id = @Id LIMIT 1;"

                Dim row = Await conn.QuerySingleOrDefaultAsync(Of PaymentEntityRow)(sql, New With {.Id = id})
                Return row?.ToPayment()
            End Using
        End Function

        Public Async Function InsertAsync(payment As Payment) As Task(Of Long) Implements IPaymentRepository.InsertAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO Payments (InvoiceId, Amount, PaymentDate, PaymentMethod, ReferenceNumber, Notes, CreatedAt, UpdatedAt)
                    VALUES (@InvoiceId, @Amount, @PaymentDate, @PaymentMethod, @ReferenceNumber, @Notes, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();"

                Dim id = Await conn.ExecuteScalarAsync(Of Long)(sql, New With {
                    .InvoiceId = payment.InvoiceId,
                    .Amount = payment.Amount,
                    .PaymentDate = payment.PaymentDate.ToString("yyyy-MM-dd"),
                    .PaymentMethod = CInt(payment.PaymentMethod),
                    .ReferenceNumber = payment.ReferenceNumber,
                    .Notes = payment.Notes,
                    .CreatedAt = payment.CreatedAt.ToString("o"),
                    .UpdatedAt = payment.UpdatedAt.ToString("o")
                })

                Return id
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements IPaymentRepository.DeleteAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Await conn.ExecuteAsync("DELETE FROM Payments WHERE Id = @Id;", New With {.Id = id})
            End Using
        End Function

        Public Async Function GetTotalPaidForInvoiceAsync(invoiceId As Long) As Task(Of Decimal) Implements IPaymentRepository.GetTotalPaidForInvoiceAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim total = Await conn.ExecuteScalarAsync(Of Decimal)("SELECT COALESCE(SUM(Amount), 0) FROM Payments WHERE InvoiceId = @InvoiceId;", New With {.InvoiceId = invoiceId})
                Return total
            End Using
        End Function

        Private Class PaymentEntityRow
            Public Property Id As Long
            Public Property InvoiceId As Long
            Public Property Amount As Decimal
            Public Property PaymentDate As String
            Public Property PaymentMethod As Integer
            Public Property ReferenceNumber As String
            Public Property Notes As String
            Public Property CreatedAt As String
            Public Property UpdatedAt As String
            Public Property InvoiceNumber As String
            Public Property CustomerName As String

            Public Function ToPayment() As Payment
                Dim pDate As DateTime
                Dim created As DateTime
                Dim updated As DateTime

                DateTime.TryParse(PaymentDate, pDate)
                DateTime.TryParse(CreatedAt, created)
                DateTime.TryParse(UpdatedAt, updated)

                Return New Payment With {
                    .Id = Id,
                    .InvoiceId = InvoiceId,
                    .Amount = Amount,
                    .PaymentDate = If(pDate = DateTime.MinValue, DateTime.Today, pDate),
                    .PaymentMethod = CType(PaymentMethod, PaymentMethod),
                    .ReferenceNumber = If(ReferenceNumber, String.Empty),
                    .Notes = If(Notes, String.Empty),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated),
                    .InvoiceNumber = If(InvoiceNumber, String.Empty),
                    .CustomerName = If(CustomerName, String.Empty)
                }
            End Function
        End Class
    End Class
End Namespace
