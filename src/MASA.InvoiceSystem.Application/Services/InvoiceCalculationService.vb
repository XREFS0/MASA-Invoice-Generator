Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions

Namespace Services
    Public Class InvoiceCalculationService
        Implements IInvoiceCalculationService

        Public Function CalculateItemTotals(quantity As Decimal, unitPrice As Decimal, discountAmount As Decimal, taxRate As Decimal) As InvoiceItemCalculationResult Implements IInvoiceCalculationService.CalculateItemTotals
            If quantity < 0 Then
                Throw New ValidationException("Quantity cannot be negative.")
            End If

            If unitPrice < 0 Then
                Throw New ValidationException("Unit price cannot be negative.")
            End If

            If discountAmount < 0 Then
                Throw New ValidationException("Discount amount cannot be negative.")
            End If

            If taxRate < 0 Then
                Throw New ValidationException("Tax rate cannot be negative.")
            End If

            Dim grossAmount = Decimal.Round(quantity * unitPrice, 2, MidpointRounding.AwayFromZero)

            Dim actualDiscount = Math.Min(discountAmount, grossAmount)
            Dim taxableAmount = Decimal.Round(grossAmount - actualDiscount, 2, MidpointRounding.AwayFromZero)

            Dim taxAmount = Decimal.Round(taxableAmount * (taxRate / 100D), 2, MidpointRounding.AwayFromZero)
            Dim totalAmount = Decimal.Round(taxableAmount + taxAmount, 2, MidpointRounding.AwayFromZero)

            Return New InvoiceItemCalculationResult With {
                .GrossAmount = grossAmount,
                .DiscountAmount = actualDiscount,
                .TaxableAmount = taxableAmount,
                .TaxAmount = taxAmount,
                .TotalAmount = totalAmount
            }
        End Function

        Public Function CalculateInvoiceTotals(items As IEnumerable(Of InvoiceItem), currentPaidAmount As Decimal, dueDate As DateTime, currentStatus As InvoiceStatus) As InvoiceCalculationResult Implements IInvoiceCalculationService.CalculateInvoiceTotals
            Dim itemList = If(items, Enumerable.Empty(Of InvoiceItem)()).ToList()

            Dim subtotal As Decimal = 0D
            Dim totalDiscount As Decimal = 0D
            Dim totalTax As Decimal = 0D
            Dim grandTotal As Decimal = 0D

            For Each item In itemList
                Dim itemRes = CalculateItemTotals(item.Quantity, item.UnitPrice, item.DiscountAmount, item.TaxRate)
                subtotal += itemRes.GrossAmount
                totalDiscount += itemRes.DiscountAmount
                totalTax += itemRes.TaxAmount
                grandTotal += itemRes.TotalAmount

                ' Update entity properties directly to ensure consistency
                item.DiscountAmount = itemRes.DiscountAmount
                item.TotalAmount = itemRes.TotalAmount
            Next

            subtotal = Decimal.Round(subtotal, 2, MidpointRounding.AwayFromZero)
            totalDiscount = Decimal.Round(totalDiscount, 2, MidpointRounding.AwayFromZero)
            totalTax = Decimal.Round(totalTax, 2, MidpointRounding.AwayFromZero)
            grandTotal = Decimal.Round(grandTotal, 2, MidpointRounding.AwayFromZero)

            Dim paid = Math.Max(0D, Decimal.Round(currentPaidAmount, 2, MidpointRounding.AwayFromZero))
            Dim remaining = Math.Max(0D, Decimal.Round(grandTotal - paid, 2, MidpointRounding.AwayFromZero))

            Dim computedStatus = DetermineInvoiceStatus(grandTotal, paid, dueDate, currentStatus)

            Return New InvoiceCalculationResult With {
                .Subtotal = subtotal,
                .DiscountAmount = totalDiscount,
                .TaxAmount = totalTax,
                .TotalAmount = grandTotal,
                .PaidAmount = paid,
                .RemainingAmount = remaining,
                .Status = computedStatus
            }
        End Function

        Public Function DetermineInvoiceStatus(totalAmount As Decimal, paidAmount As Decimal, dueDate As DateTime, currentStatus As InvoiceStatus) As InvoiceStatus Implements IInvoiceCalculationService.DetermineInvoiceStatus
            If currentStatus = InvoiceStatus.Cancelled Then
                Return InvoiceStatus.Cancelled
            End If

            If totalAmount <= 0D Then
                Return If(currentStatus = InvoiceStatus.Draft, InvoiceStatus.Draft, InvoiceStatus.Paid)
            End If

            If paidAmount >= totalAmount Then
                Return InvoiceStatus.Paid
            End If

            If paidAmount > 0D Then
                Return InvoiceStatus.PartialPayment
            End If

            If currentStatus = InvoiceStatus.Draft Then
                Return InvoiceStatus.Draft
            End If

            If dueDate.Date < DateTime.Today Then
                Return InvoiceStatus.Overdue
            End If

            Return InvoiceStatus.Sent
        End Function
    End Class
End Namespace
