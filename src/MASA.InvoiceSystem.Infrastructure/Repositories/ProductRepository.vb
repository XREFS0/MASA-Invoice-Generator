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
    Public Class ProductRepository
        Implements IProductRepository

        Private ReadOnly _connFactory As ISqliteConnectionFactory

        Public Sub New(connFactory As ISqliteConnectionFactory)
            _connFactory = connFactory
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional onlyActive As Boolean = False, Optional typeFilter As ProductType? = Nothing) As Task(Of IReadOnlyList(Of Product)) Implements IProductRepository.GetAllAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt FROM Products WHERE 1=1"
                Dim params As New DynamicParameters()

                If onlyActive Then
                    sql &= " AND IsActive = 1"
                End If

                If typeFilter.HasValue Then
                    sql &= " AND Type = @Type"
                    params.Add("@Type", CInt(typeFilter.Value))
                End If

                If Not String.IsNullOrWhiteSpace(searchQuery) Then
                    sql &= " AND (ProductCode LIKE @Query OR Name LIKE @Query OR Description LIKE @Query)"
                    params.Add("@Query", $"%{searchQuery.Trim()}%")
                End If

                sql &= " ORDER BY Name ASC;"
                Dim rows = Await conn.QueryAsync(Of ProductEntityRow)(sql, params)
                Return rows.Select(Function(r) r.ToProduct()).ToList()
            End Using
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of Product) Implements IProductRepository.GetByIdAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt FROM Products WHERE Id = @Id LIMIT 1;"
                Dim row = Await conn.QuerySingleOrDefaultAsync(Of ProductEntityRow)(sql, New With {.Id = id})
                Return row?.ToProduct()
            End Using
        End Function

        Public Async Function GetByCodeAsync(code As String) As Task(Of Product) Implements IProductRepository.GetByCodeAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt FROM Products WHERE LOWER(ProductCode) = LOWER(@Code) LIMIT 1;"
                Dim row = Await conn.QuerySingleOrDefaultAsync(Of ProductEntityRow)(sql, New With {.Code = code.Trim()})
                Return row?.ToProduct()
            End Using
        End Function

        Public Async Function InsertAsync(product As Product) As Task(Of Long) Implements IProductRepository.InsertAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    INSERT INTO Products (ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt)
                    VALUES (@ProductCode, @Name, @Description, @Type, @UnitPrice, @CostPrice, @TaxRateId, @StockQuantity, @MinimumStock, @IsActive, @CreatedAt, @UpdatedAt);
                    SELECT last_insert_rowid();"

                Dim id = Await conn.ExecuteScalarAsync(Of Long)(sql, New With {
                    .ProductCode = product.ProductCode.Trim(),
                    .Name = product.Name.Trim(),
                    .Description = product.Description,
                    .Type = CInt(product.Type),
                    .UnitPrice = product.UnitPrice,
                    .CostPrice = product.CostPrice,
                    .TaxRateId = product.TaxRateId,
                    .StockQuantity = product.StockQuantity,
                    .MinimumStock = product.MinimumStock,
                    .IsActive = If(product.IsActive, 1, 0),
                    .CreatedAt = product.CreatedAt.ToString("o"),
                    .UpdatedAt = product.UpdatedAt.ToString("o")
                })

                Return id
            End Using
        End Function

        Public Async Function UpdateAsync(product As Product) As Task Implements IProductRepository.UpdateAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    UPDATE Products
                    SET ProductCode = @ProductCode,
                        Name = @Name,
                        Description = @Description,
                        Type = @Type,
                        UnitPrice = @UnitPrice,
                        CostPrice = @CostPrice,
                        TaxRateId = @TaxRateId,
                        StockQuantity = @StockQuantity,
                        MinimumStock = @MinimumStock,
                        IsActive = @IsActive,
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id;"

                Await conn.ExecuteAsync(sql, New With {
                    .Id = product.Id,
                    .ProductCode = product.ProductCode.Trim(),
                    .Name = product.Name.Trim(),
                    .Description = product.Description,
                    .Type = CInt(product.Type),
                    .UnitPrice = product.UnitPrice,
                    .CostPrice = product.CostPrice,
                    .TaxRateId = product.TaxRateId,
                    .StockQuantity = product.StockQuantity,
                    .MinimumStock = product.MinimumStock,
                    .IsActive = If(product.IsActive, 1, 0),
                    .UpdatedAt = product.UpdatedAt.ToString("o")
                })
            End Using
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements IProductRepository.DeleteAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Await conn.ExecuteAsync("DELETE FROM Products WHERE Id = @Id;", New With {.Id = id})
            End Using
        End Function

        Public Async Function GetLowStockAsync() As Task(Of IReadOnlyList(Of Product)) Implements IProductRepository.GetLowStockAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "SELECT Id, ProductCode, Name, Description, Type, UnitPrice, CostPrice, TaxRateId, StockQuantity, MinimumStock, IsActive, CreatedAt, UpdatedAt FROM Products WHERE Type = 0 AND StockQuantity <= MinimumStock AND IsActive = 1 ORDER BY StockQuantity ASC;"
                Dim rows = Await conn.QueryAsync(Of ProductEntityRow)(sql)
                Return rows.Select(Function(r) r.ToProduct()).ToList()
            End Using
        End Function

        Public Async Function AdjustStockAsync(productId As Long, quantityDelta As Decimal) As Task Implements IProductRepository.AdjustStockAsync
            Using conn = Await _connFactory.CreateOpenConnectionAsync()
                Dim sql = "
                    UPDATE Products 
                    SET StockQuantity = MAX(0, StockQuantity + @Delta),
                        UpdatedAt = @UpdatedAt
                    WHERE Id = @Id AND Type = 0;"

                Await conn.ExecuteAsync(sql, New With {
                    .Id = productId,
                    .Delta = quantityDelta,
                    .UpdatedAt = DateTime.UtcNow.ToString("o")
                })
            End Using
        End Function

        Private Class ProductEntityRow
            Public Property Id As Long
            Public Property ProductCode As String
            Public Property Name As String
            Public Property Description As String
            Public Property Type As Integer
            Public Property UnitPrice As Decimal
            Public Property CostPrice As Decimal
            Public Property TaxRateId As Long?
            Public Property StockQuantity As Decimal
            Public Property MinimumStock As Decimal
            Public Property IsActive As Integer
            Public Property CreatedAt As String
            Public Property UpdatedAt As String

            Public Function ToProduct() As Product
                Dim created As DateTime
                Dim updated As DateTime
                DateTime.TryParse(CreatedAt, created)
                DateTime.TryParse(UpdatedAt, updated)

                Return New Product With {
                    .Id = Id,
                    .ProductCode = If(ProductCode, String.Empty),
                    .Name = If(Name, String.Empty),
                    .Description = If(Description, String.Empty),
                    .Type = CType(Type, ProductType),
                    .UnitPrice = UnitPrice,
                    .CostPrice = CostPrice,
                    .TaxRateId = TaxRateId,
                    .StockQuantity = StockQuantity,
                    .MinimumStock = MinimumStock,
                    .IsActive = (IsActive = 1),
                    .CreatedAt = If(created = DateTime.MinValue, DateTime.UtcNow, created),
                    .UpdatedAt = If(updated = DateTime.MinValue, DateTime.UtcNow, updated)
                }
            End Function
        End Class
    End Class
End Namespace
