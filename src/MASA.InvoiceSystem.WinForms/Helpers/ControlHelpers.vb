Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Helpers
    Public Module ControlHelpers
        Public Sub StyleDataGridView(grid As DataGridView)
            grid.EnableHeadersVisualStyles = False
            grid.BorderStyle = BorderStyle.None
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            grid.GridColor = AppTheme.BorderColor
            grid.BackgroundColor = Color.White
            grid.RowHeadersVisible = False
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            grid.MultiSelect = False
            grid.AllowUserToAddRows = False
            grid.AllowUserToDeleteRows = False
            grid.AllowUserToResizeRows = False
            grid.AutoGenerateColumns = False
            grid.RowTemplate.Height = 40

            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252)
            grid.ColumnHeadersDefaultCellStyle.ForeColor = AppTheme.TextMuted
            grid.ColumnHeadersDefaultCellStyle.Font = AppTheme.FontSubheader
            grid.ColumnHeadersDefaultCellStyle.Padding = New Padding(10, 8, 10, 8)
            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
            grid.ColumnHeadersHeight = 42

            grid.DefaultCellStyle.BackColor = Color.White
            grid.DefaultCellStyle.ForeColor = AppTheme.TextMain
            grid.DefaultCellStyle.Font = AppTheme.FontBody
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255)
            grid.DefaultCellStyle.SelectionForeColor = AppTheme.PrimaryDark
            grid.DefaultCellStyle.Padding = New Padding(10, 4, 10, 4)

            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(252, 253, 254)
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(239, 246, 255)
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = AppTheme.PrimaryDark
        End Sub

        Public Sub StylePrimaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = AppTheme.Primary
            btn.ForeColor = Color.White
            btn.Font = AppTheme.FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(12, 0, 12, 0)
        End Sub

        Public Sub StyleSecondaryButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderColor = AppTheme.BorderColor
            btn.FlatAppearance.BorderSize = 1
            btn.BackColor = Color.White
            btn.ForeColor = AppTheme.TextMain
            btn.Font = AppTheme.FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(12, 0, 12, 0)
        End Sub

        Public Sub StyleDangerButton(btn As Button)
            btn.FlatStyle = FlatStyle.Flat
            btn.FlatAppearance.BorderSize = 0
            btn.BackColor = AppTheme.Danger
            btn.ForeColor = Color.White
            btn.Font = AppTheme.FontBodyBold
            btn.Cursor = Cursors.Hand
            btn.Height = 36
            btn.Padding = New Padding(12, 0, 12, 0)
        End Sub

        Public Function CreateCardPanel() As Panel
            Dim pnl As New Panel()
            pnl.BackColor = AppTheme.CardBackground
            pnl.Padding = New Padding(16)
            AddHandler pnl.Paint, Sub(s, e)
                                      Dim g = e.Graphics
                                      g.SmoothingMode = SmoothingMode.AntiAlias
                                      Using pen As New Pen(AppTheme.BorderColor, 1)
                                          g.DrawRectangle(pen, 0, 0, pnl.Width - 1, pnl.Height - 1)
                                      End Using
                                  End Sub
            Return pnl
        End Function

        Public Function CreateKpiCard(title As String, value As String, subtitle As String, accentColor As Color) As Panel
            Dim card = CreateCardPanel()
            card.Size = New Size(220, 105)

            Dim pnlBar As New Panel()
            pnlBar.BackColor = accentColor
            pnlBar.Location = New Point(16, 16)
            pnlBar.Size = New Size(4, 18)

            Dim lblTitle As New Label()
            lblTitle.Text = title.ToUpper()
            lblTitle.Font = AppTheme.FontSmallBold
            lblTitle.ForeColor = AppTheme.TextMuted
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(26, 16)

            Dim lblVal As New Label()
            lblVal.Text = value
            lblVal.Font = AppTheme.FontKpiValue
            lblVal.ForeColor = AppTheme.TextMain
            lblVal.AutoSize = True
            lblVal.Location = New Point(16, 44)
            lblVal.Name = "lblKpiValue"

            Dim lblSub As New Label()
            lblSub.Text = subtitle
            lblSub.Font = AppTheme.FontSmall
            lblSub.ForeColor = AppTheme.TextSubtle
            lblSub.AutoSize = True
            lblSub.Location = New Point(16, 78)
            lblSub.Name = "lblKpiSub"

            card.Controls.Add(pnlBar)
            card.Controls.Add(lblTitle)
            card.Controls.Add(lblVal)
            card.Controls.Add(lblSub)

            Return card
        End Function
    End Module
End Namespace
