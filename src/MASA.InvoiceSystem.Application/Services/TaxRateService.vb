Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class TaxRateService
        Implements ITaxRateService

        Private ReadOnly _taxRepo As ITaxRateRepository
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of TaxRateService)

        Public Sub New(taxRepo As ITaxRateRepository, auditLogService As IAuditLogService, logger As ILogger(Of TaxRateService))
            _taxRepo = taxRepo
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetAllAsync(Optional onlyActive As Boolean = False) As Task(Of IReadOnlyList(Of TaxRateDto)) Implements ITaxRateService.GetAllAsync
            Dim rates = Await _taxRepo.GetAllAsync(onlyActive)
            Return rates.Select(Function(r) New TaxRateDto With {
                .Id = r.Id,
                .Name = r.Name,
                .Percentage = r.Percentage,
                .IsDefault = r.IsDefault,
                .IsActive = r.IsActive,
                .CreatedAt = r.CreatedAt,
                .UpdatedAt = r.UpdatedAt
            }).ToList()
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of TaxRateDto) Implements ITaxRateService.GetByIdAsync
            Dim r = Await _taxRepo.GetByIdAsync(id)
            If r Is Nothing Then
                Throw New NotFoundException("TaxRate", id)
            End If

            Return New TaxRateDto With {
                .Id = r.Id,
                .Name = r.Name,
                .Percentage = r.Percentage,
                .IsDefault = r.IsDefault,
                .IsActive = r.IsActive,
                .CreatedAt = r.CreatedAt,
                .UpdatedAt = r.UpdatedAt
            }
        End Function

        Public Async Function SaveAsync(taxRate As TaxRate) As Task(Of TaxRateDto) Implements ITaxRateService.SaveAsync
            If String.IsNullOrWhiteSpace(taxRate.Name) Then
                Throw New ValidationException("Tax rate name is required.")
            End If

            If taxRate.Percentage < 0 OrElse taxRate.Percentage > 100 Then
                Throw New ValidationException("Tax rate percentage must be between 0 and 100.")
            End If

            If taxRate.Id = 0 Then
                taxRate.CreatedAt = DateTime.UtcNow
                taxRate.UpdatedAt = DateTime.UtcNow
                Dim id = Await _taxRepo.InsertAsync(taxRate)
                taxRate.Id = id

                If taxRate.IsDefault Then
                    Await _taxRepo.SetDefaultAsync(id)
                End If

                Await _auditLogService.LogAsync(AuditAction.Created, "TaxRate", id.ToString(), $"Created tax rate '{taxRate.Name}' ({taxRate.Percentage}%).")
                _logger.LogInformation("Created tax rate {TaxRateId} - {Name}", id, taxRate.Name)
            Else
                taxRate.UpdatedAt = DateTime.UtcNow
                Await _taxRepo.UpdateAsync(taxRate)

                If taxRate.IsDefault Then
                    Await _taxRepo.SetDefaultAsync(taxRate.Id)
                End If

                Await _auditLogService.LogAsync(AuditAction.Updated, "TaxRate", taxRate.Id.ToString(), $"Updated tax rate '{taxRate.Name}' ({taxRate.Percentage}%).")
                _logger.LogInformation("Updated tax rate {TaxRateId} - {Name}", taxRate.Id, taxRate.Name)
            End If

            Return Await GetByIdAsync(taxRate.Id)
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements ITaxRateService.DeleteAsync
            Dim existing = Await _taxRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("TaxRate", id)
            End If

            Await _taxRepo.DeleteAsync(id)
            Await _auditLogService.LogAsync(AuditAction.Deleted, "TaxRate", id.ToString(), $"Deleted tax rate '{existing.Name}'.")
            _logger.LogInformation("Deleted tax rate {TaxRateId}", id)
        End Function

        Public Async Function SetDefaultAsync(id As Long) As Task Implements ITaxRateService.SetDefaultAsync
            Dim existing = Await _taxRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("TaxRate", id)
            End If

            Await _taxRepo.SetDefaultAsync(id)
            Await _auditLogService.LogAsync(AuditAction.Updated, "TaxRate", id.ToString(), $"Set '{existing.Name}' as default tax rate.")
            _logger.LogInformation("Set default tax rate to {TaxRateId}", id)
        End Function
    End Class
End Namespace
