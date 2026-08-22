Imports System.Drawing
Imports System.Windows.Forms
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Themes
    Public Module AppTheme
        Public ReadOnly Primary As Color = Color.FromArgb(37, 99, 235)
        Public ReadOnly PrimaryDark As Color = Color.FromArgb(29, 78, 216)
        Public ReadOnly PrimaryLight As Color = Color.FromArgb(239, 246, 255)

        Public ReadOnly SidebarBg As Color = Color.FromArgb(15, 23, 42)
        Public ReadOnly SidebarHeader As Color = Color.FromArgb(2, 6, 23)
        Public ReadOnly SidebarHover As Color = Color.FromArgb(30, 41, 59)
        Public ReadOnly SidebarActive As Color = Color.FromArgb(30, 58, 138)
        Public ReadOnly SidebarText As Color = Color.FromArgb(203, 213, 225)
        Public ReadOnly SidebarTextActive As Color = Color.White

        Public ReadOnly AppBackground As Color = Color.FromArgb(248, 250, 252)
        Public ReadOnly CardBackground As Color = Color.White
        Public ReadOnly BorderColor As Color = Color.FromArgb(226, 232, 240)
        Public ReadOnly HeaderBackground As Color = Color.White

        Public ReadOnly TextMain As Color = Color.FromArgb(15, 23, 42)
        Public ReadOnly TextMuted As Color = Color.FromArgb(100, 116, 139)
        Public ReadOnly TextSubtle As Color = Color.FromArgb(148, 163, 184)

        Public ReadOnly Success As Color = Color.FromArgb(22, 163, 74)
        Public ReadOnly SuccessBg As Color = Color.FromArgb(220, 252, 231)
        Public ReadOnly SuccessText As Color = Color.FromArgb(22, 101, 52)

        Public ReadOnly Warning As Color = Color.FromArgb(217, 119, 6)
        Public ReadOnly WarningBg As Color = Color.FromArgb(254, 243, 199)
        Public ReadOnly WarningText As Color = Color.FromArgb(146, 64, 14)

        Public ReadOnly Danger As Color = Color.FromArgb(220, 38, 38)
        Public ReadOnly DangerBg As Color = Color.FromArgb(254, 226, 226)
        Public ReadOnly DangerText As Color = Color.FromArgb(153, 27, 27)

        Public ReadOnly Info As Color = Color.FromArgb(2, 132, 199)
        Public ReadOnly InfoBg As Color = Color.FromArgb(224, 242, 254)
        Public ReadOnly InfoText As Color = Color.FromArgb(7, 89, 133)

        Public ReadOnly FontTitle As New Font("Segoe UI", 16.0F, FontStyle.Bold)
        Public ReadOnly FontHeader As New Font("Segoe UI", 12.0F, FontStyle.Bold)
        Public ReadOnly FontSubheader As New Font("Segoe UI", 10.0F, FontStyle.Bold)
        Public ReadOnly FontBody As New Font("Segoe UI", 9.0F, FontStyle.Regular)
        Public ReadOnly FontBodyBold As New Font("Segoe UI", 9.0F, FontStyle.Bold)
        Public ReadOnly FontSmall As New Font("Segoe UI", 8.0F, FontStyle.Regular)
        Public ReadOnly FontSmallBold As New Font("Segoe UI", 8.0F, FontStyle.Bold)
        Public ReadOnly FontKpiValue As New Font("Segoe UI", 18.0F, FontStyle.Bold)

        Public Function GetStatusColors(status As InvoiceStatus) As Tuple(Of Color, Color)
            Select Case status
                Case InvoiceStatus.Paid
                    Return Tuple.Create(SuccessBg, SuccessText)
                Case InvoiceStatus.PartialPayment
                    Return Tuple.Create(WarningBg, WarningText)
                Case InvoiceStatus.Overdue
                    Return Tuple.Create(DangerBg, DangerText)
                Case InvoiceStatus.Sent
                    Return Tuple.Create(InfoBg, InfoText)
                Case InvoiceStatus.Cancelled
                    Return Tuple.Create(Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105))
                Case Else
                    Return Tuple.Create(Color.FromArgb(241, 245, 249), Color.FromArgb(51, 65, 85))
            End Select
        End Function
    End Module
End Namespace
