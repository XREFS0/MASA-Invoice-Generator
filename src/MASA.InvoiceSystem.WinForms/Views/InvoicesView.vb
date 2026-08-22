Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class InvoicesView
        Inherits UserControl

        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _paymentService As IPaymentService

        Private txtSearch As TextBox
        Private cmbStatusFilter As ComboBox
        Private btnSearch As Button
        Private btnReset As Button
        Private btnCreateInvoice As Button
        Private btnEditInvoice As Button
        Private btnViewDetails As Button
        Private btnDuplicate As Button
        Private btnRecordPayment As Button
        Private btnDeleteInvoice As Button
        Private dgvInvoices As DataGridView
        Private lblRecordCount As Label

        Public Sub New(invoiceService As IInvoiceService, customerService As ICustomerService, productService As IProductService, taxRateService As ITaxRateService, calcService As IInvoiceCalculationService, pdfService As IInvoicePdfService, companyService As ICompanySettingService, paymentService As IPaymentService)
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
            Me.Padding = New Padding(20)

            Dim pnlCard As Panel = ControlHelpers.CreateCardPanel()
            pnlCard.Dock = DockStyle.Fill
            pnlCard.Padding = New Padding(16)

            Dim pnlToolbar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 55
            }

            Dim lblTitle As New Label With {
                .Text = "Invoice Registry",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.TextMain,
                .Location = New Point(0, 12),
                .AutoSize = True
            }

            txtSearch = New TextBox With {
                .Location = New Point(150, 12),
                .Size = New Size(170, 26),
                .Font = AppTheme.FontBody,
                .PlaceholderText = "Invoice # or Client..."
            }
            AddHandler txtSearch.KeyDown, Async Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  Await LoadInvoicesAsync()
                                              End If
                                          End Sub

            cmbStatusFilter = New ComboBox With {
                .Location = New Point(328, 12),
                .Size = New Size(120, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            cmbStatusFilter.Items.Add("All Statuses")
            For Each st In [Enum].GetValues(GetType(InvoiceStatus))
                cmbStatusFilter.Items.Add(st)
            Next
            cmbStatusFilter.SelectedIndex = 0
            AddHandler cmbStatusFilter.SelectedIndexChanged, Async Sub(s, e) Await LoadInvoicesAsync()

            btnSearch = New Button With {
                .Text = "Search",
                .Location = New Point(455, 10),
                .Size = New Size(75, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnSearch)
            AddHandler btnSearch.Click, Async Sub(s, e) Await LoadInvoicesAsync()

            btnReset = New Button With {
                .Text = "Reset",
                .Location = New Point(535, 10),
                .Size = New Size(60, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnReset)
            AddHandler btnReset.Click, Async Sub(s, e)
                                           txtSearch.Clear()
                                           cmbStatusFilter.SelectedIndex = 0
                                           Await LoadInvoicesAsync()
                                       End Sub

            btnViewDetails = New Button With {
                .Text = "View & PDF",
                .Location = New Point(600, 10),
                .Size = New Size(95, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnViewDetails)
            AddHandler btnViewDetails.Click, AddressOf BtnViewDetails_Click

            btnRecordPayment = New Button With {
                .Text = "Pay",
                .Location = New Point(700, 10),
                .Size = New Size(60, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnRecordPayment)
            AddHandler btnRecordPayment.Click, AddressOf BtnRecordPayment_Click

            btnDuplicate = New Button With {
                .Text = "Duplicate",
                .Location = New Point(765, 10),
                .Size = New Size(80, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnDuplicate)
            AddHandler btnDuplicate.Click, AddressOf BtnDuplicate_Click

            btnEditInvoice = New Button With {
                .Text = "Edit",
                .Location = New Point(850, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnEditInvoice)
            AddHandler btnEditInvoice.Click, AddressOf BtnEditInvoice_Click

            btnDeleteInvoice = New Button With {
                .Text = "Delete",
                .Location = New Point(920, 10),
                .Size = New Size(70, 30)
            }
            ControlHelpers.StyleDangerButton(btnDeleteInvoice)
            AddHandler btnDeleteInvoice.Click, AddressOf BtnDeleteInvoice_Click

            btnCreateInvoice = New Button With {
                .Text = "+ New Invoice",
                .Location = New Point(995, 10),
                .Size = New Size(115, 30)
            }
            ControlHelpers.StylePrimaryButton(btnCreateInvoice)
            AddHandler btnCreateInvoice.Click, AddressOf BtnCreateInvoice_Click

            pnlToolbar.Controls.Add(lblTitle)
            pnlToolbar.Controls.Add(txtSearch)
            pnlToolbar.Controls.Add(cmbStatusFilter)
            pnlToolbar.Controls.Add(btnSearch)
            pnlToolbar.Controls.Add(btnReset)
            pnlToolbar.Controls.Add(btnViewDetails)
            pnlToolbar.Controls.Add(btnRecordPayment)
            pnlToolbar.Controls.Add(btnDuplicate)
            pnlToolbar.Controls.Add(btnEditInvoice)
            pnlToolbar.Controls.Add(btnDeleteInvoice)
            pnlToolbar.Controls.Add(btnCreateInvoice)

            dgvInvoices = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvInvoices)
            SetupGrid()
            AddHandler dgvInvoices.CellDoubleClick, AddressOf DgvInvoices_CellDoubleClick
            AddHandler dgvInvoices.CellFormatting, AddressOf DgvInvoices_CellFormatting

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 30
            }
            lblRecordCount = New Label With {
                .Text = "Total Invoices: 0",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            pnlFooter.Controls.Add(lblRecordCount)

            pnlCard.Controls.Add(dgvInvoices)
            pnlCard.Controls.Add(pnlToolbar)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await LoadInvoicesAsync()
        End Sub

        Private Sub SetupGrid()
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoice #", .DataPropertyName = "InvoiceNumber", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Customer / Client", .DataPropertyName = "CustomerName", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Issue Date", .DataPropertyName = "IssueDate", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Due Date", .DataPropertyName = "DueDate", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd"}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colStatus", .HeaderText = "Status", .DataPropertyName = "Status", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = AppTheme.FontSmallBold}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Amount", .DataPropertyName = "TotalAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Paid", .DataPropertyName = "PaidAmount", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success}})
            dgvInvoices.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Balance Due", .DataPropertyName = "RemainingAmount", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Danger}})
        End Sub

        Public Async Function LoadInvoicesAsync() As Task
            Try
                Dim statusFilter As InvoiceStatus? = Nothing
                If cmbStatusFilter.SelectedIndex > 0 Then
                    statusFilter = CType(cmbStatusFilter.SelectedItem, InvoiceStatus)
                End If

                Dim list = Await _invoiceService.GetAllAsync(txtSearch.Text.Trim(), statusFilter)
                dgvInvoices.DataSource = list
                lblRecordCount.Text = $"Total Invoices: {list.Count}"
            Catch ex As Exception
                MessageBox.Show($"Failed to load invoices: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Function GetSelectedInvoice() As InvoiceDto
            If dgvInvoices.CurrentRow IsNot Nothing AndAlso dgvInvoices.CurrentRow.DataBoundItem IsNot Nothing Then
                Return CType(dgvInvoices.CurrentRow.DataBoundItem, InvoiceDto)
            End If
            Return Nothing
        End Function

        Private Sub DgvInvoices_CellFormatting(sender As Object, e As DataGridViewCellFormattingEventArgs)
            If dgvInvoices.Columns(e.ColumnIndex).Name = "colStatus" AndAlso e.Value IsNot Nothing Then
                Dim status As InvoiceStatus
                If [Enum].TryParse(e.Value.ToString(), status) Then
                    Dim colors = AppTheme.GetStatusColors(status)
                    e.CellStyle.BackColor = colors.Item1
                    e.CellStyle.ForeColor = colors.Item2
                End If
            End If
        End Sub

        Private Async Sub BtnCreateInvoice_Click(sender As Object, e As EventArgs)
            Using dlg As New InvoiceEditorForm(_invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadInvoicesAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditInvoice_Click(sender As Object, e As EventArgs)
            Dim inv = GetSelectedInvoice()
            If inv Is Nothing Then
                MessageBox.Show("Please select an invoice to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New InvoiceEditorForm(_invoiceService, _customerService, _productService, _taxRateService, _calcService, _pdfService, _companyService, inv.Id)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadInvoicesAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnViewDetails_Click(sender As Object, e As EventArgs)
            Dim inv = GetSelectedInvoice()
            If inv Is Nothing Then
                MessageBox.Show("Please select an invoice to view.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New InvoiceDetailsForm(_invoiceService, _paymentService, _pdfService, _companyService, inv.Id)
                dlg.ShowDialog(Me)
                Await LoadInvoicesAsync()
            End Using
        End Sub

        Private Async Sub BtnDuplicate_Click(sender As Object, e As EventArgs)
            Dim inv = GetSelectedInvoice()
            If inv Is Nothing Then
                MessageBox.Show("Please select an invoice to duplicate.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result = MessageBox.Show($"Create a new draft duplicate of invoice {inv.InvoiceNumber}?", "Duplicate Invoice", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Try
                    Dim newInv = Await _invoiceService.DuplicateAsync(inv.Id)
                    MessageBox.Show($"Invoice duplicated as #{newInv.InvoiceNumber}.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Await LoadInvoicesAsync()
                Catch ex As Exception
                    MessageBox.Show($"Failed to duplicate invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

        Private Async Sub BtnRecordPayment_Click(sender As Object, e As EventArgs)
            Dim inv = GetSelectedInvoice()
            Using dlg As New PaymentDialogForm(_paymentService, _invoiceService, inv?.Id)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadInvoicesAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnDeleteInvoice_Click(sender As Object, e As EventArgs)
            Dim inv = GetSelectedInvoice()
            If inv Is Nothing Then
                MessageBox.Show("Please select an invoice to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result = MessageBox.Show($"Are you sure you want to delete invoice #{inv.InvoiceNumber}?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Try
                    Await _invoiceService.DeleteAsync(inv.Id)
                    MessageBox.Show("Invoice deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Await LoadInvoicesAsync()
                Catch ex As BusinessRuleException
                    MessageBox.Show(ex.Message, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Catch ex As Exception
                    MessageBox.Show($"Failed to delete invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

        Private Sub DgvInvoices_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                BtnViewDetails_Click(sender, EventArgs.Empty)
            End If
        End Sub
    End Class
End Namespace
