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
    Public Class InvoiceRepository
        Implements IInvoiceRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional statusFilter As InvoiceStatus? = Nothing, Optional customerId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of Invoice)) Implements IInvoiceRepository.GetAllAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    SELECT i.Id, i.InvoiceNumber, i.CustomerId, i.IssueDate, i.DueDate, i.Status, i.Currency, 
                           i.Subtotal, i.DiscountAmount, i.TaxAmount, i.TotalAmount, i.PaidAmount, i.RemainingAmount, 
                           i.Notes, i.CreatedAt, i.UpdatedAt,
                           c.FullName as CustomerName, c.Email as CustomerEmail
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE 1=1"

                Dim params As New DynamicParameters()

                If statusFilter.HasValue Then
                    sql &= " AND i.Status = @Status"
                    params.Add("@Status", CInt(statusFilter.Value))
                End If

                If customerId.HasValue Then
                    sql &= " AND i.CustomerId = @CustomerId"
                    params.Add("@CustomerId", customerId.Value)
                End If

                If fromDate.HasValue Then
                    sql &= " AND i.IssueDate >= @FromDate"
                    params.Add("@FromDate", fromDate.Value.ToString("yyyy-MM-dd"))
                End If

                If toDate.HasValue Then
                    sql &= " AND i.IssueDate <= @ToDate"
                    params.Add("@ToDate", toDate.Value.ToString("yyyy-MM-dd"))
                End If

                If Not String.IsNullOrWhiteSpace(searchQuery) Then
                    sql &= " AND (i.InvoiceNumber LIKE @Query OR c.FullName LIKE @Query OR c.CompanyName LIKE @Query)"
                    params.Add("@Query", $"%{searchQuery.Trim()}%")
                End If

                sql &= " ORDER BY i.IssueDate DESC, i.Id DESC;"

                Dim rows = Await conn.QueryAsync(Of InvoiceEntityRow)(sql, params)
                Return rows.Select(Function(r) r.ToInvoice()).ToList()
            End Using
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of Invoice) Implements IInvoiceRepository.GetByIdAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim invoiceSql = "
                    SELECT i.Id, i.InvoiceNumber, i.CustomerId, i.IssueDate, i.DueDate, i.Status, i.Currency, 
                           i.Subtotal, i.DiscountAmount, i.TaxAmount, i.TotalAmount, i.PaidAmount, i.RemainingAmount, 
                           i.Notes, i.CreatedAt, i.UpdatedAt,
                           c.FullName as CustomerName, c.Email as CustomerEmail
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE i.Id = @Id LIMIT 1;"

                Dim row = Await conn.QuerySingleOrDefaultAsync(Of InvoiceEntityRow)(invoiceSql, New With {.Id = id})
                If row Is Nothing Then
                    Return Nothing
                End If

                Dim invoice = row.ToInvoice()

                Dim itemsSql = "
                    SELECT it.Id, it.InvoiceId, it.ProductId, it.Description, it.Quantity, it.UnitPrice, it.DiscountAmount, it.TaxRate, it.TotalAmount
                    FROM InvoiceItems it
                    WHERE it.InvoiceId = @InvoiceId
                    ORDER BY it.Id ASC;"

                Dim itemRows = Await conn.QueryAsync(Of InvoiceItemEntityRow)(itemsSql, New With {.InvoiceId = id})
                invoice.Items = itemRows.Select(Function(ir) ir.ToInvoiceItem()).ToList()

                Return invoice
            End Using
        End Function

        Public Async Function GetByInvoiceNumberAsync(invoiceNumber As String) As Task(Of Invoice) Implements IInvoiceRepository.GetByInvoiceNumberAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim invoiceSql = "
                    SELECT i.Id, i.InvoiceNumber, i.CustomerId, i.IssueDate, i.DueDate, i.Status, i.Currency, 
                           i.Subtotal, i.DiscountAmount, i.TaxAmount, i.TotalAmount, i.PaidAmount, i.RemainingAmount, 
                           i.Notes, i.CreatedAt, i.UpdatedAt,
                           c.FullName as CustomerName, c.Email as CustomerEmail
                    FROM Invoices i
                    INNER JOIN Customers c ON i.CustomerId = c.Id
                    WHERE LOWER(i.InvoiceNumber) = LOWER(@InvoiceNumber) LIMIT 1;"

                Dim row = Await conn.QuerySingleOrDefaultAsync(Of InvoiceEntityRow)(invoiceSql, New With {.InvoiceNumber = invoiceNumber.Trim()})
                Return row?.ToInvoice()
            End Using
        End Function

        Public Async Function InsertWithItemsAsync(invoice As Invoice) As Task(Of Long) Implements IInvoiceRepository.InsertWithItemsAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Dim headerSql = "
                        INSERT INTO Invoices (InvoiceNumber, CustomerId, IssueDate, DueDate, Status, Currency, Subtotal, DiscountAmount, TaxAmount, TotalAmount, PaidAmount, RemainingAmount, Notes, CreatedAt, UpdatedAt)
                        VALUES (@InvoiceNumber, @CustomerId, @IssueDate, @DueDate, @Status, @Currency, @Subtotal, @DiscountAmount, @TaxAmount, @TotalAmount, @PaidAmount, @RemainingAmount, @Notes, @CreatedAt, @UpdatedAt);
                        SELECT last_insert_rowid();"

                    Dim invoiceId = Await conn.ExecuteScalarAsync(Of Long)(headerSql, New With {
                        .InvoiceNumber = invoice.InvoiceNumber.Trim(),
                        .CustomerId = invoice.CustomerId,
                        .IssueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                        .DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                        .Status = CInt(invoice.Status),
                        .Currency = invoice.Currency,
                        .Subtotal = invoice.Subtotal,
                        .DiscountAmount = invoice.DiscountAmount,
                        .TaxAmount = invoice.TaxAmount,
                        .TotalAmount = invoice.TotalAmount,
                        .PaidAmount = invoice.PaidAmount,
                        .RemainingAmount = invoice.RemainingAmount,
                        .Notes = invoice.Notes,
                        .CreatedAt = invoice.CreatedAt.ToString("o"),
                        .UpdatedAt = invoice.UpdatedAt.ToString("o")
                    }, transaction:=transaction)

                    Dim itemSql = "
                        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, DiscountAmount, TaxRate, TotalAmount)
                        VALUES (@InvoiceId, @ProductId, @Description, @Quantity, @UnitPrice, @DiscountAmount, @TaxRate, @TotalAmount);"

                    For Each it In invoice.Items
                        Await conn.ExecuteAsync(itemSql, New With {
                            .InvoiceId = invoiceId,
                            .ProductId = it.ProductId,
                            .Description = it.Description,
                            .Quantity = it.Quantity,
                            .UnitPrice = it.UnitPrice,
                            .DiscountAmount = it.DiscountAmount,
                            .TaxRate = it.TaxRate,
                            .TotalAmount = it.TotalAmount
                        }, transaction:=transaction)
                    Next

                    transaction.Commit()
                    Return invoiceId
                End Using
            End Using
        End Function

        Public Async Function UpdateWithItemsAsync(invoice As Invoice) As Task Implements IInvoiceRepository.UpdateWithItemsAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Dim headerSql = "
                        UPDATE Invoices
                        SET InvoiceNumber = @InvoiceNumber,
                            CustomerId = @CustomerId,
                            IssueDate = @IssueDate,
                            DueDate = @DueDate,
                            Status = @Status,
                            Currency = @Currency,
                            Subtotal = @Subtotal,
                            DiscountAmount = @DiscountAmount,
                            TaxAmount = @TaxAmount,
                            TotalAmount = @TotalAmount,
                            PaidAmount = @PaidAmount,
                            RemainingAmount = @RemainingAmount,
                            Notes = @Notes,
                            UpdatedAt = @UpdatedAt
                        WHERE Id = @Id;"

                    Await conn.ExecuteAsync(headerSql, New With {
                        .Id = invoice.Id,
                        .InvoiceNumber = invoice.InvoiceNumber.Trim(),
                        .CustomerId = invoice.CustomerId,
                        .IssueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                        .DueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                        .Status = CInt(invoice.Status),
                        .Currency = invoice.Currency,
                        .Subtotal = invoice.Subtotal,
                        .DiscountAmount = invoice.DiscountAmount,
                        .TaxAmount = invoice.TaxAmount,
                        .TotalAmount = invoice.TotalAmount,
                        .PaidAmount = invoice.PaidAmount,
                        .RemainingAmount = invoice.RemainingAmount,
                        .Notes = invoice.Notes,
                        .UpdatedAt = invoice.UpdatedAt.ToString("o")
                    }, transaction:=transaction)

                    ' Delete existing items and re-insert
                    Await conn.ExecuteAsync("DELETE FROM InvoiceItems WHERE InvoiceId = @InvoiceId;", New With {.InvoiceId = invoice.Id}, transaction:=transaction)

                    Dim itemSql = "
                        INSERT INTO InvoiceItems (InvoiceId, ProductId, Description, Quantity, UnitPrice, DiscountAmount, TaxRate, TotalAmount)
                        VALUES (@InvoiceId, @ProductId, @Description, @Quantity, @UnitPrice, @DiscountAmount, @TaxRate, @TotalAmount);"

                    For Each it In invoice.Items
                        Await conn.ExecuteAsync(itemSql, New With {
                            .InvoiceId = invoice.Id,
                            .ProductId = it.ProductId,
                            .Description = it.Description,
                            .Quantity = it.Quantity,
                            .UnitPrice = it.UnitPrice,
                            .DiscountAmount = it.DiscountAmount,
                            .TaxRate = it.TaxRate,
                            .TotalAmount = it.TotalAmount
                        }, transaction:=transaction)
                    Next

                    transaction.Commit()
                End Using
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements IInvoiceRepository.DeleteAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Await conn.ExecuteAsync("DELETE FROM InvoiceItems WHERE InvoiceId = @Id;", New With {.Id = id}, transaction:=transaction)
                    Await conn.ExecuteAsync("DELETE FROM Payments WHERE InvoiceId = @Id;", New With {.Id = id}, transaction:=transaction)
                    Await conn.ExecuteAsync("DELETE FROM Invoices WHERE Id = @Id;", New With {.Id = id}, transaction:=transaction)
                    transaction.Commit()
                End Using
            End Using
        End Function

        Public Async Function UpdateStatusAsync(id As Long, status As InvoiceStatus) As Task Implements IInvoiceRepository.UpdateStatusAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "UPDATE Invoices SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id;"
                Await conn.ExecuteAsync(sql, New With {
                    .Id = id,
                    .Status = CInt(status),
                    .UpdatedAt = DateTime.UtcNow.ToString("o")
                })
            End Using
        End Function

        Public Async Function RecalculatePaidAmountAsync(invoiceId As Long) As Task Implements IInvoiceRepository.RecalculatePaidAmountAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Dim totalPaid = Await conn.ExecuteScalarAsync(Of Decimal)("SELECT COALESCE(SUM(Amount), 0) FROM Payments WHERE InvoiceId = @InvoiceId;", New With {.InvoiceId = invoiceId}, transaction:=transaction)
                    Dim invoiceRow = Await conn.QuerySingleOrDefaultAsync(Of InvoiceEntityRow)("SELECT * FROM Invoices WHERE Id = @Id;", New With {.Id = invoiceId}, transaction:=transaction)

                    If invoiceRow IsNot Nothing Then
                        Dim totalAmount = invoiceRow.TotalAmount
                        Dim remaining = Math.Max(0D, totalAmount - totalPaid)

                        Dim currentStatus = CType(invoiceRow.Status, InvoiceStatus)
                        Dim newStatus = currentStatus

                        If currentStatus <> InvoiceStatus.Cancelled Then
                            Dim dueDate As DateTime
                            DateTime.TryParse(invoiceRow.DueDate, dueDate)

                            If totalPaid >= totalAmount AndAlso totalAmount > 0 Then
                                newStatus = InvoiceStatus.Paid
                            ElseIf totalPaid > 0 Then
                                newStatus = InvoiceStatus.PartialPayment
                            ElseIf dueDate.Date < DateTime.Today AndAlso currentStatus <> InvoiceStatus.Draft Then
                                newStatus = InvoiceStatus.Overdue
                            ElseIf currentStatus <> InvoiceStatus.Draft Then
                                newStatus = InvoiceStatus.Sent
                            End If
                        End If

                        Dim updateSql = "
                            UPDATE Invoices 
                            SET PaidAmount = @PaidAmount,
                                RemainingAmount = @RemainingAmount,
                                Status = @Status,
                                UpdatedAt = @UpdatedAt
                            WHERE Id = @Id;"

                        Await conn.ExecuteAsync(updateSql, New With {
                            .Id = invoiceId,
                            .PaidAmount = totalPaid,
                            .RemainingAmount = remaining,
                            .Status = CInt(newStatus),
                            .UpdatedAt = DateTime.UtcNow.ToString("o")
                        }, transaction:=transaction)
                    End If

                    transaction.Commit()
                End Using
            End Using
        End Function

        Private Class InvoiceEntityRow
            Public Property Id As Long
            Public Property InvoiceNumber As String
            Public Property CustomerId As Long
            Public Property CustomerName As String
            Public Property CustomerEmail As String
            Public Property IssueDate As String
            Public Property DueDate As String
            Public Property Status As Integer
            Public Property Currency As String
            Public Property Subtotal As Decimal
            Public Property DiscountAmount As Decimal
            Public Property TaxAmount As Decimal
            Public Property TotalAmount As Decimal
            Public Property PaidAmount As Decimal
            Public Property RemainingAmount As Decimal
            Public Property Notes As String
            Public Property CreatedAt As String
            Public Property UpdatedAt As String

            Public Function ToInvoice() As Invoice
                Dim issue As DateTime
                Dim [due] As DateTime
                Dim created As DateTime
                Dim updated As DateTime

                DateTime.TryParse(IssueDate, issue)
                DateTime.TryParse(DueDate, [due])
                DateTime.TryParse(CreatedAt, created)
                DateTime.TryParse(UpdatedAt, updated)

                Return New Invoice With {
                    .Id = Id,
                    .InvoiceNumber = If(InvoiceNumber, String.Empty),
                    .CustomerId = CustomerId,
                    .CustomerName = If(CustomerName, String.Empty),
                    .CustomerEmail = If(CustomerEmail, String.Empty),
                    .IssueDate = If(issue = DateTime.MinValue, DateTime.Today, issue),
                    .DueDate = If([due] = DateTime.MinValue, DateTime.Today.AddDays(30), [due]),
                    .Status = CType(Status, InvoiceStatus),
                    .Currency = If(Currency, "USD"),
                    .Subtotal = Subtotal,
                    .DiscountAmount = DiscountAmount,
                    .TaxAmount = TaxAmount,
                    .TotalAmount = TotalAmount,
                    .PaidAmount = PaidAmount,
                    .RemainingAmount = RemainingAmount,
                    .Notes = If(Notes, String.Empty),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated)
                }
            End Function
        End Class

        Private Class InvoiceItemEntityRow
            Public Property Id As Long
            Public Property InvoiceId As Long
            Public Property ProductId As Long?
            Public Property Description As String
            Public Property Quantity As Decimal
            Public Property UnitPrice As Decimal
            Public Property DiscountAmount As Decimal
            Public Property TaxRate As Decimal
            Public Property TotalAmount As Decimal

            Public Function ToInvoiceItem() As InvoiceItem
                Return New InvoiceItem With {
                    .Id = Id,
                    .InvoiceId = InvoiceId,
                    .ProductId = ProductId,
                    .Description = If(Description, String.Empty),
                    .Quantity = Quantity,
                    .UnitPrice = UnitPrice,
                    .DiscountAmount = DiscountAmount,
                    .TaxRate = TaxRate,
                    .TotalAmount = TotalAmount
                }
            End Function
        End Class
    End Class
End Namespace
