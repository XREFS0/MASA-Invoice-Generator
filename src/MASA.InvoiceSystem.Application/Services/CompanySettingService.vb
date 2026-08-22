Imports System
Imports System.IO
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
    Public Class CompanySettingService
        Implements ICompanySettingService

        Private ReadOnly _settingRepo As ICompanySettingRepository
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of CompanySettingService)

        Public Sub New(settingRepo As ICompanySettingRepository, auditLogService As IAuditLogService, logger As ILogger(Of CompanySettingService))
            _settingRepo = settingRepo
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetSettingAsync() As Task(Of CompanySettingDto) Implements ICompanySettingService.GetSettingAsync
            Dim setting = Await _settingRepo.GetSettingAsync()
            If setting Is Nothing Then
                setting = New CompanySetting()
            End If

            Return New CompanySettingDto With {
                .Id = setting.Id,
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
                .UpdatedAt = setting.UpdatedAt
            }
        End Function

        Public Async Function SaveSettingAsync(setting As CompanySetting) As Task(Of CompanySettingDto) Implements ICompanySettingService.SaveSettingAsync
            EntityValidators.ValidateCompanySetting(setting)

            setting.UpdatedAt = DateTime.UtcNow
            Await _settingRepo.UpdateSettingAsync(setting)
            Await _auditLogService.LogAsync(AuditAction.Updated, "CompanySetting", "1", "Updated company profile & invoice numbering settings.")
            _logger.LogInformation("Company settings updated successfully.")

            Return Await GetSettingAsync()
        End Function

        Public Async Function SaveLogoAsync(sourceFilePath As String) As Task(Of String) Implements ICompanySettingService.SaveLogoAsync
            If Not File.Exists(sourceFilePath) Then
                Throw New ValidationException("Specified logo file does not exist.")
            End If

            Dim appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MASA Invoice System", "Branding")
            Directory.CreateDirectory(appDataDir)

            Dim extension = Path.GetExtension(sourceFilePath)
            Dim destPath = Path.Combine(appDataDir, $"company_logo{extension}")

            File.Copy(sourceFilePath, destPath, overwrite:=True)

            Dim setting = Await _settingRepo.GetSettingAsync()
            setting.LogoPath = destPath
            Await SaveSettingAsync(setting)

            Return destPath
        End Function
    End Class
End Namespace
