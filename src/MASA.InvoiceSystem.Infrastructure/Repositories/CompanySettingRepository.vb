Imports System
Imports System.Threading.Tasks
Imports Dapper
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports MASA.InvoiceSystem.Infrastructure.Data

Namespace Repositories
    Public Class CompanySettingRepository
        Implements ICompanySettingRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetSettingAsync() As Task(Of CompanySetting) Implements ICompanySettingRepository.GetSettingAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, CompanyName, LogoPath, Email, Phone, Website, Address, City, Country, PostalCode, TaxNumber, DefaultCurrency, DefaultTaxRateId, InvoicePrefix, NextInvoiceNumber, UpdatedAt FROM CompanySettings WHERE Id = 1 LIMIT 1;"
                Dim row = Await conn.QuerySingleOrDefaultAsync(Of CompanySettingEntityRow)(sql)
                Return If(row IsNot Nothing, row.ToCompanySetting(), New CompanySetting())
            End Using
        End Function

        Public Async Function UpdateSettingAsync(setting As CompanySetting) As Task Implements ICompanySettingRepository.UpdateSettingAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO CompanySettings (Id, CompanyName, LogoPath, Email, Phone, Website, Address, City, Country, PostalCode, TaxNumber, DefaultCurrency, DefaultTaxRateId, InvoicePrefix, NextInvoiceNumber, UpdatedAt)
                    VALUES (1, @CompanyName, @LogoPath, @Email, @Phone, @Website, @Address, @City, @Country, @PostalCode, @TaxNumber, @DefaultCurrency, @DefaultTaxRateId, @InvoicePrefix, @NextInvoiceNumber, @UpdatedAt)
                    ON CONFLICT(Id) DO UPDATE SET
                        CompanyName = @CompanyName,
                        LogoPath = @LogoPath,
                        Email = @Email,
                        Phone = @Phone,
                        Website = @Website,
                        Address = @Address,
                        City = @City,
                        Country = @Country,
                        PostalCode = @PostalCode,
                        TaxNumber = @TaxNumber,
                        DefaultCurrency = @DefaultCurrency,
                        DefaultTaxRateId = @DefaultTaxRateId,
                        InvoicePrefix = @InvoicePrefix,
                        NextInvoiceNumber = @NextInvoiceNumber,
                        UpdatedAt = @UpdatedAt;"

                Await conn.ExecuteAsync(sql, New With {
                    .CompanyName = setting.CompanyName,
                    .LogoPath = setting.LogoPath,
                    .Email = setting.Email,
                    .Phone = setting.Phone,
                    .Website = setting.Website,
                    .Address = setting.Address,
                    .City = setting.City,
                    .Country = setting.Country,
                    .PostalCode = setting.PostalCode,
                    .TaxNumber = setting.TaxNumber,
                    .DefaultCurrency = setting.DefaultCurrency,
                    .DefaultTaxRateId = setting.DefaultTaxRateId,
                    .InvoicePrefix = setting.InvoicePrefix,
                    .NextInvoiceNumber = setting.NextInvoiceNumber,
                    .UpdatedAt = DateTime.UtcNow.ToString("o")
                })
            End Using
        End Function

        Public Async Function GetAndIncrementNextInvoiceNumberAsync(prefix As String) As Task(Of String) Implements ICompanySettingRepository.GetAndIncrementNextInvoiceNumberAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Using transaction = conn.BeginTransaction()
                    Dim nextNumber = Await conn.ExecuteScalarAsync(Of Long)("SELECT NextInvoiceNumber FROM CompanySettings WHERE Id = 1;", transaction:=transaction)
                    If nextNumber <= 0 Then nextNumber = 1001

                    Dim formatted = $"{prefix}{nextNumber:D6}"

                    Await conn.ExecuteAsync("UPDATE CompanySettings SET NextInvoiceNumber = NextInvoiceNumber + 1, UpdatedAt = @UpdatedAt WHERE Id = 1;", New With {.UpdatedAt = DateTime.UtcNow.ToString("o")}, transaction:=transaction)

                    transaction.Commit()
                    Return formatted
                End Using
            End Using
        End Function

        Private Class CompanySettingEntityRow
            Public Property Id As Long
            Public Property CompanyName As String
            Public Property LogoPath As String
            Public Property Email As String
            Public Property Phone As String
            Public Property Website As String
            Public Property Address As String
            Public Property City As String
            Public Property Country As String
            Public Property PostalCode As String
            Public Property TaxNumber As String
            Public Property DefaultCurrency As String
            Public Property DefaultTaxRateId As Long?
            Public Property InvoicePrefix As String
            Public Property NextInvoiceNumber As Long
            Public Property UpdatedAt As String

            Public Function ToCompanySetting() As CompanySetting
                Dim updated As DateTime
                DateTime.TryParse(UpdatedAt, updated)

                Return New CompanySetting With {
                    .Id = Id,
                    .CompanyName = If(CompanyName, "MASA Global Solutions"),
                    .LogoPath = If(LogoPath, String.Empty),
                    .Email = If(Email, String.Empty),
                    .Phone = If(Phone, String.Empty),
                    .Website = If(Website, String.Empty),
                    .Address = If(Address, String.Empty),
                    .City = If(City, String.Empty),
                    .Country = If(Country, String.Empty),
                    .PostalCode = If(PostalCode, String.Empty),
                    .TaxNumber = If(TaxNumber, String.Empty),
                    .DefaultCurrency = If(DefaultCurrency, "USD"),
                    .DefaultTaxRateId = DefaultTaxRateId,
                    .InvoicePrefix = If(InvoicePrefix, "INV-"),
                    .NextInvoiceNumber = If(NextInvoiceNumber <= 0, 1001, NextInvoiceNumber),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated)
                }
            End Function
        End Class
    End Class
End Namespace
