Imports System

Namespace Entities
    Public Class Customer
        Public Property Id As Long
        Public Property FullName As String = String.Empty
        Public Property CompanyName As String = String.Empty
        Public Property Email As String = String.Empty
        Public Property Phone As String = String.Empty
        Public Property Address As String = String.Empty
        Public Property City As String = String.Empty
        Public Property Country As String = String.Empty
        Public Property PostalCode As String = String.Empty
        Public Property TaxNumber As String = String.Empty
        Public Property Notes As String = String.Empty
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow

        Public ReadOnly Property DisplayName As String
            Get
                If Not String.IsNullOrWhiteSpace(CompanyName) AndAlso Not String.IsNullOrWhiteSpace(FullName) Then
                    Return $"{FullName} ({CompanyName})"
                ElseIf Not String.IsNullOrWhiteSpace(CompanyName) Then
                    Return CompanyName
                Else
                    Return FullName
                End If
            End Get
        End Property
    End Class
End Namespace
