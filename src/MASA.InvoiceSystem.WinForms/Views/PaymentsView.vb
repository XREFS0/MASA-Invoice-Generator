Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class PaymentsView
        Inherits UserControl

        Private ReadOnly _paymentService As IPaymentService
        Private ReadOnly _invoiceService As IInvoiceService

        Private txtSearch As TextBox
        Private btnSearch As Button
        Private btnReset As Button
        Private btnRecordPayment As Button
        Private btnDeletePayment As Button
        Private dgvPayments As DataGridView
        Private lblRecordCount As Label
        Private lblTotalAmount As Label

        Public Sub New(paymentService As IPaymentService, invoiceService As IInvoiceService)
            _paymentService = paymentService
            _invoiceService = invoiceService
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody
            Me.Padding = New Padding(20)

            Dim pnlCard As Panel = ControlHelpers.CreateCardPanel()
            pnlCard.Dock = DockStyle.Fill
            pnlCard.Padding = New Padding(16)

            Dim pnlToolbar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 55
            }

            Dim lblTitle As New Label With {
                .Text = "Payment Transactions",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.TextMain,
                .Location = New Point(0, 12),
                .AutoSize = True
            }

            txtSearch = New TextBox With {
                .Location = New Point(200, 12),
                .Size = New Size(220, 26),
                .Font = AppTheme.FontBody,
                .PlaceholderText = "Search Invoice #, Client, Ref #..."
            }
            AddHandler txtSearch.KeyDown, Async Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  Await LoadPaymentsAsync()
                                              End If
                                          End Sub

            btnSearch = New Button With {
                .Text = "Search",
                .Location = New Point(430, 10),
                .Size = New Size(75, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnSearch)
            AddHandler btnSearch.Click, Async Sub(s, e) Await LoadPaymentsAsync()

            btnReset = New Button With {
                .Text = "Reset",
                .Location = New Point(512, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnReset)
            AddHandler btnReset.Click, Async Sub(s, e)
                                           txtSearch.Clear()
                                           Await LoadPaymentsAsync()
                                       End Sub

            btnDeletePayment = New Button With {
                .Text = "Delete Payment",
                .Location = New Point(585, 10),
                .Size = New Size(120, 30)
            }
            ControlHelpers.StyleDangerButton(btnDeletePayment)
            AddHandler btnDeletePayment.Click, AddressOf BtnDeletePayment_Click

            btnRecordPayment = New Button With {
                .Text = "+ Record Payment",
                .Location = New Point(715, 10),
                .Size = New Size(140, 30)
            }
            ControlHelpers.StylePrimaryButton(btnRecordPayment)
            AddHandler btnRecordPayment.Click, AddressOf BtnRecordPayment_Click

            pnlToolbar.Controls.Add(lblTitle)
            pnlToolbar.Controls.Add(txtSearch)
            pnlToolbar.Controls.Add(btnSearch)
            pnlToolbar.Controls.Add(btnReset)
            pnlToolbar.Controls.Add(btnDeletePayment)
            pnlToolbar.Controls.Add(btnRecordPayment)

            dgvPayments = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvPayments)
            SetupGrid()

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 35,
                .Padding = New Padding(0, 8, 0, 0)
            }
            lblRecordCount = New Label With {
                .Text = "Total Transactions: 0",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            lblTotalAmount = New Label With {
                .Text = "Total Collected: $0.00",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.Success,
                .Dock = DockStyle.Right,
                .AutoSize = True
            }
            pnlFooter.Controls.Add(lblRecordCount)
            pnlFooter.Controls.Add(lblTotalAmount)

            pnlCard.Controls.Add(dgvPayments)
            pnlCard.Controls.Add(pnlToolbar)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await LoadPaymentsAsync()
        End Sub

        Private Sub SetupGrid()
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment ID", .DataPropertyName = "Id", .Width = 90})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer / Client", .DataPropertyName = "CustomerName", .Width = 180})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment Date", .DataPropertyName = "PaymentDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment Method", .DataPropertyName = "PaymentMethod", .Width = 130})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Reference #", .DataPropertyName = "ReferenceNumber", .Width = 140})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Notes", .DataPropertyName = "Notes", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Amount Paid", .DataPropertyName = "Amount", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Success}})
        End Sub

        Public Async Function LoadPaymentsAsync() As Task
            Try
                Dim list = Await _paymentService.GetAllAsync(txtSearch.Text.Trim())
                dgvPayments.DataSource = list
                lblRecordCount.Text = $"Total Transactions: {list.Count}"

                Dim total = list.Sum(Function(p) p.Amount)
                lblTotalAmount.Text = $"Total Collected: {total:C2}"
            Catch ex As Exception
                MessageBox.Show($"Failed to load payments: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Function GetSelectedPayment() As PaymentDto
            If dgvPayments.CurrentRow IsNot Nothing AndAlso dgvPayments.CurrentRow.DataBoundItem IsNot Nothing Then
                Return CType(dgvPayments.CurrentRow.DataBoundItem, PaymentDto)
            End If
            Return Nothing
        End Function

        Private Async Sub BtnRecordPayment_Click(sender As Object, e As EventArgs)
            Using dlg As New PaymentDialogForm(_paymentService, _invoiceService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadPaymentsAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnDeletePayment_Click(sender As Object, e As EventArgs)
            Dim payment = GetSelectedPayment()
            If payment Is Nothing Then
                MessageBox.Show("Please select a payment to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result = MessageBox.Show($"Are you sure you want to delete payment #{payment.Id} ({payment.Amount:C2}) for invoice {payment.InvoiceNumber}? The invoice remaining balance will be updated automatically.", "Confirm Delete Payment", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Try
                    Await _paymentService.DeletePaymentAsync(payment.Id)
                    MessageBox.Show("Payment removed and invoice balance recalculated.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Await LoadPaymentsAsync()
                Catch ex As Exception
                    MessageBox.Show($"Failed to delete payment: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub
    End Class
End Namespace
