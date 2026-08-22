Imports System
Imports System.Drawing
Imports System.Linq
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class DashboardView
        Inherits UserControl

        Private ReadOnly _dashboardService As IDashboardService
        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _paymentService As IPaymentService

        Private flowKpi As FlowLayoutPanel
        Private cardRevenue As Panel
        Private cardOutstanding As Panel
        Private cardPaidInvoices As Panel
        Private cardOverdue As Panel
        Private cardCustomers As Panel
        Private cardLowStock As Panel

        Private dgvRecentInvoices As DataGridView
        Private dgvRecentPayments As DataGridView
        Private dgvMonthlySales As DataGridView

        Private btnNewInvoice As Button
        Private btnNewCustomer As Button
        Private btnNewPayment As Button
        Private btnRefresh As Button

        Public Sub New(dashboardService As IDashboardService, invoiceService As IInvoiceService, customerService As ICustomerService, productService As IProductService, taxRateService As ITaxRateService, calcService As IInvoiceCalculationService, pdfService As IInvoicePdfService, companyService As ICompanySettingService, paymentService As IPaymentService)
            _dashboardService = dashboardService
            _invoiceService = invoiceService
            _customerService = customerService
            _productService = productService
            _taxRateService = taxRateService
            _calcService = calcService
            _pdfService = pdfService
            _companyService = companyService
            _paymentService = paymentService

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody
            Me.AutoScroll = True

            Dim pnlActionBar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .Padding = New Padding(20, 12, 20, 12),
                .BackColor = AppTheme.AppBackground
            }

            Dim lblTitle As New Label With {
                .Text = "Executive Dashboard",
                .Font = AppTheme.FontTitle,
                .ForeColor = AppTheme.TextMain,
                .AutoSize = True,
                .Location = New Point(20, 14)
            }

            btnRefresh = New Button With {
                .Text = "Refresh",
                .Location = New Point(440, 12),
                .Size = New Size(95, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub(s, e) Await LoadDashboardDataAsync()

            btnNewPayment = New Button With {
                .Text = "Record Payment",
                .Location = New Point(545, 12),
                .Size = New Size(140, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnNewPayment)
            AddHandler btnNewPayment.Click, AddressOf BtnNewPayment_Click

            btnNewCustomer = New Button With {
                .Text = "New Customer",
                .Location = New Point(695, 12),
                .Size = New Size(130, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnNewCustomer)
            AddHandler btnNewCustomer.Click, AddressOf BtnNewCustomer_Click

            btnNewInvoice = New Button With {
                .Text = "+ Create Invoice",
                .Location = New Point(835, 12),
                .Size = New Size(140, 36)
            }
            ControlHelpers.StylePrimaryButton(btnNewInvoice)
            AddHandler btnNewInvoice.Click, AddressOf BtnNewInvoice_Click

            pnlActionBar.Controls.Add(lblTitle)
            pnlActionBar.Controls.Add(btnRefresh)
            pnlActionBar.Controls.Add(btnNewPayment)
            pnlActionBar.Controls.Add(btnNewCustomer)
            pnlActionBar.Controls.Add(btnNewInvoice)

            flowKpi = New FlowLayoutPanel With {
                .Dock = DockStyle.Top,
                .Height = 125,
                .Padding = New Padding(16, 0, 16, 10),
                .WrapContents = False,
                .AutoScroll = False
            }

            cardRevenue = ControlHelpers.CreateKpiCard("Total Revenue", "$0.00", "Collected payments", AppTheme.Success)
            cardOutstanding = ControlHelpers.CreateKpiCard("Outstanding Due", "$0.00", "Unpaid balance", AppTheme.Primary)
            cardPaidInvoices = ControlHelpers.CreateKpiCard("Paid Invoices", "0", "Completed orders", AppTheme.Success)
            cardOverdue = ControlHelpers.CreateKpiCard("Overdue Invoices", "0", "Action required", AppTheme.Danger)
            cardCustomers = ControlHelpers.CreateKpiCard("Clients", "0", "Active accounts", AppTheme.Info)
            cardLowStock = ControlHelpers.CreateKpiCard("Low Stock Items", "0", "Inventory threshold", AppTheme.Warning)

            flowKpi.Controls.Add(cardRevenue)
            flowKpi.Controls.Add(cardOutstanding)
            flowKpi.Controls.Add(cardPaidInvoices)
            flowKpi.Controls.Add(cardOverdue)
            flowKpi.Controls.Add(cardCustomers)
            flowKpi.Controls.Add(cardLowStock)

            Dim pnlMainGrids As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(20, 0, 20, 20)
            }

            Dim pnlLeft As Panel = ControlHelpers.CreateCardPanel()
            pnlLeft.Dock = DockStyle.Left
            pnlLeft.Width = 580

            Dim lblRecentInvTitle As New Label With {
                .Text = "RECENT INVOICES",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.TextMain,
                .Dock = DockStyle.Top,
                .Height = 30
            }

            dgvRecentInvoices = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvRecentInvoices)
            SetupRecentInvoicesGrid()
            AddHandler dgvRecentInvoices.CellDoubleClick, AddressOf DgvRecentInvoices_CellDoubleClick

            pnlLeft.Controls.Add(dgvRecentInvoices)
            pnlLeft.Controls.Add(lblRecentInvTitle)

            Dim pnlRight As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16, 0, 0, 0)
            }

            Dim pnlPaymentsCard As Panel = ControlHelpers.CreateCardPanel()
            pnlPaymentsCard.Dock = DockStyle.Top
            pnlPaymentsCard.Height = 220

            Dim lblRecentPayTitle As New Label With {
                .Text = "RECENT PAYMENTS RECEIVED",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.TextMain,
                .Dock = DockStyle.Top,
                .Height = 28
            }

            dgvRecentPayments = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvRecentPayments)
            SetupRecentPaymentsGrid()

            pnlPaymentsCard.Controls.Add(dgvRecentPayments)
            pnlPaymentsCard.Controls.Add(lblRecentPayTitle)

            Dim pnlMonthlyCard As Panel = ControlHelpers.CreateCardPanel()
            pnlMonthlyCard.Dock = DockStyle.Fill
            pnlMonthlyCard.Padding = New Padding(12)

            Dim lblMonthlyTitle As New Label With {
                .Text = "MONTHLY SALES BREAKDOWN (LAST 6 MONTHS)",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.TextMain,
                .Dock = DockStyle.Top,
                .Height = 28
            }

            dgvMonthlySales = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvMonthlySales)
            SetupMonthlySalesGrid()

            pnlMonthlyCard.Controls.Add(dgvMonthlySales)
            pnlMonthlyCard.Controls.Add(lblMonthlyTitle)

            pnlRight.Controls.Add(pnlMonthlyCard)
            pnlRight.Controls.Add(pnlPaymentsCard)

            pnlMainGrids.Controls.Add(pnlRight)
            pnlMainGrids.Controls.Add(pnlLeft)

            Me.Controls.Add(pnlMainGrids)
            Me.Controls.Add(flowKpi)
            Me.Controls.Add(pnlActionBar)

            AddHandler Me.Load, Async Sub(s, e) Await LoadDashboardDataAsync()
        End Sub

        Private Sub SetupRecentInvoicesGrid()
            dgvRecentInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 110})
            dgvRecentInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Client", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvRecentInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Due Date", .DataPropertyName = "DueDate", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvRecentInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Status", .DataPropertyName = "Status", .Width = 90})
            dgvRecentInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total", .DataPropertyName = "TotalAmount", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
        End Sub

        Private Sub SetupRecentPaymentsGrid()
            dgvRecentPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Inv #", .DataPropertyName = "InvoiceNumber", .Width = 90})
            dgvRecentPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Client", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvRecentPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Method", .DataPropertyName = "PaymentMethod", .Width = 95})
            dgvRecentPayments.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Amount", .DataPropertyName = "Amount", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Success}})
        End Sub

        Private Sub SetupMonthlySalesGrid()
            dgvMonthlySales.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Month", .DataPropertyName = "MonthLabel", .Width = 110})
            dgvMonthlySales.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoices", .DataPropertyName = "InvoiceCount", .Width = 80, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvMonthlySales.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Billed Total", .DataPropertyName = "TotalAmount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvMonthlySales.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Paid Total", .DataPropertyName = "PaidAmount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success, .Font = AppTheme.FontBodyBold}})
        End Sub

        Public Async Function LoadDashboardDataAsync() As Task
            Try
                Dim summary = Await _dashboardService.GetDashboardSummaryAsync()

                UpdateKpiValue(cardRevenue, $"{summary.TotalRevenue:C2}")
                UpdateKpiValue(cardOutstanding, $"{summary.OutstandingBalance:C2}")
                UpdateKpiValue(cardPaidInvoices, $"{summary.PaidInvoicesCount}")
                UpdateKpiValue(cardOverdue, $"{summary.OverdueInvoicesCount}")
                UpdateKpiValue(cardCustomers, $"{summary.TotalCustomersCount}")
                UpdateKpiValue(cardLowStock, $"{summary.LowStockItemsCount}")

                dgvRecentInvoices.DataSource = summary.RecentInvoices
                dgvRecentPayments.DataSource = summary.RecentPayments
                dgvMonthlySales.DataSource = summary.MonthlySales
            Catch ex As Exception
                MessageBox.Show($"Failed to load dashboard: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Sub UpdateKpiValue(card As Panel, valueText As String)
            For Each c In card.Controls
                If TypeOf c Is Label AndAlso DirectCast(c, Label).Name = "lblKpiValue" Then
                    DirectCast(c, Label).Text = valueText
                    Exit For
                End If
            Next
        End Sub

        Private Async Sub BtnNewInvoice_Click(sender As Object, e As EventArgs)
            Using dlg As New InvoiceEditorForm(_invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadDashboardDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnNewCustomer_Click(sender As Object, e As EventArgs)
            Using dlg As New CustomerDialogForm(_customerService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadDashboardDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnNewPayment_Click(sender As Object, e As EventArgs)
            Using dlg As New PaymentDialogForm(_paymentService, _invoiceService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadDashboardDataAsync()
                End If
            End Using
        End Sub

        Private Async Sub DgvRecentInvoices_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 AndAlso dgvRecentInvoices.Rows(e.RowIndex).DataBoundItem IsNot Nothing Then
                Dim inv = CType(dgvRecentInvoices.Rows(e.RowIndex).DataBoundItem, InvoiceDto)
                Using dlg As New InvoiceDetailsForm(_invoiceService, _paymentService, _pdfService, _companyService, inv.Id)
                    dlg.ShowDialog(Me)
                    Await LoadDashboardDataAsync()
                End Using
            End If
        End Sub
    End Class
End Namespace
