Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Interfaces
Imports Microsoft.Extensions.Logging

Namespace Services
    Public Class DashboardService
        Implements IDashboardService

        Private ReadOnly _invoiceRepo As IInvoiceRepository
        Private ReadOnly _paymentRepo As IPaymentRepository
        Private ReadOnly _customerRepo As ICustomerRepository
        Private ReadOnly _productRepo As IProductRepository
        Private ReadOnly _logger As ILogger(Of DashboardService)

        Public Sub New(invoiceRepo As IInvoiceRepository, paymentRepo As IPaymentRepository, customerRepo As ICustomerRepository, productRepo As IProductRepository, logger As ILogger(Of DashboardService))
            _invoiceRepo = invoiceRepo
            _paymentRepo = paymentRepo
            _customerRepo = customerRepo
            _productRepo = productRepo
            _logger = logger
        End Sub

        Public Async Function GetDashboardSummaryAsync() As Task(Of DashboardSummaryDto) Implements IDashboardService.GetDashboardSummaryAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync()
            Dim payments = Await _paymentRepo.GetAllAsync()
            Dim customers = Await _customerRepo.GetAllAsync()
            Dim lowStockProducts = Await _productRepo.GetLowStockAsync()

            Dim nonCancelledInvoices = invoices.Where(Function(i) i.Status <> InvoiceStatus.Cancelled).ToList()

            Dim totalInvoicesCount = nonCancelledInvoices.Count
            Dim paidInvoicesCount = nonCancelledInvoices.Where(Function(i) i.Status = InvoiceStatus.Paid).Count()
            Dim unpaidInvoicesCount = nonCancelledInvoices.Where(Function(i) i.Status = InvoiceStatus.Draft OrElse i.Status = InvoiceStatus.Sent OrElse i.Status = InvoiceStatus.PartialPayment).Count()
            Dim overdueInvoicesCount = nonCancelledInvoices.Where(Function(i) (i.Status <> InvoiceStatus.Paid) AndAlso i.DueDate.Date < DateTime.Today).Count()

            Dim totalRevenue = payments.Sum(Function(p) p.Amount)
            Dim outstandingBalance = nonCancelledInvoices.Sum(Function(i) i.RemainingAmount)

            ' Recent Invoices
            Dim recentInvoices = nonCancelledInvoices.OrderByDescending(Function(i) i.CreatedAt).Take(8).Select(Function(i) New InvoiceDto With {
                .Id = i.Id,
                .InvoiceNumber = i.InvoiceNumber,
                .CustomerId = i.CustomerId,
                .CustomerName = i.CustomerName,
                .IssueDate = i.IssueDate,
                .DueDate = i.DueDate,
                .Status = i.Status,
                .Currency = i.Currency,
                .TotalAmount = i.TotalAmount,
                .PaidAmount = i.PaidAmount,
                .RemainingAmount = i.RemainingAmount,
                .CreatedAt = i.CreatedAt
            }).ToList()

            ' Recent Payments
            Dim recentPayments = payments.OrderByDescending(Function(p) p.PaymentDate).Take(8).Select(Function(p) New PaymentDto With {
                .Id = p.Id,
                .InvoiceId = p.InvoiceId,
                .InvoiceNumber = p.InvoiceNumber,
                .CustomerName = p.CustomerName,
                .Amount = p.Amount,
                .PaymentDate = p.PaymentDate,
                .PaymentMethod = p.PaymentMethod,
                .ReferenceNumber = p.ReferenceNumber
            }).ToList()

            ' Monthly Sales for the last 6 months
            Dim monthlySales As New List(Of MonthlySalesDto)()
            For m As Integer = 5 To 0 Step -1
                Dim monthDate = DateTime.Today.AddMonths(-m)
                Dim startOfMonth = New DateTime(monthDate.Year, monthDate.Month, 1)
                Dim endOfMonth = startOfMonth.AddMonths(1).AddTicks(-1)

                Dim mInvoices = nonCancelledInvoices.Where(Function(i) i.IssueDate >= startOfMonth AndAlso i.IssueDate <= endOfMonth).ToList()
                Dim mPayments = payments.Where(Function(p) p.PaymentDate >= startOfMonth AndAlso p.PaymentDate <= endOfMonth).ToList()

                monthlySales.Add(New MonthlySalesDto With {
                    .Year = monthDate.Year,
                    .Month = monthDate.Month,
                    .MonthLabel = monthDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    .TotalAmount = mInvoices.Sum(Function(i) i.TotalAmount),
                    .PaidAmount = mPayments.Sum(Function(p) p.Amount),
                    .InvoiceCount = mInvoices.Count
                })
            Next

            Return New DashboardSummaryDto With {
                .TotalInvoicesCount = totalInvoicesCount,
                .PaidInvoicesCount = paidInvoicesCount,
                .UnpaidInvoicesCount = unpaidInvoicesCount,
                .OverdueInvoicesCount = overdueInvoicesCount,
                .TotalRevenue = totalRevenue,
                .OutstandingBalance = outstandingBalance,
                .TotalCustomersCount = customers.Count,
                .LowStockItemsCount = lowStockProducts.Count,
                .RecentInvoices = recentInvoices,
                .RecentPayments = recentPayments,
                .MonthlySales = monthlySales
            }
        End Function
    End Class
End Namespace
