Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Validators
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class ProductService
        Implements IProductService

        Private ReadOnly _productRepo As IProductRepository
        Private ReadOnly _taxRateRepo As ITaxRateRepository
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of ProductService)

        Public Sub New(productRepo As IProductRepository, taxRateRepo As ITaxRateRepository, auditLogService As IAuditLogService, logger As ILogger(Of ProductService))
            _productRepo = productRepo
            _taxRateRepo = taxRateRepo
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional onlyActive As Boolean = False, Optional typeFilter As ProductType? = Nothing) As Task(Of IReadOnlyList(Of ProductDto)) Implements IProductService.GetAllAsync
            Dim products = Await _productRepo.GetAllAsync(searchQuery, onlyActive, typeFilter)
            Dim taxRates = Await _taxRateRepo.GetAllAsync()
            Dim taxMap = taxRates.ToDictionary(Function(t) t.Id, Function(t) t.Percentage)

            Return products.Select(Function(p) New ProductDto With {
                .Id = p.Id,
                .ProductCode = p.ProductCode,
                .Name = p.Name,
                .Description = p.Description,
                .Type = p.Type,
                .UnitPrice = p.UnitPrice,
                .CostPrice = p.CostPrice,
                .TaxRateId = p.TaxRateId,
                .TaxRatePercentage = If(p.TaxRateId.HasValue AndAlso taxMap.ContainsKey(p.TaxRateId.Value), taxMap(p.TaxRateId.Value), 0D),
                .StockQuantity = p.StockQuantity,
                .MinimumStock = p.MinimumStock,
                .IsActive = p.IsActive,
                .CreatedAt = p.CreatedAt,
                .UpdatedAt = p.UpdatedAt
            }).ToList()
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of ProductDto) Implements IProductService.GetByIdAsync
            Dim p = Await _productRepo.GetByIdAsync(id)
            If p Is Nothing Then
                Throw New NotFoundException("Product", id)
            End If

            Dim taxPercentage As Decimal = 0D
            If p.TaxRateId.HasValue Then
                Dim tax = Await _taxRateRepo.GetByIdAsync(p.TaxRateId.Value)
                If tax IsNot Nothing Then
                    taxPercentage = tax.Percentage
                End If
            End If

            Return New ProductDto With {
                .Id = p.Id,
                .ProductCode = p.ProductCode,
                .Name = p.Name,
                .Description = p.Description,
                .Type = p.Type,
                .UnitPrice = p.UnitPrice,
                .CostPrice = p.CostPrice,
                .TaxRateId = p.TaxRateId,
                .TaxRatePercentage = taxPercentage,
                .StockQuantity = p.StockQuantity,
                .MinimumStock = p.MinimumStock,
                .IsActive = p.IsActive,
                .CreatedAt = p.CreatedAt,
                .UpdatedAt = p.UpdatedAt
            }
        End Function

        Public Async Function SaveAsync(product As Product) As Task(Of ProductDto) Implements IProductService.SaveAsync
            EntityValidators.ValidateProduct(product)

            Dim existingCode = Await _productRepo.GetByCodeAsync(product.ProductCode.Trim())
            If existingCode IsNot Nothing AndAlso existingCode.Id <> product.Id Then
                Throw New ValidationException($"Product code '{product.ProductCode}' is already in use by another item.")
            End If

            If product.Id = 0 Then
                product.CreatedAt = DateTime.UtcNow
                product.UpdatedAt = DateTime.UtcNow
                Dim id = Await _productRepo.InsertAsync(product)
                product.Id = id
                Await _auditLogService.LogAsync(AuditAction.Created, "Product", id.ToString(), $"Created product/service '{product.Name}' ({product.ProductCode}).")
                _logger.LogInformation("Created product {ProductId} - {Code}", id, product.ProductCode)
            Else
                product.UpdatedAt = DateTime.UtcNow
                Await _productRepo.UpdateAsync(product)
                Await _auditLogService.LogAsync(AuditAction.Updated, "Product", product.Id.ToString(), $"Updated product/service '{product.Name}' ({product.ProductCode}).")
                _logger.LogInformation("Updated product {ProductId} - {Code}", product.Id, product.ProductCode)
            End If

            Return Await GetByIdAsync(product.Id)
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements IProductService.DeleteAsync
            Dim existing = Await _productRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("Product", id)
            End If

            Await _productRepo.DeleteAsync(id)
            Await _auditLogService.LogAsync(AuditAction.Deleted, "Product", id.ToString(), $"Deleted product/service '{existing.Name}' ({existing.ProductCode}).")
            _logger.LogInformation("Deleted product {ProductId}", id)
        End Function

        Public Async Function GetLowStockProductsAsync() As Task(Of IReadOnlyList(Of ProductDto)) Implements IProductService.GetLowStockProductsAsync
            Dim lowStock = Await _productRepo.GetLowStockAsync()
            Return lowStock.Select(Function(p) New ProductDto With {
                .Id = p.Id,
                .ProductCode = p.ProductCode,
                .Name = p.Name,
                .Description = p.Description,
                .Type = p.Type,
                .UnitPrice = p.UnitPrice,
                .CostPrice = p.CostPrice,
                .StockQuantity = p.StockQuantity,
                .MinimumStock = p.MinimumStock,
                .IsActive = p.IsActive
            }).ToList()
        End Function
    End Class
End Namespace
