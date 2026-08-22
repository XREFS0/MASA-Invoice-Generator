Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.WinForms.Forms
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class ProductsView
        Inherits UserControl

        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService

        Private txtSearch As TextBox
        Private cmbTypeFilter As ComboBox
        Private chkOnlyActive As CheckBox
        Private btnSearch As Button
        Private btnReset As Button
        Private btnAddProduct As Button
        Private btnEditProduct As Button
        Private btnDeleteProduct As Button
        Private dgvProducts As DataGridView
        Private lblRecordCount As Label

        Public Sub New(productService As IProductService, taxRateService As ITaxRateService)
            _productService = productService
            _taxRateService = taxRateService
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
                .Text = "Products & Services",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.TextMain,
                .Location = New Point(0, 12),
                .AutoSize = True
            }

            txtSearch = New TextBox With {
                .Location = New Point(180, 12),
                .Size = New Size(180, 26),
                .Font = AppTheme.FontBody,
                .PlaceholderText = "Search SKU or name..."
            }
            AddHandler txtSearch.KeyDown, Async Sub(s, e)
                                              If e.KeyCode = Keys.Enter Then
                                                  e.SuppressKeyPress = True
                                                  Await LoadProductsAsync()
                                              End If
                                          End Sub

            cmbTypeFilter = New ComboBox With {
                .Location = New Point(368, 12),
                .Size = New Size(125, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            cmbTypeFilter.Items.Add("All Types")
            cmbTypeFilter.Items.Add("Products Only")
            cmbTypeFilter.Items.Add("Services Only")
            cmbTypeFilter.SelectedIndex = 0
            AddHandler cmbTypeFilter.SelectedIndexChanged, Async Sub(s, e) Await LoadProductsAsync()

            chkOnlyActive = New CheckBox With {
                .Text = "Active Only",
                .Checked = False,
                .Location = New Point(502, 14),
                .AutoSize = True,
                .Font = AppTheme.FontBodyBold
            }
            AddHandler chkOnlyActive.CheckedChanged, Async Sub(s, e) Await LoadProductsAsync()

            btnSearch = New Button With {
                .Text = "Search",
                .Location = New Point(602, 10),
                .Size = New Size(75, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnSearch)
            AddHandler btnSearch.Click, Async Sub(s, e) Await LoadProductsAsync()

            btnReset = New Button With {
                .Text = "Reset",
                .Location = New Point(683, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnReset)
            AddHandler btnReset.Click, Async Sub(s, e)
                                           txtSearch.Clear()
                                           cmbTypeFilter.SelectedIndex = 0
                                           chkOnlyActive.Checked = False
                                           Await LoadProductsAsync()
                                       End Sub

            btnDeleteProduct = New Button With {
                .Text = "Delete",
                .Location = New Point(755, 10),
                .Size = New Size(70, 30)
            }
            ControlHelpers.StyleDangerButton(btnDeleteProduct)
            AddHandler btnDeleteProduct.Click, AddressOf BtnDeleteProduct_Click

            btnEditProduct = New Button With {
                .Text = "Edit",
                .Location = New Point(830, 10),
                .Size = New Size(65, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnEditProduct)
            AddHandler btnEditProduct.Click, AddressOf BtnEditProduct_Click

            btnAddProduct = New Button With {
                .Text = "+ Add Item",
                .Location = New Point(900, 10),
                .Size = New Size(100, 30)
            }
            ControlHelpers.StylePrimaryButton(btnAddProduct)
            AddHandler btnAddProduct.Click, AddressOf BtnAddProduct_Click

            pnlToolbar.Controls.Add(lblTitle)
            pnlToolbar.Controls.Add(txtSearch)
            pnlToolbar.Controls.Add(cmbTypeFilter)
            pnlToolbar.Controls.Add(chkOnlyActive)
            pnlToolbar.Controls.Add(btnSearch)
            pnlToolbar.Controls.Add(btnReset)
            pnlToolbar.Controls.Add(btnDeleteProduct)
            pnlToolbar.Controls.Add(btnEditProduct)
            pnlToolbar.Controls.Add(btnAddProduct)

            dgvProducts = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvProducts)
            SetupGrid()
            AddHandler dgvProducts.CellDoubleClick, AddressOf DgvProducts_CellDoubleClick

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 30
            }
            lblRecordCount = New Label With {
                .Text = "Total Items: 0",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            pnlFooter.Controls.Add(lblRecordCount)

            pnlCard.Controls.Add(dgvProducts)
            pnlCard.Controls.Add(pnlToolbar)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await LoadProductsAsync()
        End Sub

        Private Sub SetupGrid()
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "SKU / Code", .DataPropertyName = "ProductCode", .Width = 120})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Item Name", .DataPropertyName = "Name", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Type", .DataPropertyName = "TypeDisplay", .Width = 120})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Selling Price", .DataPropertyName = "UnitPrice", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Cost Price", .DataPropertyName = "CostPrice", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "C2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Tax Rate", .DataPropertyName = "TaxRatePercentage", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "0.0\'%'" , .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvProducts.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Stock Level", .DataPropertyName = "StockQuantity", .Width = 100, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N2", .Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvProducts.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "Active", .DataPropertyName = "IsActive", .Width = 70})
        End Sub

        Public Async Function LoadProductsAsync() As Task
            Try
                Dim typeFilter As ProductType? = Nothing
                If cmbTypeFilter.SelectedIndex = 1 Then
                    typeFilter = ProductType.Product
                ElseIf cmbTypeFilter.SelectedIndex = 2 Then
                    typeFilter = ProductType.Service
                End If

                Dim list = Await _productService.GetAllAsync(txtSearch.Text.Trim(), chkOnlyActive.Checked, typeFilter)
                dgvProducts.DataSource = list
                lblRecordCount.Text = $"Total Items: {list.Count}"
            Catch ex As Exception
                MessageBox.Show($"Failed to load products: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Function GetSelectedProduct() As ProductDto
            If dgvProducts.CurrentRow IsNot Nothing AndAlso dgvProducts.CurrentRow.DataBoundItem IsNot Nothing Then
                Return CType(dgvProducts.CurrentRow.DataBoundItem, ProductDto)
            End If
            Return Nothing
        End Function

        Private Async Sub BtnAddProduct_Click(sender As Object, e As EventArgs)
            Using dlg As New ProductDialogForm(_productService, _taxRateService)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadProductsAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnEditProduct_Click(sender As Object, e As EventArgs)
            Dim prod = GetSelectedProduct()
            If prod Is Nothing Then
                MessageBox.Show("Please select an item to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New ProductDialogForm(_productService, _taxRateService, prod.Id)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    Await LoadProductsAsync()
                End If
            End Using
        End Sub

        Private Async Sub BtnDeleteProduct_Click(sender As Object, e As EventArgs)
            Dim prod = GetSelectedProduct()
            If prod Is Nothing Then
                MessageBox.Show("Please select an item to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result = MessageBox.Show($"Are you sure you want to delete item '{prod.Name}' ({prod.ProductCode})?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                Try
                    Await _productService.DeleteAsync(prod.Id)
                    MessageBox.Show("Item deleted successfully.", "Deleted", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Await LoadProductsAsync()
                Catch ex As Exception
                    MessageBox.Show($"Failed to delete item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

        Private Sub DgvProducts_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                BtnEditProduct_Click(sender, EventArgs.Empty)
            End If
        End Sub
    End Class
End Namespace
