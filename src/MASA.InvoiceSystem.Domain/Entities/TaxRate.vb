Imports System

Namespace Entities
    Public Class TaxRate
        Public Property Id As Long
        Public Property Name As String = String.Empty
        Public Property Percentage As Decimal = 0D
        Public Property IsDefault As Boolean = False
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow

        Public ReadOnly Property DisplayText As String
            Get
                Return $"{Name} ({Percentage:N2}%)"
            End Get
        End Property
    End Class
End Namespace
