Imports System
Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Interfaces

Namespace Services
    Public Class ReportService
        Implements IReportService

        Private ReadOnly _invoiceRepo As IInvoiceRepository
        Private ReadOnly _paymentRepo As IPaymentRepository
        Private ReadOnly _customerRepo As ICustomerRepository
        Private ReadOnly _productRepo As IProductRepository

        Public Sub New(invoiceRepo As IInvoiceRepository, paymentRepo As IPaymentRepository, customerRepo As ICustomerRepository, productRepo As IProductRepository)
            _invoiceRepo = invoiceRepo
            _paymentRepo = paymentRepo
            _customerRepo = customerRepo
            _productRepo = productRepo
        End Sub

        Public Async Function GetSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of SalesReportItemDto)) Implements IReportService.GetSalesReportAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync(fromDate:=fromDate.Date, toDate:=toDate.Date.AddDays(1).AddTicks(-1))

            Return invoices.Where(Function(i) i.Status <> InvoiceStatus.Cancelled).Select(Function(i) New SalesReportItemDto With {
                .InvoiceNumber = i.InvoiceNumber,
                .CustomerName = i.CustomerName,
                .IssueDate = i.IssueDate,
                .DueDate = i.DueDate,
                .Status = i.Status.ToString(),
                .Subtotal = i.Subtotal,
                .DiscountAmount = i.DiscountAmount,
                .TaxAmount = i.TaxAmount,
                .TotalAmount = i.TotalAmount,
                .PaidAmount = i.PaidAmount,
                .RemainingAmount = i.RemainingAmount
            }).ToList()
        End Function

        Public Async Function GetRevenueReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of MonthlySalesDto)) Implements IReportService.GetRevenueReportAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync(fromDate:=fromDate, toDate:=toDate)
            Dim payments = Await _paymentRepo.GetAllAsync(fromDate:=fromDate, toDate:=toDate)

            Dim results As New List(Of MonthlySalesDto)()
            Dim cur = New DateTime(fromDate.Year, fromDate.Month, 1)
            Dim endLimit = New DateTime(toDate.Year, toDate.Month, 1).AddMonths(1)

            While cur < endLimit
                Dim startOfMonth = cur
                Dim endOfMonth = cur.AddMonths(1).AddTicks(-1)

                Dim mInvoices = invoices.Where(Function(i) i.IssueDate >= startOfMonth AndAlso i.IssueDate <= endOfMonth AndAlso i.Status <> InvoiceStatus.Cancelled).ToList()
                Dim mPayments = payments.Where(Function(p) p.PaymentDate >= startOfMonth AndAlso p.PaymentDate <= endOfMonth).ToList()

                results.Add(New MonthlySalesDto With {
                    .Year = cur.Year,
                    .Month = cur.Month,
                    .MonthLabel = cur.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    .TotalAmount = mInvoices.Sum(Function(i) i.TotalAmount),
                    .PaidAmount = mPayments.Sum(Function(p) p.Amount),
                    .InvoiceCount = mInvoices.Count
                })

                cur = cur.AddMonths(1)
            End While

            Return results
        End Function

        Public Async Function GetOutstandingInvoicesReportAsync() As Task(Of IReadOnlyList(Of InvoiceDto)) Implements IReportService.GetOutstandingInvoicesReportAsync
            Dim invoices = Await _invoiceRepo.GetAllAsync()
            Return invoices.Where(Function(i) (i.Status = InvoiceStatus.Draft OrElse i.Status = InvoiceStatus.Sent OrElse i.Status = InvoiceStatus.PartialPayment OrElse i.Status = InvoiceStatus.Overdue) AndAlso i.RemainingAmount > 0).Select(Function(i) New InvoiceDto With {
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
                .RemainingAmount = i.RemainingAmount
            }).OrderBy(Function(i) i.DueDate).ToList()
        End Function

        Public Async Function GetCustomerSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of CustomerSalesReportDto)) Implements IReportService.GetCustomerSalesReportAsync
            Dim customers = Await _customerRepo.GetAllAsync()
            Dim invoices = Await _invoiceRepo.GetAllAsync(fromDate:=fromDate, toDate:=toDate)

            Dim results As New List(Of CustomerSalesReportDto)()
            For Each c In customers
                Dim cInvoices = invoices.Where(Function(i) i.CustomerId = c.Id AndAlso i.Status <> InvoiceStatus.Cancelled).ToList()
                If cInvoices.Count > 0 Then
                    results.Add(New CustomerSalesReportDto With {
                        .CustomerId = c.Id,
                        .CustomerName = c.FullName,
                        .CompanyName = c.CompanyName,
                        .Email = c.Email,
                        .InvoiceCount = cInvoices.Count,
                        .TotalBilled = cInvoices.Sum(Function(i) i.TotalAmount),
                        .TotalPaid = cInvoices.Sum(Function(i) i.PaidAmount),
                        .OutstandingBalance = cInvoices.Sum(Function(i) i.RemainingAmount)
                    })
                End If
            Next

            Return results.OrderByDescending(Function(r) r.TotalBilled).ToList()
        End Function

        Public Async Function GetProductSalesReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of ProductSalesReportDto)) Implements IReportService.GetProductSalesReportAsync
            Dim products = Await _productRepo.GetAllAsync()
            Dim invoices = Await _invoiceRepo.GetAllAsync(fromDate:=fromDate, toDate:=toDate)

            Dim itemMap As New Dictionary(Of Long, Tuple(Of Decimal, Decimal))()

            For Each inv In invoices.Where(Function(i) i.Status <> InvoiceStatus.Cancelled)
                For Each it In inv.Items
                    If it.ProductId.HasValue AndAlso it.ProductId.Value > 0 Then
                        Dim pId = it.ProductId.Value
                        If Not itemMap.ContainsKey(pId) Then
                            itemMap(pId) = Tuple.Create(0D, 0D)
                        End If
                        Dim current = itemMap(pId)
                        itemMap(pId) = Tuple.Create(current.Item1 + it.Quantity, current.Item2 + it.TotalAmount)
                    End If
                Next
            Next

            Dim results As New List(Of ProductSalesReportDto)()
            For Each p In products
                If itemMap.ContainsKey(p.Id) Then
                    Dim stats = itemMap(p.Id)
                    results.Add(New ProductSalesReportDto With {
                        .ProductId = p.Id,
                        .ProductCode = p.ProductCode,
                        .ProductName = p.Name,
                        .ProductType = p.Type.ToString(),
                        .QuantitySold = stats.Item1,
                        .TotalRevenue = stats.Item2
                    })
                End If
            Next

            Return results.OrderByDescending(Function(r) r.TotalRevenue).ToList()
        End Function

        Public Async Function GetPaymentsReportAsync(fromDate As DateTime, toDate As DateTime) As Task(Of IReadOnlyList(Of PaymentDto)) Implements IReportService.GetPaymentsReportAsync
            Dim payments = Await _paymentRepo.GetAllAsync(fromDate:=fromDate, toDate:=toDate)
            Return payments.Select(Function(p) New PaymentDto With {
                .Id = p.Id,
                .InvoiceId = p.InvoiceId,
                .InvoiceNumber = p.InvoiceNumber,
                .CustomerName = p.CustomerName,
                .Amount = p.Amount,
                .PaymentDate = p.PaymentDate,
                .PaymentMethod = p.PaymentMethod,
                .ReferenceNumber = p.ReferenceNumber,
                .Notes = p.Notes
            }).OrderByDescending(Function(p) p.PaymentDate).ToList()
        End Function
    End Class
End Namespace
