Imports System
Imports System.Collections.Generic
Imports System.Drawing
Imports System.Linq
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Application.Services
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Enums
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class InvoiceEditorForm
        Inherits Form

        Private ReadOnly _invoiceService As IInvoiceService
        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _productService As IProductService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _calcService As IInvoiceCalculationService
        Private ReadOnly _pdfService As IInvoicePdfService
        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _invoiceId As Long?

        Private cmbCustomer As ComboBox
        Private btnQuickAddCustomer As Button
        Private txtInvoiceNumber As TextBox
        Private dtpIssueDate As DateTimePicker
        Private dtpDueDate As DateTimePicker
        Private cmbStatus As ComboBox
        Private txtCurrency As TextBox
        Private txtNotes As TextBox

        Private dgvItems As DataGridView
        Private btnAddRow As Button
        Private btnAddCatalogProduct As Button
        Private btnRemoveRow As Button

        Private lblSubtotalValue As Label
        Private lblDiscountValue As Label
        Private lblTaxValue As Label
        Private lblTotalValue As Label
        Private lblPaidValue As Label
        Private lblRemainingValue As Label

        Private btnSave As Button
        Private btnSaveAndPdf As Button
        Private btnCancel As Button

        Private _customers As IReadOnlyList(Of CustomerDto)
        Private _products As IReadOnlyList(Of ProductDto)
        Private _taxRates As IReadOnlyList(Of TaxRateDto)
        Private _existingInvoice As InvoiceDto
        Private _currentPaidAmount As Decimal = 0D

        Public Property SavedInvoice As InvoiceDto

        Public Sub New(invoiceService As IInvoiceService, customerService As ICustomerService, productService As IProductService, taxRateService As ITaxRateService, calcService As IInvoiceCalculationService, pdfService As IInvoicePdfService, companyService As ICompanySettingService, Optional invoiceId As Long? = Nothing)
            _invoiceService = invoiceService
            _customerService = customerService
            _productService = productService
            _taxRateService = taxRateService
            _calcService = calcService
            _pdfService = pdfService
            _companyService = companyService
            _invoiceId = invoiceId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_invoiceId.HasValue, "Edit Invoice", "Create New Invoice")
            Me.Size = New Size(1000, 720)
            Me.MinimumSize = New Size(900, 650)
            Me.StartPosition = FormStartPosition.CenterParent
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody

            Dim pnlHeader As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 60,
                .BackColor = AppTheme.CardBackground,
                .Padding = New Padding(20, 15, 20, 15)
            }
            Dim lblTitle As New Label With {
                .Text = If(_invoiceId.HasValue, "Edit Invoice Information", "Create New Business Invoice"),
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .AutoSize = True
            }
            pnlHeader.Controls.Add(lblTitle)

            Dim pnlTopMeta As Panel = ControlHelpers.CreateCardPanel()
            pnlTopMeta.Dock = DockStyle.Top
            pnlTopMeta.Height = 125

            pnlTopMeta.Controls.Add(CreateLabel("Customer / Client *", 16, 12))
            cmbCustomer = New ComboBox With {
                .Location = New Point(16, 32),
                .Size = New Size(280, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            pnlTopMeta.Controls.Add(cmbCustomer)

            btnQuickAddCustomer = New Button With {
                .Text = "+ New",
                .Location = New Point(302, 31),
                .Size = New Size(60, 27)
            }
            ControlHelpers.StyleSecondaryButton(btnQuickAddCustomer)
            AddHandler btnQuickAddCustomer.Click, AddressOf BtnQuickAddCustomer_Click
            pnlTopMeta.Controls.Add(btnQuickAddCustomer)

            pnlTopMeta.Controls.Add(CreateLabel("Invoice Number *", 380, 12))
            txtInvoiceNumber = New TextBox With {
                .Location = New Point(380, 32),
                .Size = New Size(140, 26),
                .Font = AppTheme.FontBodyBold
            }
            pnlTopMeta.Controls.Add(txtInvoiceNumber)

            pnlTopMeta.Controls.Add(CreateLabel("Status", 540, 12))
            cmbStatus = New ComboBox With {
                .Location = New Point(540, 32),
                .Size = New Size(130, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            For Each st In [Enum].GetValues(GetType(InvoiceStatus))
                cmbStatus.Items.Add(st)
            Next
            cmbStatus.SelectedIndex = 0
            pnlTopMeta.Controls.Add(cmbStatus)

            pnlTopMeta.Controls.Add(CreateLabel("Currency", 690, 12))
            txtCurrency = New TextBox With {
                .Location = New Point(690, 32),
                .Size = New Size(80, 26),
                .Text = "EGP"
            }
            pnlTopMeta.Controls.Add(txtCurrency)

            pnlTopMeta.Controls.Add(CreateLabel("Issue Date", 16, 68))
            dtpIssueDate = New DateTimePicker With {
                .Location = New Point(16, 88),
                .Size = New Size(160, 26),
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today
            }
            AddHandler dtpIssueDate.ValueChanged, Sub(s, e) RecalculateTotals()
            pnlTopMeta.Controls.Add(dtpIssueDate)

            pnlTopMeta.Controls.Add(CreateLabel("Due Date", 200, 68))
            dtpDueDate = New DateTimePicker With {
                .Location = New Point(200, 88),
                .Size = New Size(160, 26),
                .Format = DateTimePickerFormat.Short,
                .Value = DateTime.Today.AddDays(30)
            }
            AddHandler dtpDueDate.ValueChanged, Sub(s, e) RecalculateTotals()
            pnlTopMeta.Controls.Add(dtpDueDate)

            Dim pnlCenter As New Panel With {
                .Dock = DockStyle.Fill,
                .Padding = New Padding(16)
            }

            Dim pnlGridCard As Panel = ControlHelpers.CreateCardPanel()
            pnlGridCard.Dock = DockStyle.Fill
            pnlGridCard.Padding = New Padding(12)

            Dim pnlGridToolbar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 40
            }
            Dim lblItemsTitle As New Label With {
                .Text = "LINE ITEMS & SERVICES",
                .Font = AppTheme.FontSubheader,
                .ForeColor = AppTheme.TextMain,
                .AutoSize = True,
                .Location = New Point(0, 10)
            }

            btnAddRow = New Button With {
                .Text = "+ Add Blank Row",
                .Location = New Point(180, 4),
                .Size = New Size(130, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnAddRow)
            AddHandler btnAddRow.Click, AddressOf BtnAddRow_Click

            btnAddCatalogProduct = New Button With {
                .Text = "+ From Catalog...",
                .Location = New Point(320, 4),
                .Size = New Size(130, 30)
            }
            ControlHelpers.StylePrimaryButton(btnAddCatalogProduct)
            AddHandler btnAddCatalogProduct.Click, AddressOf BtnAddCatalogProduct_Click

            btnRemoveRow = New Button With {
                .Text = "Remove Selected",
                .Location = New Point(460, 4),
                .Size = New Size(130, 30)
            }
            ControlHelpers.StyleDangerButton(btnRemoveRow)
            AddHandler btnRemoveRow.Click, AddressOf BtnRemoveRow_Click

            pnlGridToolbar.Controls.Add(lblItemsTitle)
            pnlGridToolbar.Controls.Add(btnAddRow)
            pnlGridToolbar.Controls.Add(btnAddCatalogProduct)
            pnlGridToolbar.Controls.Add(btnRemoveRow)

            dgvItems = New DataGridView With {
                .Dock = DockStyle.Fill,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False
            }
            ControlHelpers.StyleDataGridView(dgvItems)
            SetupItemsGrid()
            AddHandler dgvItems.CellValueChanged, AddressOf DgvItems_CellValueChanged
            AddHandler dgvItems.CurrentCellDirtyStateChanged, AddressOf DgvItems_CurrentCellDirtyStateChanged

            pnlGridCard.Controls.Add(dgvItems)
            pnlGridCard.Controls.Add(pnlGridToolbar)

            Dim pnlBottom As Panel = ControlHelpers.CreateCardPanel()
            pnlBottom.Dock = DockStyle.Bottom
            pnlBottom.Height = 155
            pnlBottom.Padding = New Padding(16)

            Dim lblNotes As New Label With {
                .Text = "Notes / Payment Terms & Instructions:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(16, 12),
                .AutoSize = True
            }
            txtNotes = New TextBox With {
                .Location = New Point(16, 32),
                .Size = New Size(450, 95),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            pnlBottom.Controls.Add(lblNotes)
            pnlBottom.Controls.Add(txtNotes)

            Dim summaryX As Integer = 580
            Dim valX As Integer = 780
            Dim sumY As Integer = 10

            pnlBottom.Controls.Add(CreateSummaryLabel("Subtotal:", summaryX, sumY))
            lblSubtotalValue = CreateSummaryValueLabel("$0.00", valX, sumY)
            pnlBottom.Controls.Add(lblSubtotalValue)

            sumY += 22
            pnlBottom.Controls.Add(CreateSummaryLabel("Total Discount:", summaryX, sumY))
            lblDiscountValue = CreateSummaryValueLabel("$0.00", valX, sumY, AppTheme.Success)
            pnlBottom.Controls.Add(lblDiscountValue)

            sumY += 22
            pnlBottom.Controls.Add(CreateSummaryLabel("Total Tax:", summaryX, sumY))
            lblTaxValue = CreateSummaryValueLabel("$0.00", valX, sumY)
            pnlBottom.Controls.Add(lblTaxValue)

            sumY += 26
            pnlBottom.Controls.Add(CreateSummaryLabel("Grand Total:", summaryX, sumY, isBold:=True))
            lblTotalValue = CreateSummaryValueLabel("$0.00", valX, sumY, isBold:=True)
            pnlBottom.Controls.Add(lblTotalValue)

            sumY += 24
            pnlBottom.Controls.Add(CreateSummaryLabel("Balance Due:", summaryX, sumY, isBold:=True, color:=AppTheme.Primary))
            lblRemainingValue = CreateSummaryValueLabel("$0.00", valX, sumY, isBold:=True, color:=AppTheme.Primary)
            pnlBottom.Controls.Add(lblRemainingValue)

            pnlCenter.Controls.Add(pnlGridCard)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = AppTheme.CardBackground,
                .Padding = New Padding(20, 12, 20, 12)
            }

            btnCancel = New Button With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Location = New Point(570, 12),
                .Size = New Size(90, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnCancel)

            btnSave = New Button With {
                .Text = "Save Invoice",
                .Location = New Point(670, 12),
                .Size = New Size(130, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnSave)
            AddHandler btnSave.Click, Async Sub(s, e) Await SaveInvoiceAsync(exportPdf:=False)

            btnSaveAndPdf = New Button With {
                .Text = "Save & Export PDF",
                .Location = New Point(810, 12),
                .Size = New Size(155, 36)
            }
            ControlHelpers.StylePrimaryButton(btnSaveAndPdf)
            AddHandler btnSaveAndPdf.Click, Async Sub(s, e) Await SaveInvoiceAsync(exportPdf:=True)

            pnlFooter.Controls.Add(btnCancel)
            pnlFooter.Controls.Add(btnSave)
            pnlFooter.Controls.Add(btnSaveAndPdf)

            Me.Controls.Add(pnlCenter)
            Me.Controls.Add(pnlBottom)
            Me.Controls.Add(pnlTopMeta)
            Me.Controls.Add(pnlHeader)
            Me.Controls.Add(pnlFooter)

            AddHandler Me.Load, AddressOf InvoiceEditorForm_Load
        End Sub

        Private Sub SetupItemsGrid()
            dgvItems.Columns.Clear()

            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colProductId", .Visible = False})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colDescription", .HeaderText = "Item Description *", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colQuantity", .HeaderText = "Qty *", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colUnitPrice", .HeaderText = "Unit Price ($) *", .Width = 120, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colDiscount", .HeaderText = "Discount ($)", .Width = 110, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colTaxRate", .HeaderText = "Tax (%)", .Width = 90, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight}})
            dgvItems.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "colTotal", .HeaderText = "Total ($)", .Width = 120, .ReadOnly = True, .DefaultCellStyle = New DataGridViewCellStyle With {.Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
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

        Private Function CreateSummaryLabel(text As String, x As Integer, y As Integer, Optional isBold As Boolean = False, Optional color As Color = Nothing) As Label
            Dim lbl As New Label With {
                .Text = text,
                .Font = If(isBold, AppTheme.FontSubheader, AppTheme.FontBody),
                .ForeColor = If(color = Nothing, AppTheme.TextMuted, color),
                .Location = New Point(x, y),
                .AutoSize = True
            }
            Return lbl
        End Function

        Private Function CreateSummaryValueLabel(text As String, x As Integer, y As Integer, Optional color As Color = Nothing, Optional isBold As Boolean = False) As Label
            Dim lbl As New Label With {
                .Text = text,
                .Font = If(isBold, AppTheme.FontSubheader, AppTheme.FontBody),
                .ForeColor = If(color = Nothing, AppTheme.TextMain, color),
                .Location = New Point(x, y),
                .AutoSize = False,
                .Size = New Size(160, 22),
                .TextAlign = ContentAlignment.MiddleRight
            }
            Return lbl
        End Function

        Private Async Sub InvoiceEditorForm_Load(sender As Object, e As EventArgs)
            Try
                _customers = Await _customerService.GetAllAsync()
                _products = Await _productService.GetAllAsync(onlyActive:=True)
                _taxRates = Await _taxRateService.GetAllAsync(onlyActive:=True)

                PopulateCustomerDropdown()

                If _invoiceId.HasValue Then
                    _existingInvoice = Await _invoiceService.GetByIdAsync(_invoiceId.Value)
                    txtInvoiceNumber.Text = _existingInvoice.InvoiceNumber
                    dtpIssueDate.Value = _existingInvoice.IssueDate
                    dtpDueDate.Value = _existingInvoice.DueDate
                    cmbStatus.SelectedItem = _existingInvoice.Status
                    txtCurrency.Text = _existingInvoice.Currency
                    txtNotes.Text = _existingInvoice.Notes
                    _currentPaidAmount = _existingInvoice.PaidAmount

                    SelectCustomerInDropdown(_existingInvoice.CustomerId)

                    dgvItems.Rows.Clear()
                    For Each it In _existingInvoice.Items
                        Dim rowIdx = dgvItems.Rows.Add()
                        Dim row = dgvItems.Rows(rowIdx)
                        row.Cells("colProductId").Value = it.ProductId
                        row.Cells("colDescription").Value = it.Description
                        row.Cells("colQuantity").Value = it.Quantity.ToString("N2")
                        row.Cells("colUnitPrice").Value = it.UnitPrice.ToString("N2")
                        row.Cells("colDiscount").Value = it.DiscountAmount.ToString("N2")
                        row.Cells("colTaxRate").Value = it.TaxRate.ToString("N2")
                        row.Cells("colTotal").Value = it.TotalAmount.ToString("N2")
                    Next
                Else
                    txtInvoiceNumber.Text = Await _invoiceService.GetNextInvoiceNumberAsync()
                    AddBlankRow()
                End If

                RecalculateTotals()
            Catch ex As Exception
                MessageBox.Show($"Failed to initialize invoice editor: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Sub PopulateCustomerDropdown()
            cmbCustomer.Items.Clear()
            For Each c In _customers
                cmbCustomer.Items.Add(New With {.Id = c.Id, .DisplayText = c.DisplayName})
            Next
            cmbCustomer.DisplayMember = "DisplayText"
            cmbCustomer.ValueMember = "Id"
            If cmbCustomer.Items.Count > 0 AndAlso cmbCustomer.SelectedIndex < 0 Then
                cmbCustomer.SelectedIndex = 0
            End If
        End Sub

        Private Sub SelectCustomerInDropdown(customerId As Long)
            For i As Integer = 0 To cmbCustomer.Items.Count - 1
                Dim it = cmbCustomer.Items(i)
                Dim prop = it.GetType().GetProperty("Id")
                If prop IsNot Nothing AndAlso CLng(prop.GetValue(it)) = customerId Then
                    cmbCustomer.SelectedIndex = i
                    Exit For
                End If
            Next
        End Sub

        Private Sub BtnAddRow_Click(sender As Object, e As EventArgs)
            AddBlankRow()
        End Sub

        Private Sub AddBlankRow()
            Dim defaultTaxRate As Decimal = 0D
            Dim defTax = _taxRates.FirstOrDefault(Function(t) t.IsDefault)
            If defTax IsNot Nothing Then defaultTaxRate = defTax.Percentage

            Dim rowIdx = dgvItems.Rows.Add()
            Dim row = dgvItems.Rows(rowIdx)
            row.Cells("colProductId").Value = Nothing
            row.Cells("colDescription").Value = "Service / Item"
            row.Cells("colQuantity").Value = "1.00"
            row.Cells("colUnitPrice").Value = "0.00"
            row.Cells("colDiscount").Value = "0.00"
            row.Cells("colTaxRate").Value = defaultTaxRate.ToString("N2")
            row.Cells("colTotal").Value = "0.00"

            RecalculateTotals()
        End Sub

        Private Sub BtnAddCatalogProduct_Click(sender As Object, e As EventArgs)
            If _products Is Nothing OrElse _products.Count = 0 Then
                MessageBox.Show("No active catalog items available. Add products or services first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Using dlg As New Form With {
                .Text = "Select Product or Service",
                .Size = New Size(500, 400),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MaximizeBox = False,
                .MinimizeBox = False
            }
                Dim lst As New ListBox With {.Dock = DockStyle.Fill, .Font = AppTheme.FontBody}
                For Each p In _products
                    lst.Items.Add(New With {.Product = p, .DisplayText = $"[{p.ProductCode}] {p.Name} — {p.UnitPrice:C2} ({p.TypeDisplay})"})
                Next
                lst.DisplayMember = "DisplayText"
                lst.ValueMember = "Product"
                lst.SelectedIndex = 0

                Dim pnlActions As New Panel With {.Dock = DockStyle.Bottom, .Height = 50, .Padding = New Padding(10)}
                Dim btnOk As New Button With {.Text = "Add to Invoice", .Dock = DockStyle.Right, .Width = 120}
                ControlHelpers.StylePrimaryButton(btnOk)
                AddHandler btnOk.Click, Sub() dlg.DialogResult = DialogResult.OK
                pnlActions.Controls.Add(btnOk)

                dlg.Controls.Add(lst)
                dlg.Controls.Add(pnlActions)

                If dlg.ShowDialog(Me) = DialogResult.OK AndAlso lst.SelectedIndex >= 0 Then
                    Dim selectedItem = lst.SelectedItem
                    Dim prop = selectedItem.GetType().GetProperty("Product")
                    Dim prod = CType(prop.GetValue(selectedItem), ProductDto)

                    Dim rowIdx = dgvItems.Rows.Add()
                    Dim row = dgvItems.Rows(rowIdx)
                    row.Cells("colProductId").Value = prod.Id
                    row.Cells("colDescription").Value = prod.Name
                    row.Cells("colQuantity").Value = "1.00"
                    row.Cells("colUnitPrice").Value = prod.UnitPrice.ToString("N2")
                    row.Cells("colDiscount").Value = "0.00"
                    row.Cells("colTaxRate").Value = prod.TaxRatePercentage.ToString("N2")

                    RecalculateTotals()
                End If
            End Using
        End Sub

        Private Sub BtnRemoveRow_Click(sender As Object, e As EventArgs)
            If dgvItems.CurrentRow IsNot Nothing AndAlso Not dgvItems.CurrentRow.IsNewRow Then
                dgvItems.Rows.Remove(dgvItems.CurrentRow)
                RecalculateTotals()
            End If
        End Sub

        Private Sub DgvItems_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs)
            If dgvItems.IsCurrentCellDirty Then
                dgvItems.CommitEdit(DataGridViewDataErrorContexts.Commit)
            End If
        End Sub

        Private Sub DgvItems_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
            If e.RowIndex >= 0 Then
                RecalculateTotals()
            End If
        End Sub

        Private Sub RecalculateTotals()
            If dgvItems Is Nothing Then Return

            Dim items As New List(Of InvoiceItem)()

            For Each row As DataGridViewRow In dgvItems.Rows
                If row.IsNewRow Then Continue For

                Dim desc = Convert.ToString(row.Cells("colDescription").Value)
                Dim qty As Decimal = 1D
                Dim price As Decimal = 0D
                Dim discount As Decimal = 0D
                Dim taxRate As Decimal = 0D

                Decimal.TryParse(Convert.ToString(row.Cells("colQuantity").Value), qty)
                Decimal.TryParse(Convert.ToString(row.Cells("colUnitPrice").Value), price)
                Decimal.TryParse(Convert.ToString(row.Cells("colDiscount").Value), discount)
                Decimal.TryParse(Convert.ToString(row.Cells("colTaxRate").Value), taxRate)

                qty = Math.Max(0D, qty)
                price = Math.Max(0D, price)
                discount = Math.Max(0D, discount)
                taxRate = Math.Max(0D, taxRate)

                Dim itemRes = _calcService.CalculateItemTotals(qty, price, discount, taxRate)
                row.Cells("colTotal").Value = itemRes.TotalAmount.ToString("N2")

                items.Add(New InvoiceItem With {
                    .Description = desc,
                    .Quantity = qty,
                    .UnitPrice = price,
                    .DiscountAmount = discount,
                    .TaxRate = taxRate,
                    .TotalAmount = itemRes.TotalAmount
                })
            Next

            Dim currentStatus = If(cmbStatus.SelectedItem IsNot Nothing, CType(cmbStatus.SelectedItem, InvoiceStatus), InvoiceStatus.Draft)
            Dim invRes = _calcService.CalculateInvoiceTotals(items, _currentPaidAmount, dtpDueDate.Value, currentStatus)

            Dim curr = If(String.IsNullOrWhiteSpace(txtCurrency.Text), "EGP", txtCurrency.Text.Trim())

            lblSubtotalValue.Text = $"{curr} {invRes.Subtotal:N2}"
            lblDiscountValue.Text = $"-{curr} {invRes.DiscountAmount:N2}"
            lblTaxValue.Text = $"{curr} {invRes.TaxAmount:N2}"
            lblTotalValue.Text = $"{curr} {invRes.TotalAmount:N2}"
            lblRemainingValue.Text = $"{curr} {invRes.RemainingAmount:N2}"
        End Sub

        Private Async Sub BtnQuickAddCustomer_Click(sender As Object, e As EventArgs)
            Using dlg As New CustomerDialogForm(_customerService)
                If dlg.ShowDialog(Me) = DialogResult.OK AndAlso dlg.SavedCustomer IsNot Nothing Then
                    _customers = Await _customerService.GetAllAsync()
                    PopulateCustomerDropdown()
                    SelectCustomerInDropdown(dlg.SavedCustomer.Id)
                End If
            End Using
        End Sub

        Private Async Function SaveInvoiceAsync(exportPdf As Boolean) As Task
            If cmbCustomer.SelectedIndex < 0 Then
                MessageBox.Show("Please select a customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If dgvItems.Rows.Count = 0 Then
                MessageBox.Show("Please add at least one line item.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                btnSave.Enabled = False
                btnSaveAndPdf.Enabled = False

                Dim item = cmbCustomer.SelectedItem
                Dim prop = item.GetType().GetProperty("Id")
                Dim customerId = CLng(prop.GetValue(item))

                Dim lineItems As New List(Of InvoiceItem)()
                For Each row As DataGridViewRow In dgvItems.Rows
                    If row.IsNewRow Then Continue For

                    Dim pIdVal = row.Cells("colProductId").Value
                    Dim pId As Long? = If(pIdVal IsNot Nothing AndAlso Long.TryParse(pIdVal.ToString(), Nothing), CLng(pIdVal), Nothing)

                    Dim desc = Convert.ToString(row.Cells("colDescription").Value)
                    Dim qty As Decimal = 1D
                    Dim price As Decimal = 0D
                    Dim discount As Decimal = 0D
                    Dim taxRate As Decimal = 0D

                    Decimal.TryParse(Convert.ToString(row.Cells("colQuantity").Value), qty)
                    Decimal.TryParse(Convert.ToString(row.Cells("colUnitPrice").Value), price)
                    Decimal.TryParse(Convert.ToString(row.Cells("colDiscount").Value), discount)
                    Decimal.TryParse(Convert.ToString(row.Cells("colTaxRate").Value), taxRate)

                    lineItems.Add(New InvoiceItem With {
                        .ProductId = pId,
                        .Description = desc,
                        .Quantity = qty,
                        .UnitPrice = price,
                        .DiscountAmount = discount,
                        .TaxRate = taxRate
                    })
                Next

                Dim entity As New Invoice With {
                    .Id = If(_invoiceId, 0L),
                    .InvoiceNumber = txtInvoiceNumber.Text.Trim(),
                    .CustomerId = customerId,
                    .IssueDate = dtpIssueDate.Value,
                    .DueDate = dtpDueDate.Value,
                    .Status = CType(cmbStatus.SelectedItem, InvoiceStatus),
                    .Currency = txtCurrency.Text.Trim(),
                    .Notes = txtNotes.Text.Trim(),
                    .Items = lineItems
                }

                If _invoiceId.HasValue Then
                    SavedInvoice = Await _invoiceService.UpdateAsync(entity)
                Else
                    SavedInvoice = Await _invoiceService.CreateAsync(entity)
                End If

                If exportPdf Then
                    Dim company = Await _companyService.GetSettingAsync()
                    Using sfd As New SaveFileDialog With {
                        .Filter = "PDF Document (*.pdf)|*.pdf",
                        .FileName = $"{SavedInvoice.InvoiceNumber}_{SavedInvoice.CustomerName.Replace(" ", "_")}.pdf"
                    }
                        If sfd.ShowDialog(Me) = DialogResult.OK Then
                            Await _pdfService.ExportPdfToFile(SavedInvoice, company, sfd.FileName)
                            MessageBox.Show($"Invoice saved and exported to: {sfd.FileName}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        End If
                    End Using
                Else
                    MessageBox.Show("Invoice saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If

                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As ValidationException
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show($"Failed to save invoice: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSave.Enabled = True
                btnSaveAndPdf.Enabled = True
            End Try
        End Function
    End Class
End Namespace
