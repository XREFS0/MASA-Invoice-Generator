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
    Public Class PaymentService
        Implements IPaymentService

        Private ReadOnly _paymentRepo As IPaymentRepository
        Private ReadOnly _invoiceRepo As IInvoiceRepository
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _auditLogService As IAuditLogService
        Private ReadOnly _logger As ILogger(Of PaymentService)

        Public Sub New(paymentRepo As IPaymentRepository, invoiceRepo As IInvoiceRepository, calcService As IInvoiceCalculationService, auditLogService As IAuditLogService, logger As ILogger(Of PaymentService))
            _paymentRepo = paymentRepo
            _invoiceRepo = invoiceRepo
            _calcService = calcService
            _auditLogService = auditLogService
            _logger = logger
        End Sub

        Public Async Function GetAllAsync(Optional searchQuery As String = Nothing, Optional invoiceId As Long? = Nothing, Optional fromDate As DateTime? = Nothing, Optional toDate As DateTime? = Nothing) As Task(Of IReadOnlyList(Of PaymentDto)) Implements IPaymentService.GetAllAsync
            Dim payments = Await _paymentRepo.GetAllAsync(searchQuery, invoiceId, fromDate, toDate)
            Return payments.Select(Function(p) New PaymentDto With {
                .Id = p.Id,
                .InvoiceId = p.InvoiceId,
                .InvoiceNumber = p.InvoiceNumber,
                .CustomerName = p.CustomerName,
                .Amount = p.Amount,
                .PaymentDate = p.PaymentDate,
                .PaymentMethod = p.PaymentMethod,
                .ReferenceNumber = p.ReferenceNumber,
                .Notes = p.Notes,
                .CreatedAt = p.CreatedAt
            }).ToList()
        End Function

        Public Async Function GetByIdAsync(id As Long) As Task(Of PaymentDto) Implements IPaymentService.GetByIdAsync
            Dim p = Await _paymentRepo.GetByIdAsync(id)
            If p Is Nothing Then
                Throw New NotFoundException("Payment", id)
            End If

            Return New PaymentDto With {
                .Id = p.Id,
                .InvoiceId = p.InvoiceId,
                .InvoiceNumber = p.InvoiceNumber,
                .CustomerName = p.CustomerName,
                .Amount = p.Amount,
                .PaymentDate = p.PaymentDate,
                .PaymentMethod = p.PaymentMethod,
                .ReferenceNumber = p.ReferenceNumber,
                .Notes = p.Notes,
                .CreatedAt = p.CreatedAt
            }
        End Function

        Public Async Function RecordPaymentAsync(payment As Payment) As Task(Of PaymentDto) Implements IPaymentService.RecordPaymentAsync
            Dim invoice = Await _invoiceRepo.GetByIdAsync(payment.InvoiceId)
            If invoice Is Nothing Then
                Throw New NotFoundException("Invoice", payment.InvoiceId)
            End If

            If invoice.Status = InvoiceStatus.Cancelled Then
                Throw New BusinessRuleException("Cannot record payment against a cancelled invoice.")
            End If

            EntityValidators.ValidatePayment(payment, invoice.RemainingAmount)

            payment.CreatedAt = DateTime.UtcNow
            payment.UpdatedAt = DateTime.UtcNow

            Dim paymentId = Await _paymentRepo.InsertAsync(payment)
            payment.Id = paymentId

            ' Recalculate invoice paid amount and status
            Await _invoiceRepo.RecalculatePaidAmountAsync(payment.InvoiceId)

            Await _auditLogService.LogAsync(AuditAction.PaymentRecorded, "Payment", paymentId.ToString(), $"Recorded {payment.PaymentMethod} payment of {payment.Amount:C2} for invoice {invoice.InvoiceNumber}.")
            _logger.LogInformation("Recorded payment {PaymentId} for invoice {InvoiceId}", paymentId, payment.InvoiceId)

            Return Await GetByIdAsync(paymentId)
        End Function

        Public Async Function DeletePaymentAsync(id As Long) As Task Implements IPaymentService.DeletePaymentAsync
            Dim existing = Await _paymentRepo.GetByIdAsync(id)
            If existing Is Nothing Then
                Throw New NotFoundException("Payment", id)
            End If

            Await _paymentRepo.DeleteAsync(id)
            Await _invoiceRepo.RecalculatePaidAmountAsync(existing.InvoiceId)

            Await _auditLogService.LogAsync(AuditAction.Deleted, "Payment", id.ToString(), $"Deleted payment #{id} ({existing.Amount:C2}).")
            _logger.LogInformation("Deleted payment {PaymentId}", id)
        End Function
    End Class
End Namespace
