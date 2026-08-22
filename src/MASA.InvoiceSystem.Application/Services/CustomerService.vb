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
    Public Class CustomerService
        Implements ICustomerService

        Private ReadOnly _customerRepo As ICustomerRepository
        Private ReadOnly _invoiceRepo As IInvoiceRepository
        Private ReadOnly _paymentRepo As IPaymentRepository
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of CustomerService)

        Public Sub New(customerRepo As ICustomerRepository, invoiceRepo As IInvoiceRepository, paymentRepo As IPaymentRepository, auditLogService As IAuditLogService, logger As ILogger(Of CustomerService))
            _customerRepo = customerRepo
            _invoiceRepo = invoiceRepo
            _paymentRepo = paymentRepo
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing) As Task(Of IReadOnlyList(Of CustomerDto)) Implements ICustomerService.GetAllAsync
            Dim customers = Await _customerRepo.GetAllAsync(searchQuery)
            Dim invoices = Await _invoiceRepo.GetAllAsync()

            Dim list As New List(Of CustomerDto)()
            For Each c In customers
                Dim custInvoices = invoices.Where(Function(i) i.CustomerId = c.Id AndAlso i.Status <> InvoiceStatus.Cancelled).ToList()
                Dim totalInvoiced = custInvoices.Sum(Function(i) i.TotalAmount)
                Dim totalPaid = custInvoices.Sum(Function(i) i.PaidAmount)
                Dim outstanding = custInvoices.Sum(Function(i) i.RemainingAmount)

                list.Add(New CustomerDto With {
                    .Id = c.Id,
                    .FullName = c.FullName,
                    .CompanyName = c.CompanyName,
                    .Email = c.Email,
                    .Phone = c.Phone,
                    .Address = c.Address,
                    .City = c.City,
                    .Country = c.Country,
                    .PostalCode = c.PostalCode,
                    .TaxNumber = c.TaxNumber,
                    .Notes = c.Notes,
                    .CreatedAt = c.CreatedAt,
                    .UpdatedAt = c.UpdatedAt,
                    .TotalInvoiced = totalInvoiced,
                    .TotalPaid = totalPaid,
                    .OutstandingBalance = outstanding,
                    .InvoiceCount = custInvoices.Count
                })
            Next

            Return list
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of CustomerDto) Implements ICustomerService.GetByIdAsync
            Dim c = Await _customerRepo.GetByIdAsync(id)
            If c Is Nothing Then
                Throw New NotFoundException("Customer", id)
            End If

            Dim invoices = Await _invoiceRepo.GetAllAsync(customerId:=id)
            Dim validInvoices = invoices.Where(Function(i) i.Status <> InvoiceStatus.Cancelled).ToList()

            Return New CustomerDto With {
                .Id = c.Id,
                .FullName = c.FullName,
                .CompanyName = c.CompanyName,
                .Email = c.Email,
                .Phone = c.Phone,
                .Address = c.Address,
                .City = c.City,
                .Country = c.Country,
                .PostalCode = c.PostalCode,
                .TaxNumber = c.TaxNumber,
                .Notes = c.Notes,
                .CreatedAt = c.CreatedAt,
                .UpdatedAt = c.UpdatedAt,
                .TotalInvoiced = validInvoices.Sum(Function(i) i.TotalAmount),
                .TotalPaid = validInvoices.Sum(Function(i) i.PaidAmount),
                .OutstandingBalance = validInvoices.Sum(Function(i) i.RemainingAmount),
                .InvoiceCount = validInvoices.Count
            }
        End Function

        Public Async Function SaveAsync(customer As Customer) As Task(Of CustomerDto) Implements ICustomerService.SaveAsync
            EntityValidators.ValidateCustomer(customer)

            If customer.Id = 0 Then
                customer.CreatedAt = DateTime.UtcNow
                customer.UpdatedAt = DateTime.UtcNow
                Dim id = Await _customerRepo.InsertAsync(customer)
                customer.Id = id
                Await _auditLogService.LogAsync(AuditAction.Created, "Customer", id.ToString(), $"Created customer '{customer.DisplayName}'.")
                _logger.LogInformation("Created customer {CustomerId} - {Name}", id, customer.DisplayName)
            Else
                customer.UpdatedAt = DateTime.UtcNow
                Await _customerRepo.UpdateAsync(customer)
                Await _auditLogService.LogAsync(AuditAction.Updated, "Customer", customer.Id.ToString(), $"Updated customer '{customer.DisplayName}'.")
                _logger.LogInformation("Updated customer {CustomerId} - {Name}", customer.Id, customer.DisplayName)
            End If

            Return Await GetByIdAsync(customer.Id)
        End Function

        Public Async Function DeleteAsync(id As Long) As Task Implements ICustomerService.DeleteAsync
            Dim existing = Await _customerRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("Customer", id)
            End If

            Dim hasInvoices = Await _customerRepo.HasInvoicesAsync(id)
            If hasInvoices Then
                Throw New BusinessRuleException("Cannot delete customer because they have associated invoices. Consider archiving instead or remove the invoices first.")
            End If

            Await _customerRepo.DeleteAsync(id)
            Await _auditLogService.LogAsync(AuditAction.Deleted, "Customer", id.ToString(), $"Deleted customer '{existing.DisplayName}'.")
            _logger.LogInformation("Deleted customer {CustomerId}", id)
        End Function

        Public Async Function GetCustomerInvoicesAsync(customerId As Long) As Task(Of IReadOnlyList(Of InvoiceDto)) Implements ICustomerService.GetCustomerInvoicesAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync(customerId:=customerId)
            Return invoices.Select(Function(i) New InvoiceDto With {
                .Id = i.Id,
                .InvoiceNumber = i.InvoiceNumber,
                .CustomerId = i.CustomerId,
                .CustomerName = i.CustomerName,
                .IssueDate = i.IssueDate,
                .DueDate = i.DueDate,
                .Status = i.Status,
                .Currency = i.Currency,
                .Subtotal = i.Subtotal,
                .DiscountAmount = i.DiscountAmount,
                .TaxAmount = i.TaxAmount,
                .TotalAmount = i.TotalAmount,
                .PaidAmount = i.PaidAmount,
                .RemainingAmount = i.RemainingAmount,
                .CreatedAt = i.CreatedAt
            }).ToList()
        End Function

        Public Async Function GetCustomerPaymentsAsync(customerId As Long) As Task(Of IReadOnlyList(Of PaymentDto)) Implements ICustomerService.GetCustomerPaymentsAsync
            Dim payments = Await _paymentRepo.GetAllAsync()
            Dim custPayments = payments.Where(Function(p) p.InvoiceNumber <> String.Empty).ToList()

            Dim invoices = Await _invoiceRepo.GetAllAsync(customerId:=customerId)
            Dim invoiceIds = New HashSet(Of Long)(invoices.Select(Function(i) i.Id))

            Return payments.Where(Function(p) invoiceIds.Contains(p.InvoiceId)).Select(Function(p) New PaymentDto With {
                .Id = p.Id,
                .InvoiceId = p.InvoiceId,
                .InvoiceNumber = p.InvoiceNumber,
                .CustomerName = p.CustomerName,
                .CustomerId = customerId,
                .Amount = p.Amount,
                .PaymentDate = p.PaymentDate,
                .PaymentMethod = p.PaymentMethod,
                .ReferenceNumber = p.ReferenceNumber,
                .Notes = p.Notes,
                .CreatedAt = p.CreatedAt
            }).ToList()
        End Function
    End Class
End Namespace
