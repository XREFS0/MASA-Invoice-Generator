Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data

Namespace Repositories
    Public Class CustomerRepository
        Implements ICustomerRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing) As Task(Of IReadOnlyList(Of Customer)) Implements ICustomerRepository.GetAllAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, FullName, CompanyName, Email, Phone, Address, City, Country, PostalCode, TaxNumber, Notes, CreatedAt, UpdatedAt FROM Customers"
                Dim params As New DynamicParameters()

                If Not String.IsNullOrWhiteSpace(searchQuery) Then
                    sql &= " WHERE FullName LIKE @Query OR CompanyName LIKE @Query OR Email LIKE @Query OR Phone LIKE @Query OR TaxNumber LIKE @Query"
                    params.Add("@Query", $"%{searchQuery.Trim()}%")
                End If

                sql &= " ORDER BY FullName ASC;"
                Dim result = Await conn.QueryAsync(Of CustomerEntityRow)(sql, params)

                Return result.Select(Function(r) r.ToCustomer()).ToList()
            End Using
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of Customer) Implements ICustomerRepository.GetByIdAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, FullName, CompanyName, Email, Phone, Address, City, Country, PostalCode, TaxNumber, Notes, CreatedAt, UpdatedAt FROM Customers WHERE Id = @Id LIMIT 1;"
                Dim row = Await conn.QuerySingleOrDefaultAsync(Of CustomerEntityRow)(sql, New With {.Id = id})
                Return row?.ToCustomer()
            End Using
        End Function

        Public Async Function InsertAsync(customer As Customer) As Task(Of Long) Implements ICustomerRepository.InsertAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO Customers (FullName, CompanyName, Email, Phone, Address, City, Country, PostalCode, TaxNumber, Notes, CreatedAt, UpdatedAt)
                    VALUES (@FullName, @CompanyName, @Email, @Phone, @Address, @City, @Country, @PostalCode, @TaxNumber, @Notes, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();"

                Dim id = Await conn.ExecuteScalarAsync(Of Long)(sql, New With {
                    .FullName = customer.FullName,
                    .CompanyName = customer.CompanyName,
                    .Email = customer.Email,
                    .Phone = customer.Phone,
                    .Address = customer.Address,
                    .City = customer.City,
                    .Country = customer.Country,
                    .PostalCode = customer.PostalCode,
                    .TaxNumber = customer.TaxNumber,
                    .Notes = customer.Notes,
                    .CreatedAt = customer.CreatedAt.ToString("o"),
                    .UpdatedAt = customer.UpdatedAt.ToString("o")
                })

                Return id
            End Using
        End Function

        Public Async Function UpdateAsync(customer As Customer) As Task Implements ICustomerRepository.UpdateAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    UPDATE Customers
                    SET FullName = @FullName,
                        CompanyName = @CompanyName,
                        Email = @Email,
                        Phone = @Phone,
                        Address = @Address,
                        City = @City,
                        Country = @Country,
                        PostalCode = @PostalCode,
                        TaxNumber = @TaxNumber,
                        Notes = @Notes,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id;"

                Await conn.ExecuteAsync(sql, New With {
                    .Id = customer.Id,
                    .FullName = customer.FullName,
                    .CompanyName = customer.CompanyName,
                    .Email = customer.Email,
                    .Phone = customer.Phone,
                    .Address = customer.Address,
                    .City = customer.City,
                    .Country = customer.Country,
                    .PostalCode = customer.PostalCode,
                    .TaxNumber = customer.TaxNumber,
                    .Notes = customer.Notes,
                    .UpdatedAt = customer.UpdatedAt.ToString("o")
                })
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements ICustomerRepository.DeleteAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Await conn.ExecuteAsync("DELETE FROM Customers WHERE Id = @Id;", New With {.Id = id})
            End Using
        End Function

        Public Async Function HasInvoicesAsync(customerId As Long) As Task(Of Boolean) Implements ICustomerRepository.HasInvoicesAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim count = Await conn.ExecuteScalarAsync(Of Integer)("SELECT COUNT(1) FROM Invoices WHERE CustomerId = @CustomerId;", New With {.CustomerId = customerId})
                Return count > 0
            End Using
        End Function

        Private Class CustomerEntityRow
            Public Property Id As Long
            Public Property FullName As String
            Public Property CompanyName As String
            Public Property Email As String
            Public Property Phone As String
            Public Property Address As String
            Public Property City As String
            Public Property Country As String
            Public Property PostalCode As String
            Public Property TaxNumber As String
            Public Property Notes As String
            Public Property CreatedAt As String
            Public Property UpdatedAt As String

            Public Function ToCustomer() As Customer
                Dim created As DateTime
                Dim updated As DateTime
                DateTime.TryParse(CreatedAt, created)
                DateTime.TryParse(UpdatedAt, updated)

                Return New Customer With {
                    .Id = Id,
                    .FullName = If(FullName, String.Empty),
                    .CompanyName = If(CompanyName, String.Empty),
                    .Email = If(Email, String.Empty),
                    .Phone = If(Phone, String.Empty),
                    .Address = If(Address, String.Empty),
                    .City = If(City, String.Empty),
                    .Country = If(Country, String.Empty),
                    .PostalCode = If(PostalCode, String.Empty),
                    .TaxNumber = If(TaxNumber, String.Empty),
                    .Notes = If(Notes, String.Empty),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated)
                }
            End Function
        End Class
    End Class
End Namespace
