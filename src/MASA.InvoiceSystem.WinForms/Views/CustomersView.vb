Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class CustomersView
        Inherits UserControl

        Private ReadOnly _customerService As ICustomerService

        Private txtSearch As TextBox
        Private btnSearch As Button
        Private btnReset As Button
        Private btnAddCustomer As Button
        Private btnEditCustomer As Button
        Private btnDeleteCustomer As Button
        Private btnStatement As Button
        Private dgvCustomers As DataGridView
        Private lblRecordCount As Label

        Public Sub New(customerService As ICustomerService)
            _customerService = customerService
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
                .Text = "Customer Directory",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.TextMain,
                .Location = New Point(0, 12),
                .AutoSize = True
            }

            txtSearch = New TextBox With {
                .Location = New Point(180, 12),
                .Size = New Size(220, 26),
                .Font = AppTheme.FontBody,
                .PlaceholderText = "Search by name, company, email..."
            }
            AddHandler txtSearch.KeyDown, Async Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  Await LoadCustomersAsync(txtSearch.Text.Trim())
                                              End If
                                          End Sub

            btnSearch = New Button With {
                .Text = "Search",
                .Location = New Point(408, 10),
                .Size = New Size(75, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnSearch)
            AddHandler btnSearch.Click, Async Sub(s, e) Await LoadCustomersAsync(txtSearch.Text.Trim())

            btnReset = New Button With {
                .Text = "Reset",
                .Location = New Point(488, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnReset)
            AddHandler btnReset.Click, Async Sub(s, e)
                                           txtSearch.Clear()
                                           Await LoadCustomersAsync()
                                       End Sub

            btnStatement = New Button With {
                .Text = "Statement",
                .Location = New Point(558, 10),
                .Size = New Size(85, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnStatement)
            AddHandler btnStatement.Click, AddressOf BtnStatement_Click

            btnDeleteCustomer = New Button With {
                .Text = "Delete",
                .Location = New Point(648, 10),
                .Size = New Size(70, 30)
            }
            ControlHelpers.StyleDangerButton(btnDeleteCustomer)
            AddHandler btnDeleteCustomer.Click, AddressOf BtnDeleteCustomer_Click

            btnEditCustomer = New Button With {
                .Text = "Edit",
                .Location = New Point(723, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnEditCustomer)
            AddHandler btnEditCustomer.Click, AddressOf BtnEditCustomer_Click

            btnAddCustomer = New Button With {
                .Text = "+ Add Customer",
                .Location = New Point(793, 10),
                .Size = New Size(130, 30)
            }
            ControlHelpers.StylePrimaryButton(btnAddCustomer)
            AddHandler btnAddCustomer.Click, AddressOf BtnAddCustomer_Click

            pnlToolbar.Controls.Add(lblTitle)
            pnlToolbar.Controls.Add(txtSearch)
            pnlToolbar.Controls.Add(btnSearch)
            pnlToolbar.Controls.Add(btnReset)
            pnlToolbar.Controls.Add(btnStatement)
            pnlToolbar.Controls.Add(btnDeleteCustomer)
            pnlToolbar.Controls.Add(btnEditCustomer)
            pnlToolbar.Controls.Add(btnAddCustomer)

            dgvCustomers = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvCustomers)
            SetupGrid()
            AddHandler dgvCustomers.CellDoubleClick, AddressOf DgvCustomers_CellDoubleClick

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 30
            }
            lblRecordCount = New Label With {
                .Text = "Total Customers: 0",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            pnlFooter.Controls.Add(lblRecordCount)

            pnlCard.Controls.Add(dgvCustomers)
            pnlCard.Controls.Add(pnlToolbar)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await LoadCustomersAsync()
        End Sub

        Private Sub SetupGrid()
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Full Name", .DataPropertyName = "FullName", .Width = 140})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Company", .DataPropertyName = "CompanyName", .Width = 140})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Email", .DataPropertyName = "Email", .Width = 150})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Phone", .DataPropertyName = "Phone", .Width = 120})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "City", .DataPropertyName = "City", .Width = 90})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Invoices", .DataPropertyName = "InvoiceCount", .Width = 70, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleCenter}})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Invoiced", .DataPropertyName = "TotalInvoiced", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Total Paid", .DataPropertyName = "TotalPaid", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .ForeColor = AppTheme.Success}})
            dgvCustomers.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Balance Due", .DataPropertyName = "OutstandingBalance", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold, .ForeColor = AppTheme.Danger}})
        End Sub

        Public Async Function LoadCustomersAsync(Optional query As String = Nothing) As Task
            Try
                Dim list = Await _customerService.GetAllAsync(query)
                dgvCustomers.DataSource = list
                lblRecordCount.Text = $"Total Customers: {list.Count}"
            Catch ex As Exception
                MessageBox.Show($"Failed to load customers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Function GetSelectedCustomer() As CustomerDto
            If dgvCustomers.CurrentRow IsNot Nothing AndAlso dgvCustomers.CurrentRow.DataBoundItem IsNot Nothing Then
                Return CType(dgvCustomers.CurrentRow.DataBoundItem, CustomerDto)
            End If
            Return Nothing
        End Function

        Private Async Sub BtnAddCustomer_Click(sender As Object, e As EventArgs)
            Using dlg As New CustomerDialogForm(_customerService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadCustomersAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditCustomer_Click(sender As Object, e As EventArgs)
            Dim cust = GetSelectedCustomer()
            If cust Is Nothing Then
                MessageBox.Show("Please select a customer to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New CustomerDialogForm(_customerService, cust.Id)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadCustomersAsync(txtSearch.Text.Trim())
                End If
            End Using
        End Sub

        Private Async Sub BtnDeleteCustomer_Click(sender As Object, e As EventArgs)
            Dim cust = GetSelectedCustomer()
            If cust Is Nothing Then
                MessageBox.Show("Please select a customer to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result = MessageBox.Show($"Are you sure you want to permanently delete customer '{cust.DisplayName}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Try
                    Await _customerService.DeleteAsync(cust.Id)
                    MessageBox.Show("Customer deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Await LoadCustomersAsync(txtSearch.Text.Trim())
                Catch ex As BusinessRuleException
                    MessageBox.Show(ex.Message, "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Catch ex As Exception
                    MessageBox.Show($"Failed to delete customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

        Private Sub BtnStatement_Click(sender As Object, e As EventArgs)
            Dim cust = GetSelectedCustomer()
            If cust Is Nothing Then
                MessageBox.Show("Please select a customer to view statement.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New CustomerStatementForm(_customerService, cust.Id)
                dlg.ShowDialog(Me)
            End Using
        End Sub

        Private Sub DgvCustomers_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                BtnEditCustomer_Click(sender, EventArgs.Empty)
            End If
        End Sub
    End Class
End Namespace
