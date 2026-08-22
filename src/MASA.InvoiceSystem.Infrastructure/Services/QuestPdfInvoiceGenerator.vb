Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Enums
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Namespace Services
    Public Class QuestPdfInvoiceGenerator
        Implements IInvoicePdfService

        Public Sub New()
            QuestPDF.Settings.License = LicenseType.Community
        End Sub

        Public Function GeneratePdfBytes(invoice As InvoiceDto, company As CompanySettingDto) As Byte() Implements IInvoicePdfService.GeneratePdfBytes
            Dim document = QuestPDF.Fluent.Document.Create(Sub(container)
                container.Page(Sub(page)
                    page.Size(PageSizes.A4)
                    page.Margin(35)
                    page.PageColor(Colors.White)
                    page.DefaultTextStyle(Function(x) x.FontSize(9.5F).FontFamily("Segoe UI").FontColor("#1e293b"))

                    page.Header().Element(Sub(header) ComposeHeader(header, invoice, company))
                    page.Content().Element(Sub(content) ComposeContent(content, invoice, company))
                    page.Footer().Element(Sub(footer) ComposeFooter(footer, company))
                End Sub)
            End Sub)

            Return document.GeneratePdf()
        End Function

        Public Async Function ExportPdfToFile(invoice As InvoiceDto, company As CompanySettingDto, destinationPath As String) As Task Implements IInvoicePdfService.ExportPdfToFile
            Dim bytes = GeneratePdfBytes(invoice, company)
            Dim dir = Path.GetDirectoryName(destinationPath)
            If Not String.IsNullOrEmpty(dir) AndAlso Not Directory.Exists(dir) Then
                Directory.CreateDirectory(dir)
            End If

            Await File.WriteAllBytesAsync(destinationPath, bytes)
        End Function

        Private Sub ComposeHeader(container As IContainer, invoice As InvoiceDto, company As CompanySettingDto)
            container.Column(Sub(col)
                col.Item().Row(Sub(row)
                    row.RelativeItem(3).Column(Sub(c)
                        c.Item().Text(company.CompanyName).FontSize(18).Bold().FontColor("#0f172a")

                        If Not String.IsNullOrWhiteSpace(company.Address) Then
                            c.Item().Text(company.Address).FontSize(8.5F).FontColor("#64748b")
                        End If

                        Dim cityLine = $"{company.City} {company.PostalCode} {company.Country}".Trim()
                        If Not String.IsNullOrWhiteSpace(cityLine) Then
                            c.Item().Text(cityLine).FontSize(8.5F).FontColor("#64748b")
                        End If

                        If Not String.IsNullOrWhiteSpace(company.Phone) OrElse Not String.IsNullOrWhiteSpace(company.Email) Then
                            c.Item().Text($"Tel: {company.Phone}  |  Email: {company.Email}").FontSize(8.5F).FontColor("#64748b")
                        End If

                        If Not String.IsNullOrWhiteSpace(company.TaxNumber) Then
                            c.Item().Text($"Tax ID / VAT: {company.TaxNumber}").FontSize(8.5F).FontColor("#64748b")
                        End If
                    End Sub)

                    row.RelativeItem(2).AlignRight().Column(Sub(c)
                        c.Item().Text("INVOICE").FontSize(24).Bold().FontColor("#1e40af")
                        c.Item().Text($"#{invoice.InvoiceNumber}").FontSize(12).Bold().FontColor("#334155")

                        Dim statusBg = GetStatusBackgroundColor(invoice.Status)
                        Dim statusFg = GetStatusForegroundColor(invoice.Status)

                        c.Item().PaddingTop(4).Container().Background(statusBg).PaddingVertical(3).PaddingHorizontal(8).Text(invoice.Status.ToString().ToUpper()).FontSize(8.5F).Bold().FontColor(statusFg)
                    End Sub)
                End Sub)

                col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#e2e8f0")
            End Sub)
        End Sub

        Private Sub ComposeContent(container As IContainer, invoice As InvoiceDto, company As CompanySettingDto)
            container.PaddingTop(5).Column(Sub(col)
                col.Item().Row(Sub(row)
                    row.RelativeItem().Column(Sub(c)
                        c.Item().Text("BILL TO").FontSize(8).Bold().FontColor("#64748b")
                        c.Item().Text(invoice.CustomerName).FontSize(11).Bold().FontColor("#0f172a")

                        If Not String.IsNullOrWhiteSpace(invoice.CustomerCompanyName) Then
                            c.Item().Text(invoice.CustomerCompanyName).FontSize(9).FontColor("#334155")
                        End If

                        If Not String.IsNullOrWhiteSpace(invoice.CustomerAddress) Then
                            c.Item().Text(invoice.CustomerAddress).FontSize(8.5F).FontColor("#64748b")
                        End If

                        Dim custCity = $"{invoice.CustomerCity} {invoice.CustomerCountry}".Trim()
                        If Not String.IsNullOrWhiteSpace(custCity) Then
                            c.Item().Text(custCity).FontSize(8.5F).FontColor("#64748b")
                        End If

                        If Not String.IsNullOrWhiteSpace(invoice.CustomerTaxNumber) Then
                            c.Item().Text($"Tax ID: {invoice.CustomerTaxNumber}").FontSize(8.5F).FontColor("#64748b")
                        End If
                    End Sub)

                    row.RelativeItem().AlignRight().Column(Sub(c)
                        c.Item().Row(Sub(r)
                            r.RelativeItem().AlignRight().Text("Invoice Date:").FontSize(9).FontColor("#64748b")
                            r.ConstantItem(85).AlignRight().Text(invoice.IssueDate.ToString("yyyy-MM-dd")).FontSize(9).Bold()
                        End Sub)

                        c.Item().Row(Sub(r)
                            r.RelativeItem().AlignRight().Text("Due Date:").FontSize(9).FontColor("#64748b")
                            r.ConstantItem(85).AlignRight().Text(invoice.DueDate.ToString("yyyy-MM-dd")).FontSize(9).Bold()
                        End Sub)

                        c.Item().Row(Sub(r)
                            r.RelativeItem().AlignRight().Text("Currency:").FontSize(9).FontColor("#64748b")
                            r.ConstantItem(85).AlignRight().Text(invoice.Currency).FontSize(9).Bold()
                        End Sub)
                    End Sub)
                End Sub)

                col.Item().PaddingTop(15).Table(Sub(table)
                    table.ColumnsDefinition(Sub(columns)
                        columns.RelativeColumn(5)
                        columns.RelativeColumn(1.2F)
                        columns.RelativeColumn(2)
                        columns.RelativeColumn(1.5F)
                        columns.RelativeColumn(1.2F)
                        columns.RelativeColumn(2.2F)
                    End Sub)

                    table.Header(Sub(header)
                        header.Cell().Background("#f1f5f9").Padding(6).Text("Item Description").Bold().FontSize(8.5F).FontColor("#475569")
                        header.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text("Qty").Bold().FontSize(8.5F).FontColor("#475569")
                        header.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text("Unit Price").Bold().FontSize(8.5F).FontColor("#475569")
                        header.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text("Discount").Bold().FontSize(8.5F).FontColor("#475569")
                        header.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text("Tax").Bold().FontSize(8.5F).FontColor("#475569")
                        header.Cell().Background("#f1f5f9").Padding(6).AlignRight().Text("Total").Bold().FontSize(8.5F).FontColor("#475569")
                    End Sub)

                    Dim isEven = False
                    For Each item In invoice.Items
                        Dim bg = If(isEven, "#f8fafc", "#ffffff")
                        isEven = Not isEven

                        table.Cell().Background(bg).Padding(6).Text(item.Description).FontSize(9)
                        table.Cell().Background(bg).Padding(6).AlignRight().Text($"{item.Quantity:N2}").FontSize(9)
                        table.Cell().Background(bg).Padding(6).AlignRight().Text($"{item.UnitPrice:N2}").FontSize(9)
                        table.Cell().Background(bg).Padding(6).AlignRight().Text($"{item.DiscountAmount:N2}").FontSize(9)
                        table.Cell().Background(bg).Padding(6).AlignRight().Text($"{item.TaxRate:N1}%").FontSize(9)
                        table.Cell().Background(bg).Padding(6).AlignRight().Text($"{item.TotalAmount:N2}").FontSize(9).Bold()
                    Next
                End Sub)

                col.Item().PaddingTop(12).Row(Sub(row)
                    row.RelativeItem(3).Column(Sub(c)
                        If Not String.IsNullOrWhiteSpace(invoice.Notes) Then
                            c.Item().Text("Notes & Payment Instructions:").FontSize(8.5F).Bold().FontColor("#475569")
                            c.Item().PaddingTop(2).Text(invoice.Notes).FontSize(8.5F).FontColor("#64748b")
                        End If
                    End Sub)

                    row.RelativeItem(2).AlignRight().Column(Sub(c)
                        c.Item().Row(Sub(r)
                            r.RelativeItem().Text("Subtotal:").FontSize(9).FontColor("#64748b")
                            r.ConstantItem(85).AlignRight().Text($"{invoice.Currency} {invoice.Subtotal:N2}").FontSize(9)
                        End Sub)

                        If invoice.DiscountAmount > 0 Then
                            c.Item().Row(Sub(r)
                                r.RelativeItem().Text("Discount:").FontSize(9).FontColor("#16a34a")
                                r.ConstantItem(85).AlignRight().Text($"-{invoice.Currency} {invoice.DiscountAmount:N2}").FontSize(9).FontColor("#16a34a")
                            End Sub)
                        End If

                        If invoice.TaxAmount > 0 Then
                            c.Item().Row(Sub(r)
                                r.RelativeItem().Text("Tax:").FontSize(9).FontColor("#64748b")
                                r.ConstantItem(85).AlignRight().Text($"{invoice.Currency} {invoice.TaxAmount:N2}").FontSize(9)
                            End Sub)
                        End If

                        c.Item().PaddingVertical(4).LineHorizontal(1).LineColor("#cbd5e1")

                        c.Item().Row(Sub(r)
                            r.RelativeItem().Text("Total Amount:").FontSize(11).Bold().FontColor("#0f172a")
                            r.ConstantItem(85).AlignRight().Text($"{invoice.Currency} {invoice.TotalAmount:N2}").FontSize(11).Bold().FontColor("#0f172a")
                        End Sub)

                        If invoice.PaidAmount > 0 Then
                            c.Item().Row(Sub(r)
                                r.RelativeItem().Text("Paid to Date:").FontSize(9).FontColor("#16a34a")
                                r.ConstantItem(85).AlignRight().Text($"{invoice.Currency} {invoice.PaidAmount:N2}").FontSize(9).FontColor("#16a34a")
                            End Sub)
                        End If

                        c.Item().PaddingTop(3).Container().Background("#eff6ff").Padding(6).Row(Sub(r)
                            r.RelativeItem().Text("Balance Due:").FontSize(10).Bold().FontColor("#1e40af")
                            r.ConstantItem(85).AlignRight().Text($"{invoice.Currency} {invoice.RemainingAmount:N2}").FontSize(10).Bold().FontColor("#1e40af")
                        End Sub)
                    End Sub)
                End Sub)
            End Sub)
        End Sub

        Private Sub ComposeFooter(container As IContainer, company As CompanySettingDto)
            container.Column(Sub(col)
                col.Item().LineHorizontal(1).LineColor("#e2e8f0")
                col.Item().PaddingTop(5).Row(Sub(row)
                    row.RelativeItem().Text($"Thank you for your business! — {company.CompanyName}").FontSize(8).FontColor("#94a3b8")
                    row.RelativeItem().AlignRight().Text(Sub(x)
                        x.Span("Page ").FontSize(8).FontColor("#94a3b8")
                        x.CurrentPageNumber().FontSize(8).FontColor("#94a3b8")
                        x.Span(" of ").FontSize(8).FontColor("#94a3b8")
                        x.TotalPages().FontSize(8).FontColor("#94a3b8")
                    End Sub)
                End Sub)
            End Sub)
        End Sub

        Private Function GetStatusBackgroundColor(status As InvoiceStatus) As String
            Select Case status
                Case InvoiceStatus.Paid
                    Return "#dcfce7"
                Case InvoiceStatus.PartialPayment
                    Return "#fef3c7"
                Case InvoiceStatus.Overdue
                    Return "#fee2e2"
                Case InvoiceStatus.Cancelled
                    Return "#f1f5f9"
                Case InvoiceStatus.Sent
                    Return "#dbeafe"
                Case Else
                    Return "#f1f5f9"
            End Select
        End Function

        Private Function GetStatusForegroundColor(status As InvoiceStatus) As String
            Select Case status
                Case InvoiceStatus.Paid
                    Return "#166534"
                Case InvoiceStatus.PartialPayment
                    Return "#92400e"
                Case InvoiceStatus.Overdue
                    Return "#991b1b"
                Case InvoiceStatus.Cancelled
                    Return "#475569"
                Case InvoiceStatus.Sent
                    Return "#1e40af"
                Case Else
                    Return "#334155"
            End Select
        End Function
    End Class
End Namespace
