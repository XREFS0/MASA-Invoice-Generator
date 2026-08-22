Imports System
Imports System.Collections.Generic
Imports System.Text.RegularExpressions
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions

Namespace Validators
    Public Module EntityValidators
        Private ReadOnly EmailRegex As New Regex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", RegexOptions.Compiled)

        Public Sub ValidateCustomer(customer As Customer)
            Dim errors As New List(Of String)()

            If String.IsNullOrWhiteSpace(customer.FullName) AndAlso String.IsNullOrWhiteSpace(customer.CompanyName) Then
                errors.Add("Customer must have either a Contact Name or a Company Name.")
            End If

            If Not String.IsNullOrWhiteSpace(customer.Email) AndAlso Not EmailRegex.IsMatch(customer.Email.Trim()) Then
                errors.Add("Please enter a valid email address.")
            End If

            If errors.Count > 0 Then
                Throw New ValidationException(errors)
            End If
        End Sub

        Public Sub ValidateProduct(product As Product)
            Dim errors As New List(Of String)()

            If String.IsNullOrWhiteSpace(product.ProductCode) Then
                errors.Add("Product code/SKU is required.")
            End If

            If String.IsNullOrWhiteSpace(product.Name) Then
                errors.Add("Product/Service name is required.")
            End If

            If product.UnitPrice < 0 Then
                errors.Add("Unit price cannot be negative.")
            End If

            If product.CostPrice < 0 Then
                errors.Add("Cost price cannot be negative.")
            End If

            If product.Type = ProductType.Product Then
                If product.StockQuantity < 0 Then
                    errors.Add("Stock quantity cannot be negative for physical products.")
                End If
                If product.MinimumStock < 0 Then
                    errors.Add("Minimum stock threshold cannot be negative.")
                End If
            End If

            If errors.Count > 0 Then
                Throw New ValidationException(errors)
            End If
        End Sub

        Public Sub ValidateInvoice(invoice As Invoice)
            Dim errors As New List(Of String)()

            If invoice.CustomerId <= 0 Then
                errors.Add("A valid customer must be selected for the invoice.")
            End If

            If invoice.DueDate < invoice.IssueDate Then
                errors.Add("Due date cannot precede the invoice issue date.")
            End If

            If invoice.Items Is Nothing OrElse invoice.Items.Count = 0 Then
                errors.Add("Invoice must contain at least one line item.")
            Else
                For i As Integer = 0 To invoice.Items.Count - 1
                    Dim item = invoice.Items(i)
                    Dim rowNum = i + 1

                    If String.IsNullOrWhiteSpace(item.Description) Then
                        errors.Add($"Line #{rowNum}: Description is required.")
                    End If

                    If item.Quantity <= 0 Then
                        errors.Add($"Line #{rowNum}: Quantity must be greater than zero.")
                    End If

                    If item.UnitPrice < 0 Then
                        errors.Add($"Line #{rowNum}: Unit price cannot be negative.")
                    End If

                    If item.DiscountAmount < 0 Then
                        errors.Add($"Line #{rowNum}: Discount amount cannot be negative.")
                    End If

                    If item.TaxRate < 0 OrElse item.TaxRate > 100 Then
                        errors.Add($"Line #{rowNum}: Tax rate percentage must be between 0 and 100.")
                    End If
                Next
            End If

            If errors.Count > 0 Then
                Throw New ValidationException(errors)
            End If
        End Sub

        Public Sub ValidatePayment(payment As Payment, currentRemainingBalance As Decimal)
            Dim errors As New List(Of String)()

            If payment.InvoiceId <= 0 Then
                errors.Add("A valid invoice must be selected.")
            End If

            If payment.Amount <= 0 Then
                errors.Add("Payment amount must be greater than zero.")
            End If

            If payment.Amount > currentRemainingBalance + 0.001D Then
                errors.Add($"Payment amount ({payment.Amount:C2}) cannot exceed the remaining invoice balance ({currentRemainingBalance:C2}).")
            End If

            If errors.Count > 0 Then
                Throw New ValidationException(errors)
            End If
        End Sub

        Public Sub ValidateCompanySetting(setting As CompanySetting)
            Dim errors As New List(Of String)()

            If String.IsNullOrWhiteSpace(setting.CompanyName) Then
                errors.Add("Company name is required.")
            End If

            If Not String.IsNullOrWhiteSpace(setting.Email) AndAlso Not EmailRegex.IsMatch(setting.Email.Trim()) Then
                errors.Add("Please enter a valid company email address.")
            End If

            If String.IsNullOrWhiteSpace(setting.DefaultCurrency) Then
                errors.Add("Default currency is required.")
            End If

            If setting.NextInvoiceNumber <= 0 Then
                errors.Add("Next invoice number must be greater than zero.")
            End If

            If errors.Count > 0 Then
                Throw New ValidationException(errors)
            End If
        End Sub
    End Module
End Namespace
