Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class PaymentDialogForm
        Inherits Form

        Private ReadOnly _paymentService As IPaymentService
        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _preselectedInvoiceId As Long?

        Private cmbInvoice As ComboBox
        Private lblBalanceInfo As Label
        Private dtpPaymentDate As DateTimePicker
        Private numAmount As NumericUpDown
        Private cmbPaymentMethod As ComboBox
        Private txtReferenceNumber As TextBox
        Private txtNotes As TextBox
        Private btnSave As Button
        Private btnCancel As Button

        Private _invoicesList As System.Collections.Generic.List(Of InvoiceDto)

        Public Sub New(paymentService As IPaymentService, invoiceService As IInvoiceService, Optional invoiceId As Long? = Nothing)
            _paymentService = paymentService
            _invoiceService = invoiceService
            _preselectedInvoiceId = invoiceId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Record Payment Transaction"
            Me.Size = New Size(500, 520)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.FormBorderStyle = FormBorderStyle.FixedDialog
            Me.MaximizeBox = False
            Me.MinimizeBox = False
            Me.BackColor = AppTheme.CardBackground
            Me.Font = AppTheme.FontBody

            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = AppTheme.PrimaryLight,
                .Padding = New Padding(20, 15, 20, 15)
            }
            Dim lblTitle As New Label With {
                .Text = "Record Customer Payment",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlBody As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(24, 16, 24, 16)
            }

            Dim y As Integer = 10

            pnlBody.Controls.Add(CreateLabel("Target Invoice *", 0, y))
            cmbInvoice = New ComboBox With {
                .Location = New Point(0, y + 20),
                .Size = New Size(440, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            AddHandler cmbInvoice.SelectedIndexChanged, AddressOf CmbInvoice_SelectedIndexChanged
            pnlBody.Controls.Add(cmbInvoice)

            y += 55
            lblBalanceInfo = New Label With {
                .Text = "Total: $0.00 | Paid: $0.00 | Remaining Balance: $0.00",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.PrimaryDark,
                .Location = New Point(0, y),
                .AutoSize = True
            }
            pnlBody.Controls.Add(lblBalanceInfo)

            y += 30
            pnlBody.Controls.Add(CreateLabel("Payment Amount *", 0, y))
            numAmount = New NumericUpDown With {
                .Location = New Point(0, y + 20),
                .Size = New Size(210, 26),
                .DecimalPlaces = 2,
                .Maximum = 100000000D,
                .Minimum = 0.01D,
                .Font = AppTheme.FontBodyBold
            }
            pnlBody.Controls.Add(numAmount)

            pnlBody.Controls.Add(CreateLabel("Payment Date *", 230, y))
            dtpPaymentDate = New DateTimePicker With {
                .Location = New Point(230, y + 20),
                .Size = New Size(210, 26),
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today
            }
            pnlBody.Controls.Add(dtpPaymentDate)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Payment Method *", 0, y))
            cmbPaymentMethod = New ComboBox With {
                .Location = New Point(0, y + 20),
                .Size = New Size(210, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            For Each method In [Enum].GetValues(GetType(PaymentMethod))
                cmbPaymentMethod.Items.Add(method)
            Next
            cmbPaymentMethod.SelectedIndex = 0
            pnlBody.Controls.Add(cmbPaymentMethod)

            pnlBody.Controls.Add(CreateLabel("Reference # / Check # / TXN ID", 230, y))
            txtReferenceNumber = New TextBox With {
                .Location = New Point(230, y + 20),
                .Size = New Size(210, 26),
                .Font = AppTheme.FontBody
            }
            pnlBody.Controls.Add(txtReferenceNumber)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Payment Notes / Remarks", 0, y))
            txtNotes = New TextBox With {
                .Location = New Point(0, y + 20),
                .Size = New Size(440, 50),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            pnlBody.Controls.Add(txtNotes)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = AppTheme.AppBackground,
                .Padding = New Padding(20, 12, 20, 12)
            }

            btnCancel = New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(260, 12), .Size = New Size(90, 36)}
            ControlHelpers.StyleSecondaryButton(btnCancel)

            btnSave = New Button With {.Text = "Submit Payment", .Location = New Point(360, 12), .Size = New Size(120, 36)}
            ControlHelpers.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click

            pnlFooter.Controls.Add(btnCancel)
            pnlFooter.Controls.Add(btnSave)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Load, AddressOf PaymentDialogForm_Load
        End Sub

        Private Function CreateLabel(text As String, x As Integer, y As Integer) As Label
            Return New Label With {
                .Text = text,
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(x, y),
                .AutoSize = True
            }
        End Function

        Private Async Sub PaymentDialogForm_Load(sender As Object, e As EventArgs)
            Try
                _invoicesList = Await _invoiceService.GetAllAsync()
                cmbInvoice.Items.Clear()

                For Each inv In _invoicesList.Where(Function(i) i.Status <> InvoiceStatus.Cancelled)
                    cmbInvoice.Items.Add($"{inv.InvoiceNumber} - {inv.CustomerName} (Due: {inv.RemainingAmount:C2})")
                Next

                If _preselectedInvoiceId.HasValue Then
                    Dim idx = _invoicesList.FindIndex(Function(i) i.Id = _preselectedInvoiceId.Value)
                    If idx >= 0 Then
                        cmbInvoice.SelectedIndex = idx
                    End If
                ElseIf cmbInvoice.Items.Count > 0 Then
                    cmbInvoice.SelectedIndex = 0
                End If
            Catch ex As Exception
                MessageBox.Show($"Failed to load invoice list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub CmbInvoice_SelectedIndexChanged(sender As Object, e As EventArgs)
            If cmbInvoice.SelectedIndex >= 0 AndAlso _invoicesList IsNot Nothing AndAlso cmbInvoice.SelectedIndex < _invoicesList.Count Then
                Dim selectedInv = _invoicesList(cmbInvoice.SelectedIndex)
                lblBalanceInfo.Text = $"Total: {selectedInv.TotalAmount:C2} | Paid: {selectedInv.PaidAmount:C2} | Remaining Balance: {selectedInv.RemainingAmount:C2}"
                numAmount.Maximum = Math.Max(0.01D, selectedInv.RemainingAmount)
                numAmount.Value = Math.Max(0.01D, selectedInv.RemainingAmount)
            End If
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            If cmbInvoice.SelectedIndex < 0 OrElse _invoicesList Is Nothing Then
                MessageBox.Show("Please select an invoice.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim selectedInv = _invoicesList(cmbInvoice.SelectedIndex)

            Try
                btnSave.Enabled = False
                btnSave.Text = "Submitting..."

                Dim payment As New Payment With {
                    .InvoiceId = selectedInv.Id,
                    .Amount = numAmount.Value,
                    .PaymentDate = dtpPaymentDate.Value.Date,
                    .PaymentMethod = CType(cmbPaymentMethod.SelectedItem, PaymentMethod),
                    .ReferenceNumber = txtReferenceNumber.Text.Trim(),
                    .Notes = txtNotes.Text.Trim()
                }

                Await _paymentService.RecordPaymentAsync(payment)
                MessageBox.Show("Payment recorded successfully and invoice status updated!", "Payment Processed", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As ValidationException
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As BusinessRuleException
                MessageBox.Show(ex.Message, "Business Rule", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show($"Failed to record payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSave.Enabled = True
                btnSave.Text = "Submit Payment"
            End Try
        End Sub
    End Class
End Namespace
