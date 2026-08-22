Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports Dapper
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data

Namespace Repositories
    Public Class TaxRateRepository
        Implements ITaxRateRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of IReadOnlyList(Of TaxRate)) Implements ITaxRateRepository.GetAllAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, Name, Percentage, IsDefault, IsActive, CreatedAt, UpdatedAt FROM TaxRates"
                If onlyActive Then
                    sql &= " WHERE IsActive = 1"
                End If
                sql &= " ORDER BY Percentage ASC, Name ASC;"

                Dim rows = Await conn.QueryAsync(Of TaxRateEntityRow)(sql)
                Return rows.Select(Function(r) r.ToTaxRate()).ToList()
            End Using
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of TaxRate) Implements ITaxRateRepository.GetByIdAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, Name, Percentage, IsDefault, IsActive, CreatedAt, UpdatedAt FROM TaxRates WHERE Id = @Id LIMIT 1;"
                Dim row = Await conn.QuerySingleOrDefaultAsync(Of TaxRateEntityRow)(sql, New With {.Id = id})
                Return row?.ToTaxRate()
            End Using
        End Function

        Public Async Function InsertAsync(taxRate As TaxRate) As Task(Of Long) Implements ITaxRateRepository.InsertAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO TaxRates (Name, Percentage, IsDefault, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@Name, @Percentage, @IsDefault, @IsActive, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();"

                Dim id = Await conn.ExecuteScalarAsync(Of Long)(sql, New With {
                    .Name = taxRate.Name.Trim(),
                    .Percentage = taxRate.Percentage,
                    .IsDefault = If(taxRate.IsDefault, 1, 0),
                    .IsActive = If(taxRate.IsActive, 1, 0),
                    .CreatedAt = taxRate.CreatedAt.ToString("o"),
                    .UpdatedAt = taxRate.UpdatedAt.ToString("o")
                })

                Return id
            End Using
        End Function

        Public Async Function UpdateAsync(taxRate As TaxRate) As Task Implements ITaxRateRepository.UpdateAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    UPDATE TaxRates
                    SET Name = @Name,
                        Percentage = @Percentage,
                        IsDefault = @IsDefault,
                        IsActive = @IsActive,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id;"

                Await conn.ExecuteAsync(sql, New With {
                    .Id = taxRate.Id,
                    .Name = taxRate.Name.Trim(),
                    .Percentage = taxRate.Percentage,
                    .IsDefault = If(taxRate.IsDefault, 1, 0),
                    .IsActive = If(taxRate.IsActive, 1, 0),
                    .UpdatedAt = taxRate.UpdatedAt.ToString("o")
                })
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements ITaxRateRepository.DeleteAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Await conn.ExecuteAsync("DELETE FROM TaxRates WHERE Id = @Id;", New With {.Id = id})
            End Using
        End Function

        Public Async Function SetDefaultAsync(id As Long) As Task Implements ITaxRateRepository.SetDefaultAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Await conn.ExecuteAsync("UPDATE TaxRates SET IsDefault = 0;", transaction:=transaction)
                    Await conn.ExecuteAsync("UPDATE TaxRates SET IsDefault = 1, IsActive = 1 WHERE Id = @Id;", New With {.Id = id}, transaction:=transaction)
                    Await conn.ExecuteAsync("UPDATE CompanySettings SET DefaultTaxRateId = @Id, UpdatedAt = @UpdatedAt WHERE Id = 1;", New With {.Id = id, .UpdatedAt = DateTime.UtcNow.ToString("o")}, transaction:=transaction)
                    transaction.Commit()
                End Using
            End Using
        End Function

        Private Class TaxRateEntityRow
            Public Property Id As Long
            Public Property Name As String
            Public Property Percentage As Decimal
            Public Property IsDefault As Integer
            Public Property IsActive As Integer
            Public Property CreatedAt As String
            Public Property UpdatedAt As String

            Public Function ToTaxRate() As TaxRate
                Dim created As DateTime
                Dim updated As DateTime
                DateTime.TryParse(CreatedAt, created)
                DateTime.TryParse(UpdatedAt, updated)

                Return New TaxRate With {
                    .Id = Id,
                    .Name = If(Name, String.Empty),
                    .Percentage = Percentage,
                    .IsDefault = (IsDefault = 1),
                    .IsActive = (IsActive = 1),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated)
                }
            End Function
        End Class
    End Class
End Namespace
