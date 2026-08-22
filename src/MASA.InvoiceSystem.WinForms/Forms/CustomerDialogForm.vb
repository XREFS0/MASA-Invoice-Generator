Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.Domain.Entities
Imports MASA.InvoiceSystem.Domain.Exceptions
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Forms
    Public Class CustomerDialogForm
        Inherits Form

        Private ReadOnly _customerService As ICustomerService
        Private ReadOnly _customerId As Long?

        Private txtFullName As TextBox
        Private txtCompanyName As TextBox
        Private txtEmail As TextBox
        Private txtPhone As TextBox
        Private txtTaxNumber As TextBox
        Private txtAddress As TextBox
        Private txtCity As TextBox
        Private txtPostalCode As TextBox
        Private txtCountry As TextBox
        Private txtNotes As TextBox
        Private btnSave As Button
        Private btnCancel As Button

        Public Property SavedCustomer As CustomerDto

        Public Sub New(customerService As ICustomerService, Optional customerId As Long? = Nothing)
            _customerService = customerService
            _customerId = customerId
            InitializeComponent()
        End Sub

        Private Sub InitializeComponent()
            Me.Text = If(_customerId.HasValue, "Edit Customer", "Add New Customer")
            Me.Size = New Size(540, 580)
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
                .Text = If(_customerId.HasValue, "Edit Customer Information", "Create New Customer Profile"),
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

            pnlBody.Controls.Add(CreateLabel("Contact Full Name *", 0, y))
            txtFullName = CreateTextBox(0, y + 20, 230)
            pnlBody.Controls.Add(txtFullName)

            pnlBody.Controls.Add(CreateLabel("Company / Business Name", 250, y))
            txtCompanyName = CreateTextBox(250, y + 20, 230)
            pnlBody.Controls.Add(txtCompanyName)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Email Address", 0, y))
            txtEmail = CreateTextBox(0, y + 20, 230)
            pnlBody.Controls.Add(txtEmail)

            pnlBody.Controls.Add(CreateLabel("Phone Number", 250, y))
            txtPhone = CreateTextBox(250, y + 20, 230)
            pnlBody.Controls.Add(txtPhone)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Tax / VAT ID", 0, y))
            txtTaxNumber = CreateTextBox(0, y + 20, 230)
            pnlBody.Controls.Add(txtTaxNumber)

            pnlBody.Controls.Add(CreateLabel("Country", 250, y))
            txtCountry = CreateTextBox(250, y + 20, 230)
            pnlBody.Controls.Add(txtCountry)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Street Address", 0, y))
            txtAddress = CreateTextBox(0, y + 20, 480)
            pnlBody.Controls.Add(txtAddress)

            y += 60
            pnlBody.Controls.Add(CreateLabel("City", 0, y))
            txtCity = CreateTextBox(0, y + 20, 230)
            pnlBody.Controls.Add(txtCity)

            pnlBody.Controls.Add(CreateLabel("Postal Code", 250, y))
            txtPostalCode = CreateTextBox(250, y + 20, 230)
            pnlBody.Controls.Add(txtPostalCode)

            y += 60
            pnlBody.Controls.Add(CreateLabel("Internal Notes / Terms", 0, y))
            txtNotes = New TextBox With {
                .Location = New Point(0, y + 20),
                .Size = New Size(480, 50),
                .Multiline = True,
                .ScrollBars = ScrollBars.Vertical
            }
            pnlBody.Controls.Add(txtNotes)

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 60,
                .BackColor = AppTheme.AppBackground,
                .Padding = New Padding(20, 12, 20, 12)
            }

            btnCancel = New Button With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Location = New Point(300, 12),
                .Size = New Size(90, 36)
            }
            ControlHelpers.StyleSecondaryButton(btnCancel)

            btnSave = New Button With {
                .Text = "Save Customer",
                .Location = New Point(400, 12),
                .Size = New Size(120, 36)
            }
            ControlHelpers.StylePrimaryButton(btnSave)
            AddHandler btnSave.Click, AddressOf BtnSave_Click

            pnlFooter.Controls.Add(btnCancel)
            pnlFooter.Controls.Add(btnSave)

            Me.Controls.Add(pnlBody)
            Me.Controls.Add(pnlFooter)
            Me.Controls.Add(pnlHeader)

            AddHandler Me.Load, AddressOf CustomerDialogForm_Load
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

        Private Async Sub CustomerDialogForm_Load(sender As Object, e As EventArgs)
            If _customerId.HasValue Then
                Try
                    Dim cust = Await _customerService.GetByIdAsync(_customerId.Value)
                    txtFullName.Text = cust.FullName
                    txtCompanyName.Text = cust.CompanyName
                    txtEmail.Text = cust.Email
                    txtPhone.Text = cust.Phone
                    txtTaxNumber.Text = cust.TaxNumber
                    txtAddress.Text = cust.Address
                    txtCity.Text = cust.City
                    txtPostalCode.Text = cust.PostalCode
                    txtCountry.Text = cust.Country
                    txtNotes.Text = cust.Notes
                Catch ex As Exception
                    MessageBox.Show($"Failed to load customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Me.Close()
                End Try
            End If
        End Sub

        Private Async Sub BtnSave_Click(sender As Object, e As EventArgs)
            Try
                btnSave.Enabled = False
                btnSave.Text = "Saving..."

                Dim entity As New Customer With {
                    .Id = If(_customerId, 0L),
                    .FullName = txtFullName.Text.Trim(),
                    .CompanyName = txtCompanyName.Text.Trim(),
                    .Email = txtEmail.Text.Trim(),
                    .Phone = txtPhone.Text.Trim(),
                    .TaxNumber = txtTaxNumber.Text.Trim(),
                    .Address = txtAddress.Text.Trim(),
                    .City = txtCity.Text.Trim(),
                    .PostalCode = txtPostalCode.Text.Trim(),
                    .Country = txtCountry.Text.Trim(),
                    .Notes = txtNotes.Text.Trim()
                }

                SavedCustomer = Await _customerService.SaveAsync(entity)
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Catch ex As ValidationException
                MessageBox.Show(ex.Message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Catch ex As Exception
                MessageBox.Show($"Failed to save customer: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Finally
                btnSave.Enabled = True
                btnSave.Text = "Save Customer"
            End Try
        End Sub
    End Class
End Namespace
