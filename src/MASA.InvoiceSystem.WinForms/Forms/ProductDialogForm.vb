Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class ProductDialogForm
        Inherits Form

        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _productId As Long?

        Private txtProductCode As TextBox
        Private txtName As TextBox
        Private txtDescription As TextBox
        Private rdoProduct As RadioButton
        Private rdoService As RadioButton
        Private numUnitPrice As NumericUpDown
        Private numCostPrice As NumericUpDown
        Private cmbTaxRate As ComboBox
        Private numStockQuantity As NumericUpDown
        Private numLowStockAlert As NumericUpDown
        Private chkIsActive As CheckBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Property SavedProduct As ProductDto

        Public Sub New(productService As IProductService, taxRateService As ITaxRateService, Optional productId As Long? = Nothing)
            _productService = productService
            _taxRateService = taxRateService
            _productId = productId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_productId.HasValue, "Edit Item", "Add New Product / Service")
            Me.Size = New Size(540, 560)
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
                .Text = If(_productId.HasValue, "Edit Product or Service Item", "Create New Catalog Item"),
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlBody As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(24, 16, 24, 16),
                .AutoScroll = True
            }

            Dim y As Integer = 10

            pnlBody.Controls.Add(CreateLabel("Item Type *", 0, y))
            rdoProduct = New RadioButton With {.Text = "Physical Product", .Checked = True, .Location = New Point(0, y + 20), .AutoSize = True}
            rdoService = New RadioButton With {.Text = "Service / Consulting", .Location = New Point(150, y + 20), .AutoSize = True}
            AddHandler rdoProduct.CheckedChanged, Sub()
                                                      numStockQuantity.Enabled = rdoProduct.Checked
                                                      numLowStockAlert.Enabled = rdoProduct.Checked
                                                  End Sub
            pnlBody.Controls.Add(rdoProduct)
            pnlBody.Controls.Add(rdoService)

            y += 55
            pnlBody.Controls.Add(CreateLabel("SKU / Product Code *", 0, y))
            txtProductCode = CreateTextBox(0, y + 20, 230)
            pnlBody.Controls.Add(txtProductCode)

            pnlBody.Controls.Add(CreateLabel("Item Name *", 250, y))
            txtName = CreateTextBox(250, y + 20, 230)
            pnlBody.Controls.Add(txtName)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Selling Price (Unit Price) *", 0, y))
            numUnitPrice = New NumericUpDown With {.Location = New Point(0, y + 20), .Size = New Size(230, 26), .DecimalPlaces = 2, .Maximum = 10000000D, .Minimum = 0D, .Font = AppTheme.FontBody}
            pnlBody.Controls.Add(numUnitPrice)

            pnlBody.Controls.Add(CreateLabel("Cost Price (Internal)", 250, y))
            numCostPrice = New NumericUpDown With {.Location = New Point(250, y + 20), .Size = New Size(230, 26), .DecimalPlaces = 2, .Maximum = 10000000D, .Minimum = 0D, .Font = AppTheme.FontBody}
            pnlBody.Controls.Add(numCostPrice)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Default Tax Rate", 0, y))
            cmbTaxRate = New ComboBox With {.Location = New Point(0, y + 20), .Size = New Size(230, 26), .DropDownStyle = ComboBoxStyle.DropDownList}
            pnlBody.Controls.Add(cmbTaxRate)

            chkIsActive = New CheckBox With {.Text = "Active (Available for Invoicing)", .Checked = True, .Location = New Point(250, y + 22), .AutoSize = True, .Font = AppTheme.FontBodyBold}
            pnlBody.Controls.Add(chkIsActive)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Stock Quantity", 0, y))
            numStockQuantity = New NumericUpDown With {.Location = New Point(0, y + 20), .Size = New Size(230, 26), .DecimalPlaces = 2, .Maximum = 1000000D, .Minimum = 0D, .Font = AppTheme.FontBody}
            pnlBody.Controls.Add(numStockQuantity)

            pnlBody.Controls.Add(CreateLabel("Low Stock Alert Threshold", 250, y))
            numLowStockAlert = New NumericUpDown With {.Location = New Point(250, y + 20), .Size = New Size(230, 26), .DecimalPlaces = 2, .Maximum = 1000000D, .Minimum = 0D, .Value = 5D, .Font = AppTheme.FontBody}
            pnlBody.Controls.Add(numLowStockAlert)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Item Description", 0, y))
            txtDescription = New TextBox With {
                .Location = New Point(0, y + 20),
                .Size = New Size(480, 50),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            pnlBody.Controls.Add(txtDescription)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = AppTheme.AppBackground,
                .Padding = New Padding(20, 12, 20, 12)
            }

            btnCancel = New Button With {.Text = "Cancel", .DialogResult = DialogResult.Cancel, .Location = New Point(300, 12), .Size = New Size(90, 36)}
            ControlHelpers.StyleSecondaryButton(btnCancel)

            btnSave = New Button With {.Text = "Save Item", .Location = New Point(400, 12), .Size = New Size(120, 36)}
            ControlHelpers.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click

            pnlFooter.Controls.Add(btnCancel)
            pnlFooter.Controls.Add(btnSave)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Load, AddressOf ProductDialogForm_Load
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

        Private Function CreateTextBox(x As Integer, y As Integer, width As Integer) As TextBox
            Return New TextBox With {
                .Location = New Point(x, y),
                .Size = New Size(width, 26),
                .Font = AppTheme.FontBody
            }
        End Function

        Private Async Sub ProductDialogForm_Load(sender As Object, e As EventArgs)
            Try
                Dim taxes = Await _taxRateService.GetAllAsync()
                cmbTaxRate.DisplayMember = "Name"
                cmbTaxRate.ValueMember = "Id"
                cmbTaxRate.DataSource = taxes

                Dim defaultTax = taxes.FirstOrDefault(Function(t) t.IsDefault)
                If defaultTax IsNot Nothing Then
                    cmbTaxRate.SelectedValue = defaultTax.Id
                End If

                If _productId.HasValue Then
                    Dim prod = Await _productService.GetByIdAsync(_productId.Value)
                    txtProductCode.Text = prod.ProductCode
                    txtName.Text = prod.Name
                    txtDescription.Text = prod.Description
                    numUnitPrice.Value = prod.UnitPrice
                    numCostPrice.Value = prod.CostPrice
                    chkIsActive.Checked = prod.IsActive
                    numStockQuantity.Value = prod.StockQuantity
                    numLowStockAlert.Value = prod.MinimumStock

                    If prod.Type = ProductType.Service Then
                        rdoService.Checked = True
                    Else
                        rdoProduct.Checked = True
                    End If

                    If prod.TaxRateId.HasValue Then
                        cmbTaxRate.SelectedValue = prod.TaxRateId.Value
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show($"Failed to load item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            Try
                btnSave.Enabled = False
                btnSave.Text = "Saving..."

                Dim entity As New Product With {
                    .Id = If(_productId, 0L),
                    .ProductCode = txtProductCode.Text.Trim(),
                    .Name = txtName.Text.Trim(),
                    .Description = txtDescription.Text.Trim(),
                    .Type = If(rdoService.Checked, ProductType.Service, ProductType.Product),
                    .UnitPrice = numUnitPrice.Value,
                    .CostPrice = numCostPrice.Value,
                    .TaxRateId = If(cmbTaxRate.SelectedValue IsNot Nothing, CLng(cmbTaxRate.SelectedValue), Nothing),
                    .StockQuantity = If(rdoProduct.Checked, numStockQuantity.Value, 0D),
                    .MinimumStock = If(rdoProduct.Checked, numLowStockAlert.Value, 0D),
                    .IsActive = chkIsActive.Checked
                }

                SavedProduct = Await _productService.SaveAsync(entity)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As ValidationException
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show($"Failed to save item: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSave.Enabled = True
                btnSave.Text = "Save Item"
            End Try
        End Sub
    End Class
End Namespace
