Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes
Imports MASA.InvoiceSystem.WinForms.Views

Namespace Forms
    Public Class MainForm
        Inherits Form

        Private ReadOnly _dashboardService As IDashboardService
        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _paymentService As IPaymentService
        Private ReadOnly _reportService As IReportService
        Private ReadOnly _backupService As IBackupService
        Private ReadOnly _auditLogService As IAuditLogService

        Private pnlSidebar As Panel
        Private pnlHeader As Panel
        Private pnlContent As Panel
        Private lblSectionTitle As Label
        Private lblDateTime As Label
        Private timerClock As Timer

        Private navButtons As New List(Of Button)()
        Private currentActiveButton As Button

        Private viewDashboard As DashboardView
        Private viewInvoices As InvoicesView
        Private viewCustomers As CustomersView
        Private viewProducts As ProductsView
        Private viewPayments As PaymentsView
        Private viewReports As ReportsView
        Private viewSettings As SettingsView
        Private viewAuditLogs As AuditLogsView

        Public Sub New(dashboardService As IDashboardService, invoiceService As IInvoiceService, customerService As ICustomerService, productService As IProductService, taxRateService As ITaxRateService, calcService As IInvoiceCalculationService, pdfService As IInvoicePdfService, companyService As ICompanySettingService, paymentService As IPaymentService, reportService As IReportService, backupService As IBackupService, auditLogService As IAuditLogService)
            _dashboardService = dashboardService
            _invoiceService = invoiceService
            _customerService = customerService
            _productService = productService
            _taxRateService = taxRateService
            _calcService = calcService
            _pdfService = pdfService
            _companyService = companyService
            _paymentService = paymentService
            _reportService = reportService
            _backupService = backupService
            _auditLogService = auditLogService

            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = "MASA Invoice & Billing Management System"
            Me.Size = New Size(1280, 800)
            Me.MinimumSize = New Size(1100, 700)
            Me.StartPosition = FormStartPosition.CenterScreen
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody

            pnlSidebar = New Panel With {
                .Dock = DockStyle.Left,
                .Width = 240,
                .BackColor = AppTheme.SidebarBg,
                .Padding = New Padding(0)
            }

            Dim pnlBrand As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 80,
                .BackColor = AppTheme.SidebarHeader,
                .Padding = New Padding(20, 16, 20, 16)
            }
            Dim lblBrand As New Label With {
                .Text = "MASA INVOICE",
                .Font = New Font("Segoe UI", 13.0F, FontStyle.Bold),
                .ForeColor = Color.White,
                .AutoSize = True,
                .Location = New Point(18, 16)
            }
            Dim lblSubBrand As New Label With {
                .Text = "Billing & Financial System",
                .Font = AppTheme.FontSmall,
                .ForeColor = AppTheme.TextSubtle,
                .AutoSize = True,
                .Location = New Point(19, 42)
            }
            pnlBrand.Controls.Add(lblBrand)
            pnlBrand.Controls.Add(lblSubBrand)

            Dim pnlNav As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(12, 16, 12, 16),
                .AutoScroll = True
            }

            Dim btnNavDashboard = CreateNavButton("Dashboard", 0)
            Dim btnNavInvoices = CreateNavButton("Invoices", 1)
            Dim btnNavCustomers = CreateNavButton("Customers", 2)
            Dim btnNavProducts = CreateNavButton("Products & Services", 3)
            Dim btnNavPayments = CreateNavButton("Payments", 4)
            Dim btnNavReports = CreateNavButton("Reports", 5)
            Dim btnNavSettings = CreateNavButton("Settings", 6)
            Dim btnNavAudit = CreateNavButton("Audit Trail", 7)

            AddHandler btnNavDashboard.Click, Sub() SwitchView("Executive Dashboard", viewDashboard, btnNavDashboard)
            AddHandler btnNavInvoices.Click, Sub() SwitchView("Invoice Registry", viewInvoices, btnNavInvoices)
            AddHandler btnNavCustomers.Click, Sub() SwitchView("Customer Directory", viewCustomers, btnNavCustomers)
            AddHandler btnNavProducts.Click, Sub() SwitchView("Products & Services Catalog", viewProducts, btnNavProducts)
            AddHandler btnNavPayments.Click, Sub() SwitchView("Payment Registry", viewPayments, btnNavPayments)
            AddHandler btnNavReports.Click, Sub() SwitchView("Business Intelligence & Reports", viewReports, btnNavReports)
            AddHandler btnNavSettings.Click, Sub() SwitchView("System Configuration & Backup", viewSettings, btnNavSettings)
            AddHandler btnNavAudit.Click, Sub() SwitchView("Audit Trail & Logs", viewAuditLogs, btnNavAudit)

            pnlNav.Controls.Add(btnNavAudit)
            pnlNav.Controls.Add(btnNavSettings)
            pnlNav.Controls.Add(btnNavReports)
            pnlNav.Controls.Add(btnNavPayments)
            pnlNav.Controls.Add(btnNavProducts)
            pnlNav.Controls.Add(btnNavCustomers)
            pnlNav.Controls.Add(btnNavInvoices)
            pnlNav.Controls.Add(btnNavDashboard)

            Dim pnlSidebarFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 50,
                .BackColor = AppTheme.SidebarHeader,
                .Padding = New Padding(16, 10, 16, 10)
            }
            Dim lblDbStatus As New Label With {
                .Text = "SQLite Storage: Active",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.Success,
                .AutoSize = True,
                .Location = New Point(16, 16)
            }
            pnlSidebarFooter.Controls.Add(lblDbStatus)

            pnlSidebar.Controls.Add(pnlNav)
            pnlSidebar.Controls.Add(pnlSidebarFooter)
            pnlSidebar.Controls.Add(pnlBrand)

            pnlHeader = New Panel With {
                .Dock = DockStyle.Top,
                .Height = 65,
                .BackColor = AppTheme.HeaderBackground,
                .Padding = New Padding(24, 12, 24, 12)
            }

            lblSectionTitle = New Label With {
                .Text = "Executive Dashboard",
                .Font = AppTheme.FontTitle,
                .ForeColor = AppTheme.TextMain,
                .AutoSize = True,
                .Location = New Point(24, 16)
            }

            lblDateTime = New Label With {
                .Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  HH:mm:ss"),
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Dock = DockStyle.Right,
                .TextAlign = ContentAlignment.MiddleRight,
                .AutoSize = False,
                .Width = 320
            }

            Dim btnHeaderNewInvoice As New Button With {
                .Text = "+ Quick Invoice",
                .Location = New Point(680, 14),
                .Size = New Size(130, 36)
            }
            ControlHelpers.StylePrimaryButton(btnHeaderNewInvoice)
            AddHandler btnHeaderNewInvoice.Click, AddressOf BtnHeaderNewInvoice_Click

            pnlHeader.Controls.Add(lblSectionTitle)
            pnlHeader.Controls.Add(btnHeaderNewInvoice)
            pnlHeader.Controls.Add(lblDateTime)

            pnlContent = New Panel With {
                .Dock = DockStyle.Fill,
                .BackColor = AppTheme.AppBackground
            }

            Me.Controls.Add(pnlContent)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(pnlSidebar)

            timerClock = New Timer With {.Interval = 1000}
            AddHandler timerClock.Tick, Sub() lblDateTime.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy  HH:mm:ss")
            timerClock.Start()

            AddHandler Me.Load, AddressOf MainForm_Load
        End Sub

        Private Function CreateNavButton(text As String, index As Integer) As Button
            Dim btn As New Button With {
                .Text = text,
                .Dock = DockStyle.Top,
                .Height = 44,
                .FlatStyle = FlatStyle.Flat,
                .BackColor = AppTheme.SidebarBg,
                .ForeColor = AppTheme.SidebarText,
                .Font = AppTheme.FontSubheader,
                .TextAlign = ContentAlignment.MiddleLeft,
                .Padding = New Padding(16, 0, 0, 0),
                .Cursor = Cursors.Hand,
                .Margin = New Padding(0, 0, 0, 4)
            }
            btn.FlatAppearance.BorderSize = 0
            btn.FlatAppearance.MouseOverBackColor = AppTheme.SidebarHover

            navButtons.Add(btn)
            Return btn
        End Function

        Private Sub MainForm_Load(sender As Object, e As EventArgs)
            viewDashboard = New DashboardView(_dashboardService, _invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService, _paymentService)
            viewInvoices = New InvoicesView(_invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService, _paymentService)
            viewCustomers = New CustomersView(_customerService)
            viewProducts = New ProductsView(_productService, _taxRateService)
            viewPayments = New PaymentsView(_paymentService, _invoiceService)
            viewReports = New ReportsView(_reportService)
            viewSettings = New SettingsView(_companyService, _taxRateService, _backupService)
            viewAuditLogs = New AuditLogsView(_auditLogService)

            If navButtons.Count > 0 Then
                SwitchView("Executive Dashboard", viewDashboard, navButtons(0))
            End If
        End Sub

        Private Sub SwitchView(title As String, view As UserControl, activeBtn As Button)
            lblSectionTitle.Text = title

            If currentActiveButton IsNot Nothing Then
                currentActiveButton.BackColor = AppTheme.SidebarBg
                currentActiveButton.ForeColor = AppTheme.SidebarText
            End If

            activeBtn.BackColor = AppTheme.SidebarActive
            activeBtn.ForeColor = AppTheme.SidebarTextActive
            currentActiveButton = activeBtn

            pnlContent.SuspendLayout()
            pnlContent.Controls.Clear()
            view.Dock = DockStyle.Fill
            pnlContent.Controls.Add(view)
            pnlContent.ResumeLayout()

            If TypeOf view Is DashboardView Then
                Dim dv = DirectCast(view, DashboardView)
                Dim task = dv.LoadDashboardDataAsync()
            ElseIf TypeOf view Is InvoicesView Then
                Dim iv = DirectCast(view, InvoicesView)
                Dim task = iv.LoadInvoicesAsync()
            ElseIf TypeOf view Is CustomersView Then
                Dim cv = DirectCast(view, CustomersView)
                Dim task = cv.LoadCustomersAsync()
            ElseIf TypeOf view Is ProductsView Then
                Dim pv = DirectCast(view, ProductsView)
                Dim task = pv.LoadProductsAsync()
            ElseIf TypeOf view Is PaymentsView Then
                Dim pv = DirectCast(view, PaymentsView)
                Dim task = pv.LoadPaymentsAsync()
            ElseIf TypeOf view Is ReportsView Then
                Dim rv = DirectCast(view, ReportsView)
                Dim task = rv.RunReportAsync()
            ElseIf TypeOf view Is SettingsView Then
                Dim sv = DirectCast(view, SettingsView)
                Dim task = sv.LoadSettingsAsync()
            ElseIf TypeOf view Is AuditLogsView Then
                Dim av = DirectCast(view, AuditLogsView)
                Dim task = av.LoadLogsAsync()
            End If
        End Sub

        Private Sub BtnHeaderNewInvoice_Click(sender As Object, e As EventArgs)
            Using dlg As New InvoiceEditorForm(_invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    If currentActiveButton IsNot Nothing Then
                        currentActiveButton.PerformClick()
                    End If
                End If
            End Using
        End Sub
    End Class
End Namespace
