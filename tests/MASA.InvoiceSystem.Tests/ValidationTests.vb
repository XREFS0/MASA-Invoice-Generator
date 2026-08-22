Imports System
Imports System.Collections.Generic
Imports MASA.InvoiceSystem.Application.Validators
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports Xunit

Namespace Tests
    Public Class ValidationTests
        <Fact>
        Public Sub ValidateCustomer_EmptyNames_ThrowsValidationException()
            Dim customer As New Customer With {
                .FullName = "",
                .CompanyName = ""
            }

            Assert.Throws(Of ValidationException)(Sub() EntityValidators.ValidateCustomer(customer))
        End Sub

        <Fact>
        Public Sub ValidateCustomer_InvalidEmail_ThrowsValidationException()
            Dim customer As New Customer With {
                .FullName = "John Doe",
                .Email = "invalid-email-address"
            }

            Assert.Throws(Of ValidationException)(Sub() EntityValidators.ValidateCustomer(customer))
        End Sub

        <Fact>
        Public Sub ValidateProduct_NegativePrice_ThrowsValidationException()
            Dim product As New Product With {
                .ProductCode = "SKU-001",
                .Name = "Widget",
                .UnitPrice = -10D
            }

            Assert.Throws(Of ValidationException)(Sub() EntityValidators.ValidateProduct(product))
        End Sub

        <Fact>
        Public Sub ValidateInvoice_EmptyItems_ThrowsValidationException()
            Dim invoice As New Invoice With {
                .CustomerId = 1,
                .Items = New List(Of InvoiceItem)()
            }

            Assert.Throws(Of ValidationException)(Sub() EntityValidators.ValidateInvoice(invoice))
        End Sub

        <Fact>
        Public Sub ValidatePayment_AmountExceedsRemainingBalance_ThrowsValidationException()
            Dim payment As New Payment With {
                .InvoiceId = 1,
                .Amount = 500D
            }

            ' Current remaining balance is only 300
            Assert.Throws(Of ValidationException)(Sub() EntityValidators.ValidatePayment(payment, 300D))
        End Sub

        <Fact>
        Public Sub ValidatePayment_ValidAmount_PassesWithoutException()
            Dim payment As New Payment With {
                .InvoiceId = 1,
                .Amount = 250D
            }

            ' Current remaining balance is 300
            Dim ex = Record.Exception(Sub() EntityValidators.ValidatePayment(payment, 300D))
            Assert.Null(ex)
        End Sub
    End Class
End Namespace
