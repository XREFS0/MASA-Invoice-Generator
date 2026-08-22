Imports System
Imports System.Drawing
Imports System.IO
Imports System.Text
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class ReportsView
        Inherits UserControl

        Private ReadOnly _reportService As IReportService

        Private cmbReportType As ComboBox
        Private cmbDatePreset As ComboBox
        Private dtpFromDate As DateTimePicker
        Private dtpToDate As DateTimePicker
        Private btnGenerate As Button
        Private btnExportCsv As Button
        Private dgvReport As DataGridView
        Private lblSummaryText As Label

        Public Sub New(reportService As IReportService)
            _reportService = reportService
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

            Dim pnlFilter As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 70
            }

            Dim lblReportType As New Label With {
                .Text = "Report Type:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            cmbReportType = New ComboBox With {
                .Location = New Point(0, 28),
                .Size = New Size(220, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            cmbReportType.Items.Add("1. Sales Performance Report")
            cmbReportType.Items.Add("2. Revenue & Monthly Trend")
            cmbReportType.Items.Add("3. Outstanding Balances & Aging")
            cmbReportType.Items.Add("4. Customer Sales Performance")
            cmbReportType.Items.Add("5. Product & Service Breakdown")
            cmbReportType.Items.Add("6. Payments Log Report")
            cmbReportType.SelectedIndex = 0
            AddHandler cmbReportType.SelectedIndexChanged, Async Sub(s, e) Await RunReportAsync()

            Dim lblPreset As New Label With {
                .Text = "Date Range:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(235, 8),
                .AutoSize = True
            }
            cmbDatePreset = New ComboBox With {
                .Location = New Point(235, 28),
                .Size = New Size(140, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            cmbDatePreset.Items.Add("This Month")
            cmbDatePreset.Items.Add("Last Month")
            cmbDatePreset.Items.Add("This Year")
            cmbDatePreset.Items.Add("All Time")
            cmbDatePreset.Items.Add("Custom Range")
            cmbDatePreset.SelectedIndex = 2
            AddHandler cmbDatePreset.SelectedIndexChanged, AddressOf CmbDatePreset_SelectedIndexChanged

            Dim lblFrom As New Label With {
                .Text = "From:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(390, 8),
                .AutoSize = True
            }
            dtpFromDate = New DateTimePicker With {
                .Location = New Point(390, 28),
                .Size = New Size(125, 26),
                .Format = DateTimePickerFormat.Short,
                .Value = New DateTime(DateTime.Today.Year, 1, 1)
            }

            Dim lblTo As New Label With {
                .Text = "To:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(530, 8),
                .AutoSize = True
            }
            dtpToDate = New DateTimePicker With {
                .Location = New Point(530, 28),
                .Size = New Size(125, 26),
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today
            }

            btnGenerate = New Button With {
                .Text = "Run Report",
                .Location = New Point(675, 24),
                .Size = New Size(100, 32)
            }
            ControlHelpers.StylePrimaryButton(btnGenerate)
            AddHandler btnGenerate.Click, Async Sub(s, e) Await RunReportAsync()

            btnExportCsv = New Button With {
                .Text = "Export CSV",
                .Location = New Point(785, 24),
                .Size = New Size(100, 32)
            }
            ControlHelpers.StyleSecondaryButton(btnExportCsv)
            AddHandler btnExportCsv.Click, AddressOf BtnExportCsv_Click

            pnlFilter.Controls.Add(lblReportType)
            pnlFilter.Controls.Add(cmbReportType)
            pnlFilter.Controls.Add(lblPreset)
            pnlFilter.Controls.Add(cmbDatePreset)
            pnlFilter.Controls.Add(lblFrom)
            pnlFilter.Controls.Add(dtpFromDate)
            pnlFilter.Controls.Add(lblTo)
            pnlFilter.Controls.Add(dtpToDate)
            pnlFilter.Controls.Add(btnGenerate)
            pnlFilter.Controls.Add(btnExportCsv)

            dgvReport = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvReport)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 35,
                .Padding = New Padding(0, 8, 0, 0)
            }
            lblSummaryText = New Label With {
                .Text = "Report ready.",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMain,
                .Dock = DockStyle.Fill
            }
            pnlFooter.Controls.Add(lblSummaryText)

            pnlCard.Controls.Add(dgvReport)
            pnlCard.Controls.Add(pnlFilter)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await RunReportAsync()
        End Sub

        Private Sub CmbDatePreset_SelectedIndexChanged(sender As Object, e As EventArgs)
            Dim today = DateTime.Today
            Select Case cmbDatePreset.SelectedIndex
                Case 0
                    dtpFromDate.Value = New DateTime(today.Year, today.Month, 1)
                    dtpToDate.Value = today
                Case 1
                    Dim lastMonth = today.AddMonths(-1)
                    dtpFromDate.Value = New DateTime(lastMonth.Year, lastMonth.Month, 1)
                    dtpToDate.Value = New DateTime(today.Year, today.Month, 1).AddDays(-1)
                Case 2
                    dtpFromDate.Value = New DateTime(today.Year, 1, 1)
                    dtpToDate.Value = today
                Case 3
                    dtpFromDate.Value = New DateTime(2020, 1, 1)
                    dtpToDate.Value = today.AddYears(1)
            End Select
        End Sub

        Public Async Function RunReportAsync() As Task
            Try
                dgvReport.Columns.Clear()
                Dim fromDate = dtpFromDate.Value.Date
                Dim toDate = dtpToDate.Value.Date

                Select Case cmbReportType.SelectedIndex
                    Case 0
                        Dim data = Await _reportService.GetSalesReportAsync(fromDate, toDate)
                        SetupSalesReportGrid()
                        dgvReport.DataSource = data
                        Dim totalBilled = data.Sum(Function(x) x.TotalAmount)
                        Dim totalPaid = data.Sum(Function(x) x.PaidAmount)
                        Dim totalDue = data.Sum(Function(x) x.RemainingAmount)
                        lblSummaryText.Text = $"Sales Report ({data.Count} records) | Billed: {totalBilled:C2} | Collected: {totalPaid:C2} | Balance Due: {totalDue:C2}"

                    Case 1
                        Dim data = Await _reportService.GetRevenueReportAsync(fromDate, toDate)
                        SetupRevenueGrid()
                        dgvReport.DataSource = data
                        Dim totalRev = data.Sum(Function(x) x.PaidAmount)
                        Dim totalBilled = data.Sum(Function(x) x.TotalAmount)
                        lblSummaryText.Text = $"Monthly Revenue Report ({data.Count} months) | Total Billed: {totalBilled:C2} | Total Collected: {totalRev:C2}"

                    Case 2
                        Dim data = Await _reportService.GetOutstandingInvoicesReportAsync()
                        SetupOutstandingGrid()
                        dgvReport.DataSource = data
                        Dim totalOutstanding = data.Sum(Function(x) x.RemainingAmount)
                        lblSummaryText.Text = $"Outstanding Receivables ({data.Count} unpaid invoices) | Total Balance Due: {totalOutstanding:C2}"

                    Case 3
                        Dim data = Await _reportService.GetCustomerSalesReportAsync(fromDate, toDate)
                        SetupCustomerSalesGrid()
                        dgvReport.DataSource = data
                        Dim totalCustSales = data.Sum(Function(x) x.TotalBilled)
                        lblSummaryText.Text = $"Customer Sales Performance ({data.Count} customers) | Total Volume: {totalCustSales:C2}"

                    Case 4
                        Dim data = Await _reportService.GetProductSalesReportAsync(fromDate, toDate)
                        SetupProductSalesGrid()
                        dgvReport.DataSource = data
                        Dim totalProdSales = data.Sum(Function(x) x.TotalRevenue)
                        lblSummaryText.Text = $"Product & Service Sales ({data.Count} items) | Total Item Revenue: {totalProdSales:C2}"

                    Case 5
                        Dim data = Await _reportService.GetPaymentsReportAsync(fromDate, toDate)
                        SetupPaymentsLogGrid()
                        dgvReport.DataSource = data
                        Dim totalPayments = data.Sum(Function(x) x.Amount)
                        lblSummaryText.Text = $"Payment Log ({data.Count} transactions) | Total Amount: {totalPayments:C2}"
                End Select
            Catch ex As Exception
                MessageBox.Show($"Failed to generate report: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Sub SetupSalesReportGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Date", .DataPropertyName = "IssueDate", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Due Date", .DataPropertyName = "DueDate", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Status", .DataPropertyName = "Status", .Width = 90})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Subtotal", .DataPropertyName = "Subtotal", .Width = 105, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Discount", .DataPropertyName = "DiscountAmount", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Tax", .DataPropertyName = "TaxAmount", .Width = 95, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Billed", .DataPropertyName = "TotalAmount", .Width = 115, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Collected", .DataPropertyName = "PaidAmount", .Width = 105, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Balance Due", .DataPropertyName = "RemainingAmount", .Width = 115, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Danger, .Font = AppTheme.FontBodyBold}})
        End Sub

        Private Sub SetupRevenueGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Month / Period", .DataPropertyName = "MonthLabel", .Width = 150})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoices Issued", .DataPropertyName = "InvoiceCount", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Invoiced Amount", .DataPropertyName = "TotalAmount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Payments Collected", .DataPropertyName = "PaidAmount", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Success}})
        End Sub

        Private Sub SetupOutstandingGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Issue Date", .DataPropertyName = "IssueDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Due Date", .DataPropertyName = "DueDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Status", .DataPropertyName = "Status", .Width = 100})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Billed", .DataPropertyName = "TotalAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Paid to Date", .DataPropertyName = "PaidAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Outstanding Balance", .DataPropertyName = "RemainingAmount", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Danger}})
        End Sub

        Private Sub SetupCustomerSalesGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer Name", .DataPropertyName = "CustomerName", .Width = 180, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Company", .DataPropertyName = "CompanyName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoices", .DataPropertyName = "InvoiceCount", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Billed", .DataPropertyName = "TotalBilled", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Paid", .DataPropertyName = "TotalPaid", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Balance Due", .DataPropertyName = "OutstandingBalance", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Danger}})
        End Sub

        Private Sub SetupProductSalesGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "SKU", .DataPropertyName = "ProductCode", .Width = 120})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Product / Service Name", .DataPropertyName = "ProductName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Type", .DataPropertyName = "ProductType", .Width = 120})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Units / Qty Sold", .DataPropertyName = "QuantitySold", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Revenue Generated", .DataPropertyName = "TotalRevenue", .Width = 180, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Success}})
        End Sub

        Private Sub SetupPaymentsLogGrid()
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Payment ID", .DataPropertyName = "Id", .Width = 90})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Date", .DataPropertyName = "PaymentDate", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Method", .DataPropertyName = "PaymentMethod", .Width = 130})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Ref #", .DataPropertyName = "ReferenceNumber", .Width = 140})
            dgvReport.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Amount Paid", .DataPropertyName = "Amount", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Success}})
        End Sub

        Private Sub BtnExportCsv_Click(sender As Object, e As EventArgs)
            If dgvReport.Rows.Count = 0 Then
                MessageBox.Show("No data available to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using sfd As New SaveFileDialog With {
                .Filter = "CSV File (*.csv)|*.csv",
                .FileName = $"Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            }
                If sfd.ShowDialog(Me) = DialogResult.OK Then
                    Try
                        Dim sb As New StringBuilder()

                        Dim headerCols As New List(Of String)()
                        For Each col As DataGridViewColumn In dgvReport.Columns
                            If col.Visible Then
                                headerCols.Add($"""{col.HeaderText.Replace("""", """""")}""")
                            End If
                        Next
                        sb.AppendLine(String.Join(",", headerCols))

                        For Each row As DataGridViewRow In dgvReport.Rows
                            If row.IsNewRow Then Continue For
                            Dim cellVals As New List(Of String)()
                            For Each col As DataGridViewColumn In dgvReport.Columns
                                If col.Visible Then
                                    Dim val = row.Cells(col.Index).FormattedValue?.ToString()
                                    cellVals.Add($"""{If(val, String.Empty).Replace("""", """""")}""")
                                End If
                            Next
                            sb.AppendLine(String.Join(",", cellVals))
                        Next

                        File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8)
                        MessageBox.Show($"Report exported successfully to: {sfd.FileName}", "Export Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"Failed to export CSV: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub
    End Class
End Namespace
