Imports System
Imports System.IO

Namespace Data
    Public Class DatabaseConfig
        Public Property DatabaseFilePath As String

        Public Sub New()
            Dim appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MASA Invoice System", "Data")
            DatabaseFilePath = Path.Combine(appDataFolder, "masa_invoice.db")
        End Sub

        Public Sub New(customPath As String)
            If String.IsNullOrWhiteSpace(customPath) Then
                Dim appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MASA Invoice System", "Data")
                DatabaseFilePath = Path.Combine(appDataFolder, "masa_invoice.db")
            Else
                DatabaseFilePath = customPath
            End If
        End Sub

        Public ReadOnly Property ConnectionString As String
            Get
                Return $"Data Source={DatabaseFilePath};Cache=Shared;"
            End Get
        End Property
    End Class
End Namespace
