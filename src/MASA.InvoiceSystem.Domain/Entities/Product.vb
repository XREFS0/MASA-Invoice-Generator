Imports System
Imports MASA.InvoiceSystem.Domain.Enums

Namespace Entities
    Public Class Product
        Public Property Id As Long
        Public Property ProductCode As String = String.Empty
        Public Property Name As String = String.Empty
        Public Property Description As String = String.Empty
        Public Property Type As ProductType = ProductType.Product
        Public Property UnitPrice As Decimal = 0D
        Public Property CostPrice As Decimal = 0D
        Public Property TaxRateId As Long?
        Public Property StockQuantity As Decimal = 0D
        Public Property MinimumStock As Decimal = 0D
        Public Property IsActive As Boolean = True
        Public Property CreatedAt As DateTime = DateTime.UtcNow
        Public Property UpdatedAt As DateTime = DateTime.UtcNow

        Public ReadOnly Property IsLowStock As Boolean
            Get
                Return Type = ProductType.Product AndAlso StockQuantity <= MinimumStock
            End Get
        End Property
    End Class
End Namespace
