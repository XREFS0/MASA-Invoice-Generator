Imports System
Imports System.Drawing
Imports System.Threading.Tasks
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Application.DTOs
Imports MASA.InvoiceSystem.Application.Interfaces
Imports MASA.InvoiceSystem.WinForms.Helpers
Imports MASA.InvoiceSystem.WinForms.Themes

Namespace Views
    Public Class AuditLogsView
        Inherits UserControl

        Private ReadOnly _auditLogService As IAuditLogService

        Private cmbEntityFilter As ComboBox
        Private btnRefresh As Button
        Private dgvAuditLogs As DataGridView
        Private lblRecordCount As Label

        Public Sub New(auditLogService As IAuditLogService)
            _auditLogService = auditLogService
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
                .Text = "System Activity & Audit Trail",
                .Font = AppTheme.FontHeader,
                .ForeColor = AppTheme.TextMain,
                .Location = New Point(0, 12),
                .AutoSize = True
            }

            Dim lblFilter As New Label With {
                .Text = "Filter by Entity:",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(260, 16),
                .AutoSize = True
            }

            cmbEntityFilter = New ComboBox With {
                .Location = New Point(360, 12),
                .Size = New Size(160, 26),
                .DropDownStyle = ComboBoxStyle.DropDownList
            }
            cmbEntityFilter.Items.Add("All Entities")
            cmbEntityFilter.Items.Add("Invoice")
            cmbEntityFilter.Items.Add("Customer")
            cmbEntityFilter.Items.Add("Payment")
            cmbEntityFilter.Items.Add("Product")
            cmbEntityFilter.Items.Add("TaxRate")
            cmbEntityFilter.Items.Add("CompanySetting")
            cmbEntityFilter.Items.Add("Database")
            cmbEntityFilter.SelectedIndex = 0
            AddHandler cmbEntityFilter.SelectedIndexChanged, Async Sub(s, e) Await LoadLogsAsync()

            btnRefresh = New Button With {
                .Text = "Refresh Trail",
                .Location = New Point(535, 10),
                .Size = New Size(110, 30)
            }
            ControlHelpers.StyleSecondaryButton(btnRefresh)
            AddHandler btnRefresh.Click, Async Sub(s, e) Await LoadLogsAsync()

            pnlToolbar.Controls.Add(lblTitle)
            pnlToolbar.Controls.Add(lblFilter)
            pnlToolbar.Controls.Add(cmbEntityFilter)
            pnlToolbar.Controls.Add(btnRefresh)

            dgvAuditLogs = New DataGridView With {.Dock = DockStyle.Fill}
            ControlHelpers.StyleDataGridView(dgvAuditLogs)
            SetupGrid()

            Dim pnlFooter As New Panel With {
                .Dock = DockStyle.Bottom,
                .Height = 30
            }
            lblRecordCount = New Label With {
                .Text = "Total Activities Logged: 0",
                .Font = AppTheme.FontSmallBold,
                .ForeColor = AppTheme.TextMuted,
                .Location = New Point(0, 8),
                .AutoSize = True
            }
            pnlFooter.Controls.Add(lblRecordCount)

            pnlCard.Controls.Add(dgvAuditLogs)
            pnlCard.Controls.Add(pnlToolbar)
            pnlCard.Controls.Add(pnlFooter)

            Me.Controls.Add(pnlCard)

            AddHandler Me.Load, Async Sub(s, e) Await LoadLogsAsync()
        End Sub

        Private Sub SetupGrid()
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Timestamp", .DataPropertyName = "CreatedAt", .Width = 160, .DefaultCellStyle = New DataGridViewCellStyle With {.Format = "yyyy-MM-dd HH:mm:ss"}})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Action", .DataPropertyName = "Action", .Width = 130})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Entity", .DataPropertyName = "EntityName", .Width = 130, .DefaultCellStyle = New DataGridViewCellStyle With {.Font = AppTheme.FontBodyBold}})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Entity ID", .DataPropertyName = "EntityId", .Width = 90})
            dgvAuditLogs.Columns.Add(New DataGridViewTextBoxColumn With {.HeaderText = "Description & Change Summary", .DataPropertyName = "Description", .AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill})
        End Sub

        Public Async Function LoadLogsAsync() As Task
            Try
                Dim entityFilter As String = Nothing
                If cmbEntityFilter.SelectedIndex > 0 Then
                    entityFilter = cmbEntityFilter.SelectedItem.ToString()
                End If

                Dim logs = Await _auditLogService.GetRecentLogsAsync(200, entityFilter)
                dgvAuditLogs.DataSource = logs
                lblRecordCount.Text = $"Total Activities Logged: {logs.Count}"
            Catch ex As Exception
                MessageBox.Show($"Failed to load audit logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End Function
    End Class
End Namespace
