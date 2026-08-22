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
    Public Class InvoiceService
        Implements IInvoiceService

        Private ReadOnly _invoiceRepo As IInvoiceRepository
        Private ReadOnly _customerRepo As ICustomerRepository
        Private ReadOnly _productRepo As IProductRepository
        Private ReadOnly _companyRepo As ICompanySettingRepository
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of InvoiceService)

        Public Sub New(invoiceRepo As IInvoiceRepository, customerRepo As ICustomerRepository, productRepo As IProductRepository, companyRepo As ICompanySettingRepository, calcService As IInvoiceCalculationService, auditLogService As IAuditLogService, logger As ILogger(Of InvoiceService))
            _invoiceRepo = invoiceRepo
            _customerRepo = customerRepo
            _productRepo = productRepo
            _companyRepo = companyRepo
            _calcService = calcService
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional statusFilter As InvoiceStatus? = Nothing, Optional customerId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of InvoiceDto)) Implements IInvoiceService.GetAllAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync(searchQuery, statusFilter, customerId, fromDate, toDate)
            Return invoices.Select(Function(i) MapToDto(i)).ToList()
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of InvoiceDto) Implements IInvoiceService.GetByIdAsync
            Dim invoice = Await _invoiceRepo.GetByIdAsync(id)
            If invoice Is Nothing Then
                Throw New NotFoundException("Invoice", id)
            End If

            Dim customer = Await _customerRepo.GetByIdAsync(invoice.CustomerId)
            Dim dto = MapToDto(invoice)
            If customer IsNot Nothing Then
                dto.CustomerCompanyName = customer.CompanyName
                dto.CustomerPhone = customer.Phone
                dto.CustomerAddress = customer.Address
                dto.CustomerCity = customer.City
                dto.CustomerCountry = customer.Country
                dto.CustomerTaxNumber = customer.TaxNumber
            End If

            Return dto
        End Function

        Public Async Function CreateAsync(invoice As Invoice) As Task(Of InvoiceDto) Implements IInvoiceService.CreateAsync
            EntityValidators.ValidateInvoice(invoice)

            Dim customer = Await _customerRepo.GetByIdAsync(invoice.CustomerId)
            If customer Is Nothing Then
                Throw New ValidationException("Selected customer does not exist.")
            End If

            If String.IsNullOrWhiteSpace(invoice.InvoiceNumber) Then
                Dim setting = Await _companyRepo.GetSettingAsync()
                invoice.InvoiceNumber = Await _companyRepo.GetAndIncrementNextInvoiceNumberAsync(setting.InvoicePrefix)
            Else
                Dim existing = Await _invoiceRepo.GetByInvoiceNumberAsync(invoice.InvoiceNumber.Trim())
                If existing IsNot Nothing Then
                    Throw New ValidationException($"Invoice number '{invoice.InvoiceNumber}' is already used.")
                End If
            End If

            ' Perform calculations
            Dim calcResult = _calcService.CalculateInvoiceTotals(invoice.Items, 0D, invoice.DueDate, invoice.Status)
            invoice.Subtotal = calcResult.Subtotal
            invoice.DiscountAmount = calcResult.DiscountAmount
            invoice.TaxAmount = calcResult.TaxAmount
            invoice.TotalAmount = calcResult.TotalAmount
            invoice.PaidAmount = 0D
            invoice.RemainingAmount = calcResult.RemainingAmount
            invoice.Status = calcResult.Status
            invoice.CreatedAt = DateTime.UtcNow
            invoice.UpdatedAt = DateTime.UtcNow

            Dim id = Await _invoiceRepo.InsertWithItemsAsync(invoice)
            invoice.Id = id

            ' Adjust product stock if any
            For Each item In invoice.Items
                If item.ProductId.HasValue AndAlso item.ProductId.Value > 0 Then
                    Await _productRepo.AdjustStockAsync(item.ProductId.Value, -item.Quantity)
                End If
            Next

            Await _auditLogService.LogAsync(AuditAction.Created, "Invoice", id.ToString(), $"Created invoice {invoice.InvoiceNumber} for {customer.DisplayName} ({invoice.TotalAmount:C2}).")
            _logger.LogInformation("Created invoice {InvoiceId} - {InvoiceNumber}", id, invoice.InvoiceNumber)

            Return Await GetByIdAsync(id)
        End Function

        Public Async Function UpdateAsync(invoice As Invoice) As Task(Of InvoiceDto) Implements IInvoiceService.UpdateAsync
            EntityValidators.ValidateInvoice(invoice)

            Dim existing = Await _invoiceRepo.GetByIdAsync(invoice.Id)
            If existing Is Nothing Then
                Throw New NotFoundException("Invoice", invoice.Id)
            End If

            If existing.Status = InvoiceStatus.Paid Then
                Throw New BusinessRuleException("Fully paid invoices cannot be edited directly.")
            End If

            ' Re-calculate totals preserving paid amount
            Dim calcResult = _calcService.CalculateInvoiceTotals(invoice.Items, existing.PaidAmount, invoice.DueDate, invoice.Status)
            invoice.Subtotal = calcResult.Subtotal
            invoice.DiscountAmount = calcResult.DiscountAmount
            invoice.TaxAmount = calcResult.TaxAmount
            invoice.TotalAmount = calcResult.TotalAmount
            invoice.PaidAmount = existing.PaidAmount
            invoice.RemainingAmount = calcResult.RemainingAmount
            invoice.Status = calcResult.Status
            invoice.UpdatedAt = DateTime.UtcNow

            Await _invoiceRepo.UpdateWithItemsAsync(invoice)
            Await _auditLogService.LogAsync(AuditAction.Updated, "Invoice", invoice.Id.ToString(), $"Updated invoice {invoice.InvoiceNumber}.")
            _logger.LogInformation("Updated invoice {InvoiceId} - {InvoiceNumber}", invoice.Id, invoice.InvoiceNumber)

            Return Await GetByIdAsync(invoice.Id)
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements IInvoiceService.DeleteAsync
            Dim existing = Await _invoiceRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("Invoice", id)
            End If

            If existing.PaidAmount > 0 Then
                Throw New BusinessRuleException("Cannot delete an invoice that has recorded payments. Delete payments first or cancel the invoice.")
            End If

            ' Restore inventory for physical items
            For Each item In existing.Items
                If item.ProductId.HasValue AndAlso item.ProductId.Value > 0 Then
                    Await _productRepo.AdjustStockAsync(item.ProductId.Value, item.Quantity)
                End If
            Next

            Await _invoiceRepo.DeleteAsync(id)
            Await _auditLogService.LogAsync(AuditAction.Deleted, "Invoice", id.ToString(), $"Deleted invoice {existing.InvoiceNumber}.")
            _logger.LogInformation("Deleted invoice {InvoiceId}", id)
        End Function

        Public Async Function DuplicateAsync(id As Long) As Task(Of InvoiceDto) Implements IInvoiceService.DuplicateAsync
            Dim original = Await _invoiceRepo.GetByIdAsync(id)
            If original Is Nothing Then
                Throw New NotFoundException("Invoice", id)
            End If

            Dim setting = Await _companyRepo.GetSettingAsync()
            Dim nextNumber = Await _companyRepo.GetAndIncrementNextInvoiceNumberAsync(setting.InvoicePrefix)

            Dim newInvoice As New Invoice With {
                .InvoiceNumber = nextNumber,
                .CustomerId = original.CustomerId,
                .IssueDate = DateTime.Today,
                .DueDate = DateTime.Today.AddDays(30),
                .Status = InvoiceStatus.Draft,
                .Currency = original.Currency,
                .Notes = original.Notes,
                .Items = original.Items.Select(Function(it) New InvoiceItem With {
                    .ProductId = it.ProductId,
                    .Description = it.Description,
                    .Quantity = it.Quantity,
                    .UnitPrice = it.UnitPrice,
                    .DiscountAmount = it.DiscountAmount,
                    .TaxRate = it.TaxRate,
                    .TotalAmount = it.TotalAmount
                }).ToList()
            }

            Return Await CreateAsync(newInvoice)
        End Function

        Public Async Function UpdateStatusAsync(id As Long, newStatus As InvoiceStatus) As Task Implements IInvoiceService.UpdateStatusAsync
            Dim existing = Await _invoiceRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("Invoice", id)
            End If

            Await _invoiceRepo.UpdateStatusAsync(id, newStatus)
            Await _auditLogService.LogAsync(AuditAction.StatusChanged, "Invoice", id.ToString(), $"Changed invoice {existing.InvoiceNumber} status to {newStatus}.")
            _logger.LogInformation("Updated invoice {InvoiceId} status to {Status}", id, newStatus)
        End Function

        Public Async Function GetNextInvoiceNumberAsync() As Task(Of String) Implements IInvoiceService.GetNextInvoiceNumberAsync
            Dim setting = Await _companyRepo.GetSettingAsync()
            Return $"{setting.InvoicePrefix}{setting.NextInvoiceNumber:D6}"
        End Function

        Private Function MapToDto(invoice As Invoice) As InvoiceDto
            Return New InvoiceDto With {
                .Id = invoice.Id,
                .InvoiceNumber = invoice.InvoiceNumber,
                .CustomerId = invoice.CustomerId,
                .CustomerName = invoice.CustomerName,
                .CustomerEmail = invoice.CustomerEmail,
                .IssueDate = invoice.IssueDate,
                .DueDate = invoice.DueDate,
                .Status = invoice.Status,
                .Currency = invoice.Currency,
                .Subtotal = invoice.Subtotal,
                .DiscountAmount = invoice.DiscountAmount,
                .TaxAmount = invoice.TaxAmount,
                .TotalAmount = invoice.TotalAmount,
                .PaidAmount = invoice.PaidAmount,
                .RemainingAmount = invoice.RemainingAmount,
                .Notes = invoice.Notes,
                .CreatedAt = invoice.CreatedAt,
                .UpdatedAt = invoice.UpdatedAt,
                .Items = invoice.Items.Select(Function(it) New InvoiceItemDto With {
                    .Id = it.Id,
                    .InvoiceId = it.InvoiceId,
                    .ProductId = it.ProductId,
                    .Description = it.Description,
                    .Quantity = it.Quantity,
                    .UnitPrice = it.UnitPrice,
                    .DiscountAmount = it.DiscountAmount,
                    .TaxRate = it.TaxRate,
                    .TotalAmount = it.TotalAmount
                }).ToList()
            }
        End Function
    End Class
End Namespace
