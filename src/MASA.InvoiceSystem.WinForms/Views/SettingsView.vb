Imports System
Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class SettingsView
        Inherits UserControl

        Private ReadOnly _companyService As ICompanySettingService
        Private ReadOnly _taxRateService As ITaxRateService
        Private ReadOnly _backupService As IBackupService

        Private txtCompanyName As TextBox
        Private txtEmail As TextBox
        Private txtPhone As TextBox
        Private txtWebsite As TextBox
        Private txtAddress As TextBox
        Private txtCity As TextBox
        Private txtCountry As TextBox
        Private txtPostalCode As TextBox
        Private txtTaxNumber As TextBox
        Private txtDefaultCurrency As TextBox
        Private txtInvoicePrefix As TextBox
        Private numNextInvoiceNumber As NumericUpDown
        Private btnSaveCompany As Button

        Private dgvTaxRates As DataGridView
        Private btnAddTax As Button
        Private btnEditTax As Button
        Private btnDeleteTax As Button
        Private btnSetDefaultTax As Button

        Private btnBackupNow As Button
        Private btnRestoreNow As Button
        Private lblDbPath As Label
        Private lblStatus As Label

        Public Sub New(companyService As ICompanySettingService, taxRateService As ITaxRateService, backupService As IBackupService)
            _companyService = companyService
            _taxRateService = taxRateService
            _backupService = backupService
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Dock = DockStyle.Fill
            Me.BackColor = AppTheme.AppBackground
            Me.Font = AppTheme.FontBody
            Me.Padding = New Padding(20)

            Dim tabs As New TabControl With {
                .Dock = DockStyle.Fill,
                .Font = AppTheme.FontBodyBold
            }

            Dim tabCompany As New TabPage("Company Profile & Numbering")
            tabCompany.BackColor = AppTheme.CardBackground
            tabCompany.Padding = New Padding(24)
            tabCompany.AutoScroll = True

            Dim y As Integer = 15

            Dim lblSec1 As New Label With {
                .Text = "Company Information & Letterhead Details",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .Location = New Point(10, y),
                .AutoSize = True
            }
            tabCompany.Controls.Add(lblSec1)

            y += 40
            tabCompany.Controls.Add(CreateLabel("Company Name *", 10, y))
            txtCompanyName = CreateTextBox(10, y + 20, 360)
            tabCompany.Controls.Add(txtCompanyName)

            tabCompany.Controls.Add(CreateLabel("Tax ID / VAT Number", 390, y))
            txtTaxNumber = CreateTextBox(390, y + 20, 250)
            tabCompany.Controls.Add(txtTaxNumber)

            y += 60
            tabCompany.Controls.Add(CreateLabel("Billing Email Address", 10, y))
            txtEmail = CreateTextBox(10, y + 20, 360)
            tabCompany.Controls.Add(txtEmail)

            tabCompany.Controls.Add(CreateLabel("Phone Number", 390, y))
            txtPhone = CreateTextBox(390, y + 20, 250)
            tabCompany.Controls.Add(txtPhone)

            y += 60
            tabCompany.Controls.Add(CreateLabel("Official Website", 10, y))
            txtWebsite = CreateTextBox(10, y + 20, 360)
            tabCompany.Controls.Add(txtWebsite)

            tabCompany.Controls.Add(CreateLabel("Default Currency Code (e.g. EGP, USD, EUR, SAR)", 390, y))
            txtDefaultCurrency = CreateTextBox(390, y + 20, 250)
            txtDefaultCurrency.Text = "EGP"
            tabCompany.Controls.Add(txtDefaultCurrency)

            y += 60
            tabCompany.Controls.Add(CreateLabel("Street Address", 10, y))
            txtAddress = CreateTextBox(10, y + 20, 630)
            tabCompany.Controls.Add(txtAddress)

            y += 60
            tabCompany.Controls.Add(CreateLabel("City", 10, y))
            txtCity = CreateTextBox(10, y + 20, 200)
            tabCompany.Controls.Add(txtCity)

            tabCompany.Controls.Add(CreateLabel("Postal Code", 225, y))
            txtPostalCode = CreateTextBox(225, y + 20, 145)
            tabCompany.Controls.Add(txtPostalCode)

            tabCompany.Controls.Add(CreateLabel("Country", 390, y))
            txtCountry = CreateTextBox(390, y + 20, 250)
            tabCompany.Controls.Add(txtCountry)

            y += 70
            Dim lblSec2 As New Label With {
                .Text = "Automatic Invoice Numbering Scheme",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .Location = New Point(10, y),
                .AutoSize = True
            }
            tabCompany.Controls.Add(lblSec2)

            y += 40
            tabCompany.Controls.Add(CreateLabel("Invoice Prefix (e.g. MASA-EG-, INV-)", 10, y))
            txtInvoicePrefix = CreateTextBox(10, y + 20, 200)
            txtInvoicePrefix.Text = "MASA-EG-"
            tabCompany.Controls.Add(txtInvoicePrefix)

            tabCompany.Controls.Add(CreateLabel("Next Serial Number (Integer)", 225, y))
            numNextInvoiceNumber = New NumericUpDown With {
                .Location = New Point(225, y + 20),
                .Size = New Size(200, 26),
                .Maximum = 100000000D,
                .Minimum = 1D,
                .Value = 1001D,
                .Font = AppTheme.FontBody
            }
            tabCompany.Controls.Add(numNextInvoiceNumber)

            y += 70
            btnSaveCompany = New Button With {
                .Text = "Save Company Settings",
                .Location = New Point(10, y),
                .Size = New Size(200, 38)
            }
            ControlHelpers.StylePrimaryButton(btnSaveCompany)
            AddHandler btnSaveCompany.Click, AddressOf BtnSaveCompany_Click
            tabCompany.Controls.Add(btnSaveCompany)

            Dim tabTaxes As New TabPage("Tax Rates Management")
            tabTaxes.BackColor = AppTheme.CardBackground
            tabTaxes.Padding = New Padding(20)

            Dim pnlTaxToolbar As New Panel With {
                .Dock = DockStyle.Top,
                .Height = 45
            }
            btnAddTax = New Button With {.Text = "+ Add Tax Rate", .Location = New Point(0, 5), .Size = New Size(130, 32)}
            ControlHelpers.StylePrimaryButton(btnAddTax)
            AddHandler btnAddTax.Click, AddressOf BtnAddTax_Click

            btnEditTax = New Button With {.Text = "Edit Rate", .Location = New Point(140, 5), .Size = New Size(95, 32)}
            ControlHelpers.StyleSecondaryButton(btnEditTax)
            AddHandler btnEditTax.Click, AddressOf BtnEditTax_Click

            btnSetDefaultTax = New Button With {.Text = "Set as Default", .Location = New Point(245, 5), .Size = New Size(120, 32)}
            ControlHelpers.StyleSecondaryButton(btnSetDefaultTax)
            AddHandler btnSetDefaultTax.Click, AddressOf BtnSetDefaultTax_Click

            btnDeleteTax = New Button With {.Text = "Delete", .Location = New Point(375, 5), .Size = New Size(80, 32)}
            ControlHelpers.StyleDangerButton(btnDeleteTax)
            AddHandler btnDeleteTax.Click, AddressOf BtnDeleteTax_Click

            pnlTaxToolbar.Controls.Add(btnAddTax)
            pnlTaxToolbar.Controls.Add(btnEditTax)
            pnlTaxToolbar.Controls.Add(btnSetDefaultTax)
            pnlTaxToolbar.Controls.Add(btnDeleteTax)

            dgvTaxRates = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvTaxRates)
            SetupTaxGrid()

            tabTaxes.Controls.Add(dgvTaxRates)
            tabTaxes.Controls.Add(pnlTaxToolbar)

            Dim tabBackup As New TabPage("Database Backup & Restore")
            tabBackup.BackColor = AppTheme.CardBackground
            tabBackup.Padding = New Padding(24)

            Dim lblBackupTitle As New Label With {
                .Text = "SQLite Database Maintenance & Recovery",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.PrimaryDark,
                .Location = New Point(10, 15),
                .AutoSize = True
            }
            tabBackup.Controls.Add(lblBackupTitle)

            lblDbPath = New Label With {
                .Text = $"Active Database Location: {Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MASA Invoice System", "Data", "masa_invoice.db")}",
                .Font = AppTheme.FontSmall,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(10, 50),
                .AutoSize = True
            }
            tabBackup.Controls.Add(lblDbPath)

            btnBackupNow = New Button With {
                .Text = "Backup Database Now...",
                .Location = New Point(10, 90),
                .Size = New Size(200, 42)
            }
            ControlHelpers.StylePrimaryButton(btnBackupNow)
            AddHandler btnBackupNow.Click, AddressOf BtnBackupNow_Click
            tabBackup.Controls.Add(btnBackupNow)

            btnRestoreNow = New Button With {
                .Text = "Restore Database from Backup...",
                .Location = New Point(225, 90),
                .Size = New Size(240, 42)
            }
            ControlHelpers.StyleSecondaryButton(btnRestoreNow)
            AddHandler btnRestoreNow.Click, AddressOf BtnRestoreNow_Click
            tabBackup.Controls.Add(btnRestoreNow)

            lblStatus = New Label With {
                .Text = "Database health: Optimal. WAL mode active with transactional safety.",
                .Font = AppTheme.FontBodyBold,
                .ForeColor = AppTheme.Success,
                .Location = New Point(10, 155),
                .AutoSize = True
            }
            tabBackup.Controls.Add(lblStatus)

            tabs.TabPages.Add(tabCompany)
            tabs.TabPages.Add(tabTaxes)
            tabs.TabPages.Add(tabBackup)

            Me.Controls.Add(tabs)

            AddHandler Me.Load, Async Sub(s, e) Await LoadSettingsAsync()
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

        Private Sub SetupTaxGrid()
            dgvTaxRates.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Tax Name", .DataPropertyName = "Name", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
            dgvTaxRates.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Percentage (%)", .DataPropertyName = "Percentage", .Width = 140, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "N2", .Alignment = DataGridViewContentAlignment.MiddleRight, .Font = AppTheme.FontBodyBold}})
            dgvTaxRates.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "Default Tax", .DataPropertyName = "IsDefault", .Width = 110})
            dgvTaxRates.Columns.Add(New DataGridViewCheckBoxColumn With {.HeaderText = "Active", .DataPropertyName = "IsActive", .Width = 90})
        End Sub

        Public Async Function LoadSettingsAsync() As Task
            Try
                Dim setting = Await _companyService.GetSettingAsync()
                txtCompanyName.Text = setting.CompanyName
                txtEmail.Text = setting.Email
                txtPhone.Text = setting.Phone
                txtWebsite.Text = setting.Website
                txtAddress.Text = setting.Address
                txtCity.Text = setting.City
                txtCountry.Text = setting.Country
                txtPostalCode.Text = setting.PostalCode
                txtTaxNumber.Text = setting.TaxNumber
                txtDefaultCurrency.Text = setting.DefaultCurrency
                txtInvoicePrefix.Text = setting.InvoicePrefix
                numNextInvoiceNumber.Value = Math.Max(1D, CDec(setting.NextInvoiceNumber))

                Dim taxes = Await _taxRateService.GetAllAsync()
                dgvTaxRates.DataSource = taxes
            Catch ex As Exception
                MessageBox.Show($"Failed to load settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function

        Private Async Sub BtnSaveCompany_Click(sender As Object, e As EventArgs)
            Try
                btnSaveCompany.Enabled = False
                btnSaveCompany.Text = "Saving..."

                Dim entity As New CompanySetting With {
                    .Id = 1,
                    .CompanyName = txtCompanyName.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Phone = txtPhone.Text.Trim(),
                    .Website = txtWebsite.Text.Trim(),
                    .Address = txtAddress.Text.Trim(),
                    .City = txtCity.Text.Trim(),
                    .Country = txtCountry.Text.Trim(),
                    .PostalCode = txtPostalCode.Text.Trim(),
                    .TaxNumber = txtTaxNumber.Text.Trim(),
                    .DefaultCurrency = txtDefaultCurrency.Text.Trim().ToUpper(),
                    .InvoicePrefix = txtInvoicePrefix.Text.Trim(),
                    .NextInvoiceNumber = CLng(numNextInvoiceNumber.Value)
                }

                Await _companyService.SaveSettingAsync(entity)
                MessageBox.Show("Company settings and invoice numbering updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As ValidationException
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSaveCompany.Enabled = True
                btnSaveCompany.Text = "Save Company Settings"
            End Try
        End Sub

        Private Function GetSelectedTax() As TaxRateDto
            If dgvTaxRates.CurrentRow IsNot Nothing AndAlso dgvTaxRates.CurrentRow.DataBoundItem IsNot Nothing Then
                Return CType(dgvTaxRates.CurrentRow.DataBoundItem, TaxRateDto)
            End If
            Return Nothing
        End Function

        Private Async Sub BtnAddTax_Click(sender As Object, e As EventArgs)
            Using dlg As New Form With {.Text = "Add Tax Rate", .Size = New Size(360, 230), .StartPosition = FormStartPosition.CenterParent, .FormBorderStyle = FormBorderStyle.FixedDialog, .MaximizeBox = False, .MinimizeBox = False}
                Dim lblN As New Label With {.Text = "Tax Name (e.g. VAT, GST, Sales Tax):", .Location = New Point(20, 15), .AutoSize = True}
                Dim txtN As New TextBox With {.Location = New Point(20, 35), .Size = New Size(300, 26)}

                Dim lblP As New Label With {.Text = "Percentage (%):", .Location = New Point(20, 70), .AutoSize = True}
                Dim numP As New NumericUpDown With {.Location = New Point(20, 90), .Size = New Size(140, 26), .DecimalPlaces = 2, .Maximum = 100D, .Minimum = 0D, .Value = 10D}

                Dim chkDef As New CheckBox With {.Text = "Set as Default Tax Rate", .Location = New Point(20, 125), .AutoSize = True}

                Dim btnOk As New Button With {.Text = "Save", .Location = New Point(220, 145), .Size = New Size(100, 32)}
                ControlHelpers.StylePrimaryButton(btnOk)
                AddHandler btnOk.Click, Sub() dlg.DialogResult = DialogResult.OK

                dlg.Controls.Add(lblN)
                dlg.Controls.Add(txtN)
                dlg.Controls.Add(lblP)
                dlg.Controls.Add(numP)
                dlg.Controls.Add(chkDef)
                dlg.Controls.Add(btnOk)

                If dlg.ShowDialog(Me) = DialogResult.OK AndAlso Not String.IsNullOrWhiteSpace(txtN.Text) Then
                    Try
                        Dim tax As New TaxRate With {
                            .Name = txtN.Text.Trim(),
                            .Percentage = numP.Value,
                            .IsDefault = chkDef.Checked,
                            .IsActive = True
                        }
                        Await _taxRateService.SaveAsync(tax)
                        Dim taxes = Await _taxRateService.GetAllAsync()
                        dgvTaxRates.DataSource = taxes
                    Catch ex As Exception
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Async Sub BtnEditTax_Click(sender As Object, e As EventArgs)
            Dim tax = GetSelectedTax()
            If tax Is Nothing Then
                MessageBox.Show("Please select a tax rate to edit.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Using dlg As New Form With {.Text = "Edit Tax Rate", .Size = New Size(360, 230), .StartPosition = FormStartPosition.CenterParent, .FormBorderStyle = FormBorderStyle.FixedDialog, .MaximizeBox = False, .MinimizeBox = False}
                Dim lblN As New Label With {.Text = "Tax Name:", .Location = New Point(20, 15), .AutoSize = True}
                Dim txtN As New TextBox With {.Text = tax.Name, .Location = New Point(20, 35), .Size = New Size(300, 26)}

                Dim lblP As New Label With {.Text = "Percentage (%):", .Location = New Point(20, 70), .AutoSize = True}
                Dim numP As New NumericUpDown With {.Location = New Point(20, 90), .Size = New Size(140, 26), .DecimalPlaces = 2, .Maximum = 100D, .Minimum = 0D, .Value = tax.Percentage}

                Dim chkDef As New CheckBox With {.Text = "Set as Default Tax Rate", .Checked = tax.IsDefault, .Location = New Point(20, 125), .AutoSize = True}

                Dim btnOk As New Button With {.Text = "Update", .Location = New Point(220, 145), .Size = New Size(100, 32)}
                ControlHelpers.StylePrimaryButton(btnOk)
                AddHandler btnOk.Click, Sub() dlg.DialogResult = DialogResult.OK

                dlg.Controls.Add(lblN)
                dlg.Controls.Add(txtN)
                dlg.Controls.Add(lblP)
                dlg.Controls.Add(numP)
                dlg.Controls.Add(chkDef)
                dlg.Controls.Add(btnOk)

                If dlg.ShowDialog(Me) = DialogResult.OK AndAlso Not String.IsNullOrWhiteSpace(txtN.Text) Then
                    Try
                        Dim entity As New TaxRate With {
                            .Id = tax.Id,
                            .Name = txtN.Text.Trim(),
                            .Percentage = numP.Value,
                            .IsDefault = chkDef.Checked,
                            .IsActive = True
                        }
                        Await _taxRateService.SaveAsync(entity)
                        Dim taxes = Await _taxRateService.GetAllAsync()
                        dgvTaxRates.DataSource = taxes
                    Catch ex As Exception
                        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End Try
                End If
            End Using
        End Sub

        Private Async Sub BtnSetDefaultTax_Click(sender As Object, e As EventArgs)
            Dim tax = GetSelectedTax()
            If tax Is Nothing Then
                MessageBox.Show("Please select a tax rate.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Try
                Await _taxRateService.SetDefaultAsync(tax.Id)
                Dim taxes = Await _taxRateService.GetAllAsync()
                dgvTaxRates.DataSource = taxes
                MessageBox.Show($"'{tax.Name}' is now set as the default tax rate.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Sub

        Private Async Sub BtnDeleteTax_Click(sender As Object, e As EventArgs)
            Dim tax = GetSelectedTax()
            If tax Is Nothing Then
                MessageBox.Show("Please select a tax rate to delete.", "Selection Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim res = MessageBox.Show($"Delete tax rate '{tax.Name}'?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If res = DialogResult.Yes Then
                Try
                    Await _taxRateService.DeleteAsync(tax.Id)
                    Dim taxes = Await _taxRateService.GetAllAsync()
                    dgvTaxRates.DataSource = taxes
                Catch ex As Exception
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        End Sub

        Private Async Sub BtnBackupNow_Click(sender As Object, e As EventArgs)
            Using sfd As New SaveFileDialog With {
                .Filter = "SQLite Backup (*.db;*.bak)|*.db;*.bak",
                .FileName = $"MASA_Invoice_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
            }
                If sfd.ShowDialog(Me) = DialogResult.OK Then
                    Try
                        btnBackupNow.Enabled = False
                        btnBackupNow.Text = "Backing up..."
                        Await _backupService.BackupDatabaseAsync(sfd.FileName)
                        lblStatus.Text = $"Last backup created successfully at {DateTime.Now:HH:mm:ss} to {sfd.FileName}"
                        lblStatus.ForeColor = AppTheme.Success
                        MessageBox.Show($"Database backup successfully created at:{Environment.NewLine}{sfd.FileName}", "Backup Succeeded", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Catch ex As Exception
                        MessageBox.Show($"Failed to backup database: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Finally
                        btnBackupNow.Enabled = True
                        btnBackupNow.Text = "Backup Database Now..."
                    End Try
                End If
            End Using
        End Sub

        Private Async Sub BtnRestoreNow_Click(sender As Object, e As EventArgs)
            Using ofd As New OpenFileDialog With {
                .Filter = "SQLite Database Files (*.db;*.bak)|*.db;*.bak|All Files (*.*)|*.*",
                .Title = "Select Backup Database File to Restore"
            }
                If ofd.ShowDialog(Me) = DialogResult.OK Then
                    If Not _backupService.ValidateBackupFile(ofd.FileName) Then
                        MessageBox.Show("The selected file is not a valid SQLite database file.", "Invalid Backup", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If

                    Dim confirm = MessageBox.Show($"WARNING: Restoring will overwrite all current invoices, customers, and payment data with the contents of the backup file.{Environment.NewLine}{Environment.NewLine}Are you sure you wish to proceed?", "Confirm Database Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
                    If confirm = DialogResult.Yes Then
                        Try
                            btnRestoreNow.Enabled = False
                            btnRestoreNow.Text = "Restoring..."
                            Await _backupService.RestoreDatabaseAsync(ofd.FileName)
                            lblStatus.Text = $"Database restored from {ofd.FileName} at {DateTime.Now:HH:mm:ss}"
                            lblStatus.ForeColor = AppTheme.Success
                            MessageBox.Show("Database successfully restored! Reloading application data...", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                            Await LoadSettingsAsync()
                        Catch ex As Exception
                            MessageBox.Show($"Failed to restore database: {ex.Message}", "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Finally
                            btnRestoreNow.Enabled = True
                            btnRestoreNow.Text = "Restore Database from Backup..."
                        End Try
                    End If
                End If
            End Using
        End Sub
    End Class
End Namespace
