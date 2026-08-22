Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports Xunit

Namespace Tests
    Public Class InvoiceCalculationTests
        Private ReadOnly _calcService As IInvoiceCalculationService

        Public Sub New()
            _calcService = New InvoiceCalculationService()
        End Sub

        <Fact>
        Public Sub CalculateItemTotals_StandardItem_ComputesCorrectly()
            ' Quantity 2.5, UnitPrice 100, Discount 10, Tax 10%
            ' Gross = 250.00
            ' Taxable = 250.00 - 10.00 = 240.00
            ' Tax = 24.00
            ' Total = 264.00
            Dim res = _calcService.CalculateItemTotals(2.5D, 100D, 10D, 10D)

            Assert.Equal(250.00D, res.GrossAmount)
            Assert.Equal(10.00D, res.DiscountAmount)
            Assert.Equal(240.00D, res.TaxableAmount)
            Assert.Equal(24.00D, res.TaxAmount)
            Assert.Equal(264.00D, res.TotalAmount)
        End Sub

        <Fact>
        Public Sub CalculateItemTotals_DiscountExceedsGross_CapsDiscountToGross()
            Dim res = _calcService.CalculateItemTotals(1D, 50D, 100D, 10D)

            Assert.Equal(50.00D, res.GrossAmount)
            Assert.Equal(50.00D, res.DiscountAmount)
            Assert.Equal(0.00D, res.TaxableAmount)
            Assert.Equal(0.00D, res.TaxAmount)
            Assert.Equal(0.00D, res.TotalAmount)
        End Sub

        <Fact>
        Public Sub CalculateInvoiceTotals_MultipleItems_ComputesAggregateAccurately()
            Dim items As New List(Of InvoiceItem) From {
                New InvoiceItem With {.Quantity = 2D, .UnitPrice = 50D, .DiscountAmount = 5D, .TaxRate = 10D}, ' Gross: 100, Taxable: 95, Tax: 9.50, Total: 104.50
                New InvoiceItem With {.Quantity = 1D, .UnitPrice = 200D, .DiscountAmount = 0D, .TaxRate = 20D}  ' Gross: 200, Taxable: 200, Tax: 40.00, Total: 240.00
            }

            ' Subtotal = 300.00
            ' Total Discount = 5.00
            ' Total Tax = 49.50
            ' Grand Total = 344.50
            ' Paid = 100.00 -> Remaining = 244.50, Status = PartialPayment
            Dim res = _calcService.CalculateInvoiceTotals(items, 100D, DateTime.Today.AddDays(10), InvoiceStatus.Draft)

            Assert.Equal(300.00D, res.Subtotal)
            Assert.Equal(5.00D, res.DiscountAmount)
            Assert.Equal(49.50D, res.TaxAmount)
            Assert.Equal(344.50D, res.TotalAmount)
            Assert.Equal(100.00D, res.PaidAmount)
            Assert.Equal(244.50D, res.RemainingAmount)
            Assert.Equal(InvoiceStatus.PartialPayment, res.Status)
        End Sub

        <Fact>
        Public Sub DetermineInvoiceStatus_FullyPaid_ReturnsPaidStatus()
            Dim status = _calcService.DetermineInvoiceStatus(500D, 500D, DateTime.Today.AddDays(-5), InvoiceStatus.Sent)
            Assert.Equal(InvoiceStatus.Paid, status)
        End Sub

        <Fact>
        Public Sub DetermineInvoiceStatus_Overdue_ReturnsOverdueStatus()
            Dim status = _calcService.DetermineInvoiceStatus(500D, 0D, DateTime.Today.AddDays(-1), InvoiceStatus.Sent)
            Assert.Equal(InvoiceStatus.Overdue, status)
        End Sub

        <Fact>
        Public Sub DetermineInvoiceStatus_Cancelled_RemainsCancelled()
            Dim status = _calcService.DetermineInvoiceStatus(500D, 0D, DateTime.Today.AddDays(10), InvoiceStatus.Cancelled)
            Assert.Equal(InvoiceStatus.Cancelled, status)
        End Sub
    End Class
End Namespace
