Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class CustomerStatementForm
        Inherits Form

        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _customerId As Long
        Private _customer As CustomerDto

        Private lblCustomerName As Label
        Private lblBalance As Label
        Private dgvInvoices As DataGridView
        Private dgvPayments As DataGridView

        Public Sub New(customerService As ICustomerService, customerId As Long)
            _customerService = customerService
            _customerId = customerId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "Customer Account Statement & History"
            Me.Size = New Size(900, 650)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody

            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 85,
                .BackColor = AppTheme.CardBackground,
                .Padding = New Padding(20)
            }

            lblCustomerName = New Label With {
                .Text = "Customer Statement",
                .Font = AppTheme.FontTitle,
                .ForeColor = AppTheme.TextMain,
                .AutoSize = True,
                .Location = New Point(20, 15)
            }

            lblBalance = New Label With {
                .Text = "Outstanding Balance: $0.00",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.Danger,
                .AutoSize = True,
                .Location = New Point(20, 50)
            }

            pnlHeader.Controls.Add(lblCustomerName)
            pnlHeader.Controls.Add(lblBalance)

            Dim tabs As New TabControl With {
                .Dock = DockStyle.Fill,
                .Font = AppTheme.FontBodyBold
            }

            Dim tabInvoices As New TabPage("Invoices Issued")
            dgvInvoices = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvInvoices)
            SetupInvoicesGrid()
            tabInvoices.Controls.Add(dgvInvoices)

            Dim tabPayments As New TabPage("Payment History")
            dgvPayments = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvPayments)
            SetupPaymentsGrid()
            tabPayments.Controls.Add(dgvPayments)

            tabs.TabPages.Add(tabInvoices)
            tabs.TabPages.Add(tabPayments)

            Me.Controls.Add(tabs)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Load, AddressOf CustomerStatementForm_Load
        End Sub

        Private Sub SetupInvoicesGrid()
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 130})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Issue Date", .DataPropertyName = "IssueDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Due Date", .DataPropertyName = "DueDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Status", .DataPropertyName = "Status", .Width = 110})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Amount", .DataPropertyName = "TotalAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Paid", .DataPropertyName = "PaidAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Balance Due", .DataPropertyName = "RemainingAmount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
        End Sub

        Private Sub SetupPaymentsGrid()
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment ID", .DataPropertyName = "Id", .Width = 100})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 130})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Date", .DataPropertyName = "PaymentDate", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Method", .DataPropertyName = "PaymentMethod", .Width = 130})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Ref #", .DataPropertyName = "ReferenceNumber", .Width = 140})
            dgvPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Amount Paid", .DataPropertyName = "Amount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
        End Sub

        Private Async Sub CustomerStatementForm_Load(sender As Object, e As EventArgs)
            Try
                _customer = Await _customerService.GetByIdAsync(_customerId)
                lblCustomerName.Text = $"{_customer.DisplayName} — Account Statement"
                lblBalance.Text = $"Total Invoiced: {_customer.TotalInvoiced:C2}  |  Total Paid: {_customer.TotalPaid:C2}  |  Outstanding Balance: {_customer.OutstandingBalance:C2}"

                Dim invoices = Await _customerService.GetCustomerInvoicesAsync(_customerId)
                dgvInvoices.DataSource = invoices

                Dim payments = Await _customerService.GetCustomerPaymentsAsync(_customerId)
                dgvPayments.DataSource = payments
            Catch ex As Exception
                MessageBox.Show($"Failed to load customer statement: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub
    End Class
End Namespace
