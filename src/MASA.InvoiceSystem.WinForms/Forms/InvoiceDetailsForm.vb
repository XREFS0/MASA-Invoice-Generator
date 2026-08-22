Imports System
Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class InvoiceDetailsForm
        Inherits Form

        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _paymentService As IPaymentService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _invoiceId As Long

        Private _invoice As InvoiceDto
        Private _company As CompanySettingDto

        Private lblInvoiceNum As Label
        Private lblCustomerInfo As Label
        Private lblDates As Label
        Private lblFinancials As Label
        Private dgvItems As DataGridView
        Private dgvPayments As DataGridView
        Private btnExportPdf As Button
        Private btnRecordPayment As Button
        Private btnClose As Button

        Public Sub New(invoiceService As IInvoiceService, paymentService As IPaymentService, pdfService As IInvoicePdfService, companyService As ICompanySettingService, invoiceId As Long)
            _invoiceService = invoiceService
            _paymentService = paymentService
            _pdfService = pdfService
            _companyService = companyService
            _invoiceId = invoiceId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Invoice Details & Overview"
            Me.Size = New Size(920, 680)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody

            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 110,
                .BackColor = AppTheme.CardBackground,
                .Padding = New Padding(20)
            }

            lblInvoiceNum = New Label With {
                .Text = "Invoice Details",
                .Font = AppTheme.FontTitle,
                .ForeColor = AppTheme.PrimaryDark,
                .AutoSize = True,
                .Location = New Point(20, 12)
            }

            lblCustomerInfo = New Label With {
                .Text = "Customer: Loading...",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.TextMain,
                .AutoSize = True,
                .Location = New Point(20, 48)
            }

            lblDates = New Label With {
                .Text = "Dates: ...",
                .Font = AppTheme.FontSmall,
                .ForeColor = AppTheme.TextMuted,
                .AutoSize = True,
                .Location = New Point(20, 75)
            }

            lblFinancials = New Label With {
                .Text = "Total: $0.00 | Paid: $0.00 | Due: $0.00",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.Primary,
                .AutoSize = False,
                .Size = New Size(400, 30),
                .TextAlign = ContentAlignment.MiddleRight,
                .Location = New Point(490, 20)
            }

            pnlHeader.Controls.Add(lblInvoiceNum)
            pnlHeader.Controls.Add(lblCustomerInfo)
            pnlHeader.Controls.Add(lblDates)
            pnlHeader.Controls.Add(lblFinancials)

            Dim tabs As New TabControl With {
                .Dock = DockStyle.Fill,
                .Font = AppTheme.FontBodyBold
            }

            Dim tabItems As New TabPage("Line Items")
            dgvItems = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvItems)
            SetupItemsGrid()
            tabItems.Controls.Add(dgvItems)

            Dim tabPayments As New TabPage("Payment History")
            dgvPayments = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvPayments)
            SetupPaymentsGrid()
            tabPayments.Controls.Add(dgvPayments)

            tabs.TabPages.Add(tabItems)
            tabs.TabPages.Add(tabPayments)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = AppTheme.CardBackground,
                .Padding = New Padding(20, 12, 20, 12)
            }

            btnExportPdf = New Button With {
                .Text = "Export PDF",
                .Location = New Point(20, 12),
                .Size = New Size(110, 36)
            }
            ControlHelpers.StylePrimaryButton(btnExportPdf)
            AddHandler btnExportPdf.Click, AddressOf BtnExportPdf_Click

            btnRecordPayment = New Button With {
                .Text = "Record Payment",
                .Location = New Point(140, 12),
                .Size = Size.Empty,
                .Width = 135,
                .Height = 36
            }
            ControlHelpers.StyleSecondaryButton(btnRecordPayment)
            AddHandler btnRecordPayment.Click, AddressOf BtnRecordPayment_Click

            btnClose = New Button With {
                .Text = "Close",
                .DialogResult = DialogResult.OK,
                .Location = New Point(790, 12),
                .Size = New Size(90, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnClose)

            pnlFooter.Controls.Add(btnExportPdf)
            pnlFooter.Controls.Add(btnRecordPayment)
            pnlFooter.Controls.Add(btnClose)

            Me.Controls.Add(tabs)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Load, AddressOf InvoiceDetailsForm_Load
        End Sub

        Private Sub SetupItemsGrid()
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Description", .DataPropertyName = "Description", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Quantity", .DataPropertyName = "Quantity", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Format = "N2"}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Unit Price", .DataPropertyName = "UnitPrice", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Format = "C2"}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Discount", .DataPropertyName = "DiscountAmount", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Format = "C2"}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Tax Rate", .DataPropertyName = "TaxRate", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Format = "0.0\'%'"}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Amount", .DataPropertyName = "TotalAmount", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Format = "C2", .Font = AppTheme.FontBodyBold}})
        End Sub

        Private Sub SetupPaymentsGrid()
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment ID", .DataPropertyName = "Id", .Width = 100})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Date", .DataPropertyName = "PaymentDate", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Method", .DataPropertyName = "PaymentMethod", .Width = 130})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Reference #", .DataPropertyName = "ReferenceNumber", .Width = 140})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Notes", .DataPropertyName = "Notes", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Amount Paid", .DataPropertyName = "Amount", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
        End Sub

        Private Async Sub InvoiceDetailsForm_Load(sender As Object, e As EventArgs)
            Await ReloadDataAsync()
        End Sub

        Private Async Function ReloadDataAsync() As Task
            Try
                _invoice = Await _invoiceService.GetByIdAsync(_invoiceId)
                _company = Await _companyService.GetSettingAsync()

                lblInvoiceNum.Text = $"Invoice #{_invoice.InvoiceNumber} [{_invoice.Status}]"
                lblCustomerInfo.Text = $"Client: {_invoice.CustomerName} {If(Not String.IsNullOrEmpty(_invoice.CustomerEmail), $"({_invoice.CustomerEmail})", "")}"
                lblDates.Text = $"Issued: {_invoice.IssueDate:yyyy-MM-dd}  |  Due: {_invoice.DueDate:yyyy-MM-dd}  |  Currency: {_invoice.Currency}"
                lblFinancials.Text = $"Total: {_invoice.TotalAmount:C2}  |  Paid: {_invoice.PaidAmount:C2}  |  Due: {_invoice.RemainingAmount:C2}"

                dgvItems.DataSource = _invoice.Items

                Dim payments = Await _paymentService.GetAllAsync(invoiceId:=_invoiceId)
                dgvPayments.DataSource = payments

                btnRecordPayment.Enabled = (_invoice.RemainingAmount > 0 AndAlso _invoice.Status <> Domain.Enums.InvoiceStatus.Cancelled)
            Catch ex As Exception
                MessageBox.Show($"Failed to load invoice details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Async Sub BtnExportPdf_Click(sender As Object, e As EventArgs)
            If _invoice Is Nothing Then Return

            Using sfd As New SaveFileDialog With {
                .Filter = "PDF Document (*.pdf)|*.pdf",
                .FileName = $"{_invoice.InvoiceNumber}_{_invoice.CustomerName.Replace(" ", "_")}.pdf"
            }
                If sfd.ShowDialog(Me) = DialogResult.OK Then
                    Try
                        Await _pdfService.ExportPdfToFile(_invoice, _company, sfd.FileName)
                        MessageBox.Show($"Invoice PDF exported to: {sfd.FileName}", "Export Completed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"Failed to generate PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Async Sub BtnRecordPayment_Click(sender As Object, e As EventArgs)
            Using dlg As New PaymentDialogForm(_paymentService, _invoiceService, _invoiceId)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await ReloadDataAsync()
                End If
            End Using
        End Sub
    End Class
End Namespace
