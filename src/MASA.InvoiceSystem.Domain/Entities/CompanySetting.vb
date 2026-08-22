Imports System

Namespace Entities
    Public Class CompanySetting
        Public Property Id As Long = 1
        Public Property CompanyName As String = "MASA Solutions Egypt S.A.E"
        Public Property LogoPath As String = String.Empty
        Public Property Email As String = "invoicing@masa-egypt.com"
        Public Property Phone As String = "+20 2 2736 8490"
        Public Property Website As String = "https://www.masa-egypt.com"
        Public Property Address As String = "90th Street North, Sector 1, Fifth Settlement"
        Public Property City As String = "New Cairo"
        Public Property Country As String = "Egypt"
        Public Property PostalCode As String = "11835"
        Public Property TaxNumber As String = "EG-584-920-311"
        Public Property DefaultCurrency As String = "EGP"
        Public Property DefaultTaxRateId As Long?
        Public Property InvoicePrefix As String = "MASA-EG-"
        Public Property NextInvoiceNumber As Long = 1001
        Public Property UpdatedAt As DateTime = DateTime.UtcNow
    End Class
End Namespace
